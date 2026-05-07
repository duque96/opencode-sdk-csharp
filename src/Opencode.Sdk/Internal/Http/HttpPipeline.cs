using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;
using Opencode.Diagnostics;
using Opencode.Errors;
using Opencode.Internal.Diagnostics;
using Opencode.Internal.Headers;
using Opencode.Internal.Retry;
using Opencode.Internal.Streaming;
using Opencode.Internal.Urls;

namespace Opencode.Internal.Http;

internal sealed class HttpPipeline
{
    private static readonly HttpRequestOptionsKey<int> AttemptOptionKey = new("Opencode.Attempt");
    private static readonly HttpRequestOptionsKey<TimeSpan> ElapsedOptionKey = new("Opencode.Elapsed");

    private readonly HttpPipelineOptions _options;
    private readonly DiagnosticsDispatcher _diagnostics;
    private readonly HttpRequestFactory _requestFactory;

    public HttpPipeline(HttpPipelineOptions options)
    {
        _options = options;
        _diagnostics = new DiagnosticsDispatcher(options.DiagnosticsHandler);
        RequestUriBuilder = new RequestUriBuilder();
        HeaderBuilder = new HeaderBuilder();
        _requestFactory = new HttpRequestFactory(RequestUriBuilder, HeaderBuilder, options.SerializerProvider);
        ResponseParser = new HttpResponseParser(options.SerializerProvider);
    }

    public RequestUriBuilder RequestUriBuilder { get; }

    public HeaderBuilder HeaderBuilder { get; }

    public HttpResponseParser ResponseParser { get; }

    public HttpClient HttpClient => _options.HttpClient;

    public HttpPipelineOptions Options => _options;

    public HttpRequestMessage CreateRequestMessage(HttpPipelineRequest request, int retryCount = 0)
    {
        return _requestFactory.Create(_options, request, retryCount);
    }

    public async Task<HttpResponseMessage> ExecuteAsync(HttpPipelineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveTimeout = request.Timeout ?? _options.Timeout;
        var maxRetries = request.MaxRetries ?? _options.MaxRetries;

        for (var retryCount = 0; ; retryCount++)
        {
            var attempt = retryCount + 1;
            using var requestMessage = CreateRequestMessage(request, retryCount);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken);
            timeoutCts.CancelAfter(effectiveTimeout);
            var stopwatch = Stopwatch.StartNew();

            _diagnostics.Emit(new OpencodeDiagnosticEvent
            {
                Kind = OpencodeDiagnosticEventKind.RequestStarted,
                Method = request.Method,
                Path = request.Path,
                Attempt = attempt,
                Timeout = effectiveTimeout,
                Elapsed = TimeSpan.Zero,
            });

            try
            {
                var response = await HttpClient
                    .SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token)
                    .ConfigureAwait(false);

                stopwatch.Stop();

                _diagnostics.Emit(new OpencodeDiagnosticEvent
                {
                    Kind = OpencodeDiagnosticEventKind.ResponseReceived,
                    Method = request.Method,
                    Path = request.Path,
                    Attempt = attempt,
                    Timeout = effectiveTimeout,
                    Elapsed = stopwatch.Elapsed,
                    StatusCode = response.StatusCode,
                });

                if (retryCount < maxRetries && RetryDecider.ShouldRetry(response))
                {
                    var delay = RetryExecutor.GetDelay(response.Headers, retryCount);

                    _diagnostics.Emit(new OpencodeDiagnosticEvent
                    {
                        Kind = OpencodeDiagnosticEventKind.RetryScheduled,
                        Method = request.Method,
                        Path = request.Path,
                        Attempt = attempt,
                        Timeout = effectiveTimeout,
                        Elapsed = stopwatch.Elapsed,
                        StatusCode = response.StatusCode,
                        RetryDelay = delay,
                    });

                    response.Dispose();
                    await RetryExecutor.DelayAsync(delay, request.CancellationToken).ConfigureAwait(false);
                    continue;
                }

                requestMessage.Options.Set(AttemptOptionKey, attempt);
                requestMessage.Options.Set(ElapsedOptionKey, stopwatch.Elapsed);

                return response;
            }
            catch (OperationCanceledException exception) when (!request.CancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
            {
                stopwatch.Stop();

                if (retryCount < maxRetries)
                {
                    var delay = RetryExecutor.GetDelay(headers: null, retryCount);

                    _diagnostics.Emit(new OpencodeDiagnosticEvent
                    {
                        Kind = OpencodeDiagnosticEventKind.RetryScheduled,
                        Method = request.Method,
                        Path = request.Path,
                        Attempt = attempt,
                        Timeout = effectiveTimeout,
                        Elapsed = stopwatch.Elapsed,
                        RetryDelay = delay,
                    });

                    await RetryExecutor.DelayAsync(delay, request.CancellationToken).ConfigureAwait(false);
                    continue;
                }

                var timeoutException = new RequestTimeoutException(effectiveTimeout, exception);

                _diagnostics.Emit(new OpencodeDiagnosticEvent
                {
                    Kind = OpencodeDiagnosticEventKind.ExceptionThrown,
                    Method = request.Method,
                    Path = request.Path,
                    Attempt = attempt,
                    Timeout = effectiveTimeout,
                    Elapsed = stopwatch.Elapsed,
                    ExceptionType = timeoutException.GetType().FullName,
                });

                throw timeoutException;
            }
            catch (HttpRequestException) when (retryCount < maxRetries)
            {
                stopwatch.Stop();
                var delay = RetryExecutor.GetDelay(headers: null, retryCount);

                _diagnostics.Emit(new OpencodeDiagnosticEvent
                {
                    Kind = OpencodeDiagnosticEventKind.RetryScheduled,
                    Method = request.Method,
                    Path = request.Path,
                    Attempt = attempt,
                    Timeout = effectiveTimeout,
                    Elapsed = stopwatch.Elapsed,
                    RetryDelay = delay,
                });

                await RetryExecutor.DelayAsync(delay, request.CancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException exception)
            {
                stopwatch.Stop();
                var connectionException = new ConnectionException(innerException: exception);

                _diagnostics.Emit(new OpencodeDiagnosticEvent
                {
                    Kind = OpencodeDiagnosticEventKind.ExceptionThrown,
                    Method = request.Method,
                    Path = request.Path,
                    Attempt = attempt,
                    Timeout = effectiveTimeout,
                    Elapsed = stopwatch.Elapsed,
                    ExceptionType = connectionException.GetType().FullName,
                });

                throw connectionException;
            }
        }
    }

    public async Task<T?> SendAsync<T>(HttpPipelineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await ExecuteAsync(request).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var exception = await HttpExceptionFactory.CreateAsync(response, request.CancellationToken).ConfigureAwait(false);
            var attempt = response.RequestMessage?.Options.TryGetValue(AttemptOptionKey, out var responseAttempt) is true ? responseAttempt : 1;
            var elapsed = response.RequestMessage?.Options.TryGetValue(ElapsedOptionKey, out var responseElapsed) is true ? responseElapsed : TimeSpan.Zero;

            _diagnostics.Emit(new OpencodeDiagnosticEvent
            {
                Kind = OpencodeDiagnosticEventKind.ExceptionThrown,
                Method = request.Method,
                Path = request.Path,
                Attempt = attempt,
                Timeout = request.Timeout ?? _options.Timeout,
                Elapsed = elapsed,
                StatusCode = response.StatusCode,
                ExceptionType = exception.GetType().FullName,
            });

            throw exception;
        }

        return await ResponseParser.ParseAsync<T>(response, request.CancellationToken).ConfigureAwait(false);
    }

    public IAsyncEnumerable<T> StreamAsync<T>(HttpPipelineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return StreamAsyncCore<T>(request);
    }

    private async IAsyncEnumerable<T> StreamAsyncCore<T>(
        HttpPipelineRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken, cancellationToken);
        var effectiveRequest = CloneWithCancellation(request, linkedCancellation.Token);

        using var response = await ExecuteAsync(effectiveRequest).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var exception = await HttpExceptionFactory.CreateAsync(response, linkedCancellation.Token).ConfigureAwait(false);
            var attempt = response.RequestMessage?.Options.TryGetValue(AttemptOptionKey, out var responseAttempt) is true ? responseAttempt : 1;
            var elapsed = response.RequestMessage?.Options.TryGetValue(ElapsedOptionKey, out var responseElapsed) is true ? responseElapsed : TimeSpan.Zero;

            _diagnostics.Emit(new OpencodeDiagnosticEvent
            {
                Kind = OpencodeDiagnosticEventKind.ExceptionThrown,
                Method = request.Method,
                Path = request.Path,
                Attempt = attempt,
                Timeout = request.Timeout ?? _options.Timeout,
                Elapsed = elapsed,
                StatusCode = response.StatusCode,
                ExceptionType = exception.GetType().FullName,
            });

            throw exception;
        }

        if (response.Content is null)
        {
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(linkedCancellation.Token).ConfigureAwait(false);

        await foreach (var message in SseMessageParser.ParseAsync(stream, linkedCancellation.Token))
        {
            var item = JsonSerializer.Deserialize<T>(message.Data, _options.SerializerProvider.Options)
                ?? throw new JsonException("The streamed event payload deserialized to null.");

            yield return item;
        }
    }

    private static HttpPipelineRequest CloneWithCancellation(HttpPipelineRequest request, CancellationToken cancellationToken)
    {
        return new HttpPipelineRequest
        {
            Method = request.Method,
            Path = request.Path,
            Query = request.Query,
            Headers = request.Headers,
            Body = request.Body,
            Timeout = request.Timeout,
            MaxRetries = request.MaxRetries,
            CancellationToken = cancellationToken,
        };
    }
}
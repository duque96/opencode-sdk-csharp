using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Opencode.Client;
using Opencode.Diagnostics;
using Opencode.Errors;
using Opencode.Internal.Headers;
using Opencode.Internal.Http;
using Opencode.Internal.Serialization;
using Opencode.Internal.Urls;
using Opencode.Models.App;

namespace Opencode.Sdk.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public void QueryStringBuilderSerializesPrimitiveValues()
    {
        var query = new Dictionary<string, object?>
        {
            ["a"] = "1",
            ["b"] = 2,
            ["c"] = true,
            ["d"] = null,
        };

        var result = QueryStringBuilder.Build(query);

        result.Should().Be("a=1&b=2&c=true&d=");
    }

    [Fact]
    public void QueryStringBuilderRejectsUnsupportedValues()
    {
        var query = new Dictionary<string, object?>
        {
            ["value"] = new[] { 1, 2, 3 },
        };

        var action = () => QueryStringBuilder.Build(query);

        action.Should().Throw<ArgumentException>().WithMessage("Cannot stringify type*Int32[]*");
    }

    [Fact]
    public void RequestUriBuilderCombinesBasePathAndQuery()
    {
        var result = RequestUriBuilder.Build(
            new Uri("http://localhost:54321/api/v1/", UriKind.Absolute),
            "/session/list",
            new Dictionary<string, object?>
            {
                ["limit"] = 10,
                ["enabled"] = true,
            });

        result.Should().Be(new Uri("http://localhost:54321/api/v1/session/list?limit=10&enabled=true", UriKind.Absolute));
    }

    [Fact]
    public void HeaderBuilderAppliesPrecedenceAndSupportsRemoval()
    {
        var result = HeaderBuilder.Build(
            "Opencode.Sdk/1.0.0",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Stainless-Lang"] = "dotnet",
                ["X-Stainless-Package-Version"] = "1.0.0",
            },
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Test"] = "default",
                ["Accept"] = "application/problem+json",
            },
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "application/json; charset=utf-8",
            },
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Test"] = "request",
                ["Accept"] = null,
            },
            TimeSpan.FromSeconds(45),
            1);

        result.Should().Contain(new KeyValuePair<string, string>("X-Test", "request"));
        result.Should().Contain(new KeyValuePair<string, string>("Content-Type", "application/json; charset=utf-8"));
        result.Should().Contain(new KeyValuePair<string, string>("X-Stainless-Timeout", "45"));
        result.Should().NotContainKey("Accept");
    }

    [Fact]
    public void HttpPipelineOptionsCreateUsesExpectedDefaults()
    {
        var options = new OpencodeClientOptions();

        var result = HttpPipelineOptions.Create(options);

        result.BaseUrl.Should().Be(new Uri("http://localhost:54321", UriKind.Absolute));
        result.Timeout.Should().Be(TimeSpan.FromMinutes(1));
        result.MaxRetries.Should().Be(2);
        result.HttpClient.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
        result.SdkHeaders.Should().ContainKey("X-Stainless-Lang").WhoseValue.Should().Be("dotnet");
        result.UserAgent.Should().StartWith("Opencode.Sdk/");
    }

    [Fact]
    public async Task HttpRequestFactoryCreatesJsonRequestMessage()
    {
        var options = HttpPipelineOptions.Create(new OpencodeClientOptions());
        var factory = new HttpRequestFactory(new RequestUriBuilder(), new HeaderBuilder(), new JsonSerializerProvider(new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var request = new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = "/session/create",
            Body = new { name = "demo" },
        };

        using var result = factory.Create(options, request);

        result.Method.Should().Be(HttpMethod.Post);
        result.RequestUri.Should().Be(new Uri("http://localhost:54321/session/create", UriKind.Absolute));
        result.Headers.Should().Contain(header => header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase));
        result.Content.Should().NotBeNull();
        result.Content!.Headers.ContentType.Should().BeEquivalentTo(MediaTypeHeaderValue.Parse("application/json; charset=utf-8"));
        (await result.Content.ReadAsStringAsync()).Should().Be("{\"name\":\"demo\"}");
    }

    [Fact]
    public async Task HttpResponseParserParsesJsonAndNoContentResponses()
    {
        var parser = new HttpResponseParser(new JsonSerializerProvider(new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        using var jsonResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"value\":42}", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
        };
        using var noContentResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

        var parsed = await parser.ParseAsync<TestPayload>(jsonResponse);
        var empty = await parser.ParseAsync<TestPayload>(noContentResponse);

        parsed.Should().NotBeNull();
        parsed!.Value.Should().Be(42);
        empty.Should().BeNull();
    }

    [Fact]
    public async Task HttpResponseParserRejectsNonJsonSuccessResponsesWithTypedException()
    {
        var parser = new HttpResponseParser(new JsonSerializerProvider(new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>ok</html>", Encoding.UTF8, MediaTypeHeaderValue.Parse("text/html")),
        };

        var action = () => parser.ParseAsync<TestPayload>(response).AsTask();

        var exception = await Assert.ThrowsAsync<UnexpectedResponseException>(action);
        exception.ContentType.Should().Be("text/html");
        exception.ResponseBody.Should().Be("<html>ok</html>");
    }

    [Fact]
    public async Task HttpResponseParserRejectsMalformedJsonSuccessResponsesWithTypedException()
    {
        var parser = new HttpResponseParser(new JsonSerializerProvider(new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
        };

        var action = () => parser.ParseAsync<TestPayload>(response).AsTask();

        var exception = await Assert.ThrowsAsync<UnexpectedResponseException>(action);
        exception.ContentType.Should().Be("application/json");
        exception.ResponseBody.Should().Be("not-json");
        exception.InnerException.Should().BeOfType<JsonException>();
    }

    [Fact]
    public async Task HttpPipelineExecuteAsyncRetriesTransientResponses()
    {
        var handler = new SequenceHttpMessageHandler(
            static request => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Headers = { { "retry-after-ms", "0" } },
                RequestMessage = request,
            },
            static request => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":42}", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
                RequestMessage = request,
            });

        var pipeline = CreatePipeline(handler, maxRetries: 2);
        var request = new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/session/list",
        };

        using var response = await pipeline.ExecuteAsync(request);
        var payload = await pipeline.ResponseParser.ParseAsync<TestPayload>(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload!.Value.Should().Be(42);
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].Headers.GetValues("X-Stainless-Retry-Count").Single().Should().Be("0");
        handler.Requests[1].Headers.GetValues("X-Stainless-Retry-Count").Single().Should().Be("1");
    }

    [Fact]
    public async Task HttpPipelineExecuteAsyncThrowsTimeoutWhenRequestExceedsTimeout()
    {
        var handler = new SequenceHttpMessageHandler(static (_, cancellationToken) => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
        var pipeline = CreatePipeline(handler, maxRetries: 0);
        var request = new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/session/list",
            Timeout = TimeSpan.FromMilliseconds(25),
        };

        var action = async () => await pipeline.ExecuteAsync(request);

        var exception = await Assert.ThrowsAsync<RequestTimeoutException>(action);
        exception.Timeout.Should().Be(TimeSpan.FromMilliseconds(25));
    }

    [Fact]
    public async Task HttpPipelineExecuteAsyncPreservesCallerCancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var handler = new SequenceHttpMessageHandler(static (_, cancellationToken) => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
        var pipeline = CreatePipeline(handler, maxRetries: 0);
        var request = new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/session/list",
            Timeout = TimeSpan.FromSeconds(5),
            CancellationToken = cancellationTokenSource.Token,
        };

        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(25));

        var action = async () => await pipeline.ExecuteAsync(request);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task HttpPipelineExecuteAsyncRetriesTimeouts()
    {
        var handler = new SequenceHttpMessageHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("The timeout branch should not complete without cancellation.");
            },
            static (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":7}", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
            }));

        var pipeline = CreatePipeline(handler, maxRetries: 1);
        var request = new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/session/list",
            Timeout = TimeSpan.FromMilliseconds(25),
        };

        using var response = await pipeline.ExecuteAsync(request);
        var payload = await pipeline.ResponseParser.ParseAsync<TestPayload>(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload!.Value.Should().Be(7);
        handler.Requests.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, typeof(BadRequestException))]
    [InlineData(HttpStatusCode.Unauthorized, typeof(UnauthorizedException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(ForbiddenException))]
    [InlineData(HttpStatusCode.NotFound, typeof(NotFoundException))]
    [InlineData(HttpStatusCode.Conflict, typeof(ConflictException))]
    [InlineData(HttpStatusCode.UnprocessableEntity, typeof(UnprocessableEntityException))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(RateLimitException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(InternalServerErrorException))]
    public async Task HttpPipelineSendAsyncMapsKnownHttpFailures(HttpStatusCode statusCode, Type expectedExceptionType)
    {
        var handler = new SequenceHttpMessageHandler(
            request => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{\"error\":\"failure\"}", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
                RequestMessage = request,
            });

        var pipeline = CreatePipeline(handler, maxRetries: 0);
        var request = new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/app",
        };

        var exception = await Assert.ThrowsAnyAsync<ApiException>(() => pipeline.SendAsync<TestPayload>(request));

        exception.Should().BeOfType(expectedExceptionType);
        exception.StatusCode.Should().Be(statusCode);
        exception.ResponseBody.Should().Be("{\"error\":\"failure\"}");
    }

    [Fact]
    public async Task HttpPipelineSendAsyncMapsUnknownHttpFailuresToApiException()
    {
        var handler = new SequenceHttpMessageHandler(
            static request => new HttpResponseMessage(HttpStatusCode.PaymentRequired)
            {
                Content = new StringContent("teapot-adjacent", Encoding.UTF8, MediaTypeHeaderValue.Parse("text/plain")),
                RequestMessage = request,
            });

        var pipeline = CreatePipeline(handler, maxRetries: 0);
        var request = new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/app",
        };

        var exception = await Assert.ThrowsAsync<ApiException>(() => pipeline.SendAsync<TestPayload>(request));

        exception.Should().NotBeOfType<BadRequestException>();
        exception.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        exception.ResponseBody.Should().Be("teapot-adjacent");
    }

    [Fact]
    public async Task HttpPipelineSendAsyncPreservesHeadersBodyAndRequestIdOnFailures()
    {
        var handler = new SequenceHttpMessageHandler(
            static request => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Headers = { { "x-request-id", "req-123" }, { "x-extra", "value" } },
                Content = new StringContent("{\"message\":\"invalid\"}", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/problem+json")),
                RequestMessage = request,
            });

        var pipeline = CreatePipeline(handler, maxRetries: 0);
        var request = new HttpPipelineRequest
        {
            Method = HttpMethod.Post,
            Path = "/session/create",
        };

        var exception = await Assert.ThrowsAsync<BadRequestException>(() => pipeline.SendAsync<TestPayload>(request));

        exception.RequestId.Should().Be("req-123");
        exception.ResponseBody.Should().Be("{\"message\":\"invalid\"}");
        exception.Headers.Should().ContainKey("x-request-id");
        exception.Headers["x-request-id"].Should().ContainSingle().Which.Should().Be("req-123");
        exception.Headers.Should().ContainKey("Content-Type");
    }

    [Fact]
    public async Task HttpPipelineExecuteAsyncMapsHttpRequestFailuresToConnectionException()
    {
        var handler = new SequenceHttpMessageHandler(static (_, _) => throw new HttpRequestException("boom"));
        var pipeline = CreatePipeline(handler, maxRetries: 0);
        var request = new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/app",
        };

        var exception = await Assert.ThrowsAsync<ConnectionException>(() => pipeline.ExecuteAsync(request));

        exception.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task DiagnosticsHandlerReceivesStablePipelineEvents()
    {
        var events = new List<OpencodeDiagnosticEvent>();
        var handler = new SequenceHttpMessageHandler(
            static request => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Headers = { { "retry-after-ms", "0" } },
                RequestMessage = request,
            },
            static request => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"server\"}", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
                RequestMessage = request,
            });

        var pipeline = CreatePipeline(
            handler,
            maxRetries: 1,
            diagnosticsHandler: events.Add);

        var request = new HttpPipelineRequest
        {
            Method = HttpMethod.Get,
            Path = "/app",
        };

        await Assert.ThrowsAsync<InternalServerErrorException>(() => pipeline.SendAsync<TestPayload>(request));

        events.Select(item => item.Kind).Should().ContainInOrder(
            OpencodeDiagnosticEventKind.RequestStarted,
            OpencodeDiagnosticEventKind.ResponseReceived,
            OpencodeDiagnosticEventKind.RetryScheduled,
            OpencodeDiagnosticEventKind.RequestStarted,
            OpencodeDiagnosticEventKind.ResponseReceived,
            OpencodeDiagnosticEventKind.ExceptionThrown);
        events.Last().StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        events.Last().ExceptionType.Should().Be(typeof(InternalServerErrorException).FullName);
    }

    [Fact]
    public async Task AppClientGetAsyncSurfacesTypedHttpFailures()
    {
        var handler = new SequenceHttpMessageHandler(
            static request => new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing", Encoding.UTF8, MediaTypeHeaderValue.Parse("text/plain")),
                RequestMessage = request,
            });

        var client = new OpencodeClient(new OpencodeClientOptions
        {
            HttpClient = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan,
            },
        });

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => client.App.GetAsync());

        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.ResponseBody.Should().Be("missing");
    }

    [Fact]
    public async Task AppClientGetAsyncUsesSharedPipeline()
    {
        var handler = new SequenceHttpMessageHandler(
            static request => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"git\":true,\"hostname\":\"demo-host\",\"path\":{\"config\":\"/config\",\"cwd\":\"/cwd\",\"data\":\"/data\",\"root\":\"/root\",\"state\":\"/state\"},\"time\":{\"initialized\":123}}",
                    Encoding.UTF8,
                    MediaTypeHeaderValue.Parse("application/json")),
                RequestMessage = request,
            });

        var client = new OpencodeClient(new OpencodeClientOptions
        {
            HttpClient = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan,
            },
        });

        var result = await client.App.GetAsync();

        result.Should().BeEquivalentTo(new AppInfo
        {
            Git = true,
            Hostname = "demo-host",
            Path = new AppPathInfo
            {
                Config = "/config",
                Cwd = "/cwd",
                Data = "/data",
                Root = "/root",
                State = "/state",
            },
            Time = new AppTimeInfo
            {
                Initialized = 123,
            },
        });

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/app", UriKind.Absolute));
    }

    private static HttpPipeline CreatePipeline(HttpMessageHandler handler, int maxRetries, Action<OpencodeDiagnosticEvent>? diagnosticsHandler = null)
    {
        var options = new OpencodeClientOptions
        {
            HttpClient = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan,
            },
            MaxRetries = maxRetries,
            DiagnosticsHandler = diagnosticsHandler,
        };

        return new HttpPipeline(HttpPipelineOptions.Create(options));
    }

    private sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responses;

        public SequenceHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
        {
            _responses = new Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>(
                responses.Select<Func<HttpRequestMessage, HttpResponseMessage>, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>(
                    response => (request, _) => Task.FromResult(response(Clone(request)))));
        }

        public SequenceHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task> response)
        {
            _responses = new Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>([
                async (request, cancellationToken) =>
                {
                    await response(Clone(request), cancellationToken);
                    throw new InvalidOperationException("The response delegate must complete by cancellation.");
                },
            ]);
        }

        public SequenceHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response)
        {
            _responses = new Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>([
                async (request, cancellationToken) => await response(Clone(request), cancellationToken),
            ]);
        }

        public SequenceHttpMessageHandler(params Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>[] responses)
        {
            _responses = new Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>(responses);
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No queued response is available for the request.");
            }

            var clone = Clone(request);
            Requests.Add(clone);
            return await _responses.Dequeue()(request, cancellationToken);
        }

        private static HttpRequestMessage Clone(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                var content = request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                clone.Content = new ByteArrayContent(content);

                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }
    }

    private sealed class TestPayload
    {
        public int Value { get; init; }
    }
}
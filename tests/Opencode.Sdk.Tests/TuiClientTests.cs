using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Opencode.Client;

namespace Opencode.Sdk.Tests;

public sealed class TuiClientTests
{
    [Fact]
    public async Task AppendPromptAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("true", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.Tui.AppendPromptAsync("Summarize this session");

        result.Should().BeTrue();
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/tui/append-prompt", UriKind.Absolute));
        handler.RequestBodies.Should().ContainSingle().Which.Should().Be("{\"text\":\"Summarize this session\"}");
    }

    [Fact]
    public async Task OpenHelpAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("true", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.Tui.OpenHelpAsync();

        result.Should().BeTrue();
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/tui/open-help", UriKind.Absolute));
        handler.RequestBodies.Should().ContainSingle().Which.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AppendPromptAsyncRejectsInvalidText(string? text)
    {
        var client = CreateClient(new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
        }));

        var action = () => client.Tui.AppendPromptAsync(text!);

        (await action.Should().ThrowAsync<ArgumentException>())
            .WithParameterName(nameof(text))
            .WithMessage("The prompt text must not be empty. (Parameter 'text')");
    }

    private static OpencodeClient CreateClient(HttpMessageHandler handler)
    {
        return new OpencodeClient(new OpencodeClientOptions
        {
            HttpClient = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan,
            },
        });
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public CapturingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(Clone(request));
            RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return _responseFactory(request);
        }

        private static HttpRequestMessage Clone(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}
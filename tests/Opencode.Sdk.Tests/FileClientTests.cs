using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Opencode.Client;
using Opencode.Models.File;

namespace Opencode.Sdk.Tests;

public sealed class FileClientTests
{
    [Fact]
    public async Task ReadAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"content\":\"diff --git a/sample.txt b/sample.txt\",\"type\":\"patch\"}",
                Encoding.UTF8,
                MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.File.ReadAsync("src/sample.txt");

        result.Should().BeEquivalentTo(new FileReadResponse
        {
            Content = "diff --git a/sample.txt b/sample.txt",
            Type = "patch",
        });

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/file?path=src%2Fsample.txt", UriKind.Absolute));
    }

    [Fact]
    public async Task GetStatusAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "[{\"added\":3,\"path\":\"src/sample.txt\",\"removed\":1,\"status\":\"modified\"}]",
                Encoding.UTF8,
                MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.File.GetStatusAsync();

        result.Should().BeEquivalentTo(
        [
            new FileStatusEntry
            {
                Added = 3,
                Path = "src/sample.txt",
                Removed = 1,
                Status = "modified",
            },
        ]);

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/file/status", UriKind.Absolute));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReadAsyncRejectsInvalidPath(string? path)
    {
        var client = CreateClient(new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
        }));

        var action = () => client.File.ReadAsync(path!);

        (await action.Should().ThrowAsync<ArgumentException>())
            .WithParameterName(nameof(path))
            .WithMessage("The file path must not be empty. (Parameter 'path')");
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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(Clone(request));
            return Task.FromResult(_responseFactory(request));
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
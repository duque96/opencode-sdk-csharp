using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Opencode.Client;
using Opencode.Models.Find;

namespace Opencode.Sdk.Tests;

public sealed class FindClientTests
{
    [Fact]
    public async Task FindFilesAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[\"src/alpha.cs\",\"src/beta.cs\"]", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.Find.FindFilesAsync("src");

        result.Should().Equal("src/alpha.cs", "src/beta.cs");
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/find/file?query=src", UriKind.Absolute));
    }

    [Fact]
    public async Task FindSymbolsAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "[{\"kind\":12,\"location\":{\"range\":{\"end\":{\"character\":8,\"line\":4},\"start\":{\"character\":2,\"line\":4}},\"uri\":\"file:///workspace/src/app.cs\"},\"name\":\"RunAsync\"}]",
                Encoding.UTF8,
                MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.Find.FindSymbolsAsync("Run");

        result.Should().BeEquivalentTo(
        [
            new FindSymbol
            {
                Kind = 12,
                Name = "RunAsync",
                Location = new FindSymbolLocation
                {
                    Uri = "file:///workspace/src/app.cs",
                    Range = new FindRange
                    {
                        Start = new FindPosition
                        {
                            Character = 2,
                            Line = 4,
                        },
                        End = new FindPosition
                        {
                            Character = 8,
                            Line = 4,
                        },
                    },
                },
            },
        ]);

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/find/symbol?query=Run", UriKind.Absolute));
    }

    [Fact]
    public async Task FindTextAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "[{\"absolute_offset\":24,\"line_number\":3,\"lines\":{\"text\":\"Console.WriteLine(\\\"demo\\\")\"},\"path\":{\"text\":\"src/app.cs\"},\"submatches\":[{\"end\":18,\"match\":{\"text\":\"demo\"},\"start\":14}]}]",
                Encoding.UTF8,
                MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.Find.FindTextAsync("demo");

        result.Should().BeEquivalentTo(
        [
            new FindTextMatch
            {
                AbsoluteOffset = 24,
                LineNumber = 3,
                Lines = new FindTextValue
                {
                    Text = "Console.WriteLine(\"demo\")",
                },
                Path = new FindTextValue
                {
                    Text = "src/app.cs",
                },
                Submatches =
                [
                    new FindTextSubmatch
                    {
                        End = 18,
                        Match = new FindTextValue
                        {
                            Text = "demo",
                        },
                        Start = 14,
                    },
                ],
            },
        ]);

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/find?pattern=demo", UriKind.Absolute));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindFilesAsyncRejectsInvalidQuery(string? query)
    {
        var client = CreateClient(new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
        }));

        var action = () => client.Find.FindFilesAsync(query!);

        (await action.Should().ThrowAsync<ArgumentException>())
            .WithParameterName(nameof(query))
            .WithMessage("The search query must not be empty. (Parameter 'query')");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindTextAsyncRejectsInvalidPattern(string? pattern)
    {
        var client = CreateClient(new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
        }));

        var action = () => client.Find.FindTextAsync(pattern!);

        (await action.Should().ThrowAsync<ArgumentException>())
            .WithParameterName(nameof(pattern))
            .WithMessage("The search pattern must not be empty. (Parameter 'pattern')");
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
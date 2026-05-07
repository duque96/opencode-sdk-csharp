using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Opencode.Client;
using Opencode.Errors;
using Opencode.Models.Config;

namespace Opencode.Sdk.Tests;

public sealed class ConfigClientTests
{
    [Fact]
    public async Task GetAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{" +
                "\"$schema\":\"https://opencode.ai/schema.json\"," +
                "\"agent\":{\"general\":{\"description\":\"Default agent\",\"model\":\"openai/gpt-5.4\",\"prompt\":\"Plan first\",\"temperature\":0.2,\"tools\":{\"grep_search\":true}}}," +
                "\"autoshare\":true," +
                "\"autoupdate\":false," +
                "\"disabled_providers\":[\"local\"]," +
                "\"experimental\":{\"hook\":{\"file_edited\":{\"format\":[{\"command\":[\"dotnet\",\"format\"],\"environment\":{\"DOTNET_ENVIRONMENT\":\"Development\"}}]},\"session_completed\":[{\"command\":[\"echo\",\"done\"]}]}}," +
                "\"instructions\":[\"AGENTS.md\"]," +
                "\"keybinds\":{\"app_exit\":\"ctrl+c\",\"session_new\":\"ctrl+n\"}," +
                "\"layout\":\"stretch\"," +
                "\"mcp\":{\"filesystem\":{\"type\":\"local\",\"command\":[\"npx\",\"@modelcontextprotocol/server-filesystem\"],\"enabled\":true,\"environment\":{\"ROOT\":\"/workspace\"}},\"remote-docs\":{\"type\":\"remote\",\"url\":\"https://example.test/mcp\",\"headers\":{\"Authorization\":\"Bearer token\"}}}," +
                "\"mode\":{\"plan\":{\"model\":\"openai/gpt-5.4\",\"prompt\":\"Think first\",\"tools\":{\"grep_search\":true}}}," +
                "\"model\":\"openai/gpt-5.4\"," +
                "\"provider\":{\"openai\":{\"models\":{\"gpt-5.4\":{\"id\":\"gpt-5.4\",\"attachment\":true,\"cost\":{\"input\":0.1,\"output\":0.2},\"limit\":{\"context\":128000,\"output\":8192},\"name\":\"GPT-5.4\",\"options\":{\"reasoning\":\"high\"},\"reasoning\":true,\"release_date\":\"2026-01-01\",\"temperature\":true,\"tool_call\":true}},\"api\":\"https://api.openai.com\",\"env\":[\"OPENAI_API_KEY\"],\"name\":\"OpenAI\",\"npm\":\"@opencode/openai\",\"options\":{\"baseURL\":\"https://proxy.example\"}}}," +
                "\"share\":\"manual\"," +
                "\"small_model\":\"openai/gpt-5.4-mini\"," +
                "\"theme\":\"nord\"," +
                "\"username\":\"dani\"}",
                Encoding.UTF8,
                MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.Config.GetAsync();

        result.Should().NotBeNull();
        result!.Schema.Should().Be("https://opencode.ai/schema.json");
        result.Agent.Should().ContainKey("general");
        result.Agent!["general"].Description.Should().Be("Default agent");
        result.DisabledProviders.Should().Equal("local");
        result.Experimental!.Hook!.FileEdited.Should().ContainKey("format");
        result.Keybinds.Should().Contain(new KeyValuePair<string, string>("app_exit", "ctrl+c"));
        result.Mcp.Should().ContainKey("filesystem");
        result.Mcp!["filesystem"].Command.Should().Equal("npx", "@modelcontextprotocol/server-filesystem");
        result.Provider.Should().ContainKey("openai");
        result.Provider!["openai"].Models["gpt-5.4"].ReleaseDate.Should().Be("2026-01-01");
        result.SmallModel.Should().Be("openai/gpt-5.4-mini");
        result.Share.Should().Be("manual");

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/config", UriKind.Absolute));
    }

    [Fact]
    public async Task GetAsyncSurfacesTypedHttpFailures()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("forbidden", Encoding.UTF8, MediaTypeHeaderValue.Parse("text/plain")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var action = () => client.Config.GetAsync();

        var exception = await Assert.ThrowsAsync<ForbiddenException>(action);
        exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.ResponseBody.Should().Be("forbidden");
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
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Opencode.Client;
using Opencode.Errors;
using Opencode.Models.App;

namespace Opencode.Sdk.Tests;

public sealed class AppClientTests
{
    [Fact]
    public async Task GetAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"git\":true,\"hostname\":\"demo-host\",\"path\":{\"config\":\"/config\",\"cwd\":\"/cwd\",\"data\":\"/data\",\"root\":\"/root\",\"state\":\"/state\"},\"time\":{\"initialized\":123}}",
                Encoding.UTF8,
                MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

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

    [Fact]
    public async Task InitializeAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("true", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.App.InitializeAsync();

        result.Should().BeTrue();
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/app/init", UriKind.Absolute));
        handler.RequestBodies.Should().ContainSingle().Which.Should().BeEmpty();
    }

    [Fact]
    public async Task LogAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("true", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.App.LogAsync(new AppLogRequest
        {
            Level = "debug",
            Message = "started",
            Service = "sdk-tests",
            Extra = new Dictionary<string, object?>
            {
                ["attempt"] = 2,
            },
        });

        result.Should().BeTrue();
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/log", UriKind.Absolute));
        handler.RequestBodies.Should().ContainSingle().Which.Should().Be("{\"level\":\"debug\",\"message\":\"started\",\"service\":\"sdk-tests\",\"extra\":{\"attempt\":2}}");
    }

    [Fact]
    public async Task GetModesAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "[{\"name\":\"plan\",\"tools\":{\"grep\":true},\"model\":{\"modelID\":\"gpt-5.4\",\"providerID\":\"openai\"},\"prompt\":\"You are planning.\",\"temperature\":0.2}]",
                Encoding.UTF8,
                MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.App.GetModesAsync();

        result.Should().BeEquivalentTo(
        [
            new AppModeInfo
            {
                Name = "plan",
                Tools = new Dictionary<string, bool>
                {
                    ["grep"] = true,
                },
                Model = new AppModeModelInfo
                {
                    ModelId = "gpt-5.4",
                    ProviderId = "openai",
                },
                Prompt = "You are planning.",
                Temperature = 0.2,
            },
        ]);

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/mode", UriKind.Absolute));
    }

    [Fact]
    public async Task GetProvidersAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"default\":{\"chat\":\"openai\"},\"providers\":[{\"id\":\"openai\",\"env\":[\"OPENAI_API_KEY\"],\"models\":{\"gpt-5.4\":{\"id\":\"gpt-5.4\",\"attachment\":true,\"cost\":{\"input\":0.1,\"output\":0.2},\"limit\":{\"context\":128000,\"output\":8192},\"name\":\"GPT-5.4\",\"options\":{\"reasoning\":\"high\"},\"reasoning\":true,\"release_date\":\"2026-01-01\",\"temperature\":true,\"tool_call\":true}},\"name\":\"OpenAI\",\"api\":\"https://api.openai.com\",\"npm\":\"@opencode/openai\"}]}",
                Encoding.UTF8,
                MediaTypeHeaderValue.Parse("application/json")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.App.GetProvidersAsync();

        result.Should().NotBeNull();
        result!.Defaults.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string>("chat", "openai"));
        result.Providers.Should().ContainSingle();
        result.Providers[0].Id.Should().Be("openai");
        result.Providers[0].Models.Should().ContainKey("gpt-5.4");
        result.Providers[0].Models["gpt-5.4"].ReleaseDate.Should().Be("2026-01-01");
        result.Providers[0].Models["gpt-5.4"].ToolCall.Should().BeTrue();

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/config/providers", UriKind.Absolute));
    }

    [Theory]
    [InlineData(null, "message", "service", "The log level must not be empty. (Parameter 'request')")]
    [InlineData("", "message", "service", "The log level must not be empty. (Parameter 'request')")]
    [InlineData("trace", "message", "service", "The log level must be one of: debug, info, warn, error. (Parameter 'request')")]
    [InlineData("debug", "", "service", "The log message must not be empty. (Parameter 'request')")]
    [InlineData("debug", "message", "", "The service name must not be empty. (Parameter 'request')")]
    public async Task LogAsyncRejectsInvalidInput(string? level, string? message, string? service, string expectedMessage)
    {
        var client = CreateClient(new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
        }));

        var action = () => client.App.LogAsync(new AppLogRequest
        {
            Level = level!,
            Message = message!,
            Service = service!,
        });

        (await action.Should().ThrowAsync<ArgumentException>())
            .WithParameterName("request")
            .WithMessage(expectedMessage);
    }

    [Fact]
    public async Task GetAsyncSurfacesTypedHttpFailures()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("missing", Encoding.UTF8, MediaTypeHeaderValue.Parse("text/plain")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var action = () => client.App.GetAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(action);
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.ResponseBody.Should().Be("missing");
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
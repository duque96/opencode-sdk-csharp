using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Opencode.Client;
using Opencode.Errors;
using Opencode.Models.Session;

namespace Opencode.Sdk.Tests;

public sealed class SessionClientTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        AllowOutOfOrderMetadataProperties = true,
    };

    [Fact]
    public async Task CreateAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent("""
                {"id":"session_123","time":{"created":1,"updated":2},"title":"Demo","version":"1","share":{"url":"https://share"}}
                """),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.Session.CreateAsync();

        result.Should().BeEquivalentTo(new SessionInfo
        {
            Id = "session_123",
            Time = new SessionTimeInfo
            {
                Created = 1,
                Updated = 2,
            },
            Title = "Demo",
            Version = "1",
            Share = new SessionShareInfo
            {
                Url = "https://share",
            },
        });

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/session", UriKind.Absolute));
        handler.RequestBodies.Should().ContainSingle().Which.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsyncBuildsExpectedRequestAndParsesResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent("""
                [{"id":"session_123","time":{"created":1,"updated":2},"title":"Demo","version":"1"}]
                """),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.Session.ListAsync();

        result.Should().BeEquivalentTo(
        [
            new SessionInfo
            {
                Id = "session_123",
                Time = new SessionTimeInfo
                {
                    Created = 1,
                    Updated = 2,
                },
                Title = "Demo",
                Version = "1",
            },
        ]);

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/session", UriKind.Absolute));
    }

    [Fact]
    public async Task ChatAsyncBuildsExpectedRequestAndParsesAssistantMessage()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent("""
                {
                                    "info":{
                                        "id":"msg_assistant",
                                        "parentID":"msg_user",
                                        "role":"assistant",
                                        "sessionID":"session_123",
                                        "agent":"build",
                                        "cost":0.25,
                                        "mode":"chat",
                                        "modelID":"gpt-5.4",
                                        "path":{"cwd":"/cwd","root":"/root"},
                                        "providerID":"openai",
                                        "time":{"created":10,"completed":12},
                                        "finish":"stop",
                                        "tokens":{"total":15,"cache":{"read":1,"write":2},"input":3,"output":4,"reasoning":5},
                                        "error":{"name":"ProviderAuthError","data":{"message":"Missing key","providerID":"openai"}}
                                    },
                                    "parts":[
                                        {"id":"part_step","messageID":"msg_assistant","sessionID":"session_123","type":"step-start"},
                                        {"id":"part_text","messageID":"msg_assistant","sessionID":"session_123","type":"text","text":"Hola"},
                                        {"id":"part_finish","messageID":"msg_assistant","sessionID":"session_123","type":"step-finish","reason":"stop","cost":0.25,"tokens":{"total":15,"cache":{"read":1,"write":2},"input":3,"output":4,"reasoning":5}}
                                    ]
                }
                """),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.Session.ChatAsync("session_123", new SessionChatRequest
        {
            ModelId = "gpt-5.4",
            ProviderId = "openai",
            Parts =
            [
                new TextPartInput
                {
                    Text = "Hola",
                },
            ],
            Tools = new Dictionary<string, bool>
            {
                ["grep"] = true,
            },
        });

        result.Should().BeOfType<AssistantMessage>();
        result!.ParentId.Should().Be("msg_user");
        result.Agent.Should().Be("build");
        result.Finish.Should().Be("stop");
        result.System.Should().BeNull();
        result.Tokens.Total.Should().Be(15);
        result!.Error.Should().BeOfType<ProviderAuthError>();
        result.Error.As<ProviderAuthError>().Data.ProviderId.Should().Be("openai");

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/session/session_123/message", UriKind.Absolute));
        handler.RequestBodies.Should().ContainSingle();
        JsonNode.DeepEquals(
            JsonNode.Parse(handler.RequestBodies[0]),
            JsonNode.Parse("{" +
                "\"modelID\":\"gpt-5.4\"," +
                "\"parts\":[{\"type\":\"text\",\"text\":\"Hola\"}]," +
                "\"providerID\":\"openai\"," +
                "\"tools\":{\"grep\":true}}"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task PromptAsyncBuildsExpectedRequestAndAcceptsNoContentResponse()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.NoContent)
        {
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.Session.PromptAsync("session_123", new SessionChatRequest
        {
            ModelId = "gpt-5.4",
            ProviderId = "openai",
            Parts =
            [
                new TextPartInput
                {
                    Text = "Hola",
                },
            ],
            Tools = new Dictionary<string, bool>
            {
                ["grep"] = true,
            },
        });

        result.Should().BeTrue();

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Post);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/session/session_123/prompt_async", UriKind.Absolute));
        handler.RequestBodies.Should().ContainSingle();
        JsonNode.DeepEquals(
            JsonNode.Parse(handler.RequestBodies[0]),
            JsonNode.Parse("{" +
                "\"modelID\":\"gpt-5.4\"," +
                "\"parts\":[{\"type\":\"text\",\"text\":\"Hola\"}]," +
                "\"providerID\":\"openai\"," +
                "\"tools\":{\"grep\":true}}"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task MessagesAsyncBuildsExpectedRequestAndParsesPolymorphicParts()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent("""
                [
                  {
                                        "info":{"id":"msg_user","role":"user","sessionID":"session_123","agent":"build","time":{"created":1}},
                    "parts":[
                      {"id":"part_text","messageID":"msg_user","sessionID":"session_123","type":"text","text":"Hola"},
                      {
                        "id":"part_tool",
                        "messageID":"msg_user",
                        "sessionID":"session_123",
                        "type":"tool",
                        "callID":"call_1",
                        "tool":"grep",
                        "state":{"status":"completed","input":{"pattern":"hola"},"metadata":{"matches":2},"output":"done","time":{"start":3,"end":4},"title":"Search"}
                      },
                      {"id":"part_patch","messageID":"msg_user","sessionID":"session_123","type":"patch","files":["src/a.cs"],"hash":"abc"}
                    ]
                                    },
                                    {
                                        "info":{"id":"msg_assistant","parentID":"msg_user","role":"assistant","sessionID":"session_123","agent":"build","cost":0.25,"mode":"chat","modelID":"gpt-5.4","path":{"cwd":"/cwd","root":"/root"},"providerID":"openai","time":{"created":10,"completed":12},"finish":"stop","tokens":{"total":15,"cache":{"read":1,"write":2},"input":3,"output":4,"reasoning":5}},
                                        "parts":[
                                            {"id":"part_step","messageID":"msg_assistant","sessionID":"session_123","type":"step-start"},
                                            {"id":"part_reasoning","messageID":"msg_assistant","sessionID":"session_123","type":"reasoning","text":"Thinking","time":{"start":11,"end":12},"metadata":{"anthropic":{"signature":""}}},
                                            {"id":"part_answer","messageID":"msg_assistant","sessionID":"session_123","type":"text","text":"Respuesta"},
                                            {"id":"part_finish","messageID":"msg_assistant","sessionID":"session_123","type":"step-finish","reason":"stop","cost":0.25,"tokens":{"total":15,"cache":{"read":1,"write":2},"input":3,"output":4,"reasoning":5}}
                                        ]
                  }
                ]
                """),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var result = await client.Session.MessagesAsync("session_123");

        result.Should().HaveCount(2);
        result![0].Info.Should().BeOfType<UserMessage>();
        result[0].Parts[0].Should().BeOfType<TextPart>();
        result[0].Parts[1].Should().BeOfType<ToolPart>();
        result[0].Parts[2].Should().BeOfType<PatchPart>();
        ((ToolPart)result[0].Parts[1]).State.Should().BeOfType<ToolStateCompleted>();
        result[1].Info.Should().BeOfType<AssistantMessage>();
        result[1].Parts[0].Should().BeOfType<StepStartPart>();
        result[1].Parts[1].Should().BeOfType<ReasoningPart>();
        result[1].Parts[2].Should().BeOfType<TextPart>();
        result[1].Parts[3].Should().BeOfType<StepFinishPart>();
        ((AssistantMessage)result[1].Info).Finish.Should().Be("stop");

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/session/session_123/message", UriKind.Absolute));
    }

    [Theory]
    [InlineData("DeleteAsync", "DELETE", "http://localhost:54321/session/session_123", "true")]
    [InlineData("AbortAsync", "POST", "http://localhost:54321/session/session_123/abort", "true")]
    [InlineData("ShareAsync", "POST", "http://localhost:54321/session/session_123/share", "{\"id\":\"session_123\",\"time\":{\"created\":1,\"updated\":2},\"title\":\"Demo\",\"version\":\"1\"}")]
    [InlineData("UnrevertAsync", "POST", "http://localhost:54321/session/session_123/unrevert", "{\"id\":\"session_123\",\"time\":{\"created\":1,\"updated\":2},\"title\":\"Demo\",\"version\":\"1\"}")]
    [InlineData("UnshareAsync", "DELETE", "http://localhost:54321/session/session_123/share", "{\"id\":\"session_123\",\"time\":{\"created\":1,\"updated\":2},\"title\":\"Demo\",\"version\":\"1\"}")]
    public async Task SessionOperationsBuildExpectedRoutes(string methodName, string httpMethod, string uri, string responseJson)
    {
        var handler = new CapturingHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(responseJson),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        switch (methodName)
        {
            case "DeleteAsync":
                await client.Session.DeleteAsync("session_123");
                break;
            case "AbortAsync":
                await client.Session.AbortAsync("session_123");
                break;
            case "ShareAsync":
                await client.Session.ShareAsync("session_123");
                break;
            case "UnrevertAsync":
                await client.Session.UnrevertAsync("session_123");
                break;
            case "UnshareAsync":
                await client.Session.UnshareAsync("session_123");
                break;
            default:
                throw new InvalidOperationException($"Unexpected method: {methodName}");
        }

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Method.Should().Be(httpMethod);
        handler.Requests[0].RequestUri.Should().Be(new Uri(uri, UriKind.Absolute));
    }

    [Fact]
    public async Task InitRevertAndSummarizeBuildExpectedBodies()
    {
        var responses = new Queue<string>(
        [
            "true",
            "{\"id\":\"session_123\",\"time\":{\"created\":1,\"updated\":2},\"title\":\"Demo\",\"version\":\"1\",\"revert\":{\"messageID\":\"msg_user\",\"partID\":\"part_text\"}}",
            "true",
        ]);

        var handler = new CapturingHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(responses.Dequeue()),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var initResult = await client.Session.InitAsync("session_123", new SessionInitRequest
        {
            MessageId = "msg_user",
            ModelId = "gpt-5.4",
            ProviderId = "openai",
        });

        var revertResult = await client.Session.RevertAsync("session_123", new SessionRevertRequest
        {
            MessageId = "msg_user",
            PartId = "part_text",
        });

        var summarizeResult = await client.Session.SummarizeAsync("session_123", new SessionSummarizeRequest
        {
            ModelId = "gpt-5.4",
            ProviderId = "openai",
        });

        initResult.Should().BeTrue();
        summarizeResult.Should().BeTrue();
        revertResult!.Revert!.PartId.Should().Be("part_text");

        handler.RequestBodies.Should().BeEquivalentTo(
        [
            "{\"messageID\":\"msg_user\",\"modelID\":\"gpt-5.4\",\"providerID\":\"openai\"}",
            "{\"messageID\":\"msg_user\",\"partID\":\"part_text\"}",
            "{\"modelID\":\"gpt-5.4\",\"providerID\":\"openai\"}",
        ]);
    }

    [Fact]
    public async Task SessionWorkflowRoundTripsThroughStatefulPipeline()
    {
        var handler = new StatefulSessionWorkflowHandler();
        var client = CreateClient(handler);

        var createdSession = await client.Session.CreateAsync();
        var listedSessions = await client.Session.ListAsync();
        var initialized = await client.Session.InitAsync("session_123", new SessionInitRequest
        {
            MessageId = "msg_user_1",
            ModelId = "gpt-5.4",
            ProviderId = "openai",
        });
        var assistantMessage = await client.Session.ChatAsync("session_123", new SessionChatRequest
        {
            ModelId = "gpt-5.4",
            ProviderId = "openai",
            Parts =
            [
                new TextPartInput
                {
                    Text = "Hola",
                },
            ],
            Tools = new Dictionary<string, bool>
            {
                ["grep"] = true,
            },
        });
        var messages = await client.Session.MessagesAsync("session_123");
        var sharedSession = await client.Session.ShareAsync("session_123");
        var revertedSession = await client.Session.RevertAsync("session_123", new SessionRevertRequest
        {
            MessageId = "msg_user_1",
            PartId = "part_text_1",
        });
        var unrevertedSession = await client.Session.UnrevertAsync("session_123");
        var summarized = await client.Session.SummarizeAsync("session_123", new SessionSummarizeRequest
        {
            ModelId = "gpt-5.4",
            ProviderId = "openai",
        });
        var aborted = await client.Session.AbortAsync("session_123");
        var unsharedSession = await client.Session.UnshareAsync("session_123");
        var deleted = await client.Session.DeleteAsync("session_123");
        var remainingSessions = await client.Session.ListAsync();

        createdSession!.Id.Should().Be("session_123");
        listedSessions.Should().ContainSingle();
        initialized.Should().BeTrue();

        assistantMessage.Should().NotBeNull();
        assistantMessage!.Id.Should().Be("msg_assistant_1");
        assistantMessage.ModelId.Should().Be("gpt-5.4");
        assistantMessage.ProviderId.Should().Be("openai");
        assistantMessage.Finish.Should().Be("stop");

        messages.Should().HaveCount(2);
        messages![0].Info.Should().BeOfType<UserMessage>();
        messages[0].Parts.Should().ContainSingle().Which.Should().BeOfType<TextPart>();
        messages[1].Info.Should().BeOfType<AssistantMessage>();
        messages[1].Parts.Should().HaveCount(4);
        messages[1].Parts[0].Should().BeOfType<StepStartPart>();
        messages[1].Parts[1].Should().BeOfType<ReasoningPart>();
        messages[1].Parts[2].Should().BeOfType<TextPart>();
        messages[1].Parts[3].Should().BeOfType<StepFinishPart>();

        sharedSession!.Share!.Url.Should().Be("https://share/session_123");
        revertedSession!.Revert!.MessageId.Should().Be("msg_user_1");
        revertedSession.Revert.PartId.Should().Be("part_text_1");
        unrevertedSession!.Revert.Should().BeNull();
        summarized.Should().BeTrue();
        aborted.Should().BeTrue();
        unsharedSession!.Share.Should().BeNull();
        deleted.Should().BeTrue();
        remainingSessions.Should().BeEmpty();

        handler.Requests.Select(request => (request.Method.Method, request.RequestUri!.AbsolutePath)).Should().ContainInOrder(
        [
            ("POST", "/session"),
            ("GET", "/session"),
            ("POST", "/session/session_123/init"),
            ("POST", "/session/session_123/message"),
            ("GET", "/session/session_123/message"),
            ("POST", "/session/session_123/share"),
            ("POST", "/session/session_123/revert"),
            ("POST", "/session/session_123/unrevert"),
            ("POST", "/session/session_123/summarize"),
            ("POST", "/session/session_123/abort"),
            ("DELETE", "/session/session_123/share"),
            ("DELETE", "/session/session_123"),
            ("GET", "/session"),
        ]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SessionIdOperationsRejectInvalidSessionIds(string? sessionId)
    {
        var client = CreateClient(new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
        }));

        var action = () => client.Session.DeleteAsync(sessionId!);

        (await action.Should().ThrowAsync<ArgumentException>())
            .WithParameterName(nameof(sessionId))
            .WithMessage("The session ID must not be empty. (Parameter 'sessionId')");
    }

    [Fact]
    public async Task ChatAsyncRejectsInvalidPayload()
    {
        var client = CreateClient(new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
        }));

        var action = () => client.Session.ChatAsync("session_123", new SessionChatRequest
        {
            ModelId = "gpt-5.4",
            ProviderId = "openai",
            Parts = [],
        });

        (await action.Should().ThrowAsync<ArgumentException>())
            .WithParameterName("request")
            .WithMessage("The chat request must contain at least one part. (Parameter 'request')");
    }

    [Fact]
    public async Task SessionMethodsSurfaceTypedHttpFailures()
    {
        var handler = new CapturingHttpMessageHandler(static request => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("missing", Encoding.UTF8, MediaTypeHeaderValue.Parse("text/plain")),
            RequestMessage = request,
        });

        var client = CreateClient(handler);

        var action = () => client.Session.MessagesAsync("session_123");

        var exception = await Assert.ThrowsAsync<NotFoundException>(action);
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.ResponseBody.Should().Be("missing");
    }

    [Fact]
    public void SessionPolymorphicDeserializationRejectsUnknownDiscriminator()
    {
        var action = () => JsonSerializer.Deserialize<SessionMessage>(
            """{"id":"msg_1","role":"system","sessionID":"session_123"}""",
            SerializerOptions);

        action.Should().Throw<JsonException>();
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

    private static StringContent JsonContent(string json)
    {
        return new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));
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

    private sealed class StatefulSessionWorkflowHandler : HttpMessageHandler
    {
        private const string SessionId = "session_123";
        private const string ShareUrl = "https://share/session_123";
        private const string UserMessageId = "msg_user_1";
        private const string AssistantMessageId = "msg_assistant_1";
        private const string UserPartId = "part_text_1";
        private const string AssistantPartId = "part_text_2";

        private bool _deleted;
        private bool _shared;
        private bool _reverted;

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(Clone(request));

            var response = (request.Method.Method, request.RequestUri!.AbsolutePath) switch
            {
                ("POST", "/session") => JsonResponse(BuildSessionJson()),
                ("GET", "/session") => JsonResponse(_deleted ? "[]" : $"[{BuildSessionJson()}]"),
                ("POST", "/session/session_123/init") => JsonResponse("true"),
                ("POST", "/session/session_123/message") => await HandleChatAsync(request, cancellationToken),
                ("POST", "/session/session_123/prompt_async") => await HandlePromptAsync(request, cancellationToken),
                ("GET", "/session/session_123/message") => JsonResponse(BuildMessagesJson()),
                ("POST", "/session/session_123/share") => HandleShare(),
                ("POST", "/session/session_123/revert") => await HandleRevertAsync(request, cancellationToken),
                ("POST", "/session/session_123/unrevert") => HandleUnrevert(),
                ("POST", "/session/session_123/summarize") => JsonResponse("true"),
                ("POST", "/session/session_123/abort") => JsonResponse("true"),
                ("DELETE", "/session/session_123/share") => HandleUnshare(),
                ("DELETE", "/session/session_123") => HandleDelete(),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = JsonContent("\"unexpected\""),
                    RequestMessage = request,
                },
            };

            response.RequestMessage = request;
            return response;
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

        private static HttpResponseMessage JsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent(json),
            };
        }

        private static async Task<HttpResponseMessage> HandleChatAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            body.Should().Contain("\"modelID\":\"gpt-5.4\"");
            body.Should().Contain("\"providerID\":\"openai\"");
            body.Should().Contain("\"text\":\"Hola\"");

            return JsonResponse(
                "{" +
                "\"info\":{" +
                    "\"id\":\"msg_assistant_1\"," +
                    "\"parentID\":\"msg_user_1\"," +
                    "\"role\":\"assistant\"," +
                    "\"sessionID\":\"session_123\"," +
                    "\"agent\":\"build\"," +
                    "\"cost\":0.25," +
                    "\"mode\":\"chat\"," +
                    "\"modelID\":\"gpt-5.4\"," +
                    "\"path\":{\"cwd\":\"/cwd\",\"root\":\"/root\"}," +
                    "\"providerID\":\"openai\"," +
                    "\"time\":{\"created\":10,\"completed\":12}," +
                    "\"finish\":\"stop\"," +
                    "\"tokens\":{\"total\":10,\"cache\":{\"read\":1,\"write\":2},\"input\":3,\"output\":4,\"reasoning\":5}" +
                "}," +
                "\"parts\":[" +
                    "{\"id\":\"part_step_1\",\"messageID\":\"msg_assistant_1\",\"sessionID\":\"session_123\",\"type\":\"step-start\"}," +
                    "{\"id\":\"part_reasoning_1\",\"messageID\":\"msg_assistant_1\",\"sessionID\":\"session_123\",\"type\":\"reasoning\",\"text\":\"Thinking\",\"time\":{\"start\":11,\"end\":12},\"metadata\":{\"provider\":{\"value\":true}}}," +
                    "{\"id\":\"part_text_2\",\"messageID\":\"msg_assistant_1\",\"sessionID\":\"session_123\",\"type\":\"text\",\"text\":\"Hola desde assistant\"}," +
                    "{\"id\":\"part_finish_1\",\"messageID\":\"msg_assistant_1\",\"sessionID\":\"session_123\",\"type\":\"step-finish\",\"reason\":\"stop\",\"cost\":0.25,\"tokens\":{\"total\":10,\"cache\":{\"read\":1,\"write\":2},\"input\":3,\"output\":4,\"reasoning\":5}}" +
                "]" +
                "}");
        }

        private static async Task<HttpResponseMessage> HandlePromptAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            body.Should().Contain("\"modelID\":\"gpt-5.4\"");
            body.Should().Contain("\"providerID\":\"openai\"");
            body.Should().Contain("\"text\":\"Hola\"");

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        private HttpResponseMessage HandleShare()
        {
            _shared = true;
            return JsonResponse(BuildSessionJson());
        }

        private async Task<HttpResponseMessage> HandleRevertAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            body.Should().Be("{\"messageID\":\"msg_user_1\",\"partID\":\"part_text_1\"}");

            _reverted = true;
            return JsonResponse(BuildSessionJson());
        }

        private HttpResponseMessage HandleUnrevert()
        {
            _reverted = false;
            return JsonResponse(BuildSessionJson());
        }

        private HttpResponseMessage HandleUnshare()
        {
            _shared = false;
            return JsonResponse(BuildSessionJson());
        }

        private HttpResponseMessage HandleDelete()
        {
            _deleted = true;
            return JsonResponse("true");
        }

        private string BuildSessionJson()
        {
            var shareSegment = _shared ? $",\"share\":{{\"url\":\"{ShareUrl}\"}}" : string.Empty;
            var revertSegment = _reverted ? ",\"revert\":{\"messageID\":\"msg_user_1\",\"partID\":\"part_text_1\"}" : string.Empty;

            return "{" +
                "\"id\":\"session_123\"," +
                "\"time\":{\"created\":1,\"updated\":2}," +
                "\"title\":\"Demo\"," +
                "\"version\":\"1\"" +
                shareSegment +
                revertSegment +
                "}";
        }

        private static string BuildMessagesJson()
        {
            return "[" +
                "{" +
                "\"info\":{\"id\":\"msg_user_1\",\"role\":\"user\",\"sessionID\":\"session_123\",\"agent\":\"build\",\"time\":{\"created\":1}}," +
                "\"parts\":[{" +
                    "\"id\":\"part_text_1\"," +
                    "\"messageID\":\"msg_user_1\"," +
                    "\"sessionID\":\"session_123\"," +
                    "\"type\":\"text\"," +
                    "\"text\":\"Hola\"}]" +
                "}," +
                "{" +
                "\"info\":{\"id\":\"msg_assistant_1\",\"parentID\":\"msg_user_1\",\"role\":\"assistant\",\"sessionID\":\"session_123\",\"agent\":\"build\",\"cost\":0.25,\"mode\":\"chat\",\"modelID\":\"gpt-5.4\",\"path\":{\"cwd\":\"/cwd\",\"root\":\"/root\"},\"providerID\":\"openai\",\"time\":{\"created\":10,\"completed\":12},\"finish\":\"stop\",\"tokens\":{\"total\":10,\"cache\":{\"read\":1,\"write\":2},\"input\":3,\"output\":4,\"reasoning\":5}}," +
                "\"parts\":[" +
                    "{\"id\":\"part_step_1\",\"messageID\":\"msg_assistant_1\",\"sessionID\":\"session_123\",\"type\":\"step-start\"}," +
                    "{\"id\":\"part_reasoning_1\",\"messageID\":\"msg_assistant_1\",\"sessionID\":\"session_123\",\"type\":\"reasoning\",\"text\":\"Thinking\",\"time\":{\"start\":11,\"end\":12},\"metadata\":{\"provider\":{\"value\":true}}}," +
                    "{\"id\":\"part_text_2\",\"messageID\":\"msg_assistant_1\",\"sessionID\":\"session_123\",\"type\":\"text\",\"text\":\"Hola desde assistant\"}," +
                    "{\"id\":\"part_finish_1\",\"messageID\":\"msg_assistant_1\",\"sessionID\":\"session_123\",\"type\":\"step-finish\",\"reason\":\"stop\",\"cost\":0.25,\"tokens\":{\"total\":10,\"cache\":{\"read\":1,\"write\":2},\"input\":3,\"output\":4,\"reasoning\":5}}" +
                "]" +
                "}" +
                "]";
        }
    }
}
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Opencode.Client;
using Opencode.Internal.Streaming;
using Opencode.Models.Events;
using Opencode.Models.Session;

namespace Opencode.Sdk.Tests;

public sealed class EventClientTests
{
    [Fact]
    public async Task EventListAsyncStreamsTypedEventsAndBuildsExpectedRoute()
    {
        var payload = string.Join(
            "",
            "event: message.part.delta\n",
            "data: {\"type\":\"message.part.delta\",\"properties\":{\"sessionID\":\"session_123\",\"messageID\":\"msg_delta\",\"partID\":\"part_456\",\"field\":\"text\",\"delta\":\"hello\"}}\n",
            "\n",
            "event: message.updated\n",
            "data: {\"type\":\"message.updated\",\"properties\":{\"info\":{\"id\":\"msg_1\",\"role\":\"user\",\"sessionID\":\"session_123\",\"time\":{\"created\":1}}}}\n",
            "\n",
            "event: session.error\n",
            "data: {\"type\":\"session.error\",\"properties\":{\"sessionID\":\"session_123\",\"error\":{\"name\":\"UnknownError\",\"data\":{\"message\":\"boom\"}}}}\n",
            "\n");

        var handler = new CapturingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateContent(payload),
        });

        var client = CreateClient(handler);
        var items = await ReadAllAsync(client.Event.ListAsync());

        items.Should().HaveCount(3);
        items[0].Should().BeOfType<MessagePartDeltaEvent>();
        items[1].Should().BeOfType<MessageUpdatedEvent>();
        items[2].Should().BeOfType<SessionErrorEvent>();
        ((MessagePartDeltaEvent)items[0]).Properties.SessionId.Should().Be("session_123");
        ((MessagePartDeltaEvent)items[0]).Properties.MessageId.Should().Be("msg_delta");
        ((MessagePartDeltaEvent)items[0]).Properties.PartId.Should().Be("part_456");
        ((MessagePartDeltaEvent)items[0]).Properties.Field.Should().Be("text");
        ((MessagePartDeltaEvent)items[0]).Properties.Delta.Should().Be("hello");
        ((MessageUpdatedEvent)items[1]).Properties.Info.Should().BeOfType<UserMessage>();
        ((SessionErrorEvent)items[2]).Properties.Error.Should().BeOfType<UnknownError>();

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Method.Should().Be(HttpMethod.Get);
        handler.Requests[0].RequestUri.Should().Be(new Uri("http://localhost:54321/event", UriKind.Absolute));
        handler.Requests[0].Headers.Accept.Should().ContainSingle(header => header.MediaType == "text/event-stream");
    }

    [Fact]
    public async Task EventListAsyncDisposesUnderlyingResponseWhenConsumerStopsEarly()
    {
        var stream = new ChunkSequenceStream(
        [
            Encoding.UTF8.GetBytes("data: {\"type\":\"session.idle\",\"properties\":{\"sessionID\":\"session_123\"}}\n\n"),
            Encoding.UTF8.GetBytes("data: {\"type\":\"session.idle\",\"properties\":{\"sessionID\":\"session_456\"}}\n\n"),
        ]);

        var handler = new CapturingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamingHttpContent(stream),
        });

        var client = CreateClient(handler);

        await foreach (var item in client.Event.ListAsync())
        {
            item.Should().BeOfType<SessionIdleEvent>();
            break;
        }

        stream.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task EventListAsyncCancelsActiveEnumerationAndDisposesUnderlyingResponse()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var stream = new CancelAwareStream(
            Encoding.UTF8.GetBytes("data: {\"type\":\"session.idle\",\"properties\":{\"sessionID\":\"session_123\"}}\n\n"));

        var handler = new CapturingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamingHttpContent(stream),
        });

        var client = CreateClient(handler);

        await using var enumerator = client.Event.ListAsync(cancellationTokenSource.Token).GetAsyncEnumerator();

        (await enumerator.MoveNextAsync()).Should().BeTrue();
        enumerator.Current.Should().BeOfType<SessionIdleEvent>();

        var pendingMoveNext = enumerator.MoveNextAsync().AsTask();
        await stream.WaitForBlockedReadAsync();

        await cancellationTokenSource.CancelAsync();

        Func<Task> action = async () => await pendingMoveNext;

        await action.Should().ThrowAsync<OperationCanceledException>();
        stream.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task EventListAsyncSurfacesMalformedJsonDuringEnumeration()
    {
        var handler = new CapturingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateContent("data: not-json\n\n"),
        });

        var client = CreateClient(handler);

        var action = async () => await ReadAllAsync(client.Event.ListAsync());

        await action.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task EventListAsyncSkipsUnknownEventsAndHandlesServerControlEvents()
    {
        var payload = string.Join(
            "",
            "event: connected\n",
            "data: {\"type\":\"server.connected\",\"properties\":{}}\n",
            "\n",
            "event: heartbeat\n",
            "data: {\"type\":\"server.heartbeat\",\"properties\":{}}\n",
            "\n",
            "event: session-status\n",
            "data: {\"type\":\"session.status\",\"properties\":{\"sessionID\":\"session_123\",\"status\":{\"type\":\"busy\"}}}\n",
            "\n",
            "event: future\n",
            "data: {\"type\":\"future.event\",\"properties\":{\"value\":1}}\n",
            "\n",
            "event: idle\n",
            "data: {\"type\":\"session.idle\",\"properties\":{\"sessionID\":\"session_123\"}}\n",
            "\n");

        var handler = new CapturingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateContent(payload),
        });

        var client = CreateClient(handler);
        var items = await ReadAllAsync(client.Event.ListAsync());

        items.Should().HaveCount(4);
        items[0].Should().BeOfType<ServerConnectedEvent>();
        items[1].Should().BeOfType<ServerHeartbeatEvent>();
        items[2].Should().BeOfType<SessionStatusEvent>();
        ((SessionStatusEvent)items[2]).Properties.SessionId.Should().Be("session_123");
        ((SessionStatusEvent)items[2]).Properties.Status.Type.Should().Be("busy");
        items[3].Should().BeOfType<SessionIdleEvent>();
    }

    [Fact]
    public async Task SseMessageParserCombinesMultilineDataAndIgnoresComments()
    {
        var stream = new ChunkSequenceStream(
        [
            Encoding.UTF8.GetBytes(": keep-alive\n"),
            Encoding.UTF8.GetBytes("event: ping\n"),
            Encoding.UTF8.GetBytes("data: {\n"),
            Encoding.UTF8.GetBytes("data: \"foo\":\n"),
            Encoding.UTF8.GetBytes("data: \n"),
            Encoding.UTF8.GetBytes("data:\n"),
            Encoding.UTF8.GetBytes("data: true}\r\n\r\n"),
        ]);

        var items = await ReadAllAsync(SseMessageParser.ParseAsync(stream));

        items.Should().ContainSingle();
        items[0].EventName.Should().Be("ping");
        items[0].Data.Should().Be("{\n\"foo\":\n\n\ntrue}");
        items[0].RawLines.Should().ContainInOrder("event: ping", "data: {", "data: \"foo\":", "data: ", "data:", "data: true}");
    }

    [Fact]
    public async Task SseMessageParserSupportsMultiByteCharactersAcrossChunks()
    {
        var stream = new ChunkSequenceStream(
        [
            Encoding.UTF8.GetBytes("event: completion\n"),
            Encoding.UTF8.GetBytes("data: {\"content\": \""),
            [0xD0],
            [0xB8, 0xD0, 0xB7, 0xD0],
            [0xB2, 0xD0, 0xB5, 0xD1, 0x81, 0xD1, 0x82, 0xD0, 0xBD, 0xD0, 0xB8],
            Encoding.UTF8.GetBytes("\"}\n\n"),
        ]);

        var items = await ReadAllAsync(SseMessageParser.ParseAsync(stream));

        items.Should().ContainSingle();
        items[0].EventName.Should().Be("completion");
        JsonDocument.Parse(items[0].Data).RootElement.GetProperty("content").GetString().Should().Be("известни");
    }

    private static StreamingHttpContent CreateContent(string payload)
    {
        return new StreamingHttpContent(new ChunkSequenceStream([Encoding.UTF8.GetBytes(payload)]));
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

    private static async Task<List<T>> ReadAllAsync<T>(IAsyncEnumerable<T> source)
    {
        var items = new List<T>();

        await foreach (var item in source)
        {
            items.Add(item);
        }

        return items;
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
            var capturedRequest = new HttpRequestMessage(request.Method, request.RequestUri);

            foreach (var header in request.Headers)
            {
                capturedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            Requests.Add(capturedRequest);
            return Task.FromResult(_responseFactory(request));
        }
    }

    private sealed class StreamingHttpContent : HttpContent
    {
        private readonly Stream _stream;

        public StreamingHttpContent(Stream stream)
        {
            _stream = stream;
            Headers.ContentType = MediaTypeHeaderValue.Parse("text/event-stream");
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult(_stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ChunkSequenceStream : Stream
    {
        private readonly Queue<byte[]> _chunks;
        private int _offset;

        public ChunkSequenceStream(IEnumerable<byte[]> chunks)
        {
            _chunks = new Queue<byte[]>(chunks);
        }

        public bool IsDisposed { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer.AsMemory(offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_chunks.Count == 0)
            {
                return ValueTask.FromResult(0);
            }

            var current = _chunks.Peek();
            var available = current.Length - _offset;
            var toCopy = Math.Min(available, buffer.Length);
            current.AsMemory(_offset, toCopy).CopyTo(buffer);
            _offset += toCopy;

            if (_offset >= current.Length)
            {
                _chunks.Dequeue();
                _offset = 0;
            }

            return ValueTask.FromResult(toCopy);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class CancelAwareStream : Stream
    {
        private readonly byte[] _firstChunk;
        private readonly TaskCompletionSource _blockedReadReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _firstChunkServed;

        public CancelAwareStream(byte[] firstChunk)
        {
            _firstChunk = firstChunk;
        }

        public bool IsDisposed { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public Task WaitForBlockedReadAsync()
        {
            return _blockedReadReached.Task;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer.AsMemory(offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_firstChunkServed)
            {
                _firstChunkServed = true;
                _firstChunk.AsMemory().CopyTo(buffer);
                return _firstChunk.Length;
            }

            _blockedReadReached.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
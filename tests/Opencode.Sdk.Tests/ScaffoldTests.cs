using FluentAssertions;
using Opencode;
using Opencode.Client;
using Opencode.Errors;
using Opencode.Resources.App;
using Opencode.Resources.Config;
using Opencode.Resources.Events;
using Opencode.Resources.File;
using Opencode.Resources.Find;
using Opencode.Resources.Session;
using Opencode.Resources.Tui;

namespace Opencode.Sdk.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    public void LibraryMarkerUsesExpectedRootNamespace()
    {
        var marker = new SdkAssemblyMarker();

        marker.Should().NotBeNull();
        marker.GetType().Namespace.Should().Be("Opencode");
    }

    [Fact]
    public void OpencodeClientInitializesExpectedPublicResources()
    {
        var options = new OpencodeClientOptions();

        var client = new OpencodeClient(options);

        client.Options.Should().BeSameAs(options);
        client.Options.BaseUrl.Should().Be(new Uri("http://localhost:54321", UriKind.Absolute));
        client.Options.Timeout.Should().Be(TimeSpan.FromMinutes(1));
        client.Options.MaxRetries.Should().Be(2);
        client.App.Should().BeOfType<AppClient>();
        client.Config.Should().BeOfType<ConfigClient>();
        client.Session.Should().BeOfType<SessionClient>();
        client.Event.Should().BeOfType<EventClient>();
        client.File.Should().BeOfType<FileClient>();
        client.Find.Should().BeOfType<FindClient>();
        client.Tui.Should().BeOfType<TuiClient>();
    }

    [Fact]
    public void OpencodeExceptionUsesStandardExceptionShape()
    {
        var innerException = new InvalidOperationException("inner");

        var exception = new OpencodeException("outer", innerException);

        exception.Should().BeAssignableTo<Exception>();
        exception.Message.Should().Be("outer");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void RequestTimeoutExceptionExtendsConnectionException()
    {
        var exception = new RequestTimeoutException(TimeSpan.FromSeconds(2));

        exception.Should().BeAssignableTo<ConnectionException>();
        exception.Timeout.Should().Be(TimeSpan.FromSeconds(2));
    }
}
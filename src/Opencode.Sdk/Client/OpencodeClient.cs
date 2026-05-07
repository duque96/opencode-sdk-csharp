using Opencode.Resources.App;
using Opencode.Resources.Config;
using Opencode.Resources.Events;
using Opencode.Resources.File;
using Opencode.Resources.Find;
using Opencode.Resources.Session;
using Opencode.Resources.Tui;
using Opencode.Internal.Http;

namespace Opencode.Client;

/// <summary>
/// Provides the main entry point for interacting with the Opencode API.
/// </summary>
public sealed class OpencodeClient
{
    /// <summary>
    /// Initializes a new client instance with the provided options.
    /// </summary>
    /// <param name="options">The client configuration to apply.</param>
    public OpencodeClient(OpencodeClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options;
        Pipeline = new HttpPipeline(HttpPipelineOptions.Create(options));
        App = new AppClient(this);
        Config = new ConfigClient(this);
        Session = new SessionClient(this);
        Event = new EventClient(this);
        File = new FileClient(this);
        Find = new FindClient(this);
        Tui = new TuiClient(this);
    }

    /// <summary>
    /// Gets the options used by this client instance.
    /// </summary>
    public OpencodeClientOptions Options { get; }

    internal HttpPipeline Pipeline { get; }

    /// <summary>
    /// Gets the application resource client.
    /// </summary>
    public AppClient App { get; }

    /// <summary>
    /// Gets the configuration resource client.
    /// </summary>
    public ConfigClient Config { get; }

    /// <summary>
    /// Gets the session resource client.
    /// </summary>
    public SessionClient Session { get; }

    /// <summary>
    /// Gets the event resource client.
    /// </summary>
    public EventClient Event { get; }

    /// <summary>
    /// Gets the file resource client.
    /// </summary>
    public FileClient File { get; }

    /// <summary>
    /// Gets the search resource client.
    /// </summary>
    public FindClient Find { get; }

    /// <summary>
    /// Gets the terminal UI resource client.
    /// </summary>
    public TuiClient Tui { get; }
}
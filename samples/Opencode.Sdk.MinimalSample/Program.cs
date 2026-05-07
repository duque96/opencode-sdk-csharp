using System.Globalization;
using Opencode.Client;
using Opencode.Errors;

var baseUrl = ResolveBaseUrl(args);

var client = new OpencodeClient(new OpencodeClientOptions
{
    BaseUrl = baseUrl,
    Timeout = TimeSpan.FromSeconds(15),
    MaxRetries = 0,
});

Console.WriteLine($"Connecting to {baseUrl}...");

try
{
    var app = await client.App.GetAsync()
        ?? throw new UnexpectedResponseException("The server returned an empty app payload.");

    Console.WriteLine("Connection succeeded.");
    Console.WriteLine($"Hostname: {app.Hostname}");
    Console.WriteLine($"Git workspace: {app.Git}");
    Console.WriteLine($"Root path: {app.Path.Root}");
    Console.WriteLine($"Config path: {app.Path.Config}");
    Console.WriteLine($"Initialized at: {app.Time.Initialized?.ToString(CultureInfo.InvariantCulture) ?? "<not set>"}");

    return 0;
}
catch (RequestTimeoutException exception)
{
    Console.Error.WriteLine($"Request timed out after {exception.Timeout}.");
    return 2;
}
catch (ApiException exception)
{
    Console.Error.WriteLine($"Server responded with HTTP {(int)exception.StatusCode}: {exception.Message}");

    if (!string.IsNullOrWhiteSpace(exception.ResponseBody))
    {
        Console.Error.WriteLine(exception.ResponseBody);
    }

    return 3;
}
catch (ConnectionException exception)
{
    Console.Error.WriteLine($"Could not reach the server: {exception.Message}");
    return 4;
}
catch (UnexpectedResponseException exception)
{
    Console.Error.WriteLine($"The endpoint did not return the JSON API payload expected by the SDK: {exception.Message}");
    Console.Error.WriteLine("Check that the base URL points to a real Opencode server endpoint rather than a browser-facing HTML page.");
    return 5;
}

static Uri ResolveBaseUrl(string[] args)
{
    var candidate = args.FirstOrDefault();

    if (string.IsNullOrWhiteSpace(candidate))
    {
        candidate = Environment.GetEnvironmentVariable("OPENCODE_BASE_URL") ?? "http://localhost:54321";
    }

    if (!Uri.TryCreate(candidate, UriKind.Absolute, out var baseUrl))
    {
        throw new InvalidOperationException(
            "Provide a valid absolute base URL as the first argument or through OPENCODE_BASE_URL.");
    }

    return baseUrl;
}
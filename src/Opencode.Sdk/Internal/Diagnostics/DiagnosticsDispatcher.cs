using Opencode.Diagnostics;

namespace Opencode.Internal.Diagnostics;

internal sealed class DiagnosticsDispatcher
{
    private readonly Action<OpencodeDiagnosticEvent>? _handler;

    public DiagnosticsDispatcher(Action<OpencodeDiagnosticEvent>? handler)
    {
        _handler = handler;
    }

    public void Emit(OpencodeDiagnosticEvent diagnosticEvent)
    {
        if (_handler is null)
        {
            return;
        }

        try
        {
            _handler(diagnosticEvent);
        }
        catch
        {
        }
    }
}
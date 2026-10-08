using System;

namespace PhoenixTools.Watchers;

/// <summary>
/// Event arguments that contain an exception raised by the library during operation.
/// </summary>
public class ErrorEventArgs (Exception ex): EventArgs
{
    /// <summary>
    /// The exception that occurred.
    /// </summary>
    public Exception Exception { get; } = ex;
}

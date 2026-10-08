using System;

namespace PhoenixTools.Watchers;

/// <summary>
/// Event arguments for registry change notifications raised by <see cref="RegistryWatcher"/>.
/// </summary>
public class WatcherEventArgs : EventArgs
{
    /// <summary>
    /// The full registry subkey path that changed (for example, <c>"HKEY_LOCAL_MACHINE\\SOFTWARE\\MyCompany\\MyApp"</c>).
    /// </summary>
    public required string SubKey { get; init; }

    /// <summary>
    /// Indicates whether the watcher was configured to monitor the entire subtree under <see cref="SubKey"/>.
    /// </summary>
    public required bool IsTreeWatching { get; init; }
}

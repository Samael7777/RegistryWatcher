using System;

namespace PhoenixTools.Watchers;

/// <summary>
/// Event arguments used when a watched registry subkey is deleted.
/// </summary>
public class RegSubkeyDeletedEventArgs (string path) : EventArgs
{
    /// <summary>
    /// The full registry path of the deleted subkey.
    /// </summary>
    public string RegKeyPath { get; } = path;
}

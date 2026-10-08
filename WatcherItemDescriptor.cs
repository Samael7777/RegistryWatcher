using Microsoft.Win32;

namespace PhoenixTools.Watchers;

/// <summary>
/// Represents a registry location to watch for changes.
/// </summary>
/// <remarks>
/// Instances of this class describe which registry hive and subkey should be monitored
/// and whether the watcher should include changes in the subkey subtree.
/// </remarks>
public class WatcherItemDescriptor
{
    /// <summary>
    /// The root registry hive to watch (for example, <see cref="RegistryHive.LocalMachine"/>).
    /// </summary>
    public required RegistryHive Hive { get; init; }

    /// <summary>
    /// The registry subkey path under the specified <see cref="Hive"/> to monitor.
    /// This should be a valid registry path string (for example, <c>"SOFTWARE\MyCompany\MyApp"</c>).
    /// </summary>
    public required string SubKey { get; init; }

    /// <summary>
    /// If true, changes to the entire subtree beneath <see cref="SubKey"/> are watched.
    /// If false, only changes directly to the specified subkey are watched.
    /// Default is <c>true</c>.
    /// </summary>
    public bool WatchSubtree { get; init; } = true;
}

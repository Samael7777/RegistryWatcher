using Microsoft.Win32;

namespace PhoenixTools.Watchers.Snapshots;

public class RegistrySnapshot
{
    public RegistrySnapshotNode? Root { get; private set; } = null;

    /*public static RegistrySnapshot CreateWithSubtree(RegistryKey key)
    {

    }

    private RegistrySnapshotNode CreateNode(RegistryKey key)
    {
        
    }*/
}
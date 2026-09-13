using System.Collections.Generic;

namespace PhoenixTools.Watchers.Snapshots;

public record RegistrySnapshotNode
(
    string Key,
    IReadOnlyList<RegistryValue>? Values,
    IReadOnlyList<RegistrySnapshotNode>? ChildNodes
);
using System.Diagnostics;

namespace PhoenixTools.Watchers.Snapshots;

[DebuggerDisplay("{Name}")]
public abstract record RegistryValue(string Name);

public record RegistryValueString(string Name, string Value)
    : RegistryValue(Name);


public record RegistryValueBinary(string Name, byte[] Value)
    : RegistryValue(Name);


public record RegistryValueDword(string Name, uint Value)
    : RegistryValue(Name);


public record RegistryValueQword(string Name, ulong Value)
    : RegistryValue(Name);


public record RegistryValueExpandString(string Name, string Value)
    : RegistryValue(Name);

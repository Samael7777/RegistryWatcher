// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

using System;

namespace PhoenixTools.Watchers;

/// <summary>
/// Legacy enum that mirrors the Windows predefined registry root keys.
/// This type is obsolete; prefer <see cref="Microsoft.Win32.RegistryHive"/> instead.
/// </summary>
[Obsolete("Use Microsoft.Win32.RegistryHive instead.")]
public enum RegistryRootKey : uint
{
    HKEY_CLASSES_ROOT = 0x80000000,
    HKEY_CURRENT_USER = 0x80000001,
    HKEY_LOCAL_MACHINE = 0x80000002,
    HKEY_USERS = 0x80000003,
    HKEY_PERFORMANCE_DATA = 0x80000004,
    HKEY_PERFORMANCE_TEXT = 0x80000050,
    HKEY_PERFORMANCE_NLSTEXT = 0x80000060,
    HKEY_CURRENT_CONFIG = 0x80000005,
    HKEY_DYN_DATA = 0x80000006,
    HKEY_CURRENT_USER_LOCAL_SETTINGS = 0x80000007
}

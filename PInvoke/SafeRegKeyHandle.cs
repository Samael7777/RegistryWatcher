using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PhoenixTools.Watchers.PInvoke;

[DebuggerDisplay("{handle}")]
internal class SafeRegKeyHandle : SafeHandle
{
    public SafeRegKeyHandle() : base(IntPtr.Zero, true)
    { }
    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        return IsInvalid || WinApi.RegCloseKey(handle) == 0;
    }
}
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PhoenixTools.Watchers.PInvoke
{
    [DebuggerDisplay("{handle}")]
    internal class SafeEventHandle : SafeHandle
    {
        public SafeEventHandle() : base(IntPtr.Zero, true)
        { }

        protected override bool ReleaseHandle()
        {
            return IsInvalid || WinApi.CloseHandle(handle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
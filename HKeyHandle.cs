using System;
using System.Runtime.InteropServices;
using Windows.Win32.System.Registry;

namespace PhoenixTools.Watchers
{
    internal class HKeyHandle : SafeHandle
    {
        public HKeyHandle(HKEY hKey) : base(IntPtr.Zero,  true)
        {
            handle = hKey;
        }

        protected override bool ReleaseHandle()
        {
            return false;
        }

        public override bool IsInvalid => false;
    }
}
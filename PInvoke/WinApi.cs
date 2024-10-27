// ReSharper disable StringLiteralTypo

using System;
using System.Runtime.InteropServices;

namespace PhoenixTools.Watchers.PInvoke
{
    internal static class WinApi
    {
        private const uint ErrorSuccess = 0x80070000;

        [DllImport("advapi32.dll")]
        public static extern int RegNotifyChangeKeyValue(SafeRegKeyHandle hKey,
            [MarshalAs(UnmanagedType.Bool)] bool bWatchSubtree,
            int dwNotifyFilter, SafeEventHandle hEvent, [MarshalAs(UnmanagedType.Bool)] bool fAsynchronous);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int RegOpenKeyEx(IntPtr hKey, [In] [MarshalAs(UnmanagedType.LPWStr)] string lpSubKey,
            int ulOptions, int samDesired, out SafeRegKeyHandle phkResult);

        [DllImport("advapi32.dll")]
        public static extern int RegCloseKey(IntPtr hKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeEventHandle CreateEvent(IntPtr lpEventAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bManualReset,
            [MarshalAs(UnmanagedType.Bool)] bool bInitialState, [In] [MarshalAs(UnmanagedType.LPWStr)] string lpName);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetEvent(SafeEventHandle hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ResetEvent(SafeEventHandle hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern WaitState WaitForMultipleObjects(int nCount,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] lpHandles,
            [MarshalAs(UnmanagedType.Bool)] bool bWaitAll, uint dwMilliseconds);

    
        public static void ThrowExceptionOnError(int hResult)
        {
            var exception = Marshal.GetExceptionForHR(hResult);
            if (exception != null && (uint)exception.HResult != ErrorSuccess) throw exception;
        }

        public static void ThrowExceptionOnLastWin32Error()
        {
            var error = Marshal.GetHRForLastWin32Error();
            ThrowExceptionOnError(error);
        }
    }
}
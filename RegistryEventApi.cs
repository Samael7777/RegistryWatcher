using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace PhoenixTools.Watchers
{
    internal static class RegistryEventApi
    {
        public static SafeHandle OpenRegistryKey(HKEY root, string subKey)
        {
            var error = WinApi.RegOpenKeyEx(new HKeyHandle(root), subKey, 0, REG_SAM_FLAGS.KEY_NOTIFY,
                out var handle);
            
            return error != WIN32_ERROR.NO_ERROR 
                ? throw new Win32Exception((int)error) 
                : handle;
        }

        public static SafeHandle CreateEventHandle(bool initialState, bool manualReset)
        {
            var handle = WinApi.CreateEvent(null, manualReset, initialState, string.Empty);

            return handle.IsInvalid ? 
                throw new Win32Exception() 
                : handle;
        }

        public static void TriggerUpRegistryEvent(SafeHandle registryKeyHandle, SafeHandle eventHandle, bool watchSubtree)
        {
            var error = WinApi.RegNotifyChangeKeyValue(
                registryKeyHandle,
                watchSubtree,
                REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_NAME | REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET,
                eventHandle,
                true
            );
            if (error != WIN32_ERROR.NO_ERROR)
                throw new Win32Exception((int)error);
        }

        public static WAIT_EVENT WaitForMultiplyEvents(SafeHandle[] eventHandles, bool waitForAll, uint timeoutMilliseconds)
        {
            var safeHandles = eventHandles.Where(sh =>
                {
                    if (sh.IsInvalid)
                        return false;

                    var isSuccess = false;
                    sh.DangerousAddRef(ref isSuccess);

                    return isSuccess;

                }).ToArray();
            
            var handles = safeHandles.Select(sh => new HANDLE(sh.DangerousGetHandle()))
                .ToArray();

            try
            {
                unsafe
                {
                    fixed (HANDLE* lpHandles = handles)
                    {
                        var result = WinApi.WaitForMultipleObjects((uint)handles.Length, lpHandles, waitForAll,
                            timeoutMilliseconds);

                        return result == WAIT_EVENT.WAIT_FAILED
                            ? throw new Win32Exception()
                            : result;
                    }
                }
            }
            finally
            {
                foreach (var safeHandle in safeHandles)
                {
                    safeHandle.DangerousRelease();
                }
            }
        }

        public static void SetEvent(SafeHandle eventHandle)
        {
            if (!WinApi.SetEvent(eventHandle)) 
                throw new Win32Exception();
        }

        public static void ResetEvent(SafeHandle eventHandle)
        {
            if (!WinApi.ResetEvent(eventHandle)) 
                throw new Win32Exception();
        }
    }
}
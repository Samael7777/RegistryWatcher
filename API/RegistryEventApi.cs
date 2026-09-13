using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace PhoenixTools.Watchers.API
{
    internal static class RegistryEventApi
    {
        private const REG_NOTIFY_FILTER NotifyFilter = REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_NAME
                                                       | REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET;

        public static SafeHandle CreateEventHandle(bool initialState, bool manualReset)
        {
            var handle = WinApi.CreateEvent(null, manualReset, initialState);

            return handle.IsInvalid ? 
                throw new Win32Exception() 
                : handle;
        }

        public static void TriggerUpRegistryEvent(SafeHandle registryKeyHandle, SafeHandle eventHandle, bool watchSubtree)
        {
            var error = WinApi.RegNotifyChangeKeyValue(
                registryKeyHandle,
                watchSubtree,
                NotifyFilter,
                eventHandle,
                true
            );
            if (error != WIN32_ERROR.NO_ERROR)
                throw new Win32Exception((int)error);
        }

        public static WAIT_EVENT WaitForMultipleEvents(SafeHandle[] eventHandles, bool waitForAll, uint timeoutMilliseconds)
        {
            ThrowIfInvalidHandle(eventHandles);
            
            var handles = new HANDLE[eventHandles.Length];
            var isSuccessAddRef = new bool[eventHandles.Length];
            
            try
            {
                for (var i = 0; i < eventHandles.Length; i++)
                {
                    eventHandles[i].DangerousAddRef(ref isSuccessAddRef[i]);
                    if (!isSuccessAddRef[i])
                        throw new ApplicationException("Add handle ref error.");
                    
                    handles[i] = (HANDLE)eventHandles[i].DangerousGetHandle();
                }

                var result = WinApi.WaitForMultipleObjects(
                    handles, 
                    waitForAll, 
                    timeoutMilliseconds
                );

                return result == WAIT_EVENT.WAIT_FAILED
                    ? throw new Win32Exception()
                    : result;
            }
            finally
            {
                for (var i = 0; i < isSuccessAddRef.Length; i++)
                {
                    if (isSuccessAddRef[i])
                        eventHandles[i].DangerousRelease();
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

        private static void ThrowIfInvalidHandle(ReadOnlySpan<SafeHandle> handles)
        {
            foreach (var handle in handles)
            {
                if (handle.IsInvalid)
                    throw new ApplicationException("Invalid event handle.");
            }
        }
    }
}
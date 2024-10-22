using System.Runtime.InteropServices;

namespace PhoenixTools.Watchers.PInvoke;

internal static class RegistryEventApi
{
    public static SafeRegKeyHandle OpenRegistryKey(RegistryRootKey root, string subKey)
    {
        var error = WinApi.RegOpenKeyEx((IntPtr)RegistryRootKey.HKEY_CURRENT_USER, subKey, 0, 0x0010,
            out var handle);
        WinApi.ThrowExceptionOnError(error);
        return handle;
    }

    public static SafeEventHandle CreateEventHandle(bool initialState, bool manualReset)
    {
        var handle = WinApi.CreateEvent(IntPtr.Zero, manualReset, initialState, "");
        var error = Marshal.GetHRForLastWin32Error();
        WinApi.ThrowExceptionOnError(error);
        return handle;
    }

    public static void TriggerUpRegistryEvent(SafeRegKeyHandle registryKeyHandle, SafeEventHandle eventHandle, bool watchSubtree)
    {
        var error = WinApi.RegNotifyChangeKeyValue(registryKeyHandle, watchSubtree, 
            (int)(RegNotify.ChangeName | RegNotify.ChangeLastSet),
            eventHandle, true);
        WinApi.ThrowExceptionOnError(error);
    }

    public static WaitState WaitForMultiplyEvents(IntPtr[] eventHandles, bool waitForAll, uint timeoutMilliseconds)
    {
        var result = WinApi.WaitForMultipleObjects(eventHandles.Length, eventHandles, waitForAll, timeoutMilliseconds);
        if (result == WaitState.WaitFailed) WinApi.ThrowExceptionOnLastWin32Error();
        return result;
    }
}
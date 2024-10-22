namespace PhoenixTools.Watchers.PInvoke
{
    internal static class SafeEventHandleExtension
    {
        public static void SetEvent(this SafeEventHandle eventHandle)
        {
            if (!WinApi.SetEvent(eventHandle)) WinApi.ThrowExceptionOnLastWin32Error();
        }

        public static void ResetEvent(this SafeEventHandle eventHandle)
        {
            if (!WinApi.ResetEvent(eventHandle)) WinApi.ThrowExceptionOnLastWin32Error();
        }
    }
}
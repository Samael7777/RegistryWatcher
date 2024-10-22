namespace PhoenixTools.Watchers.PInvoke
{
    internal enum WaitState : uint
    {
        WaitAbandoned = 0x00000080,
        WaitAbandoned0 = WaitAbandoned,
        WaitObject = 0,
        WaitObject0 = WaitObject,
        WaitTimeout = 0x00000102,
        WaitFailed = 0xFFFFFFFF
    }
}
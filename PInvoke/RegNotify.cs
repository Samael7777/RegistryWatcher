namespace Watchers.PInvoke;

[Flags]
internal enum RegNotify
{
    ChangeAttributes = 2,
    ChangeSecurity = 8,
    ChangeLastSet = 4,
    ChangeName = 1,
    ThreadAgnostic = 0x10000000,
}
using Watchers.PInvoke;

namespace Watchers;

public class RegistryWatcher : IDisposable
{
    private const uint TimeoutInfinite = 0xFFFFFFFF;

    private readonly SafeRegKeyHandle _regHandle;
    private readonly bool _watchSubtree;
    private readonly SafeEventHandle _cancellationEvent;

    private Task? _waitEventTask;

    public event EventHandler? RegistryChanged; 

    public RegistryWatcher(RegistryRootKey root, string subKey, bool watchSubtree)
    {
        _watchSubtree = watchSubtree;
        _regHandle = RegistryEventApi.OpenRegistryKey(root, subKey);
        _cancellationEvent = RegistryEventApi.CreateEventHandle(false, true);
        _waitEventTask = null;
    }

    public bool IsWatching => _waitEventTask == null;

    public void Start()
    {
        if (_waitEventTask != null) return;

        _cancellationEvent.ResetEvent();
        _waitEventTask = Task.Run(WaitProc);
    }

    public void Stop()
    {
        if (_waitEventTask == null) return;

        _cancellationEvent.SetEvent();
        _waitEventTask?.Wait();
        _waitEventTask = null;
    }

    private void WaitProc()
    {
        using var regChangedEvent = RegistryEventApi.CreateEventHandle(false, false);
        var events = new[] { regChangedEvent.DangerousGetHandle(), _cancellationEvent.DangerousGetHandle() };
        var isCanceled = false;

        while (!isCanceled)
        {
            RegistryEventApi.TriggerUpRegistryEvent(_regHandle, regChangedEvent, _watchSubtree);
            var triggered = RegistryEventApi.WaitForMultiplyEvents
                (events, false, TimeoutInfinite);

            switch (triggered)
            {
                case WaitState.WaitObject0:
                    RegistryChanged?.Invoke(this, EventArgs.Empty);
                    break;
                case WaitState.WaitObject0 + 1:
                    isCanceled = true;
                    break;
            }
        }
    }


    #region Dispose

    private bool _disposed;

    ~RegistryWatcher()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            //dispose managed state (managed objects)
            Stop();
            _regHandle.Dispose();
            _cancellationEvent.Dispose();
            RegistryChanged = null;
        }
        //free unmanaged resources (unmanaged objects) and override finalizer
        //set large fields to null

        _disposed = true;
    }

    #endregion
}
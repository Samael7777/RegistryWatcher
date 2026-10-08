using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.Win32;
using PhoenixTools.Watchers.API;

namespace PhoenixTools.Watchers;

/// <summary>
/// Watches one or more registry locations and raises events when changes occur.
/// </summary>
/// <remarks>
/// Use <see cref="Start"/> to begin monitoring and <see cref="Stop"/> or <see cref="Dispose"/> to stop.
/// The watcher raises <see cref="RegistryChanged"/> when a watched key (or its subtree) changes,
/// <see cref="RegistryKeyDeleted"/> when a watched subkey is deleted, and <see cref="OnError"/> for internal errors.
/// The type is thread-safe for Start/Stop/Dispose calls but disposing or stopping from inside event handlers is not allowed.
/// </remarks>
public class RegistryWatcher : IDisposable
{
    private const uint TimeoutInfinite = 0xFFFFFFFF;

    private record WatcherItem(RegistryKey Subkey, bool WatchSubtree)
    {
        public bool IsDeleted { get; set; }
    }

    private readonly object _sync = new();
    private readonly List<WatcherItem> _watcherItems = [];
    private readonly SafeHandle _cancellationEvent;
    private Task? _waitEventTask;
    private volatile int _waitThreadId;

    private bool IsOnWaitThread => _waitThreadId == Environment.CurrentManagedThreadId;

    /// <summary>
    /// Raised when a watched registry key (or subtree if configured) changes.
    /// The event handler receives a <see cref="WatcherEventArgs"/> instance describing the changed key.
    /// </summary>
    public event EventHandler? RegistryChanged;

    /// <summary>
    /// Raised when a watched registry subkey is deleted.
    /// The event handler receives a <see cref="RegSubkeyDeletedEventArgs"/> with the deleted path.
    /// </summary>
    public event EventHandler<RegSubkeyDeletedEventArgs>? RegistryKeyDeleted;

    /// <summary>
    /// Raised when the watcher encounters an error during operation. Handlers receive <see cref="ErrorEventArgs"/>.
    /// </summary>
    public event EventHandler<ErrorEventArgs>? OnError; 


    /// <summary>
    /// Gets a value indicating whether the watcher is currently running.
    /// </summary>
    public bool IsWatching
    {
        get
        {
            lock (_sync)
            {
                return _waitEventTask?.IsCompleted == false;
            }
        }
    }

    //todo
    //public override string ToString() {}
        
    /// <summary>
    /// Creates a registry watcher for a single registry location.
    /// </summary>
    /// <param name="root">The registry hive to watch (see <see cref="RegistryHive"/>).</param>
    /// <param name="subKey">The subkey path under the hive.</param>
    /// <param name="watchSubtree">If true, include changes in the entire subtree under <paramref name="subKey"/>.</param>
    public RegistryWatcher(RegistryHive root, string subKey, bool watchSubtree) : this()
    {
        try
        {
            AddWatcherItem(root, subKey, watchSubtree);
        }
        catch
        {
            _cancellationEvent.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates a registry watcher configured with multiple descriptors.
    /// </summary>
    /// <param name="descriptors">An array or span of <see cref="WatcherItemDescriptor"/> that describe items to watch.</param>
    public RegistryWatcher(ReadOnlySpan<WatcherItemDescriptor> descriptors): this()
    {
        if (descriptors.Length >= WinApi.MAXIMUM_WAIT_OBJECTS)
            throw new ArgumentOutOfRangeException(nameof(descriptors),
                $"Descriptors count must be less than {WinApi.MAXIMUM_WAIT_OBJECTS}");
        try
        {
            foreach (var descriptor in descriptors)
            {
                AddWatcherItem(descriptor);
            }
        }
        catch
        {
            _cancellationEvent.Dispose();
            foreach (var item in _watcherItems) item.Subkey.Close();

            throw;
        }
    }

    private RegistryWatcher()
    {
        _cancellationEvent = RegistryEventApi.CreateEventHandle(false, true);
    }

    [Obsolete("This constructor will be removed in future versions.")]
    public RegistryWatcher(RegistryRootKey root, string subKey, bool watchSubtree) :
        this((RegistryHive)root, subKey, watchSubtree) {}


    /// <summary>
    /// Starts monitoring the configured registry locations. This method allocates internal resources and begins
    /// a long-running background wait loop.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the watcher is already started.</exception>
    public void Start()
    {
        lock (_sync)
        {
            CheckDisposed();
            if (IsWatching)
                throw new InvalidOperationException("Watcher is already started or not stopped yet.");

            RegistryEventApi.ResetEvent(_cancellationEvent);

            _waitEventTask = Task.Factory.StartNew(
                WaitProc, 
                CancellationToken.None, 
                TaskCreationOptions.LongRunning, 
                TaskScheduler.Default);
        }
    }

    /// <summary>
    /// Stops monitoring and waits for the background wait loop to exit. This method blocks until the watcher stops.
    /// It must not be called from inside an event handler produced by this instance.
    /// </summary>
    public void Stop()
    {
        if (IsOnWaitThread)
            throw new InvalidOperationException("Stop() cannot be called from an event handler.");

        Task task;
        lock (_sync)
        {
            CheckDisposed();
            if (_waitEventTask is null) return;
            task = _waitEventTask;
            RegistryEventApi.SetEvent(_cancellationEvent);
        }

        try { task.Wait(); }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_waitEventTask, task)) _waitEventTask = null;
            }
        }
    }

    private void WaitProc()
    {
        _waitThreadId = Environment.CurrentManagedThreadId;
        var eventsCount = _watcherItems.Count + 1; //With cancellation event
        var events = new SafeHandle[eventsCount];

        try
        {
            events[0] = _cancellationEvent;

            for (var i = 1; i < eventsCount; i++)
            {
                events[i] = RegistryEventApi.CreateEventHandle(false, false);
            }

            for (var i = 1; i < events.Length; i++)
            {
                TryTriggerUpEventOrMarkDeleted(i);
            }

            while (true)
            {
                var triggered = RegistryEventApi.WaitForMultipleEvents
                    (events, false, TimeoutInfinite);

                if (triggered == WAIT_EVENT.WAIT_FAILED)
                    throw new Win32Exception();

                var index = unchecked(((uint)triggered - (uint)WAIT_EVENT.WAIT_OBJECT_0));
                if (index >= eventsCount)
                    throw new InvalidOperationException();

                if (index == 0)
                    break;

                if (!TryTriggerUpEventOrMarkDeleted((int)index))
                    continue;

                var item = _watcherItems[(int)index - 1];
                var eventArgs = new WatcherEventArgs
                {
                    SubKey = item.Subkey.Name,
                    IsTreeWatching = item.WatchSubtree
                };
                try
                {
                    RegistryChanged?.Invoke(this, eventArgs);
                }
                catch (Exception ex)
                {
                    try
                    {
                        OnError?.Invoke(this, new ErrorEventArgs(ex));
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
        catch (Exception ex)
        {
            try { OnError?.Invoke(this, new ErrorEventArgs(ex)); } catch { /* не маскируем исходную ошибку */ }
            throw;
        }
        finally
        {
            for (var i = 1; i < eventsCount; i++)
            {
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                events[i]?.Close(); //Can be null!!!
            }

            _waitThreadId = 0;
        }
        return;

        bool TryTriggerUpEventOrMarkDeleted(int index)
        {
            if (_watcherItems[index - 1].IsDeleted)
                return false;

            var regHandle = _watcherItems[index - 1].Subkey.Handle;
            var watchSubtree = _watcherItems[index - 1].WatchSubtree;
            var eventHandle = events[index];
            var error = RegistryEventApi.TriggerUpRegistryEvent(regHandle, eventHandle, watchSubtree);
            switch (error)
            {
                case WIN32_ERROR.NO_ERROR:
                    return true;

                case WIN32_ERROR.ERROR_KEY_DELETED:
                    _watcherItems[index - 1].IsDeleted = true;
                    var path = _watcherItems[index - 1].Subkey.Name;
                    _watcherItems[index - 1].Subkey.Close();
                    RegistryKeyDeleted?.Invoke(this, new RegSubkeyDeletedEventArgs(path));
                    
                    return false;

                default:
                    throw new Win32Exception((int)error);
            }
        }
    }
    
    private void AddWatcherItem(RegistryHive root, string subKey, bool watchSubtree)
    {
        using var rootKey = RegistryKey.OpenBaseKey(root, RegistryView.Default);
        var key = rootKey.OpenSubKey(subKey) 
                  ?? throw new KeyNotFoundException($"Registry key not found: {root}\\{subKey}");
        
        var item = new WatcherItem(key, watchSubtree);

        _watcherItems.Add(item);
    }

    private void AddWatcherItem(WatcherItemDescriptor descriptor)
    {
        AddWatcherItem(descriptor.Hive, descriptor.SubKey, descriptor.WatchSubtree);
    }

    #region Dispose

    private bool _disposed;
    
    /// <summary>
    /// Releases all resources used by the <see cref="RegistryWatcher"/>. This method stops watching if necessary.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    /// <summary>
    /// Protected dispose pattern implementation.
    /// </summary>
    /// <param name="disposing">True when called from Dispose(), false when called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        try
        {
            if (disposing)
            {
                if (IsOnWaitThread)
                    throw new InvalidOperationException("Dispose() cannot be called from an event handler.");

                //dispose managed state (managed objects)
                try
                {
                    Stop();
                }
                catch (Exception ex)
                {
                    try
                    {
                        OnError?.Invoke(this, new ErrorEventArgs(ex));
                    }
                    catch
                    {
                        // ignored
                    }
                }

                foreach (var item in _watcherItems) item.Subkey.Dispose();

                _cancellationEvent.Dispose();
                RegistryChanged = null;
                RegistryKeyDeleted = null;
                OnError = null;
            }
            //free unmanaged resources (unmanaged objects) and override finalizer
            //set large fields to null

        }
        finally
        {
            _disposed = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RegistryWatcher));
    }
    #endregion
}
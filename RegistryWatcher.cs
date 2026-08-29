using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace PhoenixTools.Watchers
{
    public class RegistryWatcher : IDisposable
    {
        private const uint TimeoutInfinite = 0xFFFFFFFF;

        private readonly SafeHandle _regHandle;
        private readonly bool _watchSubtree;
        private readonly SafeHandle _cancellationEvent;

        private Task _waitEventTask;

        public event EventHandler RegistryChanged; 

        // ReSharper disable once ConvertToPrimaryConstructor
        public RegistryWatcher(RegistryRootKey root, string subKey, bool watchSubtree)
        {
            _watchSubtree = watchSubtree;
            _regHandle = RegistryEventApi.OpenRegistryKey(new HKEY((IntPtr)root), subKey);
            _cancellationEvent = RegistryEventApi.CreateEventHandle(false, true);
            _waitEventTask = null;
        }

        public bool IsWatching => _waitEventTask != null;

        public void Start()
        {
            if (IsWatching) return;

            RegistryEventApi.ResetEvent(_cancellationEvent);
            _waitEventTask = Task.Run(WaitProc);
        }

        public void Stop()
        {
            if (!IsWatching) return;

            RegistryEventApi.SetEvent(_cancellationEvent);
            _waitEventTask?.Wait();
            _waitEventTask = null;
        }

        private void WaitProc()
        {
            using (var regChangedEvent = RegistryEventApi.CreateEventHandle(false, false))
            {
                var events = new[] { regChangedEvent, _cancellationEvent };
                var isCanceled = false;

                while (!isCanceled)
                {
                    RegistryEventApi.TriggerUpRegistryEvent(_regHandle, regChangedEvent, _watchSubtree);
                    var triggered = RegistryEventApi.WaitForMultiplyEvents
                        (events, false, TimeoutInfinite);

                    switch (triggered)
                    {
                        case WAIT_EVENT.WAIT_OBJECT_0:
                            RegistryChanged?.Invoke(this, EventArgs.Empty);
                            continue;
                        case WAIT_EVENT.WAIT_OBJECT_0 + 1:
                            isCanceled = true;
                            break;
                        default: throw new Win32Exception();
                    }
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
    } }
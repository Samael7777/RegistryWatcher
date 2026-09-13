using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Microsoft.Win32;
using PhoenixTools.Watchers.API;

namespace PhoenixTools.Watchers
{
    public class RegistryWatcher : IDisposable
    {
        private const uint TimeoutInfinite = 0xFFFFFFFF;
        private readonly RegistryKey _key;
        private readonly bool _watchSubtree;
        private readonly SafeHandle _cancellationEvent;

        private Task? _waitEventTask;

        public event EventHandler? RegistryChanged;
        
        public bool IsWatching => _waitEventTask != null;
        public override string ToString() => _key.Name;
        
        public RegistryWatcher(RegistryHive root, string subKey, bool watchSubtree)
        {
            using var rootKey = RegistryKey.OpenBaseKey(root, RegistryView.Default);
            _key = rootKey.OpenSubKey(subKey) 
                   ?? throw new Win32Exception();

            _watchSubtree = watchSubtree;
            _cancellationEvent = RegistryEventApi.CreateEventHandle(false, true);
            _waitEventTask = null;
        }

        [Obsolete("This constructor will be removed in future versions.")]
        public RegistryWatcher(RegistryRootKey root, string subKey, bool watchSubtree) :
            this((RegistryHive)root, subKey, watchSubtree) {}


        public void Start()
        {
            CheckDisposed();
            if (IsWatching) return;

            RegistryEventApi.ResetEvent(_cancellationEvent);
            _waitEventTask = Task.Run(WaitProc);
        }

        public void Stop()
        {
            CheckDisposed();
            if (!IsWatching) return;

            RegistryEventApi.SetEvent(_cancellationEvent);
            _waitEventTask?.Wait();
            _waitEventTask = null;
        }

        private void WaitProc()
        {
            using var regChangedEvent = RegistryEventApi.CreateEventHandle(false, false);
            var events = new[] { regChangedEvent, _cancellationEvent };
            var isCanceled = false;

            while (!isCanceled)
            {
                RegistryEventApi.TriggerUpRegistryEvent(_key.Handle, regChangedEvent, _watchSubtree);
                var triggered = RegistryEventApi.WaitForMultipleEvents
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
                _key.Dispose();
                _cancellationEvent.Dispose();
                RegistryChanged = null;
            }
            //free unmanaged resources (unmanaged objects) and override finalizer
            //set large fields to null

            _disposed = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RegistryWatcher));
        }

        #endregion
    }
}
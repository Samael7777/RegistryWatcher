using System;
using System.Timers;
using Timer = System.Timers.Timer;

namespace PhoenixTools.Watchers
{
    public class EventsFilter : IDisposable
    {
        private readonly Timer _timer;
        private EventArgs _eventArgs;

        public event EventHandler Filtered;

        public EventsFilter(double filterTimeMs)
        {
            _timer = new Timer
            {
                AutoReset = false
            };
            _timer.Elapsed += OnTimer;
            FilterTimeMilliseconds = filterTimeMs;
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            var args = _eventArgs ?? EventArgs.Empty;
            Filtered?.Invoke(this, args);
        }

        public double FilterTimeMilliseconds
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public void Input(object sender, EventArgs args)
        {
            _eventArgs = args;
            _timer.Start();
        }
        
        #region Dispose

        private bool _disposed;

        ~EventsFilter()
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
                _timer.Dispose();
                Filtered = null;
            }
            //free unmanaged resources (unmanaged objects) and override finalizer
            //set large fields to null

            _disposed = true;
        }

        #endregion
    }
}
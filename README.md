# Registry watcher
Provides the PhoenixTools.Watchers.RegisrtryWatcher class (implements `IDisposable` interface), 
which listens to the windows registry change notifications and raises events when a 
specified subkey changes.
Use `Start` method to start watching and `Stop` to stop.


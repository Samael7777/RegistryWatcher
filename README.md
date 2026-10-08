
# PhoenixTools.RegistryWatcher

PhoenixTools.RegistryWatcher is a small library that provides a lightweight API for watching
Windows registry keys and receiving notifications when keys change or are deleted.

This README documents the public surface of the library and provides basic usage examples.

Supported targets: .NET Framework, .NET Standard and modern .NET (see project files).

Public types overview
---------------------

- WatcherItem — simple model representing a single registry location (hive, subkey, subtree flag).
- WatcherItemDescriptor — descriptor used when creating a watcher from multiple items.
- WatcherEventArgs — event args for change notifications.
- RegSubkeyDeletedEventArgs — event args when a watched subkey is deleted.
- ErrorEventArgs — event args containing exceptions from the library.
- RegistryWatcher — the primary type that performs watching and raises events.
- RegistryRootKey — legacy enum mapping Windows HKEY values (obsolete; prefer RegistryHive).

Detailed API
------------

WatcherItem
 - Hive: RegistryHive — root hive (e.g. RegistryHive.LocalMachine).
 - SubKey: string — subkey path under the hive (e.g. "SOFTWARE\\MyCompany\\MyApp").
 - WatchSubtree: bool — if true, watch the entire subtree beneath SubKey; default true.

WatcherItemDescriptor
 - Same shape as WatcherItem and used to pass multiple descriptors to the RegistryWatcher constructor.

WatcherEventArgs
 - SubKey: string — the full subkey path that changed.
 - IsTreeWatching: bool — indicates whether the watcher monitored the subtree under the subkey.

RegSubkeyDeletedEventArgs
 - RegKeyPath: string — full registry path of the deleted subkey.

ErrorEventArgs
 - Exception: Exception — exception thrown internally by the library.

RegistryWatcher
 - Constructors:
   - RegistryWatcher(RegistryHive root, string subKey, bool watchSubtree) — create watcher for a single location.
   - RegistryWatcher(ReadOnlySpan<WatcherItemDescriptor> descriptors) — create watcher for multiple locations.
   - RegistryWatcher(RegistryRootKey root, string subKey, bool watchSubtree) [obsolete] — legacy constructor.

 - Events:
   - RegistryChanged — raised when a watched key (or subtree) changes; use WatcherEventArgs.
   - RegistryKeyDeleted — raised when a watched subkey is deleted; use RegSubkeyDeletedEventArgs.
   - OnError — raised when the watcher encounters an internal error; use ErrorEventArgs.

 - Methods and properties:
   - Start() — begins monitoring. Allocates internal resources and starts a background wait loop.
   - Stop() — stops monitoring and blocks until the background wait loop exits. Must not be called from an event handler.
   - IsWatching — bool property that indicates whether the watcher is currently running.
   - Dispose() — stops watching (if running) and releases all resources. Dispose must not be called from inside an event handler.

Threading and behavior notes
----------------------------

- The watcher uses a background long-running task to wait for registry notifications. Start and Stop are thread-safe
  and synchronized internally. Stopping and disposing will block until the background task exits.
- Event handlers registered for RegistryChanged / RegistryKeyDeleted must be fast — expensive work should be offloaded to
  other threads to keep notifications responsive. Do not call Stop() or Dispose() from inside event handlers.
- Exceptions thrown by event handlers are caught internally; the library will attempt to notify subscribers via OnError.

Usage examples
--------------

Watch a single key:

```csharp
using Microsoft.Win32;

var watcher = new RegistryWatcher(RegistryHive.LocalMachine, "SOFTWARE\\MyCompany\\MyApp", true);
watcher.RegistryChanged += (s, e) => Console.WriteLine($"Changed: {e.SubKey}");
watcher.RegistryKeyDeleted += (s, e) => Console.WriteLine($"Deleted: {e.RegKeyPath}");
watcher.OnError += (s, e) => Console.Error.WriteLine($"Watcher error: {e.Exception}");

watcher.Start();
// ... later
watcher.Stop();
watcher.Dispose();
```

Watch multiple keys:

```csharp
var descriptors = new[]
{
	new WatcherItemDescriptor { Hive = RegistryHive.LocalMachine, SubKey = "SOFTWARE\\MyCompany\\App1", WatchSubtree = true },
	new WatcherItemDescriptor { Hive = RegistryHive.CurrentUser, SubKey = "SOFTWARE\\MyCompany\\App2", WatchSubtree = false }
};

var watcher = new RegistryWatcher(descriptors);
// subscribe and start as above
```

Notes
-----

- Prefer Microsoft.Win32.RegistryHive for specifying root hives. The RegistryRootKey enum exists for compatibility and is marked obsolete.
- The library exposes XML documentation in source files; consult the code comments for more details on behaviour and exceptions.


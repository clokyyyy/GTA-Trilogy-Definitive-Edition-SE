using Avalonia.Threading;

namespace GtaDe.App.Services;

/// <summary>
/// Watches the open save file for changes made outside the editor.
/// </summary>
/// <remarks>
/// GTA III rewrites its save files while it is running, so a file opened here can be replaced
/// underneath the user at any moment. Without this they would keep editing a stale buffer and then
/// overwrite whatever the game had just written. The watcher only reports; what to do about it is
/// the shell's decision.
/// </remarks>
public sealed class SaveFileWatcher : IDisposable
{
    private readonly Action _onChanged;
    private FileSystemWatcher? _watcher;
    private DateTime _lastReport = DateTime.MinValue;

    public SaveFileWatcher(Action onChanged) => _onChanged = onChanged;

    public void Watch(string? path)
    {
        Stop();

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var folder = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
        {
            return;
        }

        _watcher = new FileSystemWatcher(folder, Path.GetFileName(path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnFileEvent;
        _watcher.Created += OnFileEvent;
        _watcher.Renamed += OnFileEvent;
    }

    public void Stop()
    {
        if (_watcher is null)
        {
            return;
        }

        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFileEvent;
        _watcher.Created -= OnFileEvent;
        _watcher.Renamed -= OnFileEvent;
        _watcher.Dispose();
        _watcher = null;
    }

    /// <summary>
    /// Call around the editor's own writes so saving does not look like an outside change.
    /// </summary>
    public IDisposable Suppress() => new Suppression(this);

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        if (_watcher is null || !_watcher.EnableRaisingEvents)
        {
            return;
        }

        // Windows raises several events for one write, so collapse anything within a second.
        var now = DateTime.UtcNow;
        if (now - _lastReport < TimeSpan.FromSeconds(1))
        {
            return;
        }

        _lastReport = now;
        Dispatcher.UIThread.Post(_onChanged);
    }

    public void Dispose() => Stop();

    private sealed class Suppression : IDisposable
    {
        private readonly SaveFileWatcher _owner;
        private readonly bool _wasEnabled;

        public Suppression(SaveFileWatcher owner)
        {
            _owner = owner;
            _wasEnabled = owner._watcher?.EnableRaisingEvents ?? false;

            if (owner._watcher is not null)
            {
                owner._watcher.EnableRaisingEvents = false;
            }
        }

        public void Dispose()
        {
            if (_owner._watcher is not null)
            {
                _owner._lastReport = DateTime.UtcNow;
                _owner._watcher.EnableRaisingEvents = _wasEnabled;
            }
        }
    }
}

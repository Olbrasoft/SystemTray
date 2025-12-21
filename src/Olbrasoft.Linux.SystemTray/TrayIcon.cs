using Microsoft.Extensions.Logging;
using Olbrasoft.Linux.SystemTray.Internal;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Olbrasoft.Linux.SystemTray;

/// <summary>
/// Implementation of ITrayIcon using D-Bus StatusNotifierItem.
/// Provides a system tray icon with support for dynamic icon changes and animations.
/// </summary>
public class TrayIcon : ITrayIcon
{
    private readonly ILogger<TrayIcon> _logger;
    private readonly IIconRenderer _iconRenderer;
    private readonly string _id;

    private Connection? _connection;
    private OrgFreedesktopDBusProxy? _dBus;
    private OrgKdeStatusNotifierWatcherProxy? _statusNotifierWatcher;
    private StatusNotifierItemHandler? _sniHandler;
    private PathHandler? _pathHandler;

    private IDisposable? _serviceWatchDisposable;

    private string? _sysTrayServiceName;
    private bool _isDisposed;
    private bool _serviceConnected;
    private bool _isVisible = true;

    // Current icon state
    private (int, int, byte[]) _currentIcon = (1, 1, new byte[] { 255, 0, 0, 0 }); // Empty pixmap
    private string _tooltipText = "";

    // Animation support
    private Timer? _animationTimer;
    private string[]? _animationIconPaths;
    private int _currentFrameIndex;
    private readonly object _animationLock = new();

    /// <inheritdoc />
    public string Id => _id;

    /// <inheritdoc />
    public bool IsVisible { get; private set; }

    /// <inheritdoc />
    public bool IsAnimating
    {
        get
        {
            lock (_animationLock)
            {
                return _animationTimer is not null;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler? Clicked;

    /// <inheritdoc />
    public event EventHandler? MenuRequested;

    public TrayIcon(ILogger<TrayIcon> logger, IIconRenderer iconRenderer, string id)
    {
        _logger = logger;
        _iconRenderer = iconRenderer;
        _id = id;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TrayIcon));

        try
        {
            _connection = new Connection(Address.Session!);
            await _connection.ConnectAsync();

            _dBus = new OrgFreedesktopDBusProxy(_connection, "org.freedesktop.DBus", "/org/freedesktop/DBus");

            // Standard paths as per StatusNotifierItem spec
            _pathHandler = new PathHandler("/StatusNotifierItem");
            _sniHandler = new StatusNotifierItemHandler(_connection, _logger, "/MenuBar", _id, _id);
            _sniHandler.ActivationDelegate += OnActivation;

            IsVisible = true;

            // Start watching for StatusNotifierWatcher service
            await WatchAsync(cancellationToken);

            _logger.LogInformation("TrayIcon '{Id}' initialized successfully", _id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize TrayIcon '{Id}'", _id);
            IsVisible = false;
            throw;
        }
    }

    private async Task WatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            _serviceWatchDisposable = await _dBus!.WatchNameOwnerChangedAsync(
                (exception, change) =>
                {
                    if (exception is null && change.Item1 == "org.kde.StatusNotifierWatcher")
                    {
                        OnNameChange(change.Item1, change.Item3);
                    }
                },
                emitOnCapturedContext: false);

            var nameOwner = await _dBus.GetNameOwnerAsync("org.kde.StatusNotifierWatcher");
            OnNameChange("org.kde.StatusNotifierWatcher", nameOwner);
        }
        catch (DBusException ex) when (ex.ErrorName == "org.freedesktop.DBus.Error.NameHasNoOwner")
        {
            _logger.LogWarning("StatusNotifierWatcher service not available. Tray icon will not be visible.");
        }
        catch (Exception ex)
        {
            _serviceWatchDisposable = null;
            _logger.LogError(ex, "Failed to watch StatusNotifierWatcher service");
        }
    }

    private void OnNameChange(string name, string? newOwner)
    {
        if (_isDisposed || _connection is null || name != "org.kde.StatusNotifierWatcher")
            return;

        if (!_serviceConnected && newOwner is not null)
        {
            _serviceConnected = true;
            _statusNotifierWatcher = new OrgKdeStatusNotifierWatcherProxy(_connection, "org.kde.StatusNotifierWatcher", "/StatusNotifierWatcher");

            DestroyTrayIcon();

            if (_isVisible)
                _ = CreateTrayIconAsync();
        }
        else if (_serviceConnected && newOwner is null)
        {
            DestroyTrayIcon();
            _serviceConnected = false;
        }
    }

    private async Task CreateTrayIconAsync()
    {
        if (_connection is null || !_serviceConnected || _isDisposed || _statusNotifierWatcher is null)
            return;

        try
        {
            // Add SNI handler if needed
            if (_sniHandler!.PathHandler is null)
                _pathHandler!.Add(_sniHandler);

            _connection.RemoveMethodHandler(_pathHandler!.Path);
            _connection.AddMethodHandler(_pathHandler);

            // Register with unique connection name only
            _sysTrayServiceName = _connection.UniqueName!;
            await _statusNotifierWatcher.RegisterStatusNotifierItemAsync(_sysTrayServiceName);

            // Set initial state IMMEDIATELY after registration
            _sniHandler.SetTitleAndTooltip(_tooltipText);
            _sniHandler.SetIcon(_currentIcon);

            _logger.LogInformation("Tray icon registered as {ServiceName}", _sysTrayServiceName);

            // Re-emit signals after delays for GNOME Shell's appindicator extension
            _ = Task.Run(async () =>
            {
                await Task.Delay(100);
                if (!_isDisposed && _sniHandler?.PathHandler is not null)
                {
                    _sniHandler.SetIcon(_currentIcon);
                }

                await Task.Delay(400);
                if (!_isDisposed && _sniHandler?.PathHandler is not null)
                {
                    _sniHandler.SetIcon(_currentIcon);
                    _logger.LogDebug("Re-emitted icon signals for GNOME Shell");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tray icon");
        }
    }

    private void DestroyTrayIcon()
    {
        if (_connection is null || !_serviceConnected || _isDisposed || _sniHandler is null || _sysTrayServiceName is null)
            return;

        try
        {
            _pathHandler!.Remove(_sniHandler);
            _connection.RemoveMethodHandler(_pathHandler.Path);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error destroying tray icon");
        }
    }

    private void OnActivation()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void SetIcon(string iconPath, string? tooltip = null)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TrayIcon));

        try
        {
            var pixmap = _iconRenderer.GetCachedIcon(iconPath);
            _currentIcon = pixmap;
            _tooltipText = tooltip ?? _tooltipText;

            if (_sniHandler?.PathHandler is not null)
            {
                _sniHandler.SetIcon(_currentIcon);
                if (tooltip is not null)
                    _sniHandler.SetTitleAndTooltip(tooltip);
            }

            _logger.LogDebug("Set icon: {IconPath} ({Width}x{Height})", iconPath, _currentIcon.Item1, _currentIcon.Item2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set icon: {IconPath}", iconPath);
            throw;
        }
    }

    /// <inheritdoc />
    public void StartAnimation(string[] iconPaths, int intervalMs = 150, string? tooltip = null)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TrayIcon));

        if (iconPaths.Length == 0)
            throw new ArgumentException("Icon paths array cannot be empty", nameof(iconPaths));

        lock (_animationLock)
        {
            // Stop any existing animation
            StopAnimationInternal();

            // Pre-cache all frames
            _iconRenderer.PreCacheIcons(iconPaths);

            _animationIconPaths = iconPaths;
            _currentFrameIndex = 0;
            _tooltipText = tooltip ?? _tooltipText;

            // Set first frame immediately
            var firstFrame = _iconRenderer.GetCachedIcon(iconPaths[0]);
            _currentIcon = firstFrame;
            _sniHandler?.SetAnimationFrame(_currentIcon, _currentFrameIndex);

            if (tooltip is not null)
                _sniHandler?.SetTitleAndTooltip(tooltip);

            // Start timer for subsequent frames
            _animationTimer = new Timer(AnimationCallback, null, intervalMs, intervalMs);
            _logger.LogDebug("Animation started with {FrameCount} frames, interval {Interval}ms", iconPaths.Length, intervalMs);
        }
    }

    /// <inheritdoc />
    public void StopAnimation()
    {
        lock (_animationLock)
        {
            StopAnimationInternal();
        }
    }

    private void StopAnimationInternal()
    {
        _animationTimer?.Dispose();
        _animationTimer = null;
        _animationIconPaths = null;
        _currentFrameIndex = 0;
    }

    private void AnimationCallback(object? state)
    {
        if (_isDisposed)
            return;

        lock (_animationLock)
        {
            if (_animationIconPaths is null || _animationIconPaths.Length == 0)
                return;

            _currentFrameIndex = (_currentFrameIndex + 1) % _animationIconPaths.Length;
            var iconPath = _animationIconPaths[_currentFrameIndex];

            var pixmap = _iconRenderer.GetCachedIcon(iconPath);
            _currentIcon = pixmap;
            _sniHandler?.SetAnimationFrame(_currentIcon, _currentFrameIndex);
        }
    }

    /// <inheritdoc />
    public void SetMenu(ITrayMenu menu)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TrayIcon));

        // TODO: Implement D-Bus menu support
        throw new NotImplementedException("Menu support not yet implemented");
    }

    /// <inheritdoc />
    public void Hide()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TrayIcon));

        _isVisible = false;
        DestroyTrayIcon();
    }

    /// <inheritdoc />
    public void Show()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TrayIcon));

        _isVisible = true;
        if (_serviceConnected)
            _ = CreateTrayIconAsync();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        IsVisible = false;

        StopAnimation();
        DestroyTrayIcon();

        _serviceWatchDisposable?.Dispose();
        _connection?.Dispose();

        _logger.LogInformation("TrayIcon '{Id}' disposed", _id);
    }
}

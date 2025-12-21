using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Olbrasoft.SystemTray.Linux;

/// <summary>
/// Manages multiple tray icons simultaneously.
/// Use this for scenarios like GestureEvolution where multiple icons (left hand, robot, right hand) are displayed.
/// </summary>
public class TrayIconManager : ITrayIconManager
{
    private readonly ILogger<TrayIconManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IIconRenderer _iconRenderer;
    private readonly ConcurrentDictionary<string, ITrayIcon> _icons = new();
    private bool _isDisposed;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ITrayIcon> Icons => _icons;

    public TrayIconManager(ILogger<TrayIconManager> logger, ILoggerFactory loggerFactory, IIconRenderer iconRenderer)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _iconRenderer = iconRenderer;
    }

    /// <inheritdoc />
    public async Task<ITrayIcon> CreateIconAsync(string id, string iconPath, string? tooltip = null, CancellationToken cancellationToken = default)
    {
        return await CreateIconAsync(id, iconPath, tooltip, null, cancellationToken);
    }

    /// <summary>
    /// Creates a new tray icon with optional context menu support.
    /// </summary>
    /// <param name="id">Unique identifier for the icon</param>
    /// <param name="iconPath">Path to the SVG icon file</param>
    /// <param name="tooltip">Optional tooltip text</param>
    /// <param name="menuHandler">Optional menu handler for context menu. Must implement ITrayMenuHandler interface.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created tray icon instance</returns>
    /// <exception cref="ObjectDisposedException">Manager has been disposed</exception>
    /// <exception cref="ArgumentException">ID is null or whitespace</exception>
    /// <exception cref="InvalidOperationException">Icon with this ID already exists</exception>
    public async Task<ITrayIcon> CreateIconAsync(
        string id,
        string iconPath,
        string? tooltip = null,
        ITrayMenuHandler? menuHandler = null,
        CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TrayIconManager));

        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Icon ID cannot be null or whitespace", nameof(id));

        if (_icons.ContainsKey(id))
            throw new InvalidOperationException($"Tray icon with ID '{id}' already exists");

        try
        {
            var trayIconLogger = _loggerFactory.CreateLogger<TrayIcon>();
            var trayIcon = new TrayIcon(trayIconLogger, _iconRenderer, id, menuHandler);

            await trayIcon.InitializeAsync(cancellationToken);
            trayIcon.SetIcon(iconPath, tooltip);

            _icons[id] = trayIcon;

            _logger.LogInformation("Created tray icon '{Id}' {MenuStatus}", id, menuHandler != null ? "with menu" : "without menu");
            return trayIcon;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tray icon '{Id}'", id);
            throw;
        }
    }

    /// <inheritdoc />
    public ITrayIcon? GetIcon(string id)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TrayIconManager));

        return _icons.TryGetValue(id, out var icon) ? icon : null;
    }

    /// <inheritdoc />
    public void RemoveIcon(string id)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TrayIconManager));

        if (_icons.TryRemove(id, out var icon))
        {
            icon.Dispose();
            _logger.LogInformation("Removed tray icon '{Id}'", id);
        }
    }

    /// <inheritdoc />
    public void RemoveAllIcons()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TrayIconManager));

        foreach (var kvp in _icons)
        {
            kvp.Value.Dispose();
        }

        _icons.Clear();
        _logger.LogInformation("Removed all tray icons");
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        RemoveAllIcons();

        _logger.LogInformation("TrayIconManager disposed");
    }
}

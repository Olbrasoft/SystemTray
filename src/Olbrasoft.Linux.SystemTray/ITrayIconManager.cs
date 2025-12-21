namespace Olbrasoft.Linux.SystemTray;

/// <summary>
/// Manages multiple tray icons simultaneously.
/// Use this for scenarios like GestureEvolution where multiple icons (left hand, robot, right hand) are displayed.
/// </summary>
public interface ITrayIconManager : IDisposable
{
    /// <summary>
    /// Gets all currently managed tray icons.
    /// </summary>
    IReadOnlyDictionary<string, ITrayIcon> Icons { get; }

    /// <summary>
    /// Creates a new tray icon and adds it to the manager.
    /// </summary>
    /// <param name="id">Unique identifier for this tray icon.</param>
    /// <param name="iconPath">Initial SVG icon path.</param>
    /// <param name="tooltip">Optional tooltip text.</param>
    /// <returns>The created tray icon.</returns>
    Task<ITrayIcon> CreateIconAsync(string id, string iconPath, string? tooltip = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tray icon by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the tray icon.</param>
    /// <returns>The tray icon, or null if not found.</returns>
    ITrayIcon? GetIcon(string id);

    /// <summary>
    /// Removes and disposes a tray icon.
    /// </summary>
    /// <param name="id">Identifier of the tray icon to remove.</param>
    void RemoveIcon(string id);

    /// <summary>
    /// Removes and disposes all tray icons.
    /// </summary>
    void RemoveAllIcons();
}

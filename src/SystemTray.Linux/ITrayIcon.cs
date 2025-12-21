namespace Olbrasoft.SystemTray.Linux;

/// <summary>
/// Represents a system tray icon with support for dynamic icon changes, animations, and menus.
/// Hides all D-Bus complexity behind a simple C# interface.
/// </summary>
public interface ITrayIcon : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this tray icon.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets a value indicating whether this tray icon is currently visible in the system tray.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Gets a value indicating whether an animation is currently playing.
    /// </summary>
    bool IsAnimating { get; }

    /// <summary>
    /// Initializes and shows the tray icon in the system tray.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the tray icon to display a single static icon.
    /// </summary>
    /// <param name="iconPath">Path to the SVG icon file.</param>
    /// <param name="tooltip">Optional tooltip text to display on hover.</param>
    void SetIcon(string iconPath, string? tooltip = null);

    /// <summary>
    /// Starts an animated icon cycle using multiple frames.
    /// </summary>
    /// <param name="iconPaths">Array of SVG icon paths to cycle through.</param>
    /// <param name="intervalMs">Interval between frames in milliseconds (default: 150ms).</param>
    /// <param name="tooltip">Optional tooltip text to display on hover.</param>
    void StartAnimation(string[] iconPaths, int intervalMs = 150, string? tooltip = null);

    /// <summary>
    /// Stops the current animation and keeps the last displayed frame.
    /// </summary>
    void StopAnimation();

    /// <summary>
    /// Sets the context menu for this tray icon.
    /// </summary>
    /// <param name="menu">Menu configuration to display on right-click.</param>
    void SetMenu(ITrayMenu menu);

    /// <summary>
    /// Hides the tray icon from the system tray.
    /// </summary>
    void Hide();

    /// <summary>
    /// Shows the tray icon in the system tray (after being hidden).
    /// </summary>
    void Show();

    /// <summary>
    /// Event raised when the tray icon is clicked.
    /// </summary>
    event EventHandler? Clicked;
}

namespace Olbrasoft.SystemTray.Linux;

/// <summary>
/// Represents a context menu for a tray icon.
/// </summary>
public interface ITrayMenu
{
    /// <summary>
    /// Gets the menu items in this menu.
    /// </summary>
    IReadOnlyList<ITrayMenuItem> Items { get; }

    /// <summary>
    /// Adds a menu item to this menu.
    /// </summary>
    /// <param name="item">Menu item to add.</param>
    void AddItem(ITrayMenuItem item);

    /// <summary>
    /// Removes a menu item from this menu.
    /// </summary>
    /// <param name="item">Menu item to remove.</param>
    void RemoveItem(ITrayMenuItem item);

    /// <summary>
    /// Clears all menu items from this menu.
    /// </summary>
    void Clear();
}

/// <summary>
/// Represents a single menu item in a tray icon context menu.
/// </summary>
public interface ITrayMenuItem
{
    /// <summary>
    /// Gets the unique identifier for this menu item.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets or sets the display text for this menu item.
    /// </summary>
    string Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this menu item is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this menu item is a separator.
    /// </summary>
    bool IsSeparator { get; }

    /// <summary>
    /// Gets the submenu items for this menu item (if any).
    /// </summary>
    IReadOnlyList<ITrayMenuItem>? SubItems { get; }

    /// <summary>
    /// Event raised when this menu item is clicked.
    /// </summary>
    event EventHandler? Clicked;
}

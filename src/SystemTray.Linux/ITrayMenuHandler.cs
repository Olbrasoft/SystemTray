using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Olbrasoft.SystemTray.Linux;

/// <summary>
/// Represents a D-Bus menu handler for a tray icon context menu.
/// Implement this interface to create custom menu handlers for your tray icons.
/// </summary>
/// <remarks>
/// This interface wraps the DBusMenu (com.canonical.dbusmenu) protocol handler.
/// Your implementation should inherit from the generated ComCanonicalDbusmenuHandler class
/// and implement this interface to provide menu functionality.
///
/// Example:
/// <code>
/// internal class MyMenuHandler : ComCanonicalDbusmenuHandler, ITrayMenuHandler
/// {
///     public MyMenuHandler(Connection connection, ILogger logger)
///         : base(emitOnCapturedContext: false)
///     {
///         Connection = connection;
///         Version = 4;
///         TextDirection = "ltr";
///         Status = "normal";
///         IconThemePath = Array.Empty&lt;string&gt;();
///     }
///
///     public override Connection Connection { get; }
///
///     protected override ValueTask&lt;(uint, (int, Dictionary&lt;string, VariantValue&gt;, VariantValue[]))&gt;
///         OnGetLayoutAsync(Message request, int parentId, int recursionDepth, string[] propertyNames)
///     {
///         // Build and return menu structure
///     }
///
///     protected override ValueTask OnEventAsync(Message request, int id, string eventId, VariantValue data, uint timestamp)
///     {
///         // Handle menu item clicks
///     }
/// }
/// </code>
/// </remarks>
public interface ITrayMenuHandler
{
    /// <summary>
    /// Gets the D-Bus connection used by this menu handler.
    /// Required for D-Bus communication.
    /// </summary>
    Connection Connection { get; }
}

using Microsoft.Extensions.Logging;
using Tmds.DBus.SourceGenerator;
using Tmds.DBus.Protocol;

namespace Olbrasoft.Linux.SystemTray.Internal;

/// <summary>
/// D-Bus handler for StatusNotifierItem interface.
/// Implements the server-side of the SNI protocol.
/// </summary>
internal class StatusNotifierItemHandler : OrgKdeStatusNotifierItemHandler
{
    private readonly ILogger _logger;

    public StatusNotifierItemHandler(Connection connection, ILogger logger, string menuPath, string id, string title)
    {
        Connection = connection;
        _logger = logger;

        // Set default values
        Category = "ApplicationStatus";
        Id = id;
        Title = title;
        Status = "Active";
        IconName = "";
        IconPixmap = Array.Empty<(int, int, byte[])>();
        OverlayIconName = "";
        OverlayIconPixmap = Array.Empty<(int, int, byte[])>();
        AttentionIconName = "";
        AttentionIconPixmap = Array.Empty<(int, int, byte[])>();
        AttentionMovieName = "";
        IconThemePath = "";
        Menu = new ObjectPath(menuPath);
        ItemIsMenu = false;
        ToolTip = ("", Array.Empty<(int, int, byte[])>(), "", "");
        WindowId = 0;
    }

    public override Connection Connection { get; }

    public event Action? ActivationDelegate;

    protected override ValueTask OnContextMenuAsync(Message message, int x, int y)
    {
        _logger.LogDebug("ContextMenu requested at ({X}, {Y})", x, y);
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnActivateAsync(Message message, int x, int y)
    {
        _logger.LogDebug("Activate requested at ({X}, {Y})", x, y);
        ActivationDelegate?.Invoke();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnSecondaryActivateAsync(Message message, int x, int y)
    {
        _logger.LogDebug("SecondaryActivate requested at ({X}, {Y})", x, y);
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnScrollAsync(Message message, int delta, string orientation)
    {
        _logger.LogDebug("Scroll: delta={Delta}, orientation={Orientation}", delta, orientation);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Sets the icon pixmap. This is the key method that bypasses icon caching.
    /// </summary>
    public void SetIcon((int, int, byte[]) dbusPixmap)
    {
        IconPixmap = new[] { dbusPixmap };
        IconName = ""; // Clear icon name to force pixmap usage
        Status = "Active";

        // Emit signals to notify the tray about the change
        EmitNewIcon();
        EmitNewStatus(Status);
    }

    /// <summary>
    /// Sets the icon pixmap for animation frames.
    /// Changes Id with timestamp to force GNOME Shell to invalidate its cache.
    /// </summary>
    public void SetAnimationFrame((int, int, byte[]) dbusPixmap, int frameIndex)
    {
        // Change Id with timestamp to bust GNOME's icon cache
        Id = $"{Id}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{frameIndex}";

        IconPixmap = new[] { dbusPixmap };
        IconName = "";
        Status = "Active";

        // Emit all signals to force refresh
        EmitNewTitle();
        EmitNewIcon();
    }

    /// <summary>
    /// Sets the attention icon pixmap for animation.
    /// Uses NeedsAttention status to force GNOME Shell to refresh the icon.
    /// </summary>
    public void SetAttentionIcon((int, int, byte[]) dbusPixmap)
    {
        AttentionIconPixmap = new[] { dbusPixmap };
        AttentionIconName = "";
        Status = "NeedsAttention";

        // Emit signals - NeedsAttention forces shell to use AttentionIconPixmap
        EmitNewAttentionIcon();
        EmitNewStatus(Status);
    }

    /// <summary>
    /// Sets the title and tooltip text.
    /// </summary>
    public void SetTitleAndTooltip(string text)
    {
        Title = text;
        ToolTip = ("", Array.Empty<(int, int, byte[])>(), text, "");

        EmitNewTitle();
        EmitNewToolTip();
    }

    /// <summary>
    /// Sets the status of the tray icon.
    /// </summary>
    public void SetStatus(string status)
    {
        Status = status;
        EmitNewStatus(status);
    }
}

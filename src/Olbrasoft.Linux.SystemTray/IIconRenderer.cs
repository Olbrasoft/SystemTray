namespace Olbrasoft.Linux.SystemTray;

/// <summary>
/// Handles rendering of SVG icons to ARGB pixmap data for D-Bus StatusNotifierItem.
/// </summary>
public interface IIconRenderer
{
    /// <summary>
    /// Renders an SVG icon to ARGB pixmap data.
    /// </summary>
    /// <param name="svgPath">Path to the SVG file.</param>
    /// <param name="size">Target size in pixels (width and height).</param>
    /// <returns>Rendered icon data (width, height, ARGB byte array).</returns>
    (int width, int height, byte[] argbData) RenderIcon(string svgPath, int size = 48);

    /// <summary>
    /// Pre-caches multiple icons for fast access during animations.
    /// </summary>
    /// <param name="svgPaths">Paths to SVG files to pre-cache.</param>
    /// <param name="size">Target size in pixels (width and height).</param>
    void PreCacheIcons(string[] svgPaths, int size = 48);

    /// <summary>
    /// Gets a cached icon, rendering it if not already cached.
    /// </summary>
    /// <param name="svgPath">Path to the SVG file.</param>
    /// <param name="size">Target size in pixels (width and height).</param>
    /// <returns>Cached icon data (width, height, ARGB byte array).</returns>
    (int width, int height, byte[] argbData) GetCachedIcon(string svgPath, int size = 48);

    /// <summary>
    /// Clears the icon cache.
    /// </summary>
    void ClearCache();
}

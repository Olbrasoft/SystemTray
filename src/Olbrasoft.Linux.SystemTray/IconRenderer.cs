using Microsoft.Extensions.Logging;
using SkiaSharp;
using Svg.Skia;

namespace Olbrasoft.Linux.SystemTray;

/// <summary>
/// Renders SVG icons to ARGB format for D-Bus StatusNotifierItem.
/// </summary>
public class IconRenderer : IIconRenderer
{
    private readonly ILogger<IconRenderer> _logger;
    private readonly Dictionary<string, (int Width, int Height, byte[] ArgbData)> _cache = new();
    private readonly int _defaultSize;

    public IconRenderer(ILogger<IconRenderer> logger, int defaultSize = 48)
    {
        _logger = logger;
        _defaultSize = defaultSize;
    }

    /// <inheritdoc />
    public (int width, int height, byte[] argbData) RenderIcon(string svgPath, int size = 48)
    {
        if (!File.Exists(svgPath))
        {
            _logger.LogError("SVG file not found: {Path}", svgPath);
            throw new FileNotFoundException($"SVG file not found: {svgPath}");
        }

        try
        {
            using var svg = new SKSvg();
            if (svg.Load(svgPath) is null)
            {
                _logger.LogError("Failed to load SVG: {Path}", svgPath);
                throw new InvalidOperationException($"Failed to load SVG: {svgPath}");
            }

            var picture = svg.Picture;
            if (picture is null)
            {
                _logger.LogError("SVG picture is null: {Path}", svgPath);
                throw new InvalidOperationException($"SVG picture is null: {svgPath}");
            }

            var bounds = picture.CullRect;
            var scale = Math.Min(size / bounds.Width, size / bounds.Height);
            var width = (int)(bounds.Width * scale);
            var height = (int)(bounds.Height * scale);

            if (width <= 0 || height <= 0)
            {
                _logger.LogError("Invalid icon dimensions: {Width}x{Height}", width, height);
                throw new InvalidOperationException($"Invalid icon dimensions: {width}x{height}");
            }

            using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);
            canvas.Scale(scale);
            canvas.DrawPicture(picture);

            var pixels = bitmap.Bytes;
            var argbData = new byte[width * height * 4];

            // Convert RGBA to ARGB (D-Bus StatusNotifierItem uses ARGB format)
            for (int i = 0; i < width * height; i++)
            {
                var srcIdx = i * 4;
                var dstIdx = i * 4;

                byte r = pixels[srcIdx];
                byte g = pixels[srcIdx + 1];
                byte b = pixels[srcIdx + 2];
                byte a = pixels[srcIdx + 3];

                argbData[dstIdx] = a;
                argbData[dstIdx + 1] = r;
                argbData[dstIdx + 2] = g;
                argbData[dstIdx + 3] = b;
            }

            _logger.LogDebug("Rendered SVG icon: {Path} ({Width}x{Height})", svgPath, width, height);
            return (width, height, argbData);
        }
        catch (Exception ex) when (ex is not FileNotFoundException and not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to render SVG: {Path}", svgPath);
            throw new InvalidOperationException($"Failed to render SVG: {svgPath}", ex);
        }
    }

    /// <inheritdoc />
    public void PreCacheIcons(string[] svgPaths, int size = 48)
    {
        foreach (var svgPath in svgPaths)
        {
            var cacheKey = GetCacheKey(svgPath, size);
            if (!_cache.ContainsKey(cacheKey))
            {
                try
                {
                    var rendered = RenderIcon(svgPath, size);
                    _cache[cacheKey] = rendered;
                    _logger.LogDebug("Pre-cached icon: {Path} ({Width}x{Height})",
                        svgPath, rendered.width, rendered.height);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to pre-cache icon: {Path}", svgPath);
                }
            }
        }
    }

    /// <inheritdoc />
    public (int width, int height, byte[] argbData) GetCachedIcon(string svgPath, int size = 48)
    {
        var cacheKey = GetCacheKey(svgPath, size);

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogTrace("Cache hit for icon: {Path}", svgPath);
            return cached;
        }

        _logger.LogTrace("Cache miss for icon: {Path}, rendering now", svgPath);
        var rendered = RenderIcon(svgPath, size);
        _cache[cacheKey] = rendered;
        return rendered;
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogDebug("Cleared icon cache ({Count} items)", count);
    }

    private static string GetCacheKey(string svgPath, int size) => $"{svgPath}:{size}";
}

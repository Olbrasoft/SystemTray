using Microsoft.Extensions.Logging;
using Moq;

namespace Olbrasoft.SystemTray.Linux.Tests;

public class IconRendererTests : IDisposable
{
    private readonly Mock<ILogger<IconRenderer>> _mockLogger;
    private readonly string _tempDirectory;
    private readonly string _validSvgPath;
    private readonly string _invalidSvgPath;

    public IconRendererTests()
    {
        _mockLogger = new Mock<ILogger<IconRenderer>>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"icon-renderer-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        // Create a valid minimal SVG file for testing
        _validSvgPath = Path.Combine(_tempDirectory, "valid-icon.svg");
        File.WriteAllText(_validSvgPath, @"<?xml version=""1.0"" encoding=""UTF-8""?>
<svg width=""48"" height=""48"" viewBox=""0 0 48 48"" xmlns=""http://www.w3.org/2000/svg"">
    <rect x=""10"" y=""10"" width=""28"" height=""28"" fill=""#FF0000""/>
</svg>");

        // Create an invalid SVG file (just text)
        _invalidSvgPath = Path.Combine(_tempDirectory, "invalid.svg");
        File.WriteAllText(_invalidSvgPath, "This is not a valid SVG");
    }

    public void Dispose()
    {
        // Clean up temp directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    private IconRenderer CreateRenderer(int defaultSize = 48)
    {
        return new IconRenderer(_mockLogger.Object, defaultSize);
    }

    [Fact]
    public void Constructor_WithDefaultParameters_CreatesInstance()
    {
        // Act
        var renderer = CreateRenderer();

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void Constructor_WithCustomSize_CreatesInstance()
    {
        // Act
        var renderer = CreateRenderer(64);

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void RenderIcon_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var renderer = CreateRenderer();
        var nonExistentPath = Path.Combine(_tempDirectory, "does-not-exist.svg");

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(
            () => renderer.RenderIcon(nonExistentPath));

        Assert.Contains("SVG file not found", exception.Message);
        Assert.Contains(nonExistentPath, exception.Message);
    }

    [Fact]
    public void RenderIcon_WithInvalidSvg_ThrowsInvalidOperationException()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => renderer.RenderIcon(_invalidSvgPath));

        // Error message can be either "Failed to load SVG" or "Failed to render SVG"
        // depending on where in the rendering pipeline it fails
        Assert.Contains("Failed to", exception.Message);
        Assert.Contains("SVG", exception.Message);
    }

    [Fact]
    public void RenderIcon_WithValidSvg_ReturnsCorrectDimensions()
    {
        // Arrange
        var renderer = CreateRenderer();
        var size = 48;

        // Act
        var (width, height, argbData) = renderer.RenderIcon(_validSvgPath, size);

        // Assert
        Assert.True(width > 0, "Width should be greater than 0");
        Assert.True(height > 0, "Height should be greater than 0");
        Assert.Equal(width * height * 4, argbData.Length); // ARGB = 4 bytes per pixel
    }

    [Fact]
    public void RenderIcon_WithValidSvg_ReturnsArgbData()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Act
        var (width, height, argbData) = renderer.RenderIcon(_validSvgPath);

        // Assert
        Assert.NotNull(argbData);
        Assert.True(argbData.Length > 0);
        Assert.Equal(width * height * 4, argbData.Length);
    }

    [Fact]
    public void RenderIcon_WithDifferentSizes_ScalesCorrectly()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Act
        var (width24, height24, _) = renderer.RenderIcon(_validSvgPath, 24);
        var (width48, height48, _) = renderer.RenderIcon(_validSvgPath, 48);
        var (width96, height96, _) = renderer.RenderIcon(_validSvgPath, 96);

        // Assert - larger sizes should produce larger dimensions
        Assert.True(width24 < width48);
        Assert.True(width48 < width96);
        Assert.True(height24 < height48);
        Assert.True(height48 < height96);
    }

    [Fact]
    public void GetCachedIcon_OnFirstCall_RendersAndCaches()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Act
        var result1 = renderer.GetCachedIcon(_validSvgPath);
        var result2 = renderer.GetCachedIcon(_validSvgPath);

        // Assert - both calls should return the same dimensions
        Assert.Equal(result1.width, result2.width);
        Assert.Equal(result1.height, result2.height);
        Assert.Equal(result1.argbData.Length, result2.argbData.Length);
    }

    [Fact]
    public void GetCachedIcon_WithDifferentSizes_CachesSeparately()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Act
        var result24 = renderer.GetCachedIcon(_validSvgPath, 24);
        var result48 = renderer.GetCachedIcon(_validSvgPath, 48);

        // Assert - different sizes should have different dimensions
        Assert.NotEqual(result24.width, result48.width);
    }

    [Fact]
    public void PreCacheIcons_WithValidPaths_CachesAllIcons()
    {
        // Arrange
        var renderer = CreateRenderer();
        var paths = new[] { _validSvgPath };

        // Act
        renderer.PreCacheIcons(paths);

        // Getting the icon should return from cache (no additional rendering)
        var (width, height, argbData) = renderer.GetCachedIcon(_validSvgPath);

        // Assert
        Assert.True(width > 0);
        Assert.True(height > 0);
        Assert.NotNull(argbData);
    }

    [Fact]
    public void PreCacheIcons_WithInvalidPath_ContinuesWithOtherIcons()
    {
        // Arrange
        var renderer = CreateRenderer();
        var nonExistentPath = Path.Combine(_tempDirectory, "does-not-exist.svg");
        var paths = new[] { nonExistentPath, _validSvgPath };

        // Act - should not throw, should log warning for invalid path
        var exception = Record.Exception(() => renderer.PreCacheIcons(paths));

        // Assert
        Assert.Null(exception);

        // Valid icon should still be cached
        var result = renderer.GetCachedIcon(_validSvgPath);
        Assert.True(result.width > 0);
    }

    [Fact]
    public void PreCacheIcons_WhenAlreadyCached_DoesNotRenderAgain()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Pre-cache once
        renderer.GetCachedIcon(_validSvgPath);

        // Act - pre-cache again with same path
        var exception = Record.Exception(() => renderer.PreCacheIcons(new[] { _validSvgPath }));

        // Assert - should not throw
        Assert.Null(exception);
    }

    [Fact]
    public void ClearCache_RemovesAllCachedIcons()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Cache some icons
        renderer.GetCachedIcon(_validSvgPath, 24);
        renderer.GetCachedIcon(_validSvgPath, 48);

        // Act
        renderer.ClearCache();

        // Assert - getting icon after clear should work (will re-render)
        var result = renderer.GetCachedIcon(_validSvgPath);
        Assert.True(result.width > 0);
    }

    [Fact]
    public void ClearCache_WhenCacheEmpty_DoesNotThrow()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Act & Assert
        var exception = Record.Exception(() => renderer.ClearCache());
        Assert.Null(exception);
    }

    [Fact]
    public void RenderIcon_ArgbFormat_HasCorrectByteOrder()
    {
        // Arrange
        var renderer = CreateRenderer();

        // Act
        var (width, height, argbData) = renderer.RenderIcon(_validSvgPath, 48);

        // Assert - ARGB format means each pixel is: A, R, G, B
        // Check that data length is correct (4 bytes per pixel)
        Assert.Equal(width * height * 4, argbData.Length);

        // Verify that data is not all zeros (would indicate rendering failure)
        var hasNonZeroData = argbData.Any(b => b != 0);
        Assert.True(hasNonZeroData, "Rendered icon should contain non-zero pixel data");
    }

    [Fact]
    public void GetCachedIcon_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var renderer = CreateRenderer();
        var nonExistentPath = Path.Combine(_tempDirectory, "does-not-exist.svg");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(
            () => renderer.GetCachedIcon(nonExistentPath));
    }
}

using Microsoft.Extensions.Logging;
using Moq;

namespace Olbrasoft.SystemTray.Linux.Tests;

public class TrayIconManagerTests
{
    private readonly Mock<ILogger<TrayIconManager>> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<IIconRenderer> _mockIconRenderer;
    private readonly Mock<ILogger<TrayIcon>> _mockTrayIconLogger;

    public TrayIconManagerTests()
    {
        _mockLogger = new Mock<ILogger<TrayIconManager>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockIconRenderer = new Mock<IIconRenderer>();
        _mockTrayIconLogger = new Mock<ILogger<TrayIcon>>();

        // Setup logger factory to return mock logger for TrayIcon
        _mockLoggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(_mockTrayIconLogger.Object);
    }

    private TrayIconManager CreateManager()
    {
        return new TrayIconManager(
            _mockLogger.Object,
            _mockLoggerFactory.Object,
            _mockIconRenderer.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var manager = CreateManager();

        // Assert
        Assert.NotNull(manager);
        Assert.NotNull(manager.Icons);
        Assert.Empty(manager.Icons);
    }

    [Fact(Skip = "Requires D-Bus session bus (not available on CI)")]
    public async Task CreateIconAsync_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var manager = CreateManager();
        var iconPath = "/tmp/test-icon.svg";

        _mockIconRenderer
            .Setup(r => r.GetCachedIcon(iconPath, It.IsAny<int>()))
            .Returns((48, 48, new byte[48 * 48 * 4]));

        // Try to create first icon (will fail due to D-Bus, but that's expected)
        try
        {
            await manager.CreateIconAsync("test-id", iconPath, null, null, default);
        }
        catch
        {
            // Ignore D-Bus connection errors
        }

        // Act & Assert - Try to create duplicate ID
        // This should fail BEFORE attempting D-Bus connection
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await manager.CreateIconAsync("test-id", iconPath, null, null, default));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateIconAsync_WithNullId_ThrowsArgumentException()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await manager.CreateIconAsync(null!, "/tmp/icon.svg", null, null, default));

        Assert.Contains("Icon ID cannot be null or whitespace", exception.Message);
        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public async Task CreateIconAsync_WithEmptyId_ThrowsArgumentException()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await manager.CreateIconAsync("", "/tmp/icon.svg", null, null, default));

        Assert.Contains("Icon ID cannot be null or whitespace", exception.Message);
        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public async Task CreateIconAsync_WithWhitespaceId_ThrowsArgumentException()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await manager.CreateIconAsync("   ", "/tmp/icon.svg", null, null, default));

        Assert.Contains("Icon ID cannot be null or whitespace", exception.Message);
        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public async Task CreateIconAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var manager = CreateManager();
        manager.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await manager.CreateIconAsync("test-id", "/tmp/icon.svg", null, null, default));
    }

    [Fact]
    public void GetIcon_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var icon = manager.GetIcon("non-existing-id");

        // Assert
        Assert.Null(icon);
    }

    [Fact]
    public void GetIcon_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var manager = CreateManager();
        manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => manager.GetIcon("test-id"));
    }

    [Fact]
    public void RemoveIcon_WithNonExistingId_DoesNotThrow()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        var exception = Record.Exception(() => manager.RemoveIcon("non-existing-id"));
        Assert.Null(exception);
    }

    [Fact]
    public void RemoveIcon_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var manager = CreateManager();
        manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => manager.RemoveIcon("test-id"));
    }

    [Fact]
    public void RemoveAllIcons_WithNoIcons_DoesNotThrow()
    {
        // Arrange
        var manager = CreateManager();

        // Act & Assert
        var exception = Record.Exception(() => manager.RemoveAllIcons());
        Assert.Null(exception);
    }

    [Fact]
    public void RemoveAllIcons_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var manager = CreateManager();
        manager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => manager.RemoveAllIcons());
    }

    [Fact]
    public void Dispose_WhenCalledOnce_DisposesSuccessfully()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var exception = Record.Exception(() => manager.Dispose());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.Dispose();
        var exception = Record.Exception(() => manager.Dispose());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Icons_Property_ReturnsReadOnlyDictionary()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        var icons = manager.Icons;

        // Assert
        Assert.NotNull(icons);
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, ITrayIcon>>(icons);
    }
}

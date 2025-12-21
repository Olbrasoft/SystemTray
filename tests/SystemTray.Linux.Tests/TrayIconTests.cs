using Microsoft.Extensions.Logging;
using Moq;

namespace Olbrasoft.SystemTray.Linux.Tests;

public class TrayIconTests
{
    private readonly Mock<ILogger<TrayIcon>> _mockLogger;
    private readonly Mock<IIconRenderer> _mockIconRenderer;
    private readonly string _testId = "test-icon-id";

    public TrayIconTests()
    {
        _mockLogger = new Mock<ILogger<TrayIcon>>();
        _mockIconRenderer = new Mock<IIconRenderer>();

        // Setup default icon renderer behavior
        _mockIconRenderer
            .Setup(r => r.GetCachedIcon(It.IsAny<string>(), It.IsAny<int>()))
            .Returns((48, 48, new byte[48 * 48 * 4]));
    }

    private TrayIcon CreateTrayIcon(string? id = null, ITrayMenuHandler? menuHandler = null)
    {
        return new TrayIcon(
            _mockLogger.Object,
            _mockIconRenderer.Object,
            id ?? _testId,
            menuHandler);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var trayIcon = CreateTrayIcon();

        // Assert
        Assert.NotNull(trayIcon);
    }

    [Fact]
    public void Id_Property_ReturnsCorrectId()
    {
        // Arrange
        var expectedId = "my-custom-id";

        // Act
        var trayIcon = CreateTrayIcon(expectedId);

        // Assert
        Assert.Equal(expectedId, trayIcon.Id);
    }

    [Fact]
    public void IsAnimating_BeforeStartAnimation_ReturnsFalse()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();

        // Act & Assert
        Assert.False(trayIcon.IsAnimating);
    }

    [Fact]
    public void IsVisible_BeforeInitialize_ReturnsFalse()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();

        // Act & Assert
        Assert.False(trayIcon.IsVisible);
    }

    [Fact]
    public void SetIcon_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();
        trayIcon.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            trayIcon.SetIcon("/tmp/icon.svg"));
    }

    [Fact]
    public void StartAnimation_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();
        trayIcon.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            trayIcon.StartAnimation(new[] { "/tmp/icon1.svg", "/tmp/icon2.svg" }));
    }

    [Fact]
    public void StartAnimation_WithEmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            trayIcon.StartAnimation(Array.Empty<string>()));

        Assert.Contains("Icon paths array cannot be empty", exception.Message);
        Assert.Equal("iconPaths", exception.ParamName);
    }

    [Fact]
    public void StopAnimation_BeforeStartingAnimation_DoesNotThrow()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();

        // Act & Assert
        var exception = Record.Exception(() => trayIcon.StopAnimation());
        Assert.Null(exception);
    }

    [Fact]
    public void Hide_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();
        trayIcon.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => trayIcon.Hide());
    }

    [Fact]
    public void Show_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();
        trayIcon.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => trayIcon.Show());
    }

    [Fact]
    public void SetMenu_Always_ThrowsInvalidOperationException()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();
        var mockMenu = new Mock<ITrayMenu>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            trayIcon.SetMenu(mockMenu.Object));

        Assert.Contains("Menu must be set during TrayIcon construction", exception.Message);
        Assert.Contains("ITrayMenuHandler", exception.Message);
    }

    [Fact]
    public void Dispose_WhenCalledOnce_DisposesSuccessfully()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();

        // Act
        var exception = Record.Exception(() => trayIcon.Dispose());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();

        // Act
        trayIcon.Dispose();
        var exception = Record.Exception(() => trayIcon.Dispose());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_SetsIsVisibleToFalse()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();

        // Act
        trayIcon.Dispose();

        // Assert
        Assert.False(trayIcon.IsVisible);
    }

    [Fact]
    public void Clicked_Event_CanBeSubscribed()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();
        var eventRaised = false;

        // Act
        trayIcon.Clicked += (sender, args) => eventRaised = true;

        // Assert - just verify subscription doesn't throw
        Assert.False(eventRaised); // Event not raised yet
    }

    [Fact]
    public async Task InitializeAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();
        trayIcon.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await trayIcon.InitializeAsync());
    }

    [Fact]
    public void Constructor_WithMenuHandler_StoresMenuHandler()
    {
        // Arrange
        var mockMenuHandler = new Mock<ITrayMenuHandler>();

        // Act
        var trayIcon = CreateTrayIcon(menuHandler: mockMenuHandler.Object);

        // Assert - verify it was created without throwing
        Assert.NotNull(trayIcon);
        Assert.Equal(_testId, trayIcon.Id);
    }

    [Fact]
    public void StartAnimation_WithValidPaths_PreCachesIcons()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();
        var iconPaths = new[] { "/tmp/icon1.svg", "/tmp/icon2.svg", "/tmp/icon3.svg" };

        // Act
        var exception = Record.Exception(() =>
            trayIcon.StartAnimation(iconPaths, intervalMs: 100));

        // Assert - should call PreCacheIcons on renderer
        // Note: This will throw because D-Bus is not available, but we can verify PreCacheIcons was attempted
        _mockIconRenderer.Verify(
            r => r.PreCacheIcons(iconPaths, It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public void StopAnimation_AfterDispose_DoesNotThrow()
    {
        // Arrange
        var trayIcon = CreateTrayIcon();
        trayIcon.Dispose();

        // Act & Assert - StopAnimation should be safe to call even when disposed
        var exception = Record.Exception(() => trayIcon.StopAnimation());
        Assert.Null(exception);
    }
}

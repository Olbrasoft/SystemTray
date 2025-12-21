# Olbrasoft.Linux.SystemTray

A modern .NET library for creating system tray icons on Linux using the D-Bus StatusNotifierItem protocol.

## Features

- ✅ **Clean C# API** - Hides D-Bus complexity behind simple interfaces
- ✅ **Dynamic Icon Changes** - Update icons on-the-fly
- ✅ **Animations** - Frame-based icon animations with customizable intervals
- ✅ **Multiple Icons** - Display multiple tray icons simultaneously (like GestureEvolution's 3-hand approach)
- ✅ **SVG Support** - Renders SVG icons to ARGB pixmaps using SkiaSharp
- ✅ **Cache Busting** - Workarounds for GNOME Shell icon caching issues
- ✅ **.NET 10** - Built for the latest .NET

## Installation

```bash
dotnet add package Olbrasoft.Linux.SystemTray
```

## Quick Start

### Single Icon

```csharp
using Olbrasoft.Linux.SystemTray;
using Microsoft.Extensions.Logging;

// Create logger and icon renderer
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<TrayIcon>();
var iconRenderer = new IconRenderer(loggerFactory.CreateLogger<IconRenderer>());

// Create tray icon
var trayIcon = new TrayIcon(logger, iconRenderer, "my-app");
await trayIcon.InitializeAsync();

// Set icon
trayIcon.SetIcon("/path/to/icon.svg", "My Application");

// Handle clicks
trayIcon.Clicked += (sender, args) => Console.WriteLine("Icon clicked!");

// Cleanup
trayIcon.Dispose();
```

### Animated Icon

```csharp
// Pre-cache animation frames
string[] frames = new[]
{
    "/path/to/frame1.svg",
    "/path/to/frame2.svg",
    "/path/to/frame3.svg"
};

// Start animation (150ms interval)
trayIcon.StartAnimation(frames, intervalMs: 150, tooltip: "Working...");

// Stop animation
trayIcon.StopAnimation();
```

### Multiple Icons

```csharp
var manager = new TrayIconManager(
    loggerFactory.CreateLogger<TrayIconManager>(),
    loggerFactory,
    iconRenderer
);

// Create left hand icon
var leftIcon = await manager.CreateIconAsync(
    "left-hand",
    "/icons/left-hand.svg",
    "Left Hand Gesture"
);

// Create robot icon
var robotIcon = await manager.CreateIconAsync(
    "robot",
    "/icons/robot.svg",
    "Main App"
);

// Create right hand icon
var rightIcon = await manager.CreateIconAsync(
    "right-hand",
    "/icons/right-hand.svg",
    "Right Hand Gesture"
);

// Later: remove specific icon
manager.RemoveIcon("left-hand");

// Cleanup all icons
manager.Dispose();
```

## Architecture

This library is based on the D-Bus StatusNotifierItem protocol, which is the modern standard for system tray icons on Linux (replacing the deprecated X11 system tray).

### Key Components

- **ITrayIcon** - Main interface for single tray icon
- **ITrayIconManager** - Manager for multiple simultaneous icons
- **IIconRenderer** - SVG to ARGB rendering with caching
- **StatusNotifierItemHandler** - Internal D-Bus protocol implementation

### D-Bus Workarounds

The library implements several workarounds for GNOME Shell icon caching issues:

1. **Timestamp in Icon ID** - Changes icon ID with timestamp during animations
2. **Signal Re-emission** - Re-emits D-Bus signals after delays for slow shells
3. **Unique Connection Names** - Uses D-Bus unique names to avoid duplicate detection

## Requirements

- **.NET 10** or later
- **Linux** with D-Bus session bus
- **StatusNotifierWatcher** service (provided by GNOME Shell, KDE Plasma, or standalone)

## Dependencies

- `Tmds.DBus.Protocol` - D-Bus communication
- `SkiaSharp` - SVG rendering
- `Svg.Skia` - SVG loading
- `Microsoft.Extensions.Logging` - Logging abstraction

## Background

This library was extracted from three Olbrasoft projects:

1. **PushToTalk** - Advanced D-Bus implementation with cache busting
2. **VirtualAssistant** - GTK/AppIndicator approach
3. **GestureEvolution** - Multiple simultaneous icons (3 hands/robot)

The goal was to create a reusable NuGet package that other applications can easily integrate without copying D-Bus boilerplate code.

## License

MIT

## Contributing

Contributions are welcome! Please open an issue or pull request on GitHub.

## Credits

Based on Avalonia's DBusTrayIconImpl:
- https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.FreeDesktop/DBusTrayIconImpl.cs

Developed by Olbrasoft (https://olbrasoft.cz)

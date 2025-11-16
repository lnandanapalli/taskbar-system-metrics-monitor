# Taskbar System Monitor

A lightweight, real-time system monitoring application for Windows that displays CPU, RAM, disk, network, and GPU usage graphs directly on your taskbar, similar to the system monitors found in Parrot OS and Kali Linux.

## Features

- **Real-time Monitoring**: Live graphs for CPU, RAM, disk, network, and GPU usage
- **Taskbar Integration**: Displays seamlessly on the Windows taskbar, positioned to the left of the system tray
- **Transparent Overlay**: Semi-transparent background that doesn't interfere with other taskbar elements
- **Historical Data**: Shows the last 60 seconds of data with smooth line graphs
- **Color-coded Metrics**: Each metric has its own color for easy identification:
  - 🔴 CPU Usage (Pink/Red)
  - 🟢 RAM Usage (Green)
  - 🟣 Disk Activity (Purple)
  - 🟡 Network Activity (Yellow)
  - 🔵 GPU Usage (Blue)
- **System Tray Integration**: Right-click menu with settings and quick access to system tools
- **Auto-startup**: Option to start with Windows
- **Lightweight**: Minimal resource usage

## System Requirements

- Windows 10 or later
- .NET 8.0 Runtime
- Administrator privileges (recommended for full functionality)

## Installation

1. Download the latest release from the releases page
2. Extract the files to a folder of your choice
3. Run `TaskbarSystemMonitor.exe`
4. Right-click the system tray icon and select "Start with Windows" to enable auto-startup

## Building from Source

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

### Build Instructions

```bash
# Clone the repository
git clone https://github.com/yourusername/taskbarmonitor.git
cd taskbarmonitor

# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Run the application
dotnet run
```

### Publishing as Single Executable

```bash
# Publish as a single executable
dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

## Usage

1. **Launch**: Run the application - it will automatically position itself on your taskbar
2. **View Metrics**: The overlay shows real-time graphs for all system metrics
3. **Settings**: Right-click the system tray icon to access settings and options
4. **Exit**: Right-click the system tray icon and select "Exit"

### Tray Icon Menu Options

- **Settings**: Configure visibility options and update intervals
- **Task Manager**: Quick access to Windows Task Manager
- **Resource Monitor**: Quick access to Windows Resource Monitor
- **Start with Windows**: Toggle auto-startup functionality
- **About**: Information about the application
- **Exit**: Close the application

## Configuration

The application includes a settings dialog accessible from the system tray menu:

- **Metrics Visibility**: Toggle individual metrics on/off
- **Update Interval**: Adjust how frequently data is updated (500ms - 5000ms)
- **Opacity**: Control the transparency of the overlay (50% - 100%)
- **Startup**: Enable/disable starting with Windows

## Technical Details

### Performance Counters Used

- **CPU**: `Processor % Processor Time _Total`
- **RAM**: `Memory Available MBytes`
- **Disk**: `PhysicalDisk Disk Read/Write Bytes/sec _Total`
- **Network**: `Network Interface Bytes Sent/Received/sec`
- **GPU**: GPU-specific performance counters (when available)

### Windows API Integration

- Uses Windows API for taskbar positioning
- Implements transparent overlay with proper Z-order
- Maintains position relative to system tray

## Troubleshooting

### Common Issues

1. **Application not visible**: Check if it's running in the system tray
2. **Positioning issues**: The app auto-adjusts position every 5 seconds
3. **Missing GPU data**: GPU monitoring requires compatible graphics drivers
4. **Performance counters not available**: Run as administrator for full access

### Performance Considerations

- The application uses approximately 10-20MB of RAM
- CPU usage is typically < 1%
- Update interval can be adjusted to reduce resource usage

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Areas for Improvement

- Additional metrics (temperature, fan speeds)
- Customizable themes and colors
- More positioning options
- Configuration file support
- Multi-monitor support

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by system monitors in Parrot OS and Kali Linux
- Built with Windows Forms and .NET
- Uses Windows Performance Counters for accurate system metrics

## Support

If you encounter any issues or have suggestions, please create an issue on the GitHub repository.

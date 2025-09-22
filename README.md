# Process Killer
<img width="1125" height="609" alt="image" src="https://github.com/user-attachments/assets/fdebee72-b51c-43dd-8050-984e1b36cd3e" />
<img width="256" height="135" alt="image" src="https://github.com/user-attachments/assets/f202429d-e335-4522-884a-28e93038e5b0" />


A simple Windows utility that allows you to quickly terminate processes using hotkeys or a system tray menu. Features a yellow border highlight to show which window will be closed.

## Features

- **Global Hotkey**: Press F12 (or Ctrl+F12) to kill the active window
- **Visual Confirmation**: Yellow border appears around the target window
- **System Tray Integration**: Right-click menu for manual process termination
- **Safety Protection**: Prevents killing critical system processes
- **Lightweight**: Runs quietly in the background

## Building the Application

### Prerequisites
- Visual Studio 2019 or later
- Target framework .NET 10.0
- Windows 10/11

### Build Steps

1. **Clone or download** this repository
2. **Open** `Process Killer.sln` in Visual Studio
3. **Set build configuration** to `Release`
4. **Build** → **Build Solution** (or press `Ctrl+Shift+B`)
5. **Find the executable** in `bin\Release\` folder

## Running the Application

### ⚠️ Important: Run as Administrator

**This application requires Administrator privileges** to:
- Register global hotkeys
- Terminate other processes
- Access process information

**To run as Administrator:**
1. Right-click `ProcessKiller.exe`
2. Select **"Run as administrator"**
3. Click **Yes** when prompted by UAC

### Usage

1. **Start the application** - it will minimize to the system tray
2. **Press F12** while any window is active to kill that process
3. **Alternative**: Right-click the tray icon → "Kill Active Window"
4. **Confirm** in the dialog that appears (the target window will be highlighted in yellow)

## Hotkeys

- **F12**: Kill active window process
- **Ctrl+F12**: Alternative hotkey (if F12 is taken by another app)

## Safety Features

The application prevents termination of critical system processes:
- explorer.exe
- winlogon.exe
- csrss.exe
- services.exe
- dwm.exe
- lsass.exe

## System Tray Menu

- **Kill Active Window**: Manually kill the currently focused window
- **Test Window Detection**: Debug tool to verify window detection
- **Open Task Manager**: Quick access to Windows Task Manager
- **Exit**: Close the application

## Troubleshooting

**Hotkey not working?**
- Ensure you're running as Administrator
- Check if another application is using the same hotkey
- Try the alternative Ctrl+F12 combination

**Can't kill a process?**
- Some processes require elevated privileges
- Protected system processes cannot be terminated
- Try running as Administrator

**Yellow border not appearing?**
- Window might be minimized or behind other windows
- Some fullscreen applications may not show the border

## Disclaimer

Use responsibly. Terminating processes can cause data loss if applications have unsaved work.

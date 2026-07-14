<p align="center">
  <img src="assets/icon.png" alt="App Volume Keys icon" width="96" height="96">
</p>

<h1 align="center">App Volume Keys</h1>

<p align="center">
  A tiny Windows tray app for controlling the volume of the app you are currently using.
</p>

<p align="center">
  <a href="releases/latest/download/AppVolumeKeys.exe">
    <img alt="Download AppVolumeKeys.exe" src="https://img.shields.io/badge/Download-AppVolumeKeys.exe-72a4f2?style=for-the-badge&logo=windows">
  </a>
  <a href="https://ko-fi.com/S6S1CPXYA">
    <img alt="Support on Ko-fi" src="https://img.shields.io/badge/Support_on-Ko--fi-ff5f5f?style=for-the-badge">
  </a>
</p>

---

## Why this exists

Windows lets you control system volume quickly, but controlling the volume of the foreground app is still awkward. App Volume Keys keeps that simple: choose two keys, keep the app in the tray, and adjust the volume of whatever app you are actively using.

By default:

- `Page Up` raises the current app's volume.
- `Page Down` lowers the current app's volume.

You can change both keys from the app's settings window.

## Features

- Assign custom keys for app volume up and app volume down.
- Controls the foreground app's audio session when Windows exposes one.
- Falls back to system volume when the active app does not have its own audio session.
- Runs quietly in the system tray.
- Left-click the tray icon to enable or disable the utility.
- Quickly press physical `Volume Up`, then `Volume Down` to disable it.
- Optional Start with Windows setting.
- Lightweight single-file Windows executable.

## Download

Get the latest Windows build here:

**[Download AppVolumeKeys.exe](releases/latest/download/AppVolumeKeys.exe)**

Windows may show an "unknown publisher" warning because the app is not code-signed.

## How to use

1. Launch `AppVolumeKeys.exe`.
2. Click `Volume up key` or `Volume down key`.
3. Press the key you want to assign.
4. Leave the app running in the tray.

Settings are saved automatically.

## Build from source

App Volume Keys is a small C# WinForms app targeting .NET Framework 4.8.

From the repository root:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /optimize+ /win32icon:AppVolumeKeys.ico /out:AppVolumeKeys.exe /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll Program.cs
```

## Settings location

User settings are stored at:

```text
%APPDATA%\AppVolumeKeys\settings.ini
```

The Start with Windows option uses the current user's Windows Run registry key.

## Support the project

If App Volume Keys saves you a little friction every day, support is appreciated. Donations help cover the time spent polishing, testing, and maintaining small utilities like this.

No pressure, no locked features, and no subscriptions. The app stays free either way.

<p>
  <a href="https://ko-fi.com/S6S1CPXYA">
    <img alt="Support me on Ko-fi" src="https://storage.ko-fi.com/cdn/kofi2.png?v=3" height="36">
  </a>
</p>

## License

MIT License. See [LICENSE](LICENSE).

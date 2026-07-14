# App Volume Keys

Tiny Windows tray utility for controlling the foreground app's volume with keys you choose.

[![Download AppVolumeKeys.exe](https://img.shields.io/badge/Download-AppVolumeKeys.exe-72a4f2?style=for-the-badge&logo=windows)](releases/latest/download/AppVolumeKeys.exe)
[![Support me on Ko-fi](https://img.shields.io/badge/Support_me_on-Ko--fi-72a4f2?style=for-the-badge)](https://ko-fi.com/S6S1CPXYA)

## Features

- Assign your own volume up and volume down keys.
- Defaults to `Page Up` for volume up and `Page Down` for volume down.
- Controls the current foreground app when possible.
- Falls back to system volume if the foreground app has no audio session.
- Runs in the Windows tray.
- Left-click the tray icon to enable or disable the utility.
- Quickly press physical `Volume Up`, then `Volume Down` to disable it.
- Optional Start with Windows setting.

## Download

Download the latest build from the releases page:

**[Download AppVolumeKeys.exe](releases/latest/download/AppVolumeKeys.exe)**

Windows may show an "unknown publisher" warning because the exe is not code-signed.

## Build From Source

This project is a lightweight C# WinForms app targeting .NET Framework 4.8.

From the repository root:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /optimize+ /win32icon:AppVolumeKeys.ico /out:AppVolumeKeys.exe /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll Program.cs
```

## Settings

Settings are saved to:

```text
%APPDATA%\AppVolumeKeys\settings.ini
```

Startup is configured under the current user's Windows Run registry key.

## Support

If this app helps you, you can support the developer on Ko-fi:

[Support me on Ko-fi](https://ko-fi.com/S6S1CPXYA)

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AppVolumeKeys
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var app = new MainForm())
            {
                Application.Run(app);
            }
        }
    }

    sealed class MainForm : Form
    {
        readonly Settings settings = Settings.Load();
        readonly KeyboardHook hook = new KeyboardHook();
        readonly NotifyIcon tray = new NotifyIcon();
        readonly Label status = new Label();
        readonly Button downButton = new Button();
        readonly Button upButton = new Button();
        readonly Button donateButton = new Button();
        readonly CheckBox enabledBox = new CheckBox();
        readonly CheckBox startupBox = new CheckBox();
        readonly NumericUpDown stepBox = new NumericUpDown();
        bool capturingDownKey;
        bool capturingUpKey;
        DateTime lastVolumeUp = DateTime.MinValue;
        readonly Icon appIcon = AppIcon.Create();

        public MainForm()
        {
            Text = "App Volume Keys";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(430, 430);
            Font = new Font("Segoe UI", 9F);
            Icon = appIcon;
            BackColor = Color.FromArgb(246, 248, 252);

            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(430, 78),
                BackColor = Color.White
            };

            var iconBox = new PictureBox
            {
                Location = new Point(20, 19),
                Size = new Size(40, 40),
                Image = appIcon.ToBitmap(),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            var title = new Label
            {
                Text = "App Volume Keys",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 33, 52),
                Location = new Point(72, 16)
            };

            var help = new Label
            {
                Text = "Assign keys that control the foreground app volume.",
                AutoSize = true,
                ForeColor = Color.FromArgb(91, 101, 121),
                Location = new Point(75, 45)
            };
            header.Controls.AddRange(new Control[] { iconBox, title, help });

            var keysBox = new GroupBox
            {
                Text = "App volume keys",
                Location = new Point(18, 94),
                Size = new Size(394, 130),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(24, 33, 52)
            };

            var upLabel = new Label { Text = "Volume up key", AutoSize = true, Location = new Point(18, 34), ForeColor = Color.FromArgb(60, 70, 90) };
            upButton.Location = new Point(250, 27);
            upButton.Size = new Size(120, 32);
            upButton.Click += delegate { BeginCapture(false); };

            var downLabel = new Label { Text = "Volume down key", AutoSize = true, Location = new Point(18, 74), ForeColor = Color.FromArgb(60, 70, 90) };
            downButton.Location = new Point(250, 67);
            downButton.Size = new Size(120, 32);
            downButton.Click += delegate { BeginCapture(true); };

            keysBox.Controls.AddRange(new Control[] { upLabel, upButton, downLabel, downButton });

            var behaviorBox = new GroupBox
            {
                Text = "Settings",
                Location = new Point(18, 240),
                Size = new Size(394, 108),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(24, 33, 52)
            };

            var stepLabel = new Label { Text = "Volume step", AutoSize = true, Location = new Point(18, 32), ForeColor = Color.FromArgb(60, 70, 90) };
            stepBox.Location = new Point(125, 28);
            stepBox.Size = new Size(60, 24);
            stepBox.Minimum = 1;
            stepBox.Maximum = 25;
            stepBox.Value = settings.StepPercent;
            var stepSuffix = new Label { Text = "%", AutoSize = true, Location = new Point(190, 32), ForeColor = Color.FromArgb(60, 70, 90) };
            stepBox.ValueChanged += delegate
            {
                settings.StepPercent = (int)stepBox.Value;
                settings.Save();
            };

            enabledBox.Text = "Enabled";
            enabledBox.AutoSize = true;
            enabledBox.Location = new Point(250, 31);
            enabledBox.Checked = settings.Enabled;
            enabledBox.CheckedChanged += delegate { SetEnabled(enabledBox.Checked, true); };

            startupBox.Text = "Start with Windows";
            startupBox.AutoSize = true;
            startupBox.Location = new Point(18, 68);
            if (!settings.StartupInitialized)
            {
                Startup.SetEnabled(true);
                settings.StartupInitialized = true;
                settings.Save();
            }
            startupBox.Checked = Startup.IsEnabled();
            startupBox.CheckedChanged += delegate
            {
                Startup.SetEnabled(startupBox.Checked);
                SetStatus(startupBox.Checked ? "Will start with Windows." : "Will not start with Windows.");
            };

            behaviorBox.Controls.AddRange(new Control[] { stepLabel, stepBox, stepSuffix, enabledBox, startupBox });

            donateButton.Text = "Support me on Ko-fi";
            donateButton.Location = new Point(18, 376);
            donateButton.Size = new Size(394, 38);
            donateButton.BackColor = ColorTranslator.FromHtml("#72a4f2");
            donateButton.ForeColor = Color.White;
            donateButton.FlatStyle = FlatStyle.Flat;
            donateButton.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            donateButton.FlatAppearance.BorderSize = 0;
            donateButton.Click += delegate { OpenUrl("https://ko-fi.com/S6S1CPXYA"); };

            status.AutoSize = false;
            status.Location = new Point(18, 354);
            status.Size = new Size(394, 18);
            status.ForeColor = SystemColors.GrayText;

            StyleButton(downButton);
            StyleButton(upButton);
            Controls.AddRange(new Control[] { header, keysBox, behaviorBox, status, donateButton });

            var menu = new ContextMenuStrip();
            menu.Items.Add("Toggle enabled", null, delegate { ToggleEnabled(); });
            menu.Items.Add("Settings", null, delegate { ShowWindow(); });
            menu.Items.Add("Exit", null, delegate { Close(); });
            tray.Text = "App Volume Keys";
            tray.Icon = appIcon;
            tray.ContextMenuStrip = menu;
            tray.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) ToggleEnabled();
            };
            tray.Visible = true;

            hook.KeyPressed += OnGlobalKey;
            hook.Install();
            UpdateButtons();
            SetEnabled(settings.Enabled, false);
            SetStatus("Tray click toggles. Physical Volume Up then Volume Down disables.");
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            hook.Dispose();
            tray.Visible = false;
            tray.Dispose();
            base.OnFormClosing(e);
        }

        void ShowWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        void BeginCapture(bool downKey)
        {
            capturingDownKey = downKey;
            capturingUpKey = !downKey;
            SetStatus("Press a replacement key. Esc cancels.");
        }

        void OnGlobalKey(object sender, KeyEventArgs e)
        {
            if (capturingDownKey || capturingUpKey)
            {
                if (e.KeyCode != Keys.Escape)
                {
                    if (capturingDownKey)
                    {
                        settings.DownKey = e.KeyCode;
                    }
                    else
                    {
                        settings.UpKey = e.KeyCode;
                    }
                    settings.Save();
                    UpdateButtons();
                    SetStatus("Saved.");
                }
                else
                {
                    SetStatus("Canceled.");
                }
                capturingDownKey = false;
                capturingUpKey = false;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.VolumeUp)
            {
                lastVolumeUp = DateTime.UtcNow;
            }
            else if (e.KeyCode == Keys.VolumeDown && (DateTime.UtcNow - lastVolumeUp).TotalMilliseconds <= 750)
            {
                SetEnabled(false, true);
                SetStatus("Disabled by Volume Up then Volume Down.");
                e.SuppressKeyPress = true;
                return;
            }

            if (!settings.Enabled)
            {
                return;
            }

            if (e.KeyCode == settings.DownKey)
            {
                Adjust(-settings.StepPercent);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == settings.UpKey)
            {
                Adjust(settings.StepPercent);
                e.SuppressKeyPress = true;
            }
        }

        void Adjust(int deltaPercent)
        {
            string target;
            bool ok = Audio.AdjustForegroundAppVolume(deltaPercent / 100f, out target);
            if (!ok)
            {
                ok = Audio.AdjustSystemVolume(deltaPercent / 100f, out target);
            }
            SetStatus(ok ? "Adjusted " + target + " by " + deltaPercent + "%." : "Could not adjust volume.");
        }

        void UpdateButtons()
        {
            downButton.Text = FriendlyKey(settings.DownKey);
            upButton.Text = FriendlyKey(settings.UpKey);
        }

        void ToggleEnabled()
        {
            SetEnabled(!settings.Enabled, true);
            SetStatus(settings.Enabled ? "Enabled." : "Disabled.");
        }

        void SetEnabled(bool enabled, bool save)
        {
            settings.Enabled = enabled;
            if (enabledBox.Checked != enabled) enabledBox.Checked = enabled;
            tray.Text = "App Volume Keys - " + (enabled ? "enabled" : "disabled");
            tray.Icon = enabled ? appIcon : SystemIcons.Warning;
            if (save) settings.Save();
        }

        void SetStatus(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetStatus), text);
                return;
            }
            status.Text = text;
        }

        static string FriendlyKey(Keys key)
        {
            if (key == Keys.PageDown) return "Page Down";
            if (key == Keys.PageUp) return "Page Up";
            return key.ToString();
        }

        static void StyleButton(Button button)
        {
            button.BackColor = Color.FromArgb(235, 241, 252);
            button.ForeColor = Color.FromArgb(30, 74, 135);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(200, 216, 240);
            button.FlatAppearance.BorderSize = 1;
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        }

        static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
    }

    static class AppIcon
    {
        public static Icon Create()
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            using (var blue = new SolidBrush(Color.FromArgb(114, 164, 242)))
            using (var dark = new SolidBrush(Color.FromArgb(27, 38, 59)))
            using (var white = new SolidBrush(Color.White))
            using (var pen = new Pen(Color.White, 2))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                g.FillEllipse(blue, 2, 2, 28, 28);
                g.FillPolygon(dark, new[]
                {
                    new Point(8, 13),
                    new Point(13, 13),
                    new Point(19, 8),
                    new Point(19, 24),
                    new Point(13, 19),
                    new Point(8, 19)
                });
                g.DrawArc(pen, 19, 10, 7, 12, -45, 90);
                g.FillEllipse(white, 23, 6, 4, 4);
                g.FillEllipse(white, 23, 22, 4, 4);
            }
            IntPtr handle = bitmap.GetHicon();
            return (Icon)Icon.FromHandle(handle).Clone();
        }
    }

    sealed class Settings
    {
        public Keys DownKey = Keys.PageDown;
        public Keys UpKey = Keys.PageUp;
        public int StepPercent = 5;
        public bool Enabled = true;
        public bool StartupInitialized;

        static string DirectoryPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AppVolumeKeys"); }
        }

        static string FilePath
        {
            get { return Path.Combine(DirectoryPath, "settings.ini"); }
        }

        public static Settings Load()
        {
            var settings = new Settings();
            try
            {
                if (!File.Exists(FilePath)) return settings;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2) continue;
                    if (parts[0] == "DownKey") settings.DownKey = (Keys)Enum.Parse(typeof(Keys), parts[1]);
                    if (parts[0] == "UpKey") settings.UpKey = (Keys)Enum.Parse(typeof(Keys), parts[1]);
                    if (parts[0] == "Enabled") settings.Enabled = string.Equals(parts[1], "true", StringComparison.OrdinalIgnoreCase);
                    if (parts[0] == "StartupInitialized") settings.StartupInitialized = string.Equals(parts[1], "true", StringComparison.OrdinalIgnoreCase);
                    if (parts[0] == "StepPercent")
                    {
                        int value;
                        if (int.TryParse(parts[1], out value)) settings.StepPercent = Math.Max(1, Math.Min(25, value));
                    }
                }
            }
            catch
            {
                return new Settings();
            }
            return settings;
        }

        public void Save()
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllLines(FilePath, new[]
            {
                "DownKey=" + DownKey,
                "UpKey=" + UpKey,
                "Enabled=" + Enabled,
                "StartupInitialized=" + StartupInitialized,
                "StepPercent=" + StepPercent
            });
        }
    }

    static class Startup
    {
        const string AppName = "AppVolumeKeys";
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsEnabled()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    return key != null && key.GetValue(AppName) != null;
                }
            }
            catch { return false; }
        }

        public static void SetEnabled(bool enabled)
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (key == null) return;
                if (enabled)
                {
                    key.SetValue(AppName, "\"" + Application.ExecutablePath + "\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }
    }

    sealed class KeyboardHook : IDisposable
    {
        public event EventHandler<KeyEventArgs> KeyPressed;

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x0100;
        const int WM_SYSKEYDOWN = 0x0104;
        readonly LowLevelKeyboardProc proc;
        IntPtr hookId = IntPtr.Zero;

        public KeyboardHook()
        {
            proc = HookCallback;
        }

        public void Install()
        {
            if (hookId != IntPtr.Zero) return;
            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
            {
                hookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName), 0);
            }
        }

        public void Dispose()
        {
            if (hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
            }
        }

        IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var args = new KeyEventArgs((Keys)vkCode);
                var handler = KeyPressed;
                if (handler != null) handler(this, args);
                if (args.SuppressKeyPress) return (IntPtr)1;
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    static class Audio
    {
        public static bool AdjustForegroundAppVolume(float delta, out string target)
        {
            target = null;
            int pid = GetForegroundProcessId();
            if (pid <= 0) return false;

            try
            {
                IMMDevice device = GetDefaultRenderDevice();
                object obj;
                device.Activate(typeof(IAudioSessionManager2).GUID, 0, IntPtr.Zero, out obj);
                var manager = (IAudioSessionManager2)obj;
                IAudioSessionEnumerator enumerator;
                manager.GetSessionEnumerator(out enumerator);
                int count;
                enumerator.GetCount(out count);

                for (int i = 0; i < count; i++)
                {
                    IAudioSessionControl session;
                    enumerator.GetSession(i, out session);
                    var control2 = session as IAudioSessionControl2;
                    if (control2 == null) continue;
                    int sessionPid;
                    control2.GetProcessId(out sessionPid);
                    if (sessionPid != pid) continue;

                    var simple = session as ISimpleAudioVolume;
                    if (simple == null) continue;
                    float volume;
                    simple.GetMasterVolume(out volume);
                    simple.SetMasterVolume(Clamp(volume + delta), Guid.Empty);
                    target = GetProcessName(pid);
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public static bool AdjustSystemVolume(float delta, out string target)
        {
            target = "system volume";
            try
            {
                IMMDevice device = GetDefaultRenderDevice();
                object obj;
                device.Activate(typeof(IAudioEndpointVolume).GUID, 0, IntPtr.Zero, out obj);
                var endpoint = (IAudioEndpointVolume)obj;
                float volume;
                endpoint.GetMasterVolumeLevelScalar(out volume);
                endpoint.SetMasterVolumeLevelScalar(Clamp(volume + delta), Guid.Empty);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static IMMDevice GetDefaultRenderDevice()
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            IMMDevice device;
            enumerator.GetDefaultAudioEndpoint(0, 1, out device);
            return device;
        }

        static int GetForegroundProcessId()
        {
            IntPtr window = GetForegroundWindow();
            int pid;
            GetWindowThreadProcessId(window, out pid);
            return pid;
        }

        static string GetProcessName(int pid)
        {
            try { return Process.GetProcessById(pid).ProcessName; }
            catch { return "foreground app"; }
        }

        static float Clamp(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumerator { }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr devices);
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        int Activate([MarshalAs(UnmanagedType.LPStruct)] Guid iid, int dwClsCtx, IntPtr activationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
    }

    [ComImport]
    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionManager2
    {
        int GetAudioSessionControl(IntPtr audioSessionGuid, int streamFlags, out IntPtr sessionControl);
        int GetSimpleAudioVolume(IntPtr audioSessionGuid, int streamFlags, out IntPtr audioVolume);
        int GetSessionEnumerator(out IAudioSessionEnumerator sessionEnumerator);
        int RegisterSessionNotification(IntPtr sessionNotification);
        int UnregisterSessionNotification(IntPtr sessionNotification);
        int RegisterDuckNotification(string sessionId, IntPtr duckNotification);
        int UnregisterDuckNotification(IntPtr duckNotification);
    }

    [ComImport]
    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionEnumerator
    {
        int GetCount(out int sessionCount);
        int GetSession(int sessionCount, out IAudioSessionControl session);
    }

    [ComImport]
    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionControl
    {
        int GetState(out int state);
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string displayName);
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string displayName, Guid eventContext);
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string iconPath);
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string iconPath, Guid eventContext);
        int GetGroupingParam(out Guid groupingId);
        int SetGroupingParam(Guid groupingId, Guid eventContext);
        int RegisterAudioSessionNotification(IntPtr client);
        int UnregisterAudioSessionNotification(IntPtr client);
    }

    [ComImport]
    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionControl2
    {
        int GetState(out int state);
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string displayName);
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string displayName, Guid eventContext);
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string iconPath);
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string iconPath, Guid eventContext);
        int GetGroupingParam(out Guid groupingId);
        int SetGroupingParam(Guid groupingId, Guid eventContext);
        int RegisterAudioSessionNotification(IntPtr client);
        int UnregisterAudioSessionNotification(IntPtr client);
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string retVal);
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string retVal);
        int GetProcessId(out int retVal);
        int IsSystemSoundsSession();
        int SetDuckingPreference(bool optOut);
    }

    [ComImport]
    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ISimpleAudioVolume
    {
        int SetMasterVolume(float level, Guid eventContext);
        int GetMasterVolume(out float level);
        int SetMute(bool mute, Guid eventContext);
        int GetMute(out bool mute);
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr notify);
        int UnregisterControlChangeNotify(IntPtr notify);
        int GetChannelCount(out uint channelCount);
        int SetMasterVolumeLevel(float levelDb, Guid eventContext);
        int SetMasterVolumeLevelScalar(float level, Guid eventContext);
        int GetMasterVolumeLevel(out float levelDb);
        int GetMasterVolumeLevelScalar(out float level);
    }
}

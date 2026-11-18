// 모니터 가리기 - NAPDA (만든 사람 Hononoo)
// Copyright (c) 2026 Hononoo (NAPDA). MIT License.
// 이 저작권 표기와 "만든 사람 Hononoo" 표기는 수정·재배포할 때도 지우면 안 됩니다 (LICENSE 참고).
//
// 본체에 연결된 모니터만 검게 가리고, 원격(크롬 원격 데스크톱 등)에는 원래 화면이 그대로 보이게 한다.
// 본체 모니터가 실제로 켜져 있는지도 DDC/CI로 확인해서 트레이 아이콘으로 알려 준다.
// 윈도우 10 2004 미만(지원이 끝난 7·8.1 포함)에서는 검은 창 대신 모니터 전원을 DDC/CI로 꺼서 가린다.
//
// 빌드: build.bat - 윈도우에 기본으로 있는 .NET Framework의 csc.exe로 빌드.
// 오래된 PC에서도 돌도록 C# 5 문법과 .NET Framework 4.0에 있는 API만 쓴다.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using IPersistFile = System.Runtime.InteropServices.ComTypes.IPersistFile;

[assembly: AssemblyTitle(Napda.MonitorCover.About.Name)]
[assembly: AssemblyDescription("본체 모니터만 가리고 원격 화면은 그대로 보이게 하는 도구 - " + Napda.MonitorCover.About.Company + ", 만든 사람 " + Napda.MonitorCover.About.Author)]
[assembly: AssemblyCompany(Napda.MonitorCover.About.Company)]
[assembly: AssemblyProduct(Napda.MonitorCover.About.Name)]
[assembly: AssemblyCopyright(Napda.MonitorCover.About.Copyright)]
[assembly: AssemblyVersion(Napda.MonitorCover.About.Version)]
[assembly: AssemblyFileVersion(Napda.MonitorCover.About.Version)]

namespace Napda.MonitorCover
{
    static class About
    {
        public const string Name = "모니터 가리기";
        public const string Company = "NAPDA";
        public const string Author = "Hononoo";
        public const string ShortVersion = "1.6";
        public const string Version = ShortVersion + ".0.0";
        public const string Credit = Name + " - " + Company + " by " + Author;
        public const string Copyright = "Copyright (c) 2026 " + Author + " (" + Company + "). MIT License.";
        // 블로그·포트폴리오 주소가 생기면 여기에 넣는다. 정보 창에서 바로 열 수 있다.
        public const string Link = "";

        public const string Text =
            Name + " v" + ShortVersion + "\n" +
            "만든 곳: " + Company + " (냅다)\n" +
            "만든 사람: " + Author + "\n\n" +
            "본체 모니터만 검게 가리고, 원격 화면에는 원래 화면이 그대로 보이게 해요.\n\n" +
            Copyright + "\n" +
            "수정하거나 다시 배포할 때도 만든 사람 표기를 지우면 안 돼요.";
    }

    static class Native
    {
        public const int WS_EX_TOPMOST = 0x8;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WS_EX_TOOLWINDOW = 0x80;
        public const int WS_EX_LAYERED = 0x80000;
        public const int WS_EX_NOACTIVATE = 0x8000000;
        public const uint LWA_ALPHA = 0x2;
        public const uint WDA_EXCLUDEFROMCAPTURE = 0x11;
        public const uint SWP_NOSIZE = 0x1;
        public const uint SWP_NOMOVE = 0x2;
        public const uint SWP_NOACTIVATE = 0x10;
        public const uint SWP_SHOWWINDOW = 0x40;
        public const uint SWP_NOOWNERZORDER = 0x200;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int WM_HOTKEY = 0x312;
        public const uint MOD_ALT = 0x1;
        public const uint MOD_CONTROL = 0x2;
        public const uint MOD_SHIFT = 0x4;
        public const uint MOD_WIN = 0x8;
        public const uint MOD_NOREPEAT = 0x4000;
        public const uint GA_ROOT = 2;
        public const uint MONITOR_DEFAULTTONULL = 0;
        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x1;
        public const int ASFW_ANY = -1;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
        [DllImport("user32.dll")]
        public static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);
        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT pt);
        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);
        [DllImport("user32.dll")]
        public static extern bool AllowSetForegroundWindow(int dwProcessId);
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")]
        static extern bool SetProcessDpiAwarenessContext(IntPtr value);
        [DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(uint esFlags);

        public const uint SRCCOPY = 0x00CC0020;
        public const uint CAPTUREBLT = 0x40000000;
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int w, int h, IntPtr hdcSrc, int xSrc, int ySrc, uint rop);
        public const uint PW_RENDERFULLCONTENT = 0x2;
        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);
        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);
        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, PHYSICAL_MONITOR[] pPhysicalMonitorArray);
        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr hMonitor, byte bVCPCode, IntPtr pvct, out uint pdwCurrentValue, out uint pdwMaximumValue);
        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, uint dwNewValue);

        // 모니터마다 배율이 달라도 실제 픽셀 단위로 화면을 꽉 채우기 위해 (없는 윈도우에서는 건너뜀)
        public static void EnableDpiAwareness()
        {
            try
            {
                if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)) return;
            }
            catch (EntryPointNotFoundException) { }
            try
            {
                SetProcessDPIAware();
            }
            catch (EntryPointNotFoundException) { }
        }
    }

    // 설정과 오류 기록은 PC·사용자마다 따로 둔다.
    // exe만 다른 PC로 복사해 가면 그 PC에서는 처음 실행처럼 동작하고,
    // AppData는 회사 보안 설정(제어된 폴더 액세스 등)에 막히는 일이 거의 없는 위치다.
    static class AppPaths
    {
        public static string DataDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), About.Company, "MonitorCover"); }
        }

        // 예전 버전(1.3 MonitorBlackout, 1.4 NapdaBlack)이 쓰던 설정 파일 위치
        public static string[] LegacySettingsFiles
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return new[]
                {
                    Path.Combine(appData, "NapdaBlack", "settings.ini"),
                    Path.Combine(appData, "MonitorBlackout", "settings.ini"),
                };
            }
        }

        public static string SettingsFile
        {
            get { return Path.Combine(DataDir, "settings.ini"); }
        }

        // 가림 화면으로 고른 이미지는 원본을 옮기거나 지워도 되게 여기로 복사해 둔다
        public static string CoverImageFile(string extension)
        {
            return Path.Combine(DataDir, "cover-image" + extension);
        }

        public static string ErrorLogFile
        {
            get { return Path.Combine(DataDir, "error.log"); }
        }
    }

    static class ErrorLog
    {
        public static void Write(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataDir);
                File.AppendAllText(AppPaths.ErrorLogFile,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + About.Credit + " v" + About.Version + "\r\n" + ex + "\r\n\r\n",
                    Encoding.UTF8);
            }
            catch (Exception) { }
        }
    }

    static class Ui
    {
        public static Font Font
        {
            get { return SystemFonts.MessageBoxFont; }
        }

        public static float Scale
        {
            get
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                    return g.DpiX / 96f;
            }
        }

        public static Label Label(string text)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Anchor = AnchorStyles.Left;
            label.Margin = new Padding(0, 6, 10, 6);
            return label;
        }

        public static Label Note(string text, int maxWidth)
        {
            Label label = Label(text);
            label.ForeColor = SystemColors.GrayText;
            label.MaximumSize = new Size(maxWidth, 0);
            return label;
        }

        public static Label Credit()
        {
            Label label = Label(About.Credit);
            label.ForeColor = SystemColors.GrayText;
            label.Margin = new Padding(0, 12, 0, 0);
            return label;
        }

        public static void AddRow(TableLayoutPanel grid, Control left, Control right)
        {
            int row = grid.RowCount++;
            grid.Controls.Add(left, 0, row);
            grid.Controls.Add(right, 1, row);
        }

        public static void AddWide(TableLayoutPanel grid, Control control)
        {
            int row = grid.RowCount++;
            grid.Controls.Add(control, 0, row);
            grid.SetColumnSpan(control, 2);
        }

        public static TableLayoutPanel Grid(float scale)
        {
            var grid = new TableLayoutPanel();
            grid.AutoSize = true;
            grid.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grid.ColumnCount = 2;
            grid.Padding = new Padding((int)(14 * scale));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            return grid;
        }
    }

    // 본체 모니터에만 보이는 검은 창.
    // - 레이어드+투명 스타일: 클릭이 뒤 창으로 그대로 통과 (원격 조작·자동화가 평소처럼 동작)
    // - NOACTIVATE: 포커스를 뺏지 않음
    // - WDA_EXCLUDEFROMCAPTURE: 화면 캡처에서 빠짐 → 원격 접속 화면에는 원래 화면이 보임
    class OverlayForm : Form
    {
        readonly Rectangle target;
        readonly bool excludeFromCapture;

        public bool ExcludedFromCapture { get; private set; }

        public OverlayForm(Rectangle bounds, Color fill, bool excludeFromCapture)
        {
            target = bounds;
            this.excludeFromCapture = excludeFromCapture;
            FormBorderStyle = FormBorderStyle.None;
            ControlBox = false;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = fill;
            Bounds = bounds;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT | Native.WS_EX_NOACTIVATE
                            | Native.WS_EX_TOOLWINDOW | Native.WS_EX_TOPMOST;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.SetLayeredWindowAttributes(Handle, 0, 255, Native.LWA_ALPHA);
            if (!excludeFromCapture) return;
            try
            {
                uint affinity;
                ExcludedFromCapture = Native.SetWindowDisplayAffinity(Handle, Native.WDA_EXCLUDEFROMCAPTURE)
                                   && Native.GetWindowDisplayAffinity(Handle, out affinity)
                                   && affinity == Native.WDA_EXCLUDEFROMCAPTURE;
            }
            catch (EntryPointNotFoundException)
            {
                ExcludedFromCapture = false; // 윈도우 7 이전
            }
        }

        // 다른 "항상 위" 창이 올라와도 다시 맨 위로, 크기가 틀어졌으면 다시 꽉 채우기
        public void PinTop()
        {
            if (Native.IsIconic(Handle)) Native.ShowWindow(Handle, Native.SW_SHOWNOACTIVATE);
            Native.SetWindowPos(Handle, Native.HWND_TOPMOST, target.X, target.Y, target.Width, target.Height,
                                Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW | Native.SWP_NOOWNERZORDER);
        }

        // 이 화면 크기에 맞춰 미리 그려 둔 가림 이미지. 창이 닫힐 때 같이 정리한다.
        public void SetCover(Bitmap cover)
        {
            BackgroundImageLayout = ImageLayout.None;
            BackgroundImage = cover;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && BackgroundImage != null)
            {
                BackgroundImage.Dispose();
                BackgroundImage = null;
            }
            base.Dispose(disposing);
        }
    }

    enum PowerState { Unknown, Off, On }

    class MonitorReport
    {
        public int On, Off, Unknown;
        public bool Ready;
        public string Detail = "";

        public int Total
        {
            get { return On + Off + Unknown; }
        }

        public PowerState Overall
        {
            get
            {
                if (!Ready) return PowerState.Unknown;
                if (On > 0) return PowerState.On;
                if (Off > 0 && Unknown == 0) return PowerState.Off;
                return PowerState.Unknown;
            }
        }

        public string Label
        {
            get
            {
                if (!Ready) return "확인 중";
                switch (Overall)
                {
                    case PowerState.On: return Total > 1 ? "켜짐 (" + On + "/" + Total + "대)" : "켜짐";
                    case PowerState.Off: return "꺼짐";
                    default: return "확인 불가";
                }
            }
        }

        public string Key
        {
            get { return On + "/" + Off + "/" + Unknown + "/" + Ready; }
        }
    }

    // 모니터와 DDC/CI로 직접 주고받는다 (VCP D6 = 전원 상태).
    static class MonitorPower
    {
        const byte VcpPowerMode = 0xD6;
        public const uint On = 1;
        public const uint Off = 4; // DPM 꺼짐 (모니터 절전과 같은 상태)

        // 이 화면에 연결된 모니터마다 action을 실행하고, 성공한 수를 돌려준다
        static int ForEachMonitor(Rectangle bounds, Func<IntPtr, bool> action, out string name)
        {
            name = "";
            var center = new Native.POINT();
            center.X = bounds.X + bounds.Width / 2;
            center.Y = bounds.Y + bounds.Height / 2;
            IntPtr hmon = Native.MonitorFromPoint(center, Native.MONITOR_DEFAULTTONULL);
            uint count;
            if (hmon == IntPtr.Zero || !Native.GetNumberOfPhysicalMonitorsFromHMONITOR(hmon, out count) || count == 0) return 0;
            var monitors = new Native.PHYSICAL_MONITOR[count];
            if (!Native.GetPhysicalMonitorsFromHMONITOR(hmon, count, monitors)) return 0;
            try
            {
                name = monitors[0].szPhysicalMonitorDescription;
                int ok = 0;
                foreach (Native.PHYSICAL_MONITOR monitor in monitors)
                {
                    if (action(monitor.hPhysicalMonitor)) ok++;
                }
                return ok;
            }
            finally
            {
                Native.DestroyPhysicalMonitors(count, monitors);
            }
        }

        // 1 = 켜짐, 2~5 = 대기/꺼짐, 응답이 없으면 -1
        public static int QueryPowerMode(Rectangle bounds, out string name)
        {
            int result = -1;
            ForEachMonitor(bounds, delegate(IntPtr monitor)
            {
                uint current, max;
                if (!Native.GetVCPFeatureAndVCPFeatureReply(monitor, VcpPowerMode, IntPtr.Zero, out current, out max)) return false;
                if (result != 1) result = (int)current;
                return true;
            }, out name);
            return result;
        }

        // 모든 화면의 모니터에 전원 상태를 보낸다. 받아들인 모니터 수를 돌려준다.
        public static int SetAll(uint mode)
        {
            int accepted = 0;
            foreach (Screen screen in Screen.AllScreens)
            {
                string name;
                try
                {
                    accepted += ForEachMonitor(screen.Bounds, delegate(IntPtr monitor)
                    {
                        return Native.SetVCPFeature(monitor, VcpPowerMode, mode);
                    }, out name);
                }
                catch (DllNotFoundException) { } // DDC/CI 기능이 없는 아주 오래된 윈도우
            }
            return accepted;
        }
    }

    // 3초마다 본체 모니터 전원 상태를 확인한다 (DDC/CI 응답이 느릴 수 있어 별도 스레드).
    class MonitorWatcher
    {
        readonly HashSet<string> answeredBefore = new HashSet<string>();

        // 백그라운드 스레드에서 상태가 바뀔 때만 호출된다
        public event Action<MonitorReport> Changed;

        public void Start()
        {
            var thread = new Thread(delegate()
            {
                string lastKey = null;
                while (true)
                {
                    MonitorReport report;
                    try
                    {
                        report = Check();
                    }
                    catch (Exception ex)
                    {
                        report = new MonitorReport();
                        report.Ready = true;
                        report.Unknown = 1;
                        report.Detail = ex.Message;
                    }
                    Action<MonitorReport> handler = Changed;
                    if (handler != null && report.Key != lastKey)
                    {
                        lastKey = report.Key;
                        handler(report);
                    }
                    Thread.Sleep(3000);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        public MonitorReport Check()
        {
            var report = new MonitorReport();
            var detail = new StringBuilder();
            var present = new HashSet<string>();
            foreach (Screen screen in Screen.AllScreens)
            {
                string name;
                int mode = MonitorPower.QueryPowerMode(screen.Bounds, out name);
                present.Add(screen.DeviceName);
                detail.AppendLine(screen.DeviceName.TrimStart('\\', '.') + " (" + name + "): "
                                  + (mode == 1 ? "켜짐" : mode > 1 ? "꺼짐/대기 (D6=" + mode + ")" : "응답 없음"));
                if (mode == 1)
                {
                    report.On++;
                    answeredBefore.Add(screen.DeviceName);
                }
                else if (mode > 1)
                {
                    report.Off++;
                    answeredBefore.Add(screen.DeviceName);
                }
                else if (answeredBefore.Contains(screen.DeviceName))
                {
                    report.Off++; // 아까까지 대답하던 모니터가 조용해짐 = 전원이 꺼졌을 가능성이 큼
                }
                else
                {
                    report.Unknown++;
                }
            }
            // 모니터를 끄면 연결 자체가 끊기는 경우(DisplayPort 등)도 꺼짐으로 친다
            foreach (string device in answeredBefore)
            {
                if (!present.Contains(device))
                {
                    report.Off++;
                    detail.AppendLine(device.TrimStart('\\', '.') + ": 연결 끊김");
                }
            }
            report.Ready = true;
            report.Detail = detail.ToString();
            return report;
        }
    }

    class HotkeyWindow : NativeWindow, IDisposable
    {
        public event EventHandler Pressed;

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY && Pressed != null) Pressed(this, EventArgs.Empty);
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            DestroyHandle();
        }
    }

    static class HotkeyParser
    {
        // "Ctrl+Alt+B" 같은 글자를 RegisterHotKey 값으로. 실수로 일반 타자를 막지 않게 조합키 하나 이상 필수.
        public static bool TryParse(string text, out uint mods, out uint vk)
        {
            mods = 0;
            vk = 0;
            if (string.IsNullOrEmpty(text)) return false;
            string[] parts = text.Split('+');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                string lower = part.ToLowerInvariant();
                if (i < parts.Length - 1)
                {
                    if (lower == "ctrl" || lower == "control") mods |= Native.MOD_CONTROL;
                    else if (lower == "alt") mods |= Native.MOD_ALT;
                    else if (lower == "shift") mods |= Native.MOD_SHIFT;
                    else if (lower == "win") mods |= Native.MOD_WIN;
                    else return false;
                    continue;
                }
                if (part.Length == 1 && char.IsDigit(part[0])) part = "D" + part;
                Keys key;
                if (!Enum.TryParse(part, true, out key)) return false;
                if ((key & Keys.Modifiers) != 0 || key == Keys.None) return false;
                vk = (uint)key;
            }
            return mods != 0 && vk != 0;
        }
    }

    // 비밀번호는 그대로 저장하지 않고 소금(salt)을 친 PBKDF2 해시로만 저장한다
    static class PasswordHash
    {
        const int Iterations = 20000;

        public static string Create(string password)
        {
            var salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(Derive(password, salt));
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored) || password == null) return false;
            string[] parts = stored.Split(':');
            if (parts.Length != 2) return false;
            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[0]);
                expected = Convert.FromBase64String(parts[1]);
            }
            catch (FormatException)
            {
                return false;
            }
            if (salt.Length < 8) return false;
            byte[] actual = Derive(password, salt);
            int diff = actual.Length ^ expected.Length;
            for (int i = 0; i < actual.Length && i < expected.Length; i++) diff |= actual[i] ^ expected[i];
            return diff == 0;
        }

        static byte[] Derive(string password, byte[] salt)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, salt, Iterations))
                return kdf.GetBytes(32);
        }
    }

    class Settings
    {
        public string Hotkey = "Ctrl+Alt+B";
        public bool StartHidden;
        public string PasswordHash = "";
        public string CoverMode = "color"; // color | image
        public string CoverColor = "#000000";
        public string CoverImage = "";
        public readonly string FilePath;

        public Settings(string filePath)
        {
            FilePath = filePath;
        }

        public bool Exists
        {
            get { return File.Exists(FilePath); }
        }

        // 예전 버전의 설정 파일(exe 옆, 이름 바꾸기 전 AppData 폴더)이 있으면 새 위치로 옮긴다
        public static void MigrateLegacy(string newPath, params string[] legacyPaths)
        {
            try
            {
                if (File.Exists(newPath)) return;
                foreach (string legacyPath in legacyPaths)
                {
                    if (!File.Exists(legacyPath)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                    File.Copy(legacyPath, newPath);
                    File.Delete(legacyPath);
                    return;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex);
            }
        }

        public void Load()
        {
            string[] lines;
            try
            {
                if (!File.Exists(FilePath)) return;
                lines = File.ReadAllLines(FilePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex);
                return;
            }
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                string val = line.Substring(eq + 1).Trim();
                if (key == "hotkey") Hotkey = val;
                else if (key == "starthidden") StartHidden = val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
                else if (key == "passwordhash") PasswordHash = val;
                else if (key == "covermode") CoverMode = val == "image" ? "image" : "color";
                else if (key == "covercolor") CoverColor = val;
                else if (key == "coverimage") CoverImage = val;
            }
        }

        // 저장에 실패하면 이유를 돌려준다 (프로그램은 계속 돈다)
        public string TrySave()
        {
            var sb = new StringBuilder();
            sb.AppendLine("; " + About.Credit + " - 설정 파일");
            sb.AppendLine("; 트레이 메뉴 > 해제 방식 설정에서 바꾸는 걸 권해요.");
            sb.AppendLine(";");
            sb.AppendLine("; hotkey: 가리기/보이기 전환 단축키 (Ctrl, Alt, Shift, Win 중 하나 이상 + 키 하나)");
            sb.AppendLine("hotkey=" + Hotkey);
            sb.AppendLine(";");
            sb.AppendLine("; startHidden: true면 프로그램이 켜지자마자 화면을 가려요.");
            sb.AppendLine(";   켜 두면 비밀번호·단축키를 잊었을 때 재부팅으로 풀 수 없어요.");
            sb.AppendLine("startHidden=" + (StartHidden ? "true" : "false"));
            sb.AppendLine(";");
            sb.AppendLine("; passwordHash: 비밀번호를 그대로 적지 말고 해제 방식 설정에서 정하세요. 줄을 지우면 비밀번호 없음.");
            sb.AppendLine("passwordHash=" + PasswordHash);
            sb.AppendLine(";");
            sb.AppendLine("; coverMode: 가릴 때 보일 화면 (color = 단색, image = 이미지). 가림 화면 설정에서 바꾸는 걸 권해요.");
            sb.AppendLine("coverMode=" + CoverMode);
            sb.AppendLine("coverColor=" + CoverColor);
            sb.AppendLine("coverImage=" + CoverImage);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(true));
                return null;
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex);
                return ex.Message;
            }
        }
    }

    static class Art
    {
        public static readonly Color OnColor = Color.FromArgb(235, 72, 60);
        public static readonly Color OffColor = Color.FromArgb(150, 150, 150);
        public static readonly Color UnknownColor = Color.FromArgb(240, 180, 40);

        public static Color ColorFor(PowerState state)
        {
            switch (state)
            {
                case PowerState.On: return OnColor;
                case PowerState.Off: return OffColor;
                default: return UnknownColor;
            }
        }

        // 모니터 모양 아이콘. 가리는 중이면 검은 화면에 사선, 보이는 중이면 파란 화면.
        // monitorOn이면 오른쪽 아래에 빨간 점 (본체 모니터 전원이 켜져 있음).
        public static Bitmap Draw(int size, bool hidden, bool monitorOn)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            using (var body = new SolidBrush(Color.FromArgb(120, 124, 135)))
            using (var screen = new SolidBrush(hidden ? Color.FromArgb(15, 15, 18) : Color.FromArgb(70, 160, 255)))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                float s = size / 16f;
                var frame = new RectangleF(0.5f * s, 1.5f * s, 15f * s, 10f * s);
                g.FillRectangle(body, frame);
                RectangleF inner = RectangleF.Inflate(frame, -1.25f * s, -1.25f * s);
                g.FillRectangle(screen, inner);
                g.FillRectangle(body, 6.5f * s, 11.5f * s, 3f * s, 2f * s);
                g.FillRectangle(body, 4f * s, 13.5f * s, 8f * s, 1.5f * s);
                if (hidden)
                {
                    using (var pen = new Pen(Color.FromArgb(235, 235, 235), Math.Max(1f, 1.2f * s)))
                        g.DrawLine(pen, inner.Left + s, inner.Bottom - s, inner.Right - s, inner.Top + s);
                }
                if (monitorOn)
                {
                    var spot = new RectangleF(9f * s, 8.5f * s, 7f * s, 7f * s);
                    using (var ring = new SolidBrush(Color.White))
                        g.FillEllipse(ring, spot);
                    using (var red = new SolidBrush(OnColor))
                        g.FillEllipse(red, RectangleF.Inflate(spot, -1f * s, -1f * s));
                }
            }
            return bmp;
        }

        // 여러 크기를 담은 .ico (32비트 DIB 항목) - exe 아이콘, 트레이 아이콘에 같이 씀
        public static byte[] BuildIco(bool hidden, bool monitorOn)
        {
            int[] sizes = { 16, 20, 24, 32, 48, 64, 128 };
            var images = new List<byte[]>();
            foreach (int size in sizes)
            {
                using (Bitmap bmp = Draw(size, hidden, monitorOn))
                    images.Add(ToIconDib(bmp));
            }
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write((short)0);
                w.Write((short)1);
                w.Write((short)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    w.Write((byte)sizes[i]);
                    w.Write((byte)sizes[i]);
                    w.Write((byte)0);
                    w.Write((byte)0);
                    w.Write((short)1);
                    w.Write((short)32);
                    w.Write(images[i].Length);
                    w.Write(offset);
                    offset += images[i].Length;
                }
                foreach (byte[] image in images) w.Write(image);
                w.Flush();
                return ms.ToArray();
            }
        }

        static byte[] ToIconDib(Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            int maskStride = (w + 31) / 32 * 4;
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(40);
                bw.Write(w);
                bw.Write(h * 2);
                bw.Write((short)1);
                bw.Write((short)32);
                bw.Write(0);
                bw.Write(w * h * 4 + maskStride * h);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    var row = new byte[w * 4];
                    for (int y = h - 1; y >= 0; y--)
                    {
                        Marshal.Copy(new IntPtr(data.Scan0.ToInt64() + (long)y * data.Stride), row, 0, row.Length);
                        bw.Write(row);
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
                bw.Write(new byte[maskStride * h]);
                bw.Flush();
                return ms.ToArray();
            }
        }

        public static Icon LoadIcon(bool hidden, bool monitorOn)
        {
            using (var ms = new MemoryStream(BuildIco(hidden, monitorOn)))
                return new Icon(ms, SystemInformation.SmallIconSize);
        }

        // 창 제목 표시줄·작업 표시줄용 (모든 크기를 담은 아이콘)
        public static Icon LoadAppIcon()
        {
            using (var ms = new MemoryStream(BuildIco(false, false)))
                return new Icon(ms);
        }
    }

    // 바로가기(.lnk)는 윈도우 셸의 IShellLink로 직접 만든다.
    // WScript.Shell을 쓰지 않으니 회사 정책으로 스크립트 호스트를 막아 둔 PC에서도 된다.
    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    class ShellLinkObject { }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
    interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    static class Shortcut
    {
        public static string DesktopPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), About.Name + ".lnk"); }
        }

        // 예전 이름으로 만든 바로가기 (1.4는 '냅다가려'였다)
        public static readonly string[] LegacyFileNames = { "냅다가려.lnk" };

        public static void Create(string lnkPath, string target, string args, string description)
        {
            var link = (IShellLinkW)new ShellLinkObject();
            try
            {
                link.SetPath(target);
                link.SetArguments(args);
                link.SetWorkingDirectory(Path.GetDirectoryName(target));
                link.SetIconLocation(target, 0);
                link.SetDescription(description);
                ((IPersistFile)link).Save(lnkPath, true);
            }
            finally
            {
                Marshal.FinalReleaseComObject(link);
            }
        }

        public static string ReadTarget(string lnkPath)
        {
            var link = (IShellLinkW)new ShellLinkObject();
            try
            {
                ((IPersistFile)link).Load(lnkPath, 0);
                var path = new StringBuilder(1024);
                link.GetPath(path, path.Capacity, IntPtr.Zero, 0);
                return path.ToString();
            }
            finally
            {
                Marshal.FinalReleaseComObject(link);
            }
        }
    }

    // 시작프로그램 폴더의 바로가기로 자동 실행한다.
    // 레지스트리 Run 목록을 무시하게 해 둔 회사 PC에서도 되고, 사용자가 눈으로 확인·삭제하기 쉽다.
    static class AutoStart
    {
        const string LegacyRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string LegacyValueName = "MonitorBlackout";

        public static string LinkPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), About.Name + ".lnk"); }
        }

        public static bool Exists
        {
            get { return File.Exists(LinkPath); }
        }

        public static bool IsOn(string exe)
        {
            try
            {
                return Exists && string.Equals(Shortcut.ReadTarget(LinkPath), exe, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Set(bool on, string exe)
        {
            // --startup: 로그인할 때는 창 없이 트레이에서 조용히 시작
            if (on) Shortcut.Create(LinkPath, exe, "--startup", About.Credit + " - 윈도우 시작 시 자동 실행");
            else if (Exists) File.Delete(LinkPath);
        }

        // 1.2 이하 버전은 레지스트리 Run 목록을 썼다 → 시작프로그램 바로가기로 옮긴다
        public static void MigrateFromRunKey(string exe)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(LegacyRunKey, true))
            {
                if (key == null || key.GetValue(LegacyValueName) == null) return;
                Set(true, exe);
                key.DeleteValue(LegacyValueName, false);
            }
        }
    }

    // 누른 조합을 그대로 받아 적는 단축키 입력 칸
    class HotkeyBox : TextBox
    {
        public HotkeyBox()
        {
            ReadOnly = true;
            BackColor = SystemColors.Window;
            ShortcutsEnabled = false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys mods = keyData & Keys.Modifiers;
            Keys key = keyData & Keys.KeyCode;
            // 조합키 없이 누른 Tab·Enter·Esc, Shift+Tab은 창 이동·저장·취소에 쓴다
            if (mods == Keys.None || (mods == Keys.Shift && key == Keys.Tab)) return base.ProcessCmdKey(ref msg, keyData);
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu || key == Keys.LWin || key == Keys.RWin) return true;
            Text = Describe(mods, key);
            return true;
        }

        public static string Describe(Keys mods, Keys key)
        {
            var parts = new List<string>();
            if ((mods & Keys.Control) != 0) parts.Add("Ctrl");
            if ((mods & Keys.Alt) != 0) parts.Add("Alt");
            if ((mods & Keys.Shift) != 0) parts.Add("Shift");
            parts.Add(key >= Keys.D0 && key <= Keys.D9 ? ((char)('0' + (key - Keys.D0))).ToString() : key.ToString());
            return string.Join("+", parts.ToArray());
        }
    }

    // 해제 방식 설정: 사람마다 다른 단축키를 직접 눌러서 정하고, 원하면 비밀번호도 건다.
    class UnlockSettingsForm : Form
    {
        readonly HotkeyBox hotkeyBox = new HotkeyBox();
        readonly CheckBox usePassword = new CheckBox();
        readonly TextBox password = new TextBox();
        readonly TextBox confirm = new TextBox();
        readonly Label error = new Label();
        readonly bool hadPassword;

        public string Hotkey
        {
            get { return hotkeyBox.Text; }
        }

        // null이면 비밀번호 그대로, ""이면 비밀번호 없앰, 그 밖에는 새 해시
        public string PasswordHashResult { get; private set; }

        public UnlockSettingsForm(string hotkey, bool hasPassword, bool startHidden)
        {
            hadPassword = hasPassword;
            float scale = Ui.Scale;
            int wide = (int)(380 * scale);
            Text = "해제 방식 설정 - " + About.Credit;
            Font = Ui.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            hotkeyBox.Text = hotkey;
            hotkeyBox.Width = (int)(220 * scale);
            usePassword.Text = "화면을 다시 보이게 할 때 비밀번호 묻기";
            usePassword.AutoSize = true;
            usePassword.Checked = hasPassword;
            usePassword.Margin = new Padding(0, 10, 0, 4);
            usePassword.CheckedChanged += delegate { UpdateEnabled(); };
            password.UseSystemPasswordChar = true;
            password.Width = hotkeyBox.Width;
            confirm.UseSystemPasswordChar = true;
            confirm.Width = hotkeyBox.Width;
            error.AutoSize = true;
            error.ForeColor = Color.Firebrick;
            error.MaximumSize = new Size(wide, 0);

            TableLayoutPanel grid = Ui.Grid(scale);
            Ui.AddRow(grid, Ui.Label("단축키"), hotkeyBox);
            Ui.AddWide(grid, Ui.Note("칸을 누른 다음 원하는 조합을 그대로 누르세요. Ctrl·Alt·Shift 중 하나 이상과 키 하나예요. (예: Ctrl+Alt+B)", wide));
            Ui.AddWide(grid, usePassword);
            Ui.AddRow(grid, Ui.Label("비밀번호"), password);
            Ui.AddRow(grid, Ui.Label("비밀번호 확인"), confirm);
            if (hasPassword) Ui.AddWide(grid, Ui.Note("비밀번호 칸을 비워 두면 지금 비밀번호를 그대로 써요.", wide));
            Ui.AddWide(grid, Ui.Note(startHidden
                ? "주의: '실행하면 바로 가리기'가 켜져 있어서, 비밀번호나 단축키를 잊으면 재부팅해도 가려진 채로 시작해요. 그때는 원격으로 접속해서 바탕화면 버튼으로 풀어야 해요."
                : "비밀번호나 단축키를 잊으면 PC를 재부팅하세요. 화면이 보이는 상태로 시작하니 여기서 다시 정하면 돼요.", wide));
            Ui.AddWide(grid, error);

            var buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, (int)(8 * scale), 0, 0);
            var cancel = new Button();
            cancel.Text = "취소";
            cancel.AutoSize = true;
            cancel.DialogResult = DialogResult.Cancel;
            var save = new Button();
            save.Text = "저장";
            save.AutoSize = true;
            save.Click += delegate { OnSave(); };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            Ui.AddWide(grid, buttons);
            Ui.AddWide(grid, Ui.Credit());

            Controls.Add(grid);
            AcceptButton = save;
            CancelButton = cancel;
            UpdateEnabled();
        }

        void UpdateEnabled()
        {
            password.Enabled = usePassword.Checked;
            confirm.Enabled = usePassword.Checked;
        }

        void OnSave()
        {
            uint mods, vk;
            if (!HotkeyParser.TryParse(hotkeyBox.Text, out mods, out vk))
            {
                error.Text = "단축키는 Ctrl·Alt·Shift 중 하나 이상과 키 하나를 같이 눌러서 정해 주세요.";
                return;
            }
            if (!usePassword.Checked)
            {
                PasswordHashResult = hadPassword ? "" : null;
            }
            else if (hadPassword && password.Text.Length == 0 && confirm.Text.Length == 0)
            {
                PasswordHashResult = null;
            }
            else if (password.Text.Length < 4)
            {
                error.Text = "비밀번호는 4자 이상으로 정해 주세요.";
                return;
            }
            else if (password.Text != confirm.Text)
            {
                error.Text = "두 비밀번호가 서로 달라요.";
                return;
            }
            else
            {
                PasswordHashResult = PasswordHash.Create(password.Text);
            }
            DialogResult = DialogResult.OK;
        }
    }

    // 가린 화면을 다시 보이게 할 때 비밀번호를 묻는 작은 창.
    // 검은 창보다 위에 떠서 본체 모니터에서도 보인다. 30초 동안 입력이 없으면 닫힌다.
    class PasswordPrompt : Form
    {
        const int MaxTries = 5;
        // 창을 닫았다 다시 열어도 틀린 횟수와 잠금은 이어진다
        static int failures;
        static DateTime lockedUntil = DateTime.MinValue;

        readonly Func<string, bool> verify;
        readonly TextBox box = new TextBox();
        readonly Label message = new Label();
        readonly System.Windows.Forms.Timer idle = new System.Windows.Forms.Timer();
        readonly System.Windows.Forms.Timer unlockTimer = new System.Windows.Forms.Timer();

        public event EventHandler Unlocked;

        public PasswordPrompt(Func<string, bool> verify)
        {
            this.verify = verify;
            float scale = Ui.Scale;
            Text = About.Credit;
            Font = Ui.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            KeyPreview = true;

            box.UseSystemPasswordChar = true;
            box.Width = (int)(260 * scale);
            message.AutoSize = true;
            message.ForeColor = Color.Firebrick;
            message.MaximumSize = new Size((int)(320 * scale), 0);

            TableLayoutPanel grid = Ui.Grid(scale);
            Ui.AddWide(grid, Ui.Label("화면을 다시 보이게 하려면 비밀번호를 입력하세요."));
            Ui.AddWide(grid, box);
            Ui.AddWide(grid, message);
            Ui.AddWide(grid, Ui.Credit());
            Controls.Add(grid);

            box.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                RestartIdle();
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                Check();
            };
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) Close();
            };
            idle.Interval = 30000;
            idle.Tick += delegate { Close(); };
            unlockTimer.Interval = 1000;
            unlockTimer.Tick += delegate { RefreshLock(); };
        }

        public void ShowOnTop()
        {
            Show();
            PinTop();
            Activate();
            box.Focus();
            RestartIdle();
            RefreshLock();
        }

        public void PinTop()
        {
            Native.SetWindowPos(Handle, Native.HWND_TOPMOST, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
        }

        void RestartIdle()
        {
            idle.Stop();
            idle.Start();
        }

        void RefreshLock()
        {
            TimeSpan left = lockedUntil - DateTime.Now;
            if (left > TimeSpan.Zero)
            {
                box.Enabled = false;
                message.Text = "여러 번 틀려서 " + (int)Math.Ceiling(left.TotalSeconds) + "초 뒤에 다시 입력할 수 있어요.";
                unlockTimer.Start();
                return;
            }
            unlockTimer.Stop();
            if (!box.Enabled)
            {
                box.Enabled = true;
                message.Text = "";
                box.Focus();
            }
        }

        void Check()
        {
            if (verify(box.Text))
            {
                failures = 0;
                EventHandler handler = Unlocked;
                Close();
                if (handler != null) handler(this, EventArgs.Empty);
                return;
            }
            box.Clear();
            failures++;
            if (failures >= MaxTries)
            {
                failures = 0;
                lockedUntil = DateTime.Now.AddSeconds(30);
                RefreshLock();
            }
            else
            {
                message.Text = "비밀번호가 맞지 않아요. (" + failures + "/" + MaxTries + ")";
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            idle.Dispose();
            unlockTimer.Dispose();
            base.OnFormClosed(e);
        }
    }

    // 가림 화면 모양: 단색이거나, 내가 고른 이미지를 화면 비율에 맞춰 꽉 채운 것
    static class CoverLook
    {
        public static Color ParseColor(string text)
        {
            try
            {
                return ColorTranslator.FromHtml(text);
            }
            catch (Exception)
            {
                return Color.Black;
            }
        }

        public static string ToHex(Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        // 파일을 잠그지 않게 메모리로 읽어 온다. 못 읽으면 null.
        public static Bitmap LoadImage(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
                using (var stream = new MemoryStream(File.ReadAllBytes(path)))
                using (Image image = Image.FromStream(stream))
                    return new Bitmap(image);
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex);
                return null;
            }
        }

        // 비율을 지키면서 화면을 꽉 채우고, 남는 부분은 가운데를 기준으로 잘라 낸다 (바탕화면의 "채우기")
        public static Bitmap Render(Size size, Color background, Image image)
        {
            var bmp = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height), PixelFormat.Format32bppRgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(background);
                if (image != null && image.Width > 0 && image.Height > 0)
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    RectangleF area = FillArea(size, image.Size);
                    g.DrawImage(image, area);
                }
            }
            return bmp;
        }

        public static RectangleF FillArea(Size target, Size source)
        {
            float scale = Math.Max(target.Width / (float)source.Width, target.Height / (float)source.Height);
            float w = source.Width * scale, h = source.Height * scale;
            return new RectangleF((target.Width - w) / 2f, (target.Height - h) / 2f, w, h);
        }
    }

    // 만든 사람 명함. exe 안에 넣어 두어 프로그램에서 지우거나 숨길 수 없다.
    static class CardImage
    {
        const string ResourceName = "namecard.png";

        public static Image Load()
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                {
                    if (stream == null) return null;
                    using (Image image = Image.FromStream(stream))
                        return new Bitmap(image);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex);
                return null;
            }
        }
    }

    // 명함을 크게 보여 주는 창
    class CardViewer : Form
    {
        public CardViewer(Image card, Icon icon)
        {
            Text = "만든 사람 명함 - " + About.Credit;
            Icon = icon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            BackColor = Color.White;

            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            float fit = Math.Min(1f, Math.Min(work.Width * 0.8f / card.Width, work.Height * 0.8f / card.Height));
            ClientSize = new Size((int)(card.Width * fit), (int)(card.Height * fit));
            var picture = new PictureBox();
            picture.Image = card;
            picture.SizeMode = PictureBoxSizeMode.Zoom;
            picture.Dock = DockStyle.Fill;
            picture.Cursor = Cursors.Hand;
            picture.Click += delegate { Close(); };
            Controls.Add(picture);
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) Close();
            };
        }
    }

    // 가림 화면 설정: 단색이나 내가 고른 이미지로 본체 모니터를 가린다
    class CoverSettingsForm : Form
    {
        readonly RadioButton colorMode = new RadioButton();
        readonly RadioButton imageMode = new RadioButton();
        readonly Button colorButton = new Button();
        readonly Button imageButton = new Button();
        readonly Label imageName = new Label();
        readonly PictureBox preview = new PictureBox();
        readonly Label error = new Label();
        Color color;
        string imagePath;
        Bitmap image;

        public string Mode
        {
            get { return imageMode.Checked ? "image" : "color"; }
        }

        public Color CoverColor
        {
            get { return color; }
        }

        public string ImagePath
        {
            get { return imagePath; }
        }

        public CoverSettingsForm(string mode, Color currentColor, string currentImage)
        {
            color = currentColor;
            imagePath = currentImage;
            image = CoverLook.LoadImage(currentImage);
            float scale = Ui.Scale;
            int wide = (int)(380 * scale);
            Text = "가림 화면 설정 - " + About.Credit;
            Font = Ui.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Label title = Ui.Label("본체 모니터를 가릴 때 보일 화면");
            title.Font = new Font(Ui.Font, FontStyle.Bold);
            colorMode.Text = "단색";
            colorMode.AutoSize = true;
            imageMode.Text = "이미지";
            imageMode.AutoSize = true;
            colorButton.Text = "색 고르기...";
            colorButton.AutoSize = true;
            imageButton.Text = "이미지 고르기...";
            imageButton.AutoSize = true;
            imageName.AutoSize = true;
            imageName.ForeColor = SystemColors.GrayText;
            imageName.MaximumSize = new Size(wide, 0);
            error.AutoSize = true;
            error.ForeColor = Color.Firebrick;
            error.MaximumSize = new Size(wide, 0);
            Rectangle primary = Screen.PrimaryScreen.Bounds;
            preview.Size = new Size(wide, Math.Max(1, wide * primary.Height / Math.Max(1, primary.Width)));
            preview.BorderStyle = BorderStyle.FixedSingle;
            preview.Margin = new Padding(0, (int)(6 * scale), 0, (int)(4 * scale));

            if (mode == "image" && image != null) imageMode.Checked = true;
            else colorMode.Checked = true;
            colorMode.CheckedChanged += delegate { UpdatePreview(); };
            colorButton.Click += delegate { PickColor(); };
            imageButton.Click += delegate { PickImage(); };

            var colorRow = new FlowLayoutPanel();
            colorRow.AutoSize = true;
            colorRow.WrapContents = false;
            colorRow.Controls.Add(colorMode);
            colorRow.Controls.Add(colorButton);
            var imageRow = new FlowLayoutPanel();
            imageRow.AutoSize = true;
            imageRow.WrapContents = false;
            imageRow.Controls.Add(imageMode);
            imageRow.Controls.Add(imageButton);

            var buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            var cancel = new Button();
            cancel.Text = "취소";
            cancel.AutoSize = true;
            cancel.DialogResult = DialogResult.Cancel;
            var save = new Button();
            save.Text = "저장";
            save.AutoSize = true;
            save.Click += delegate { OnSave(); };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);

            TableLayoutPanel grid = Ui.Grid(scale);
            Ui.AddWide(grid, title);
            Ui.AddWide(grid, colorRow);
            Ui.AddWide(grid, imageRow);
            Ui.AddWide(grid, imageName);
            Ui.AddWide(grid, preview);
            Ui.AddWide(grid, Ui.Note("이미지는 화면 비율에 맞춰 꽉 채워요. 남는 부분은 가운데를 기준으로 잘라서 늘어나 보이지 않아요. 가림 화면은 본체 모니터에만 보이고 원격 화면에는 안 보여요.", wide));
            Ui.AddWide(grid, error);
            Ui.AddWide(grid, buttons);
            Ui.AddWide(grid, Ui.Credit());
            Controls.Add(grid);
            AcceptButton = save;
            CancelButton = cancel;
            UpdatePreview();
        }

        void PickColor()
        {
            using (var dialog = new ColorDialog())
            {
                dialog.Color = color;
                dialog.FullOpen = true;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                color = dialog.Color;
            }
            colorMode.Checked = true;
            UpdatePreview();
        }

        void PickImage()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "가림 화면에 쓸 이미지 고르기";
                dialog.Filter = "이미지 (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                Bitmap loaded = CoverLook.LoadImage(dialog.FileName);
                if (loaded == null)
                {
                    error.Text = "이 파일은 이미지로 열 수 없어요. 다른 파일을 골라 주세요.";
                    return;
                }
                if (image != null) image.Dispose();
                image = loaded;
                imagePath = dialog.FileName;
            }
            error.Text = "";
            imageMode.Checked = true;
            UpdatePreview();
        }

        void UpdatePreview()
        {
            colorButton.BackColor = color;
            colorButton.ForeColor = color.GetBrightness() < 0.5f ? Color.White : Color.Black;
            colorButton.UseVisualStyleBackColor = false;
            imageName.Text = image != null ? "고른 이미지: " + Path.GetFileName(imagePath) : "아직 고른 이미지가 없어요.";
            Image old = preview.Image;
            preview.Image = CoverLook.Render(preview.ClientSize, imageMode.Checked ? Color.Black : color, imageMode.Checked ? image : null);
            if (old != null) old.Dispose();
        }

        void OnSave()
        {
            if (imageMode.Checked && image == null)
            {
                error.Text = "이미지를 먼저 골라 주세요.";
                return;
            }
            DialogResult = DialogResult.OK;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (image != null) image.Dispose();
                if (preview.Image != null) preview.Image.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // 트레이 아이콘을 잘 모르는 사람도 쓸 수 있게 앞에 띄우는 창.
    // 큰 버튼 하나로 가리고 풀고, 닫아도 프로그램은 트레이에서 계속 돈다.
    class MainWindow : Form
    {
        readonly Label dot = new Label();
        readonly Label monitorText = new Label();
        readonly Label stateText = new Label();
        readonly Button toggleButton = new Button();
        readonly Label hint = new Label();
        readonly CheckBox autoStart = new CheckBox();
        bool updating;

        public bool AllowClose;
        public event EventHandler ToggleClicked;
        public event EventHandler SettingsClicked;
        public event EventHandler QuitClicked;
        public event EventHandler AutoStartClicked;
        public event EventHandler HiddenToTray;
        public event EventHandler CoverClicked;
        public event EventHandler CardClicked;

        public string ToggleText
        {
            get { return toggleButton.Text; }
        }

        public string HintText
        {
            get { return hint.Text; }
        }

        public MainWindow(Icon icon)
        {
            float scale = Ui.Scale;
            int wide = (int)(340 * scale);
            Text = About.Credit;
            Icon = icon;
            Font = Ui.Font;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var bold = new Font(Ui.Font, FontStyle.Bold);
            dot.Text = "●";
            dot.AutoSize = true;
            dot.Font = bold;
            dot.Margin = new Padding(0);
            monitorText.AutoSize = true;
            monitorText.Font = bold;
            monitorText.Margin = new Padding(0);
            var monitorRow = new FlowLayoutPanel();
            monitorRow.AutoSize = true;
            monitorRow.WrapContents = false;
            monitorRow.Margin = new Padding(0, 0, 0, (int)(4 * scale));
            monitorRow.Controls.Add(dot);
            monitorRow.Controls.Add(monitorText);

            stateText.AutoSize = true;
            stateText.MaximumSize = new Size(wide, 0);
            stateText.Margin = new Padding(0, 0, 0, (int)(10 * scale));

            toggleButton.Font = new Font(Ui.Font.FontFamily, Ui.Font.Size * 1.4f, FontStyle.Bold);
            toggleButton.Size = new Size(wide, (int)(56 * scale));
            toggleButton.Margin = new Padding(0);
            toggleButton.Click += delegate { Raise(ToggleClicked); };

            hint.AutoSize = true;
            hint.MaximumSize = new Size(wide, 0);
            hint.ForeColor = SystemColors.GrayText;
            hint.Margin = new Padding(0, (int)(8 * scale), 0, (int)(8 * scale));

            autoStart.Text = "윈도우를 켤 때 자동으로 실행";
            autoStart.AutoSize = true;
            autoStart.CheckedChanged += delegate
            {
                if (!updating) Raise(AutoStartClicked);
            };

            var settings = new Button();
            settings.Text = "해제 방식 설정";
            settings.AutoSize = true;
            settings.Click += delegate { Raise(SettingsClicked); };
            var cover = new Button();
            cover.Text = "가림 화면 설정";
            cover.AutoSize = true;
            cover.Click += delegate { Raise(CoverClicked); };
            var quit = new Button();
            quit.Text = About.Name + " 끝내기";
            quit.AutoSize = true;
            quit.Click += delegate { Raise(QuitClicked); };
            var buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0, (int)(6 * scale), 0, 0);
            buttons.Controls.Add(settings);
            buttons.Controls.Add(cover);
            buttons.Controls.Add(quit);

            TableLayoutPanel grid = Ui.Grid(scale);
            Ui.AddWide(grid, monitorRow);
            Ui.AddWide(grid, stateText);
            Ui.AddWide(grid, toggleButton);
            Ui.AddWide(grid, hint);
            Ui.AddWide(grid, autoStart);
            Ui.AddWide(grid, buttons);
            Ui.AddWide(grid, Ui.Credit());

            // 만든 사람 명함 (exe 안에 들어 있어서 프로그램에서 지울 수 없다). 누르면 크게 보인다.
            Image card = CardImage.Load();
            if (card != null)
            {
                Label cardTitle = Ui.Note("만든 사람 명함 · 누르면 크게 볼 수 있어요", wide);
                cardTitle.Margin = new Padding(0, (int)(10 * scale), 0, (int)(4 * scale));
                var cardBox = new PictureBox();
                cardBox.Image = card;
                cardBox.SizeMode = PictureBoxSizeMode.Zoom;
                cardBox.Size = new Size(wide, wide * card.Height / card.Width);
                cardBox.BorderStyle = BorderStyle.FixedSingle;
                cardBox.BackColor = Color.White;
                cardBox.Cursor = Cursors.Hand;
                cardBox.Margin = new Padding(0);
                cardBox.Click += delegate { Raise(CardClicked); };
                Ui.AddWide(grid, cardTitle);
                Ui.AddWide(grid, cardBox);
            }
            Controls.Add(grid);
        }

        void Raise(EventHandler handler)
        {
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public void ShowState(bool hidden, MonitorReport monitor, string hotkey, bool hasPassword, bool startHidden, bool autoStartOn)
        {
            updating = true;
            dot.ForeColor = Art.ColorFor(monitor.Overall);
            monitorText.Text = " 본체 모니터 " + monitor.Label;
            stateText.Text = hidden ? "지금 본체 화면을 가리고 있어요. 원격에서는 그대로 보여요." : "지금 본체 화면이 그대로 보여요.";
            toggleButton.Text = hidden ? "화면 보이기" : "화면 가리기";
            string how = hotkey.Length > 0 ? "키보드에서 " + hotkey + "를 누르세요" : "바탕화면 '" + About.Name + "' 아이콘을 더블클릭하세요";
            hint.Text = "가린 화면을 다시 보려면 " + how + "." + (hasPassword ? " 비밀번호 입력 창이 떠요." : "")
                      + (startHidden ? "" : "\n잊어버리면 PC를 재부팅하면 풀려요.");
            autoStart.Checked = autoStartOn;
            updating = false;
        }

        public void Present()
        {
            if (!Visible) Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // X를 누르면 말없이 트레이로 사라지지 않고, 계속 실행할지 끝낼지 직접 고르게 한다
            if (!AllowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                CloseChoice choice;
                using (var ask = new CloseChoiceForm())
                {
                    ask.ShowDialog(this);
                    choice = ask.Choice;
                }
                if (choice == CloseChoice.KeepRunning)
                {
                    Hide();
                    Raise(HiddenToTray);
                }
                else if (choice == CloseChoice.Quit)
                {
                    // 닫히는 중에 다시 닫지 않도록 이 처리가 끝난 뒤에 끝낸다
                    BeginInvoke((MethodInvoker)delegate { Raise(QuitClicked); });
                }
                return;
            }
            base.OnFormClosing(e);
        }
    }

    enum CloseChoice { Cancel, KeepRunning, Quit }

    // 창을 닫을 때 고르는 작은 창. 트레이를 모르는 사람도 다시 찾을 수 있게 위치를 알려 준다.
    class CloseChoiceForm : Form
    {
        public CloseChoice Choice { get; private set; }

        public CloseChoiceForm()
        {
            float scale = Ui.Scale;
            int wide = (int)(340 * scale);
            Text = About.Credit;
            Font = Ui.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Label title = Ui.Label("창을 닫을까요?");
            title.Font = new Font(Ui.Font, FontStyle.Bold);
            var keep = new Button();
            keep.Text = "창만 닫기 (계속 실행)";
            keep.Size = new Size(wide, (int)(40 * scale));
            keep.Click += delegate { Pick(CloseChoice.KeepRunning); };
            var quit = new Button();
            quit.Text = About.Name + " 끝내기";
            quit.Size = keep.Size;
            quit.Click += delegate { Pick(CloseChoice.Quit); };
            var cancel = new Button();
            cancel.Text = "취소";
            cancel.AutoSize = true;
            cancel.Anchor = AnchorStyles.Right;
            cancel.Click += delegate { Pick(CloseChoice.Cancel); };

            TableLayoutPanel grid = Ui.Grid(scale);
            Ui.AddWide(grid, title);
            Ui.AddWide(grid, Ui.Note("창만 닫으면 " + About.Name + "는 계속 실행돼요. 화면 가리기와 단축키도 그대로 동작해요.\n"
                                   + "창을 다시 열려면 화면 오른쪽 아래, 시계 옆에 있는 " + About.Name + " 아이콘을 누르세요. 안 보이면 ^ 를 먼저 누르세요.", wide));
            Ui.AddWide(grid, keep);
            Ui.AddWide(grid, quit);
            Ui.AddWide(grid, cancel);
            Controls.Add(grid);
            AcceptButton = keep;
            CancelButton = cancel;
        }

        void Pick(CloseChoice choice)
        {
            Choice = choice;
            DialogResult = DialogResult.OK;
        }
    }

    class TrayApp : ApplicationContext
    {
        const int HotkeyId = 0xB1AC;

        readonly string exePath = Application.ExecutablePath;
        readonly Settings settings = new Settings(AppPaths.SettingsFile);
        readonly NotifyIcon tray = new NotifyIcon();
        readonly HotkeyWindow hotkeyWindow = new HotkeyWindow();
        readonly Control invoker = new Control();
        readonly System.Windows.Forms.Timer guard = new System.Windows.Forms.Timer();
        readonly System.Windows.Forms.Timer powerCheck = new System.Windows.Forms.Timer();
        readonly List<OverlayForm> overlays = new List<OverlayForm>();
        readonly MonitorWatcher watcher = new MonitorWatcher();
        // [가리는 중?, 본체 모니터 켜짐?]
        readonly Icon[,] icons = new Icon[2, 2];
        readonly MainWindow window;
        ToolStripMenuItem statusItem, toggleItem, startHiddenItem, autoStartItem;
        PasswordPrompt prompt;
        MonitorReport monitor = new MonitorReport();
        string screenLayout = "";
        string hotkeyText = "";
        bool hidden;
        bool powerMode; // 구형 윈도우: 검은 창 대신 모니터 전원을 DDC/CI로 꺼 둔 상태

        public TrayApp(bool? initial, bool showWindow)
        {
            IntPtr unused = invoker.Handle; // 다른 스레드에서 BeginInvoke를 받으려면 핸들이 먼저 있어야 함
            var legacySettings = new List<string>(AppPaths.LegacySettingsFiles);
            legacySettings.Insert(0, Path.Combine(Path.GetDirectoryName(exePath), "settings.ini"));
            Settings.MigrateLegacy(settings.FilePath, legacySettings.ToArray());
            bool firstRun = !settings.Exists;
            settings.Load();
            if (firstRun) settings.TrySave();

            for (int h = 0; h < 2; h++)
                for (int on = 0; on < 2; on++)
                    icons[h, on] = Art.LoadIcon(h == 1, on == 1);

            BuildMenu();
            window = new MainWindow(Art.LoadAppIcon());
            window.ToggleClicked += delegate { Toggle(); };
            window.SettingsClicked += delegate { OpenUnlockSettings(); };
            window.QuitClicked += delegate { RequestReveal(QuitNow); };
            window.AutoStartClicked += delegate { ToggleAutoStart(); };
            window.CoverClicked += delegate { OpenCoverSettings(); };
            window.CardClicked += delegate { ShowCard(); };
            window.HiddenToTray += delegate
            {
                Balloon(About.Name + "는 여기서 계속 실행 중이에요", "이 아이콘을 누르면 창이 다시 열려요.", ToolTipIcon.Info);
            };
            tray.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) ShowWindow();
            };
            tray.Visible = true;

            hotkeyWindow.Pressed += delegate { Toggle(); };
            string hotkeyError = ApplyHotkey(settings.Hotkey);

            guard.Interval = 500;
            guard.Tick += delegate { Guard(); };
            guard.Start();
            powerCheck.Interval = 4000;
            powerCheck.Tick += delegate
            {
                powerCheck.Stop();
                VerifyPowerOff();
            };

            watcher.Changed += delegate(MonitorReport report)
            {
                invoker.BeginInvoke((MethodInvoker)delegate { OnMonitorReport(report); });
            };
            watcher.Start();

            StartSignalListener();
            KeepLinksCurrent(firstRun);

            if (initial.HasValue ? initial.Value : settings.StartHidden) SetHidden(true);
            else UpdateTray();
            if (showWindow) ShowWindow();

            if (hotkeyError != null)
                Balloon("단축키를 등록하지 못했어요", hotkeyError + " 해제 방식 설정에서 다른 조합으로 바꿔 주세요.", ToolTipIcon.Warning);
            else if (firstRun && !hidden && !showWindow)
                Balloon(About.Credit, HotkeyHint() + "바탕화면 '" + About.Name + "' 아이콘으로 본체 화면을 가리거나 다시 보이게 해요. 원격 화면에는 계속 보여요.", ToolTipIcon.Info);
        }

        void ShowWindow()
        {
            UpdateTray();
            window.Present();
        }

        void ToggleAutoStart()
        {
            try
            {
                AutoStart.Set(!AutoStart.IsOn(exePath), exePath);
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex);
                Balloon("자동 실행 설정을 바꾸지 못했어요", ex.Message, ToolTipIcon.Warning);
            }
            UpdateTray();
        }

        string HotkeyHint()
        {
            return hotkeyText.Length > 0 ? hotkeyText + " 또는 " : "";
        }

        void BuildMenu()
        {
            var menu = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem(About.Name + " 창 열기", null, delegate { ShowWindow(); });
            openItem.Font = new Font(openItem.Font, FontStyle.Bold);
            statusItem = new ToolStripMenuItem("", null, delegate { ShowMonitorDetail(); });
            toggleItem = new ToolStripMenuItem("", null, delegate { Toggle(); });
            startHiddenItem = new ToolStripMenuItem("실행하면 바로 가리기", null, delegate
            {
                settings.StartHidden = !settings.StartHidden;
                string error = settings.TrySave();
                if (error != null) Balloon("설정을 저장하지 못했어요", error, ToolTipIcon.Warning);
                UpdateTray();
            });
            autoStartItem = new ToolStripMenuItem("윈도우 시작 시 자동 실행", null, delegate { ToggleAutoStart(); });
            menu.Items.Add(openItem);
            menu.Items.Add(statusItem);
            menu.Items.Add(toggleItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("해제 방식 설정...", null, delegate { OpenUnlockSettings(); }));
            menu.Items.Add(startHiddenItem);
            menu.Items.Add(autoStartItem);
            menu.Items.Add(new ToolStripMenuItem("바탕화면 버튼 만들기", null, delegate { MakeDesktopButton(true); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("가림 화면 설정...", null, delegate { OpenCoverSettings(); }));
            menu.Items.Add(new ToolStripMenuItem("만든 사람 명함 보기", null, delegate { ShowCard(); }));
            menu.Items.Add(new ToolStripMenuItem(About.Credit + " (v" + About.ShortVersion + ")", null, delegate { ShowAbout(); }));
            menu.Items.Add(new ToolStripMenuItem("종료", null, delegate { RequestReveal(QuitNow); }));
            menu.Opening += delegate
            {
                settings.Load(); // 메모장 등으로 파일을 직접 고쳤어도 체크 표시가 맞게
                startHiddenItem.Checked = settings.StartHidden;
                autoStartItem.Checked = AutoStart.IsOn(exePath);
            };
            tray.ContextMenuStrip = menu;
        }

        void UpdateTray()
        {
            bool on = monitor.Overall == PowerState.On;
            statusItem.Text = "본체 모니터: " + monitor.Label;
            statusItem.Image = DotImage(Art.ColorFor(monitor.Overall));
            toggleItem.Text = hidden ? "화면 보이기" : "화면 가리기";
            toggleItem.ShortcutKeyDisplayString = hotkeyText;
            tray.Icon = icons[hidden ? 1 : 0, on ? 1 : 0];
            tray.Text = About.Credit + "\n본체 모니터 " + monitor.Label + " · " + (hidden ? "화면 가리는 중" : "화면 보이는 중");
            window.ShowState(hidden, monitor, hotkeyText, settings.PasswordHash.Length > 0, settings.StartHidden, AutoStart.IsOn(exePath));
        }

        static Bitmap DotImage(Color color)
        {
            var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            using (var brush = new SolidBrush(color))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(brush, 3, 3, 10, 10);
            }
            return bmp;
        }

        void OnMonitorReport(MonitorReport report)
        {
            bool wasOn = monitor.Overall == PowerState.On;
            bool firstReport = !monitor.Ready;
            monitor = report;
            UpdateTray();
            // 구형 윈도우 모드: 가리는 중에 누가 모니터를 켜면 다시 끈다
            if (hidden && powerMode && report.On > 0) MonitorPower.SetAll(MonitorPower.Off);
            if (!hidden && !firstReport && !wasOn && report.Overall == PowerState.On)
                Balloon("본체 모니터가 켜졌어요", "지금 본체 화면이 그대로 보여요. " + HotkeyHint() + "바탕화면 버튼으로 가릴 수 있어요.", ToolTipIcon.Warning);
        }

        void ShowMonitorDetail()
        {
            MessageBox.Show(
                "본체 모니터: " + monitor.Label + "\n\n" + monitor.Detail + "\n" +
                "모니터에 DDC/CI로 전원 상태를 직접 물어봐서 판단해요 (3초마다).\n" +
                "계속 '응답 없음'이면 모니터 메뉴(OSD)에서 DDC/CI를 켜 주세요." +
                (powerMode ? "\n\n지금은 구형 윈도우 방식(모니터 전원 끄기)으로 가리고 있어요." : ""),
                About.Credit, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void ShowAbout()
        {
            if (About.Link.Length == 0)
            {
                MessageBox.Show(About.Text, About.Credit, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(About.Text + "\n\n" + About.Link + "\n\n주소를 열까요?", About.Credit,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                try
                {
                    Process.Start(About.Link);
                }
                catch (Exception ex)
                {
                    ErrorLog.Write(ex);
                }
            }
        }

        // 가리는 중에는 알림 풍선도 본체 화면 위에 뜰 수 있으니 오류만 띄운다
        void Balloon(string title, string text, ToolTipIcon icon)
        {
            if (hidden && icon == ToolTipIcon.Info) return;
            tray.ShowBalloonTip(6000, title, text, icon);
        }

        void Toggle()
        {
            if (hidden) RequestReveal(null);
            else SetHidden(true);
        }

        // 비밀번호가 걸려 있으면 먼저 묻고, 맞을 때만 화면을 보이게 한 뒤 after를 실행한다
        void RequestReveal(Action after)
        {
            if (!hidden || settings.PasswordHash.Length == 0)
            {
                SetHidden(false);
                if (after != null) after();
                return;
            }
            if (prompt != null && !prompt.IsDisposed)
            {
                prompt.ShowOnTop();
                return;
            }
            prompt = new PasswordPrompt(delegate(string text) { return PasswordHash.Verify(text, settings.PasswordHash); });
            prompt.Unlocked += delegate
            {
                SetHidden(false);
                if (after != null) after();
            };
            prompt.FormClosed += delegate { prompt = null; };
            prompt.ShowOnTop();
        }

        void SetHidden(bool value)
        {
            if (value)
            {
                if (hidden) return;
                string error = Cover();
                if (error != null)
                {
                    UpdateTray();
                    Balloon("화면을 가리지 못했어요", error, ToolTipIcon.Error);
                    return;
                }
                hidden = true;
                Native.SetThreadExecutionState(Native.ES_CONTINUOUS | Native.ES_SYSTEM_REQUIRED);
            }
            else
            {
                hidden = false;
                Uncover();
                Native.SetThreadExecutionState(Native.ES_CONTINUOUS);
            }
            UpdateTray();
        }

        // 화면마다 검은 창을 하나씩 띄운다. 원격에서 빠지는 설정이 안 되는 구형 윈도우에서는
        // 검은 창을 띄우면 원격 화면까지 까매지니, 대신 모니터 전원을 DDC/CI로 끈다.
        string Cover()
        {
            // 이미 가리는 중이면(가림 화면을 바꿀 때) 새 창을 먼저 띄운 뒤 옛 창을 치워서 화면이 한순간도 드러나지 않게 한다
            var previous = new List<OverlayForm>(overlays);
            overlays.Clear();
            if (powerMode) Uncover();
            screenLayout = CurrentLayout();
            // 원격 데스크톱(RDP)으로 접속한 동안은 윈도우가 본체 모니터를 잠금 화면으로 바꿔 두니 띄울 필요가 없다
            if (SystemInformation.TerminalServerSession)
            {
                DisposeAll(previous);
                return null;
            }
            Color fill = CoverLook.ParseColor(settings.CoverColor);
            Bitmap picture = settings.CoverMode == "image" ? CoverLook.LoadImage(settings.CoverImage) : null;
            try
            {
                foreach (Screen screen in Screen.AllScreens)
                {
                    var form = new OverlayForm(screen.Bounds, picture != null ? Color.Black : fill, true);
                    IntPtr handle = form.Handle;
                    if (!form.ExcludedFromCapture)
                    {
                        form.Dispose();
                        DisposeAll(previous);
                        Uncover();
                        return CoverByPower();
                    }
                    if (picture != null) form.SetCover(CoverLook.Render(screen.Bounds.Size, Color.Black, picture));
                    overlays.Add(form);
                    form.Show();
                    form.PinTop();
                }
            }
            finally
            {
                if (picture != null) picture.Dispose();
            }
            DisposeAll(previous);
            return null;
        }

        static void DisposeAll(List<OverlayForm> forms)
        {
            foreach (OverlayForm form in forms)
            {
                if (!form.IsDisposed) form.Dispose();
            }
        }

        void OpenCoverSettings()
        {
            settings.Load();
            using (var form = new CoverSettingsForm(settings.CoverMode, CoverLook.ParseColor(settings.CoverColor), settings.CoverImage))
            {
                if ((window.Visible ? form.ShowDialog(window) : form.ShowDialog()) != DialogResult.OK) return;
                settings.CoverColor = CoverLook.ToHex(form.CoverColor);
                if (form.Mode == "image" && !string.Equals(form.ImagePath, settings.CoverImage, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // 원본을 옮기거나 지워도 계속 쓸 수 있게 설정 폴더로 복사해 둔다
                        Directory.CreateDirectory(AppPaths.DataDir);
                        foreach (string old in Directory.GetFiles(AppPaths.DataDir, "cover-image.*")) File.Delete(old);
                        string target = AppPaths.CoverImageFile(Path.GetExtension(form.ImagePath).ToLowerInvariant());
                        File.Copy(form.ImagePath, target, true);
                        settings.CoverImage = target;
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Write(ex);
                        Balloon("이미지를 저장하지 못했어요", ex.Message, ToolTipIcon.Warning);
                        return;
                    }
                }
                settings.CoverMode = form.Mode;
                string saveError = settings.TrySave();
                if (saveError != null) Balloon("설정을 저장하지 못했어요", saveError, ToolTipIcon.Warning);
                if (hidden && !powerMode)
                {
                    string error = Cover();
                    if (error != null) SetHidden(false);
                }
                Balloon("가림 화면을 바꿨어요", form.Mode == "image" ? "고른 이미지로 가려요." : "고른 색으로 가려요.", ToolTipIcon.Info);
            }
        }

        void ShowCard()
        {
            Image card = CardImage.Load();
            if (card == null) return;
            using (var viewer = new CardViewer(card, Art.LoadAppIcon()))
            {
                if (window.Visible) viewer.ShowDialog(window);
                else viewer.ShowDialog();
            }
            card.Dispose();
        }

        string CoverByPower()
        {
            if (MonitorPower.SetAll(MonitorPower.Off) == 0)
                return "이 PC는 윈도우 10 2004 미만이라 검은 화면을 쓸 수 없고, 모니터도 DDC/CI 전원 제어에 응답하지 않아요. 모니터 메뉴(OSD)에서 DDC/CI를 켜 보세요.";
            powerMode = true;
            powerCheck.Stop();
            powerCheck.Start();
            return null;
        }

        // 끄기 명령을 받기만 하고 따르지 않는 모니터가 많아서, 몇 초 뒤 정말 꺼졌는지 확인한다.
        // 안 꺼졌으면 가린 척하지 않고 풀어 둔 채 알린다.
        void VerifyPowerOff()
        {
            if (!hidden || !powerMode) return;
            foreach (Screen screen in Screen.AllScreens)
            {
                string name;
                if (MonitorPower.QueryPowerMode(screen.Bounds, out name) != 1) continue;
                SetHidden(false);
                Balloon("화면을 가리지 못했어요",
                        "이 윈도우는 검은 화면 방식을 쓸 수 없고, 모니터(" + name + ")도 전원 끄기 명령을 따르지 않아요.",
                        ToolTipIcon.Error);
                return;
            }
        }

        void Uncover()
        {
            foreach (OverlayForm form in overlays)
            {
                if (!form.IsDisposed) form.Dispose();
            }
            overlays.Clear();
            if (powerMode)
            {
                powerMode = false;
                MonitorPower.SetAll(MonitorPower.On);
            }
        }

        static string CurrentLayout()
        {
            var sb = new StringBuilder();
            foreach (Screen screen in Screen.AllScreens) sb.Append(screen.Bounds.ToString());
            return sb.ToString();
        }

        // 0.5초마다: 모니터를 껐다 켜서 연결이 바뀌었으면 다시 덮고, 다른 창이 위로 올라왔으면 다시 맨 위로
        void Guard()
        {
            if (!hidden || powerMode) return;
            if (SystemInformation.TerminalServerSession)
            {
                if (overlays.Count > 0) Uncover();
                return;
            }
            if (overlays.Count == 0 || CurrentLayout() != screenLayout || overlays.Exists(f => f.IsDisposed))
            {
                string error = Cover();
                if (error != null) SetHidden(false);
            }
            else
            {
                foreach (OverlayForm form in overlays) form.PinTop();
            }
            // 비밀번호 창은 검은 창보다 위에 있어야 본체 모니터에서 보인다
            if (prompt != null && !prompt.IsDisposed) prompt.PinTop();
        }

        bool RegisterHotkey(uint mods, uint vk)
        {
            return Native.RegisterHotKey(hotkeyWindow.Handle, HotkeyId, mods | Native.MOD_NOREPEAT, vk)
                || Native.RegisterHotKey(hotkeyWindow.Handle, HotkeyId, mods, vk); // MOD_NOREPEAT를 모르는 비스타 이하
        }

        // 새 단축키 등록. 실패하면 이전 단축키를 되살리고 이유를 돌려준다.
        string ApplyHotkey(string text)
        {
            uint mods, vk;
            if (!HotkeyParser.TryParse(text, out mods, out vk))
                return "'" + text + "'는 단축키 형식이 아니에요 (예: Ctrl+Alt+B).";
            string previous = hotkeyText;
            Native.UnregisterHotKey(hotkeyWindow.Handle, HotkeyId);
            if (RegisterHotkey(mods, vk))
            {
                hotkeyText = text;
                return null;
            }
            hotkeyText = "";
            if (previous.Length > 0 && HotkeyParser.TryParse(previous, out mods, out vk) && RegisterHotkey(mods, vk))
                hotkeyText = previous;
            return "'" + text + "'는 다른 프로그램이 이미 쓰고 있어요.";
        }

        void OpenUnlockSettings()
        {
            settings.Load();
            // 설정 창에서 누르는 조합이 바로 가리기/보이기로 실행되지 않게 잠시 해제
            Native.UnregisterHotKey(hotkeyWindow.Handle, HotkeyId);
            string current = hotkeyText.Length > 0 ? hotkeyText : settings.Hotkey;
            using (var form = new UnlockSettingsForm(current, settings.PasswordHash.Length > 0, settings.StartHidden))
            {
                bool saved = (window.Visible ? form.ShowDialog(window) : form.ShowDialog()) == DialogResult.OK;
                string hotkeyError = ApplyHotkey(saved ? form.Hotkey : current);
                if (!saved) return;
                if (hotkeyError == null) settings.Hotkey = form.Hotkey;
                if (form.PasswordHashResult != null) settings.PasswordHash = form.PasswordHashResult;
                string saveError = settings.TrySave();
                UpdateTray();
                if (saveError != null)
                    Balloon("설정을 저장하지 못했어요", saveError, ToolTipIcon.Warning);
                else if (hotkeyError != null)
                    Balloon("단축키를 바꾸지 못했어요", hotkeyError + (hotkeyText.Length > 0 ? " 지금은 " + hotkeyText + " 그대로예요." : ""), ToolTipIcon.Warning);
                else
                    Balloon("해제 방식을 저장했어요", "단축키 " + hotkeyText + " · 비밀번호 " + (settings.PasswordHash.Length > 0 ? "사용" : "안 씀"), ToolTipIcon.Info);
            }
        }

        void MakeDesktopButton(bool announce)
        {
            try
            {
                Shortcut.Create(Shortcut.DesktopPath, exePath, "--toggle", "본체 모니터 가리기/보이기 전환 - " + About.Credit);
                if (announce) Balloon("바탕화면 버튼을 만들었어요", "'" + About.Name + "' 아이콘을 더블클릭하면 가리기/보이기가 바뀌어요.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex);
                Balloon("바탕화면 버튼을 만들지 못했어요", ex.Message, ToolTipIcon.Warning);
            }
        }

        // 처음 실행이면 바탕화면 버튼을 만들고, 폴더를 옮겼으면 버튼·자동 실행이 새 위치를 가리키게 고친다
        void KeepLinksCurrent(bool firstRun)
        {
            // 예전 이름으로 만든 바로가기는 지우고 지금 이름으로 다시 만든다
            bool hadOldDesktop = false, hadOldStartup = false;
            try
            {
                foreach (string oldName in Shortcut.LegacyFileNames)
                {
                    string oldDesktop = Path.Combine(Path.GetDirectoryName(Shortcut.DesktopPath), oldName);
                    string oldStartup = Path.Combine(Path.GetDirectoryName(AutoStart.LinkPath), oldName);
                    if (File.Exists(oldDesktop))
                    {
                        File.Delete(oldDesktop);
                        hadOldDesktop = true;
                    }
                    if (File.Exists(oldStartup))
                    {
                        File.Delete(oldStartup);
                        hadOldStartup = true;
                    }
                }
                if (hadOldStartup) AutoStart.Set(true, exePath);
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex);
            }

            if (firstRun || hadOldDesktop)
            {
                MakeDesktopButton(false);
            }
            else if (File.Exists(Shortcut.DesktopPath))
            {
                try
                {
                    if (!string.Equals(Shortcut.ReadTarget(Shortcut.DesktopPath), exePath, StringComparison.OrdinalIgnoreCase))
                        MakeDesktopButton(false);
                }
                catch (Exception ex)
                {
                    ErrorLog.Write(ex);
                }
            }
            try
            {
                AutoStart.MigrateFromRunKey(exePath);
                // 위치나 실행 옵션(--startup)이 예전 것일 수 있으니 켜져 있으면 늘 새로 만든다
                if (AutoStart.Exists) AutoStart.Set(true, exePath);
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex);
            }
        }

        // 두 번째로 실행된 exe(바탕화면 버튼 등)가 보내는 명령을 받는다
        void StartSignalListener()
        {
            var handles = new WaitHandle[Program.Signals.Length];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = new EventWaitHandle(false, EventResetMode.AutoReset, Program.Signals[i]);
            var listener = new Thread(delegate()
            {
                while (true)
                {
                    int index = WaitHandle.WaitAny(handles);
                    invoker.BeginInvoke((MethodInvoker)delegate { OnSignal(index); });
                }
            });
            listener.IsBackground = true;
            listener.Start();
        }

        void OnSignal(int index)
        {
            switch (index)
            {
                case Program.SignalToggle: Toggle(); break;
                case Program.SignalOn: SetHidden(true); break;
                case Program.SignalOff: RequestReveal(null); break;
                case Program.SignalHello: ShowWindow(); break;
                case Program.SignalQuit: RequestReveal(QuitNow); break;
            }
        }

        void QuitNow()
        {
            guard.Stop();
            powerCheck.Stop();
            if (prompt != null && !prompt.IsDisposed) prompt.Dispose();
            Uncover();
            Native.SetThreadExecutionState(Native.ES_CONTINUOUS);
            Native.UnregisterHotKey(hotkeyWindow.Handle, HotkeyId);
            hotkeyWindow.Dispose();
            window.AllowClose = true;
            window.Close();
            tray.Visible = false;
            tray.Dispose();
            ExitThread();
        }
    }

    static class SelfTest
    {
        // 기능별로 확인하고, 화면 구석에 작은 분홍 사각형을 잠깐 띄워서
        // 1) 일반 창은 캡처에 잡히는지 (테스트가 제대로 동작하는지 대조군)
        // 2) 가림 창은 캡처에서 빠지는지 (= 원격에선 안 보이는지), 클릭이 뒤로 통과하는지
        // 를 확인한다. 화면 전체를 가리지는 않는다.
        public static int Run(string outPath)
        {
            var log = new StringBuilder();
            int fails = 0;
            Action<bool, string> check = delegate(bool ok, string what)
            {
                log.AppendLine((ok ? "PASS  " : "FAIL  ") + what);
                if (!ok) fails++;
            };

            uint mods, vk;
            check(HotkeyParser.TryParse("Ctrl+Alt+B", out mods, out vk) && mods == (Native.MOD_CONTROL | Native.MOD_ALT) && vk == (uint)Keys.B, "hotkey Ctrl+Alt+B");
            check(HotkeyParser.TryParse("ctrl + shift + f12", out mods, out vk) && mods == (Native.MOD_CONTROL | Native.MOD_SHIFT) && vk == (uint)Keys.F12, "hotkey ctrl+shift+f12");
            check(HotkeyParser.TryParse("Ctrl+Alt+Win+1", out mods, out vk) && mods == (Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_WIN) && vk == (uint)Keys.D1, "hotkey with digit and Win");
            check(!HotkeyParser.TryParse("B", out mods, out vk), "hotkey without modifier rejected");
            check(!HotkeyParser.TryParse("Ctrl+Nope", out mods, out vk), "unknown key rejected");
            check(!HotkeyParser.TryParse("Ctrl+Alt", out mods, out vk), "modifier-only rejected");

            string captured = HotkeyBox.Describe(Keys.Control | Keys.Shift, Keys.D7);
            check(captured == "Ctrl+Shift+7" && HotkeyParser.TryParse(captured, out mods, out vk) && vk == (uint)Keys.D7, "captured hotkey text parses back (" + captured + ")");
            captured = HotkeyBox.Describe(Keys.Control | Keys.Alt, Keys.F9);
            check(captured == "Ctrl+Alt+F9" && HotkeyParser.TryParse(captured, out mods, out vk) && vk == (uint)Keys.F9, "captured function key parses back (" + captured + ")");

            using (Image card = CardImage.Load())
                check(card != null && card.Width > 0, "business card is built into the exe");
            RectangleF wideOnTall = CoverLook.FillArea(new Size(100, 100), new Size(200, 100));
            check(wideOnTall.Height == 100 && wideOnTall.Width == 200 && wideOnTall.X == -50, "cover image fills the screen and crops the sides evenly");
            RectangleF tallOnWide = CoverLook.FillArea(new Size(1920, 1080), new Size(1080, 1920));
            check(tallOnWide.Width == 1920 && Math.Abs(tallOnWide.Y + (tallOnWide.Height - 1080) / 2f) < 0.01f, "portrait image on a wide screen keeps its ratio");
            check(CoverLook.ToHex(CoverLook.ParseColor("#1A2B3C")) == "#1A2B3C" && CoverLook.ParseColor("nonsense").ToArgb() == Color.Black.ToArgb(), "cover color reads and writes");

            string hash = PasswordHash.Create("hononoo-1234");
            check(PasswordHash.Verify("hononoo-1234", hash), "password verifies");
            check(!PasswordHash.Verify("hononoo-1235", hash), "wrong password rejected");
            check(!PasswordHash.Verify("hononoo-1234", "") && !PasswordHash.Verify("x", "garbage"), "empty or broken hash rejected");
            check(PasswordHash.Create("same") != PasswordHash.Create("same"), "each hash gets its own salt");

            string tmpDir = Path.Combine(Path.GetTempPath(), "mb_selftest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmpDir);
            try
            {
                var saved = new Settings(Path.Combine(tmpDir, "sub", "settings.ini"));
                saved.Hotkey = "Ctrl+Shift+F9";
                saved.StartHidden = true;
                saved.PasswordHash = hash;
                check(saved.TrySave() == null, "settings save creates its folder");
                var loaded = new Settings(saved.FilePath);
                loaded.Load();
                check(loaded.Hotkey == "Ctrl+Shift+F9" && loaded.StartHidden && loaded.PasswordHash == hash, "settings save/load roundtrip with password");

                string legacy = Path.Combine(tmpDir, "legacy.ini");
                File.WriteAllText(legacy, "hotkey=Ctrl+Alt+F1\r\n");
                string moved = Path.Combine(tmpDir, "appdata", "settings.ini");
                Settings.MigrateLegacy(moved, Path.Combine(tmpDir, "missing.ini"), legacy);
                var migrated = new Settings(moved);
                migrated.Load();
                check(!File.Exists(legacy) && migrated.Hotkey == "Ctrl+Alt+F1", "old settings next to exe move to AppData");

                string lnk = Path.Combine(tmpDir, "테스트 바로가기.lnk");
                Shortcut.Create(lnk, Application.ExecutablePath, "--toggle", About.Credit);
                check(string.Equals(Shortcut.ReadTarget(lnk), Application.ExecutablePath, StringComparison.OrdinalIgnoreCase), "shortcut via IShellLink (no WScript)");
            }
            finally
            {
                try { Directory.Delete(tmpDir, true); } catch (Exception) { }
            }

            FileVersionInfo info = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);
            check(info.CompanyName == About.Company && info.ProductName == About.Name
                  && info.LegalCopyright.Contains(About.Author) && info.Comments.Contains(About.Author),
                  "exe properties: company " + info.CompanyName + ", " + info.LegalCopyright);

            using (var ms = new MemoryStream(Art.BuildIco(true, true)))
            using (var icon = new Icon(ms, 32, 32))
                check(icon.Width == 32 && icon.Height == 32, "generated icon loads");

            using (var window = new MainWindow(Art.LoadAppIcon()))
            {
                IntPtr unused = window.Handle;
                var monitorOn = new MonitorReport();
                monitorOn.Ready = true;
                monitorOn.On = 1;
                window.ShowState(false, monitorOn, "Ctrl+Alt+B", false, false, true);
                check(window.ToggleText == "화면 가리기" && window.HintText.Contains("Ctrl+Alt+B") && window.HintText.Contains("재부팅"),
                      "window: shows cover button and how to undo it");
                window.ShowState(true, monitorOn, "", true, true, false);
                check(window.ToggleText == "화면 보이기" && window.HintText.Contains(About.Name) && window.HintText.Contains("비밀번호") && !window.HintText.Contains("재부팅"),
                      "window: covered state shows reveal button and password hint");
            }
            using (var ask = new CloseChoiceForm())
            {
                IntPtr unused = ask.Handle;
                check(ask.Choice == CloseChoice.Cancel, "close dialog defaults to cancel");
            }

            var report = new MonitorReport();
            check(report.Label == "확인 중" && report.Overall == PowerState.Unknown, "monitor report starts as checking");
            report.Ready = true;
            report.On = 1;
            report.Off = 1;
            check(report.Overall == PowerState.On && report.Label == "켜짐 (1/2대)", "any monitor on counts as on");

            Rectangle screen = Screen.PrimaryScreen.Bounds;
            var box = new Rectangle(screen.Right - 200, screen.Top + 80, 100, 100);

            using (ShowAndWait(new OverlayForm(box, Probe, false)))
            {
                double share = CaptureShare(box, IsProbe);
                check(share > 0.9, "control: normal window shows up in screen capture (" + share.ToString("P0") + ")");
            }

            using (OverlayForm overlay = ShowAndWait(new OverlayForm(box, Probe, true)))
            {
                check(overlay.ExcludedFromCapture, "overlay: display affinity set to EXCLUDEFROMCAPTURE");
                double share = CaptureShare(box, IsProbe);
                check(share < 0.05, "overlay: absent from screen capture (" + share.ToString("P0") + " probe pixels)");
                check(ClickThrough(box, overlay.Handle), "overlay: clicks pass through to the window behind");
            }

            MonitorReport real = new MonitorWatcher().Check();
            log.AppendLine("INFO  monitor power right now: " + real.Label);
            foreach (string line in real.Detail.Split('\n'))
            {
                if (line.Trim().Length > 0) log.AppendLine("INFO    " + line.Trim());
            }

            log.AppendLine(fails == 0 ? "ALL PASSED" : fails + " FAILED");
            File.WriteAllText(outPath, log.ToString(), new UTF8Encoding(false));
            return fails == 0 ? 0 : 1;
        }

        static readonly Color Probe = Color.FromArgb(255, 0, 255);

        static bool IsProbe(Color c)
        {
            return c.R == Probe.R && c.G == Probe.G && c.B == Probe.B;
        }

        static OverlayForm ShowAndWait(OverlayForm form)
        {
            IntPtr handle = form.Handle;
            form.Show();
            form.PinTop();
            Pump(700);
            return form;
        }

        static void Pump(int milliseconds)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < milliseconds)
            {
                Application.DoEvents();
                Thread.Sleep(20);
            }
        }

        // 원격 프로그램처럼 레이어드 창까지 포함해서 화면을 찍는다 (CAPTUREBLT)
        static double CaptureShare(Rectangle area, Func<Color, bool> match)
        {
            using (var shot = new Bitmap(area.Width, area.Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(shot))
                {
                    IntPtr dest = g.GetHdc();
                    IntPtr screenDc = Native.GetDC(IntPtr.Zero);
                    try
                    {
                        Native.BitBlt(dest, 0, 0, area.Width, area.Height, screenDc, area.X, area.Y, Native.SRCCOPY | Native.CAPTUREBLT);
                    }
                    finally
                    {
                        Native.ReleaseDC(IntPtr.Zero, screenDc);
                        g.ReleaseHdc(dest);
                    }
                }
                int hits = 0;
                for (int y = 0; y < shot.Height; y++)
                {
                    for (int x = 0; x < shot.Width; x++)
                    {
                        if (match(shot.GetPixel(x, y))) hits++;
                    }
                }
                return hits / (double)(shot.Width * shot.Height);
            }
        }

        static bool ClickThrough(Rectangle box, IntPtr handle)
        {
            var center = new Native.POINT();
            center.X = box.X + box.Width / 2;
            center.Y = box.Y + box.Height / 2;
            IntPtr hit = Native.WindowFromPoint(center);
            return hit != IntPtr.Zero && Native.GetAncestor(hit, Native.GA_ROOT) != handle;
        }
    }

    // 구형 윈도우용 대체 모드 진단: 모니터를 DDC/CI로 잠깐 껐다가 다시 켠다
    // 블로그·README에 넣을 그림을 만든다.
    // 창은 화면 밖에서 그려서 찍기 때문에 사용자 화면에는 아무것도 뜨지 않는다.
    static class DocImages
    {
        public static int Run(string dir)
        {
            Directory.CreateDirectory(dir);
            var monitor = new MonitorReport();
            monitor.Ready = true;
            monitor.On = 1;

            using (var window = new MainWindow(Art.LoadAppIcon()))
            {
                window.AllowClose = true;
                window.ShowState(false, monitor, "Ctrl+Alt+B", false, false, true);
                Capture(window, Path.Combine(dir, "window-visible.png"));
                window.ShowState(true, monitor, "Ctrl+Alt+B", true, false, true);
                Capture(window, Path.Combine(dir, "window-covered.png"));
            }
            using (var form = new UnlockSettingsForm("Ctrl+Shift+F9", false, false))
                Capture(form, Path.Combine(dir, "unlock-settings.png"));
            using (var form = new CloseChoiceForm())
                Capture(form, Path.Combine(dir, "close-choice.png"));
            using (var form = new CoverSettingsForm("color", Color.FromArgb(13, 15, 20), ""))
                Capture(form, Path.Combine(dir, "cover-settings.png"));
            using (var form = new PasswordPrompt(delegate(string text) { return false; }))
                Capture(form, Path.Combine(dir, "password-prompt.png"));
            DrawConcept(Path.Combine(dir, "concept.png"));
            DrawScenes(Path.Combine(dir, "scenes"));
            return 0;
        }

        // 창 안쪽은 WinForms가 직접 그리게 하고(화면 밖이어도 됨), 제목 표시줄은 윈도우 10 모양으로 그려 붙인다
        static void Capture(Form form, string path)
        {
            form.StartPosition = FormStartPosition.Manual;
            form.ShowInTaskbar = false;
            form.Location = new Point(-20000, -20000);
            form.Show();
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 400)
            {
                Application.DoEvents();
                Thread.Sleep(20);
            }
            Size client = form.ClientSize;
            const int titleHeight = 34;
            using (var inside = new Bitmap(client.Width, client.Height, PixelFormat.Format32bppArgb))
            using (var shot = new Bitmap(client.Width + 2, client.Height + titleHeight + 1, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(inside))
                    g.Clear(form.BackColor);
                foreach (Control child in form.Controls)
                    child.DrawToBitmap(inside, new Rectangle(child.Location, child.Size));
                using (Graphics g = Graphics.FromImage(shot))
                using (var titleFont = new Font("Malgun Gothic", 12f, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var pen = new Pen(Color.FromArgb(40, 40, 40), 1.2f))
                {
                    g.Clear(Color.FromArgb(170, 170, 175));
                    g.FillRectangle(Brushes.White, 1, 1, client.Width, titleHeight - 1);
                    if (form.Icon != null && form.ShowIcon && form.FormBorderStyle != FormBorderStyle.FixedDialog)
                        g.DrawIcon(new Icon(form.Icon, 16, 16), new Rectangle(10, 9, 16, 16));
                    int textX = form.FormBorderStyle == FormBorderStyle.FixedDialog ? 12 : 34;
                    TextRenderer.DrawText(g, form.Text, titleFont, new Point(textX, 9), Color.Black);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    int x = client.Width - 24;
                    g.DrawLine(pen, x - 5, 12, x + 5, 22);
                    g.DrawLine(pen, x + 5, 12, x - 5, 22);
                    if (form.MinimizeBox) g.DrawLine(pen, x - 51, 17, x - 41, 17);
                    g.DrawImage(inside, 1, titleHeight);
                }
                shot.Save(path, ImageFormat.Png);
            }
            form.Hide();
        }

        // 시연 장면 6개 (GIF·영상용). 실제 검은 화면은 화면 녹화에 안 찍히게 만든 거라, 흐름을 그림으로 보여 준다.
        public static void DrawScenes(string dir)
        {
            Directory.CreateDirectory(dir);
            DrawScene(Path.Combine(dir, "scene1.png"), 1, "평소에는 본체 모니터와 원격 화면이 똑같이 보여요", false, false, false, false);
            DrawScene(Path.Combine(dir, "scene2.png"), 2, "모니터 가리기 창에서 [화면 가리기]를 눌러요", false, true, false, false);
            DrawScene(Path.Combine(dir, "scene3.png"), 3, "본체 모니터만 까맣게! 원격 화면은 그대로예요", true, false, false, false);
            DrawScene(Path.Combine(dir, "scene4.png"), 4, "누가 모니터를 껐다 켜도 까만 화면뿐이에요", true, false, true, false);
            DrawScene(Path.Combine(dir, "scene5.png"), 5, "자리에 돌아오면 내 단축키를 눌러요", true, false, false, true);
            DrawScene(Path.Combine(dir, "scene6.png"), 6, "다시 원래 화면! 내 화면은 나만 봐요", false, false, false, false);
        }

        const int SceneW = 1200, SceneH = 630;
        static readonly Rectangle LeftMonitor = new Rectangle(70, 130, 440, 270);
        static readonly Rectangle RightMonitor = new Rectangle(690, 130, 440, 270);

        static void DrawScene(string path, int step, string caption, bool leftBlack, bool showApp, bool powerOn, bool showKeys)
        {
            Color dark = Color.FromArgb(13, 15, 20), accent = Color.FromArgb(255, 90, 54), gray = Color.FromArgb(107, 112, 128);
            using (var bmp = new Bitmap(SceneW, SceneH, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var titleFont = new Font("Malgun Gothic", 30f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var labelFont = new Font("Malgun Gothic", 21f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var smallFont = new Font("Malgun Gothic", 17f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var stepFont = new Font("Malgun Gothic", 15f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var darkBrush = new SolidBrush(dark))
            using (var grayBrush = new SolidBrush(gray))
            using (var accentBrush = new SolidBrush(accent))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.FromArgb(244, 245, 247));
                var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                FillRound(g, accentBrush, new Rectangle(SceneW / 2 - 34, 22, 68, 28), 14);
                g.DrawString(step + " / 6", stepFont, Brushes.White, new RectangleF(SceneW / 2 - 34, 22, 68, 28), center);
                g.DrawString(caption, titleFont, darkBrush, new RectangleF(0, 58, SceneW, 50), center);

                DrawMonitor(g, LeftMonitor);
                DrawMonitor(g, RightMonitor);
                Rectangle leftScreen = Rectangle.Inflate(LeftMonitor, -14, -14);
                Rectangle rightScreen = Rectangle.Inflate(RightMonitor, -14, -14);
                if (leftBlack)
                {
                    g.FillRectangle(Brushes.Black, leftScreen);
                    if (powerOn)
                    {
                        using (var faint = new SolidBrush(Color.FromArgb(90, 94, 104)))
                            g.DrawString("모니터는 켜졌지만…", smallFont, faint, leftScreen, center);
                    }
                }
                else
                {
                    DrawDesk(g, leftScreen, showApp);
                }
                DrawDesk(g, rightScreen, showApp);

                if (powerOn)
                {
                    // 모니터 전원 버튼이 눌린 모습
                    var button = new Rectangle(LeftMonitor.Right - 40, LeftMonitor.Bottom - 8, 16, 16);
                    using (var glow = new SolidBrush(Color.FromArgb(90, 255, 90, 54)))
                        g.FillEllipse(glow, Rectangle.Inflate(button, 10, 10));
                    g.FillEllipse(accentBrush, button);
                    g.DrawString("전원 ON", stepFont, accentBrush, LeftMonitor.Right - 100, LeftMonitor.Bottom + 14);
                }

                g.DrawString("회사 자리의 실제 모니터", labelFont, darkBrush, new RectangleF(LeftMonitor.X, 440, LeftMonitor.Width, 32), center);
                g.DrawString("집·휴대폰에서 원격으로 본 화면", labelFont, darkBrush, new RectangleF(RightMonitor.X, 440, RightMonitor.Width, 32), center);

                var pc = new Rectangle(SceneW / 2 - 50, 206, 100, 120);
                FillRound(g, darkBrush, pc, 12);
                using (var led = new SolidBrush(Color.FromArgb(80, 220, 120)))
                    g.FillEllipse(led, pc.X + 44, pc.Bottom - 26, 12, 12);
                g.DrawString("PC는 계속 일해요", smallFont, darkBrush, new RectangleF(SceneW / 2 - 100, 334, 200, 26), center);
                using (var pen = new Pen(accent, 4f))
                {
                    pen.CustomEndCap = new AdjustableArrowCap(5, 5);
                    g.DrawLine(pen, pc.X - 8, pc.Y + 60, LeftMonitor.Right + 18, pc.Y + 60);
                    g.DrawLine(pen, pc.Right + 8, pc.Y + 60, RightMonitor.X - 18, pc.Y + 60);
                }

                if (showKeys)
                {
                    string[] keys = { "Ctrl", "+", "Alt", "+", "B" };
                    int x = SceneW / 2 - 150;
                    foreach (string key in keys)
                    {
                        if (key == "+")
                        {
                            g.DrawString("+", labelFont, darkBrush, x, 498);
                            x += 30;
                            continue;
                        }
                        int w = key.Length > 1 ? 74 : 52;
                        var cap = new Rectangle(x, 490, w, 46);
                        FillRound(g, darkBrush, new Rectangle(cap.X, cap.Y + 4, cap.Width, cap.Height), 8);
                        FillRound(g, Brushes.White, cap, 8);
                        using (var edge = new Pen(dark, 2f)) g.DrawRectangle(edge, cap.X, cap.Y, cap.Width, cap.Height);
                        g.DrawString(key, labelFont, darkBrush, new RectangleF(cap.X, cap.Y, cap.Width, cap.Height), center);
                        x += w + 12;
                    }
                }

                g.FillRectangle(accentBrush, 0, SceneH - 64, SceneW, 4);
                g.DrawString(About.Name + " - " + About.Company + " by " + About.Author, smallFont, grayBrush, new RectangleF(0, SceneH - 52, SceneW, 40), center);
                bmp.Save(path, ImageFormat.Png);
            }
        }

        // 평소 화면: 메신저·문서 창을 단순한 모양으로. showApp이면 모니터 가리기 창과 마우스 클릭도 그린다.
        static void DrawDesk(Graphics g, Rectangle screen, bool showApp)
        {
            using (var desk = new SolidBrush(Color.FromArgb(214, 228, 245)))
                g.FillRectangle(desk, screen);
            var chat = new Rectangle(screen.X + 22, screen.Y + 22, 170, 190);
            var doc = new Rectangle(screen.X + 212, screen.Y + 38, 170, 174);
            FillRound(g, Brushes.White, chat, 10);
            FillRound(g, Brushes.White, doc, 10);
            using (var bubbleA = new SolidBrush(Color.FromArgb(225, 230, 238)))
            using (var bubbleB = new SolidBrush(Color.FromArgb(70, 160, 255)))
            {
                FillRound(g, bubbleA, new Rectangle(chat.X + 12, chat.Y + 18, 110, 26), 12);
                FillRound(g, bubbleB, new Rectangle(chat.Right - 112, chat.Y + 56, 100, 26), 12);
                FillRound(g, bubbleA, new Rectangle(chat.X + 12, chat.Y + 94, 130, 26), 12);
                FillRound(g, bubbleB, new Rectangle(chat.Right - 92, chat.Y + 132, 80, 26), 12);
            }
            using (var line = new SolidBrush(Color.FromArgb(214, 218, 226)))
            {
                g.FillRectangle(line, doc.X + 14, doc.Y + 18, 110, 10);
                for (int i = 0; i < 6; i++) g.FillRectangle(line, doc.X + 14, doc.Y + 44 + i * 20, i % 3 == 2 ? 90 : 142, 8);
            }
            if (!showApp) return;

            var app = new Rectangle(screen.X + screen.Width / 2 - 95, screen.Y + 50, 190, 140);
            using (var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                FillRound(g, shadow, new Rectangle(app.X + 3, app.Y + 5, app.Width, app.Height), 10);
            FillRound(g, Brushes.White, app, 10);
            using (var titleFont = new Font("Malgun Gothic", 12f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var buttonFont = new Font("Malgun Gothic", 15f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var accent = new SolidBrush(Color.FromArgb(255, 90, 54)))
            {
                g.DrawString(About.Name, titleFont, Brushes.Black, app.X + 12, app.Y + 10);
                var button = new Rectangle(app.X + 14, app.Y + 52, app.Width - 28, 44);
                FillRound(g, accent, button, 8);
                var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("화면 가리기", buttonFont, Brushes.White, new RectangleF(button.X, button.Y, button.Width, button.Height), center);
                // 마우스 커서
                var tip = new Point(button.Right - 30, button.Bottom - 12);
                Point[] arrow = { tip, new Point(tip.X, tip.Y + 26), new Point(tip.X + 7, tip.Y + 20), new Point(tip.X + 12, tip.Y + 30), new Point(tip.X + 17, tip.Y + 28), new Point(tip.X + 12, tip.Y + 18), new Point(tip.X + 20, tip.Y + 18) };
                g.FillPolygon(Brushes.White, arrow);
                using (var edge = new Pen(Color.Black, 1.5f)) g.DrawPolygon(edge, arrow);
            }
        }

        // "본체 모니터는 까맣게, 원격 화면은 그대로"를 한 장으로 보여 주는 설명 그림
        static void DrawConcept(string path)
        {
            const int W = 1200, H = 630;
            Color dark = Color.FromArgb(13, 15, 20), accent = Color.FromArgb(255, 90, 54), gray = Color.FromArgb(107, 112, 128);
            using (var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var titleFont = new Font("Malgun Gothic", 30f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var labelFont = new Font("Malgun Gothic", 22f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var smallFont = new Font("Malgun Gothic", 17f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var darkBrush = new SolidBrush(dark))
            using (var grayBrush = new SolidBrush(gray))
            using (var accentBrush = new SolidBrush(accent))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.FromArgb(244, 245, 247));
                var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                g.DrawString("본체 모니터는 까맣게, 원격 화면은 그대로", titleFont, darkBrush, new RectangleF(0, 28, W, 50), center);

                var left = new Rectangle(70, 120, 440, 270);
                var right = new Rectangle(690, 120, 440, 270);
                DrawMonitor(g, left);
                DrawMonitor(g, right);

                // 본체 모니터: 아무것도 안 보임
                Rectangle leftScreen = Rectangle.Inflate(left, -14, -14);
                using (var black = new SolidBrush(Color.Black))
                    g.FillRectangle(black, leftScreen);
                using (var faint = new SolidBrush(Color.FromArgb(90, 94, 104)))
                    g.DrawString("아무것도 안 보여요", smallFont, faint, leftScreen, center);

                // 원격 화면: 평소 화면 (메신저·문서 창을 단순한 모양으로)
                Rectangle rightScreen = Rectangle.Inflate(right, -14, -14);
                using (var desk = new SolidBrush(Color.FromArgb(214, 228, 245)))
                    g.FillRectangle(desk, rightScreen);
                var chat = new Rectangle(rightScreen.X + 22, rightScreen.Y + 22, 170, 190);
                var doc = new Rectangle(rightScreen.X + 212, rightScreen.Y + 38, 170, 174);
                using (var white = new SolidBrush(Color.White))
                {
                    FillRound(g, white, chat, 10);
                    FillRound(g, white, doc, 10);
                }
                using (var bubbleA = new SolidBrush(Color.FromArgb(225, 230, 238)))
                using (var bubbleB = new SolidBrush(Color.FromArgb(70, 160, 255)))
                {
                    FillRound(g, bubbleA, new Rectangle(chat.X + 12, chat.Y + 18, 110, 26), 12);
                    FillRound(g, bubbleB, new Rectangle(chat.Right - 112, chat.Y + 56, 100, 26), 12);
                    FillRound(g, bubbleA, new Rectangle(chat.X + 12, chat.Y + 94, 130, 26), 12);
                    FillRound(g, bubbleB, new Rectangle(chat.Right - 92, chat.Y + 132, 80, 26), 12);
                }
                using (var line = new SolidBrush(Color.FromArgb(214, 218, 226)))
                {
                    g.FillRectangle(line, doc.X + 14, doc.Y + 18, 110, 10);
                    for (int i = 0; i < 6; i++) g.FillRectangle(line, doc.X + 14, doc.Y + 44 + i * 20, i % 3 == 2 ? 90 : 142, 8);
                }

                g.DrawString("회사 자리의 실제 모니터", labelFont, darkBrush, new RectangleF(left.X, 430, left.Width, 34), center);
                g.DrawString("옆자리에서 봐도 까만 화면이에요", smallFont, grayBrush, new RectangleF(left.X, 466, left.Width, 28), center);
                g.DrawString("집·휴대폰에서 원격으로 본 화면", labelFont, darkBrush, new RectangleF(right.X, 430, right.Width, 34), center);
                g.DrawString("평소처럼 보이고, 그대로 일할 수 있어요", smallFont, grayBrush, new RectangleF(right.X, 466, right.Width, 28), center);

                // 가운데 PC
                var pc = new Rectangle(W / 2 - 50, 196, 100, 120);
                FillRound(g, darkBrush, pc, 12);
                using (var led = new SolidBrush(Color.FromArgb(80, 220, 120)))
                    g.FillEllipse(led, pc.X + 44, pc.Bottom - 26, 12, 12);
                g.DrawString("PC는 계속 일해요", smallFont, darkBrush, new RectangleF(W / 2 - 100, 324, 200, 26), center);
                using (var pen = new Pen(accent, 4f))
                {
                    pen.CustomEndCap = new AdjustableArrowCap(5, 5);
                    g.DrawLine(pen, pc.X - 8, pc.Y + 60, left.Right + 18, pc.Y + 60);
                    g.DrawLine(pen, pc.Right + 8, pc.Y + 60, right.X - 18, pc.Y + 60);
                }

                g.FillRectangle(accentBrush, 0, H - 64, W, 4);
                g.DrawString(About.Name + " - " + About.Company + " by " + About.Author, smallFont, grayBrush, new RectangleF(0, H - 52, W, 40), center);
                bmp.Save(path, ImageFormat.Png);
            }
        }

        static void DrawMonitor(Graphics g, Rectangle r)
        {
            using (var frame = new SolidBrush(Color.FromArgb(42, 45, 53)))
            {
                FillRound(g, frame, r, 14);
                g.FillRectangle(frame, r.X + r.Width / 2 - 22, r.Bottom, 44, 18);
                FillRound(g, frame, new Rectangle(r.X + r.Width / 2 - 80, r.Bottom + 16, 160, 12), 6);
            }
        }

        static void FillRound(Graphics g, Brush brush, Rectangle r, int radius)
        {
            using (var path = new GraphicsPath())
            {
                int d = radius * 2;
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }
    }

    static class PowerTest
    {
        public static int Run(string outPath, uint offMode)
        {
            var log = new StringBuilder();
            Action<string> state = delegate(string label)
            {
                string name;
                log.AppendLine(label + ": D6=" + MonitorPower.QueryPowerMode(Screen.PrimaryScreen.Bounds, out name) + " (" + name + ")");
            };
            state("before");
            log.AppendLine("off(" + offMode + ") accepted by " + MonitorPower.SetAll(offMode) + " monitor(s)");
            Thread.Sleep(3000);
            state("3s after off");
            log.AppendLine("on accepted by " + MonitorPower.SetAll(MonitorPower.On) + " monitor(s)");
            Thread.Sleep(2500);
            state("2.5s after on");
            File.WriteAllText(outPath, log.ToString(), new UTF8Encoding(false));
            return 0;
        }
    }

    static class Program
    {
        public const int SignalToggle = 0, SignalOn = 1, SignalOff = 2, SignalHello = 3, SignalQuit = 4;
        public static readonly string[] Signals =
        {
            @"Local\NAPDA.MonitorCover.Toggle",
            @"Local\NAPDA.MonitorCover.On",
            @"Local\NAPDA.MonitorCover.Off",
            @"Local\NAPDA.MonitorCover.Hello",
            @"Local\NAPDA.MonitorCover.Quit",
        };
        const string MutexName = @"Local\NAPDA.MonitorCover.Instance";

        // 사용법: "모니터 가리기.exe" [--toggle | --on | --off | --quit | --startup]
        //   아무것도 없이 실행하면 창이 뜨고, --startup(로그인 때 자동 실행)은 창 없이 트레이에서 시작한다.
        //   이미 실행 중이면 그 프로그램에 명령만 전달하고 끝난다.
        //   --probe-monitors <파일>: 본체 모니터 전원 상태를 확인해서 파일에 적는다 (진단용)
        //   --test-power <파일> [끄기 값 4|5]: 모니터를 DDC/CI로 3초 껐다 켜 본다 (구형 윈도우 대체 모드 진단용)
        [STAThread]
        static int Main(string[] args)
        {
            string cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "";
            if (cmd == "--write-icon" && args.Length > 1)
            {
                File.WriteAllBytes(args[1], Art.BuildIco(true, false));
                return 0;
            }

            // 예상 못 한 오류가 나도 프로그램이 조용히 죽지 않고 기록을 남긴다
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) { ErrorLog.Write(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { ErrorLog.Write(e.ExceptionObject as Exception); };

            Native.EnableDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (cmd == "--selftest") return SelfTest.Run(args.Length > 1 ? args[1] : "selftest.txt");
            if (cmd == "--test-power")
                return PowerTest.Run(args.Length > 1 ? args[1] : "power-test.txt", args.Length > 2 ? uint.Parse(args[2]) : MonitorPower.Off);
            if (cmd == "--doc-images") return DocImages.Run(args.Length > 1 ? args[1] : "docs");
            if (cmd == "--probe-monitors")
            {
                MonitorReport report = new MonitorWatcher().Check();
                File.WriteAllText(args.Length > 1 ? args[1] : "monitors.txt", "본체 모니터: " + report.Label + "\r\n" + report.Detail, new UTF8Encoding(false));
                return 0;
            }

            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    if (cmd == "--startup") return 0;
                    int signal = cmd == "--toggle" ? SignalToggle
                               : cmd == "--on" ? SignalOn
                               : cmd == "--off" ? SignalOff
                               : cmd == "--quit" ? SignalQuit
                               : SignalHello;
                    // 실행 중인 쪽이 비밀번호 창을 앞으로 띄울 수 있게 포커스 권한을 넘긴다
                    Native.AllowSetForegroundWindow(Native.ASFW_ANY);
                    try
                    {
                        using (EventWaitHandle ev = EventWaitHandle.OpenExisting(Signals[signal]))
                            ev.Set();
                    }
                    catch (WaitHandleCannotBeOpenedException) { }
                    return 0;
                }
                if (cmd == "--quit" || cmd == "--off") return 0;

                bool? initial = null;
                if (cmd == "--toggle" || cmd == "--on") initial = true;
                Application.Run(new TrayApp(initial, cmd.Length == 0));
                GC.KeepAlive(mutex);
            }
            return 0;
        }
    }
}

using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QuickShot;

/// <summary>
/// "Bộ não" của app. Sống suốt vòng đời process:
///  - hiện icon dưới tray
///  - đăng ký hotkey theo settings.json (định vùng / chụp vùng / full-screen / active window)
///  - giữ vùng đã chọn (_savedRegion) để chụp lại nhiều lần
/// </summary>
public sealed class TrayContext : ApplicationContext
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private readonly NotifyIcon _tray;
    private HotkeyWindow _hotkeys = null!;
    private AppSettings _settings = null!;
    private Rectangle? _savedRegion;   // vùng đã "nhớ"; null = chưa định nghĩa

    public TrayContext()
    {
        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "QuickShot — xem menu chuột phải để biết phím tắt",
        };

        _settings = AppSettings.Load(out var warning);
        RegisterHotkeys();
        BuildMenu();

        if (warning != null)
            ShowBalloon("Setting", warning);
    }

    // ---- Đăng ký 4 hotkey dựa trên settings hiện tại ----
    private void RegisterHotkeys()
    {
        _hotkeys?.Dispose();
        _hotkeys = new HotkeyWindow();

        var failed = new List<string>();
        RegisterOne(_settings.DefineRegionHotkey, "Ctrl+Alt+R", "Định nghĩa vùng", DefineRegion, failed);
        RegisterOne(_settings.CaptureRegionHotkey, "Ctrl+Alt+S", "Chụp vùng đã lưu", CaptureRegion, failed);
        RegisterOne(_settings.FullScreenHotkey, "Ctrl+Alt+F", "Chụp toàn màn hình", CaptureFullScreen, failed);
        RegisterOne(_settings.ActiveWindowHotkey, "Ctrl+Alt+W", "Chụp cửa sổ hiện tại", CaptureActiveWindow, failed);

        if (failed.Count > 0)
            ShowBalloon("Cảnh báo hotkey", "Không đăng ký được: " + string.Join("; ", failed));
    }

    private void RegisterOne(string spec, string fallbackSpec, string label, Action action, List<string> failed)
    {
        if (HotkeyParser.TryParse(spec, out var mod, out var key) && _hotkeys.Register(mod, key, action))
            return;

        // spec trong settings.json lỗi hoặc bị app khác chiếm -> thử về mặc định
        if (spec != fallbackSpec &&
            HotkeyParser.TryParse(fallbackSpec, out var fmod, out var fkey) &&
            _hotkeys.Register(fmod, fkey, action))
        {
            failed.Add($"{label} (dùng mặc định {fallbackSpec} vì \"{spec}\" không dùng được)");
        }
        else
        {
            failed.Add($"{label} ({spec})");
        }
    }

    // ---- Menu chuột phải, label hiển thị đúng hotkey đang cấu hình ----
    private void BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add($"Định nghĩa vùng ({_settings.DefineRegionHotkey})", null, (_, _) => DefineRegion());
        menu.Items.Add($"Chụp vùng đã lưu ({_settings.CaptureRegionHotkey})", null, (_, _) => CaptureRegion());
        menu.Items.Add($"Chụp toàn màn hình ({_settings.FullScreenHotkey})", null, (_, _) => CaptureFullScreen());
        menu.Items.Add($"Chụp cửa sổ hiện tại ({_settings.ActiveWindowHotkey})", null, (_, _) => CaptureActiveWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Nạp lại setting", null, (_, _) => ReloadSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Thoát", null, (_, _) => ExitApp());
        _tray.ContextMenuStrip = menu;
    }

    private void ReloadSettings()
    {
        _settings = AppSettings.Load(out var warning);
        RegisterHotkeys();
        BuildMenu();
        ShowBalloon("Setting", warning ?? "Đã nạp lại cấu hình mới.");
    }

    // ---- Hotkey 1: kéo chọn & nhớ vùng ----
    private void DefineRegion()
    {
        using var selector = new RegionSelector();
        selector.ShowDialog();

        if (selector.Result is { } r)
        {
            _savedRegion = r;
            ShowBalloon("Đã nhớ vùng", $"{r.Width} x {r.Height}. Nhấn {_settings.CaptureRegionHotkey} để chụp.");
        }
    }

    // ---- Hotkey 2: chụp lại vùng đã nhớ ----
    private void CaptureRegion()
    {
        if (_savedRegion is not { } region)
        {
            ShowBalloon("Chưa có vùng", $"Nhấn {_settings.DefineRegionHotkey} để định nghĩa vùng trước.");
            return;
        }

        CaptureAndSave(region);
    }

    // ---- Hotkey 3: chụp toàn màn hình (màn hình chứa con trỏ chuột) ----
    private void CaptureFullScreen()
    {
        var screen = Screen.FromPoint(Cursor.Position);
        CaptureAndSave(screen.Bounds);
    }

    // ---- Hotkey 4: chụp cửa sổ đang active ----
    private void CaptureActiveWindow()
    {
        IntPtr hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero || IsIconic(hWnd) || !GetWindowRect(hWnd, out var r))
        {
            ShowBalloon("Không thể chụp", "Không tìm thấy cửa sổ hợp lệ để chụp.");
            return;
        }

        var rect = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            ShowBalloon("Không thể chụp", "Không tìm thấy cửa sổ hợp lệ để chụp.");
            return;
        }

        CaptureAndSave(rect);
    }

    // ---- Logic chụp + lưu dùng chung cho cả 3 chế độ ----
    private void CaptureAndSave(Rectangle region)
    {
        try
        {
            using var bmp = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                // Copy pixel từ màn hình vào bitmap
                g.CopyFromScreen(region.Location, Point.Empty, region.Size);
            }

            // 1) Copy clipboard (clone để bitmap không bị dispose mất)
            Clipboard.SetImage(new Bitmap(bmp));

            // 2) Lưu file vào thư mục đã cấu hình (mặc định %TEMP%)
            string file = Path.Combine(
                _settings.ResolvedSaveFolder,
                $"QuickShot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            bmp.Save(file, ImageFormat.Png);

            // 3) Hiệu ứng bay ảnh về góc màn hình thay cho balloon "Đã chụp"
            var flyout = new CaptureFlyoutForm(new Bitmap(bmp), region, file);
            flyout.Show();
        }
        catch (Exception ex)
        {
            ShowBalloon("Lỗi chụp", ex.Message);
        }
    }

    private void ShowBalloon(string title, string text)
    {
        _tray.BalloonTipTitle = title;
        _tray.BalloonTipText = text;
        _tray.ShowBalloonTip(2000);
    }

    private void ExitApp()
    {
        _tray.Visible = false;
        _tray.Dispose();
        _hotkeys.Dispose();
        ExitThread();
    }
}

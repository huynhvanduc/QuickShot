using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QuickShot;

/// <summary>
/// Cửa sổ chính của app. Hiện trên taskbar (để người dùng biết app đang chạy/đã tắt),
/// khởi động ở trạng thái thu nhỏ, và vẫn giữ icon + menu ở khay hệ thống như cũ.
///  - đăng ký hotkey theo settings.json (định vùng / chụp vùng / full-screen / active window)
///  - giữ vùng đã chọn (_savedRegion) để chụp lại nhiều lần
/// </summary>
public sealed class MainWindow : Form
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    // Hotkey mặc định khi cấu hình trong settings.json bị lỗi/trùng — lấy từ AppSettings để luôn đồng bộ.
    private static readonly AppSettings DefaultHotkeys = new();

    private readonly NotifyIcon _tray;
    private readonly Label _statusLabel;
    private HotkeyWindow _hotkeys = null!;
    private AppSettings _settings = null!;
    private Rectangle? _savedRegion;   // vùng đã "nhớ"; null = chưa định nghĩa

    // Icon riêng của app, nhúng làm Embedded Resource — đọc thẳng từ assembly lúc runtime
    // để dùng chung cho cả window lẫn tray, không phụ thuộc file rời khi phát hành single-file.
    private static readonly Icon AppIcon = LoadAppIcon();

    private static Icon LoadAppIcon()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("QuickShot.app_runtime.ico");
        return stream != null ? new Icon(stream) : SystemIcons.Application;
    }

    public MainWindow()
    {
        Text = "QuickShot";
        Icon = AppIcon;
        ShowInTaskbar = true;
        ClientSize = new Size(380, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            Font = new Font("Consolas", 9.5f),
            TextAlign = ContentAlignment.TopLeft,
        };
        Controls.Add(_statusLabel);

        _tray = new NotifyIcon
        {
            Icon = AppIcon,
            Visible = true,
            Text = "QuickShot — xem menu chuột phải để biết phím tắt",
        };
        _tray.DoubleClick += (_, _) => RestoreFromTray();

        _settings = AppSettings.Load(out var settingsWarning);
        var hotkeyWarning = RegisterHotkeys();
        BuildMenu();
        RefreshStatusText();

        var problems = new List<string>();
        if (settingsWarning != null) problems.Add(settingsWarning);
        if (hotkeyWarning != null) problems.Add(hotkeyWarning);

        if (problems.Count > 0)
            ShowBalloon("Setting", string.Join(" ", problems));
        else
            ShowBalloon("QuickShot đã sẵn sàng",
                $"Nhấn {_settings.DefineRegionHotkey} để khoanh vùng chụp. Xem menu chuột phải để biết thêm.");

        // Khởi động thu nhỏ sẵn: chỉ hiện icon taskbar, không che màn hình.
        WindowState = FormWindowState.Minimized;
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    // ---- Đăng ký 4 hotkey dựa trên settings hiện tại. Trả về lời cảnh báo nếu có hotkey lỗi, null nếu ổn. ----
    private string? RegisterHotkeys()
    {
        _hotkeys?.Dispose();
        _hotkeys = new HotkeyWindow();

        var failed = new List<string>();
        RegisterOne(_settings.DefineRegionHotkey, DefaultHotkeys.DefineRegionHotkey, "Định nghĩa vùng", DefineRegion, failed);
        RegisterOne(_settings.CaptureRegionHotkey, DefaultHotkeys.CaptureRegionHotkey, "Chụp vùng đã lưu", CaptureRegion, failed);
        RegisterOne(_settings.FullScreenHotkey, DefaultHotkeys.FullScreenHotkey, "Chụp toàn màn hình", CaptureFullScreen, failed);
        RegisterOne(_settings.ActiveWindowHotkey, DefaultHotkeys.ActiveWindowHotkey, "Chụp cửa sổ hiện tại", CaptureActiveWindow, failed);

        return failed.Count > 0 ? "Không đăng ký được: " + string.Join("; ", failed) : null;
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
        menu.Items.Add("Xem trạng thái", null, (_, _) => RestoreFromTray());
        menu.Items.Add("Nạp lại setting", null, (_, _) => ReloadSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Thoát", null, (_, _) => Close());
        _tray.ContextMenuStrip = menu;
    }

    // ---- Nội dung cửa sổ trạng thái: hotkey hiện tại + tình trạng vùng đã lưu ----
    private void RefreshStatusText()
    {
        string regionStatus = _savedRegion is { } r
            ? $"Đã lưu vùng: {r.Width} x {r.Height}"
            : "Chưa định nghĩa vùng";

        _statusLabel.Text =
            "Phím tắt hiện tại:\n" +
            $"  Định nghĩa vùng      : {_settings.DefineRegionHotkey}\n" +
            $"  Chụp vùng đã lưu     : {_settings.CaptureRegionHotkey}\n" +
            $"  Chụp toàn màn hình   : {_settings.FullScreenHotkey}\n" +
            $"  Chụp cửa sổ hiện tại : {_settings.ActiveWindowHotkey}\n" +
            "\n" +
            $"Trạng thái vùng: {regionStatus}";
    }

    private void ReloadSettings()
    {
        _settings = AppSettings.Load(out var settingsWarning);
        var hotkeyWarning = RegisterHotkeys();
        BuildMenu();
        RefreshStatusText();

        var problems = new List<string>();
        if (settingsWarning != null) problems.Add(settingsWarning);
        if (hotkeyWarning != null) problems.Add(hotkeyWarning);

        ShowBalloon("Setting", problems.Count > 0 ? string.Join(" ", problems) : "Đã nạp lại cấu hình mới.");
    }

    // ---- Hotkey 1: kéo chọn & nhớ vùng ----
    private void DefineRegion()
    {
        using var selector = new RegionSelector();
        selector.ShowDialog();

        if (selector.Result is { } r)
        {
            _savedRegion = r;
            RefreshStatusText();
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
        if (hWnd == IntPtr.Zero || IsIconic(hWnd))
        {
            ShowBalloon("Không thể chụp", "Không tìm thấy cửa sổ hợp lệ để chụp.");
            return;
        }

        // Ưu tiên DWM extended frame bounds: khớp đúng viền nhìn thấy được,
        // tránh phần viền ẩn (resize border) mà GetWindowRect cộng thêm ở cửa sổ maximized.
        bool gotRect = DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out var r,
            Marshal.SizeOf<RECT>()) == 0;
        if (!gotRect)
            gotRect = GetWindowRect(hWnd, out r);

        if (!gotRect)
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

            // 1) Copy clipboard — resize theo ClipboardWidth/HeightInches nếu đã cấu hình,
            // để dán vào Excel/SharePoint ra đúng sẵn Width/Height mong muốn.
            Clipboard.SetImage(BuildClipboardImage(bmp));

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

    // Tạo bản ảnh dùng cho clipboard: kéo đúng ClipboardWidth/HeightInches (quy đổi pixel) nếu đã
    // cấu hình, kể cả méo tỷ lệ — nếu chưa cấu hình (0) thì dùng nguyên bản gốc.
    private Bitmap BuildClipboardImage(Bitmap original)
    {
        if (_settings.ClipboardPixelSize is not { } size)
            return new Bitmap(original);

        var resized = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(resized);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(original, new Rectangle(0, 0, size.Width, size.Height));
        return resized;
    }

    private void ShowBalloon(string title, string text)
    {
        _tray.BalloonTipTitle = title;
        _tray.BalloonTipText = text;
        _tray.ShowBalloonTip(2000);
    }

    // Bấm X hoặc "Thoát" ở menu tray đều gọi Close() -> dọn dẹp ở đây rồi thoát hẳn app,
    // để icon taskbar biến mất đúng lúc app thật sự đã tắt.
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        _tray.Visible = false;
        _tray.Dispose();
        _hotkeys.Dispose();
    }
}

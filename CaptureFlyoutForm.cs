using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace QuickShot;

/// <summary>
/// Hiệu ứng sau khi chụp: viền nét đứt lóe lên đúng khung vùng vừa chụp để xác nhận,
/// rồi ảnh co dần về một khung nhỏ ở góc dưới-phải màn hình, giữ lại một lúc rồi mờ dần
/// biến mất. Click vào lúc đang hiện để mở file ảnh đã lưu.
/// </summary>
public sealed class CaptureFlyoutForm : Form
{
    private const int HighlightMs = 350;
    private const int FlyMs = 400;
    private const int HoldMs = 1500;
    private const int FadeMs = 300;
    private const int ThumbMaxW = 180;
    private const int ThumbMaxH = 120;
    private const int EdgeMargin = 12;

    private enum Phase { Highlight, Fly, Hold, Fade }

    private readonly Rectangle _startBounds;
    private readonly Rectangle _endBounds;
    private readonly string _filePath;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _stopwatch = new();
    private Bitmap? _image;
    private Phase _phase = Phase.Highlight;

    public CaptureFlyoutForm(Bitmap image, Rectangle capturedBounds, string filePath)
    {
        _image = image;
        _filePath = filePath;
        _startBounds = capturedBounds;
        _endBounds = ComputeEndBounds(capturedBounds);

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Bounds = _startBounds;
        BackgroundImage = _image;
        BackgroundImageLayout = ImageLayout.Stretch;
        Cursor = Cursors.Hand;

        MouseDown += (_, _) => OpenFileAndClose();

        _timer = new System.Windows.Forms.Timer { Interval = 15 };
        _timer.Tick += OnTick;
    }

    private static Rectangle ComputeEndBounds(Rectangle capturedBounds)
    {
        var screen = Screen.FromRectangle(capturedBounds);
        double scale = Math.Min(
            (double)ThumbMaxW / capturedBounds.Width,
            (double)ThumbMaxH / capturedBounds.Height);
        scale = Math.Min(scale, 1.0);

        int w = Math.Max(1, (int)(capturedBounds.Width * scale));
        int h = Math.Max(1, (int)(capturedBounds.Height * scale));

        var wa = screen.WorkingArea;
        return new Rectangle(wa.Right - w - EdgeMargin, wa.Bottom - h - EdgeMargin, w, h);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _stopwatch.Start();
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        long elapsed = _stopwatch.ElapsedMilliseconds;

        switch (_phase)
        {
            case Phase.Highlight:
                if (elapsed >= HighlightMs)
                {
                    _phase = Phase.Fly;
                    _stopwatch.Restart();
                    Invalidate(); // xóa viền nét đứt trước khi bắt đầu co nhỏ
                }
                break;

            case Phase.Fly:
                double t = Math.Min(1.0, elapsed / (double)FlyMs);
                double eased = 1 - Math.Pow(1 - t, 3); // ease-out cubic
                Bounds = Lerp(_startBounds, _endBounds, eased);
                if (t >= 1.0)
                {
                    _phase = Phase.Hold;
                    _stopwatch.Restart();
                }
                break;

            case Phase.Hold:
                if (elapsed >= HoldMs)
                {
                    _phase = Phase.Fade;
                    _stopwatch.Restart();
                }
                break;

            case Phase.Fade:
                double ft = Math.Min(1.0, elapsed / (double)FadeMs);
                Opacity = 1.0 - ft;
                if (ft >= 1.0)
                    CloseAndDispose();
                break;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_phase != Phase.Highlight) return;

        using var pen = new Pen(Color.DeepSkyBlue, 3) { DashStyle = DashStyle.Dash };
        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;
        e.Graphics.DrawRectangle(pen, rect);
    }

    private static Rectangle Lerp(Rectangle a, Rectangle b, double t) => new(
        a.X + (int)((b.X - a.X) * t),
        a.Y + (int)((b.Y - a.Y) * t),
        a.Width + (int)((b.Width - a.Width) * t),
        a.Height + (int)((b.Height - a.Height) * t));

    private void OpenFileAndClose()
    {
        try
        {
            Process.Start(new ProcessStartInfo(_filePath) { UseShellExecute = true });
        }
        catch
        {
            // Không mở được thì thôi, không phải lỗi nghiêm trọng.
        }
        CloseAndDispose();
    }

    private void CloseAndDispose()
    {
        _timer.Stop();
        Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        _timer.Dispose();
        BackgroundImage = null;
        _image?.Dispose();
        _image = null;
    }

    // Ẩn khỏi Alt+Tab, không cướp focus của cửa sổ đang dùng
    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x80;
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;
}

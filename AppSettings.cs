using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickShot;

/// <summary>
/// Cấu hình người dùng, đọc/ghi tại settings.json cạnh file .exe.
/// Nếu file chưa tồn tại, tự tạo với giá trị mặc định.
/// </summary>
public sealed class AppSettings
{
    public string DefineRegionHotkey { get; set; } = "Ctrl+Shift+R";
    public string CaptureRegionHotkey { get; set; } = "Ctrl+Shift+S";
    public string FullScreenHotkey { get; set; } = "Ctrl+Shift+F";
    public string ActiveWindowHotkey { get; set; } = "Ctrl+Shift+W";
    public string SaveFolder { get; set; } = @"C:\Temp\shot";

    // Kích thước cố định (inch) áp cho ảnh copy vào Clipboard, để dán vào Excel/SharePoint
    // luôn ra đúng Width/Height mong muốn mà không cần kéo tay. 0 = không áp dụng, giữ nguyên gốc.
    public double ClipboardWidthInches { get; set; } = 0;
    public double ClipboardHeightInches { get; set; } = 0;

    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    /// <summary>Nạp settings từ đĩa. Nếu lỗi/không tồn tại, trả về mặc định và giải thích qua "warning".</summary>
    public static AppSettings Load(out string? warning)
    {
        warning = null;

        if (!File.Exists(FilePath))
        {
            var defaults = new AppSettings();
            defaults.Save();
            return defaults;
        }

        try
        {
            string json = File.ReadAllText(FilePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json);
            if (loaded is null)
            {
                warning = "settings.json rỗng hoặc không hợp lệ, dùng cấu hình mặc định.";
                return new AppSettings();
            }
            return loaded;
        }
        catch (Exception ex)
        {
            warning = $"Lỗi đọc settings.json ({ex.Message}), dùng cấu hình mặc định.";
            return new AppSettings();
        }
    }

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, options));
    }

    /// <summary>Thư mục lưu ảnh thực tế: dùng SaveFolder nếu hợp lệ, ngược lại rơi về %TEMP%.</summary>
    [JsonIgnore]
    public string ResolvedSaveFolder
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SaveFolder))
                return Path.GetTempPath();

            try
            {
                Directory.CreateDirectory(SaveFolder);
                return SaveFolder;
            }
            catch
            {
                return Path.GetTempPath();
            }
        }
    }

    private const int ClipboardDpi = 96; // chuẩn DPI mặc định của GDI+/Office khi không có metadata khác

    /// <summary>Kích thước pixel quy đổi từ ClipboardWidthInches/HeightInches, null nếu chưa cấu hình (= 0).</summary>
    [JsonIgnore]
    public Size? ClipboardPixelSize
    {
        get
        {
            if (ClipboardWidthInches <= 0 || ClipboardHeightInches <= 0)
                return null;

            int w = Math.Max(1, (int)Math.Round(ClipboardWidthInches * ClipboardDpi));
            int h = Math.Max(1, (int)Math.Round(ClipboardHeightInches * ClipboardDpi));
            return new Size(w, h);
        }
    }
}

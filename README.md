# QuickShot — Hướng dẫn sử dụng

Công cụ chụp màn hình nhỏ gọn, chạy nền ở khay hệ thống (system tray).

## Phím tắt mặc định

| Phím tắt | Chức năng |
|---|---|
| `Ctrl+Shift+R` | Kéo chuột chọn 1 vùng màn hình, lưu lại để chụp nhiều lần |
| `Ctrl+Shift+S` | Chụp lại đúng vùng đã lưu ở trên |
| `Ctrl+Shift+F` | Chụp toàn màn hình (màn hình đang chứa con trỏ chuột, nếu có nhiều màn hình) |
| `Ctrl+Shift+W` | Chụp đúng cửa sổ đang active |

Mỗi lần chụp thành công:
- Ảnh được **copy vào Clipboard** → dán (Ctrl+V) ngay vào bất kỳ đâu.
- Ảnh được **lưu thành file PNG** vào thư mục đã cấu hình (mặc định `C:\Temp\shot`).
- **Viền nét đứt** lóe lên đúng khung vùng vừa chụp để xác nhận, sau đó ảnh **thu nhỏ dần và bay về góc dưới-phải màn hình**, giữ khoảng 1.5 giây rồi tự mờ dần biến mất. **Click vào ảnh nhỏ đó** trong lúc còn hiện để mở file ảnh vừa lưu.

Cũng có thể dùng các chức năng trên qua **menu chuột phải** vào icon QuickShot ở khay hệ thống.

## Cách dùng cơ bản

1. Mở `QuickShot.exe` — app chạy nền, khởi động ở trạng thái thu nhỏ. Có icon ở khay hệ thống (góc dưới-phải màn hình, có thể cần bấm mũi tên "^" để thấy icon ẩn) **và** icon trên taskbar (click vào để mở cửa sổ xem nhanh 4 phím tắt hiện tại + trạng thái vùng đã lưu). Icon trên taskbar mất đi nghĩa là app đã tắt hẳn.
2. Nhấn `Ctrl+Shift+R`, kéo chuột để chọn vùng cần chụp, thả chuột để xác nhận (nhấn `Esc` nếu muốn hủy).
3. Từ giờ, mỗi lần muốn chụp lại đúng vùng đó, chỉ cần nhấn `Ctrl+Shift+S` — không cần chọn lại vùng.
4. Muốn chụp toàn màn hình hoặc 1 cửa sổ cụ thể, dùng `Ctrl+Shift+F` / `Ctrl+Shift+W` bất kỳ lúc nào, không cần định nghĩa vùng trước.
5. Thoát app: chuột phải vào icon tray → **Thoát**.

## Tùy chỉnh phím tắt và nơi lưu ảnh

App đọc cấu hình từ file **`settings.json`** nằm cùng thư mục với `QuickShot.exe`. Nếu chưa có file này, app sẽ tự tạo với giá trị mặc định khi khởi động lần đầu.

Ví dụ nội dung file:

```json
{
  "DefineRegionHotkey": "Ctrl+Shift+R",
  "CaptureRegionHotkey": "Ctrl+Shift+S",
  "FullScreenHotkey": "Ctrl+Shift+F",
  "ActiveWindowHotkey": "Ctrl+Shift+W",
  "SaveFolder": "C:\\Temp\\shot",
  "ClipboardWidthInches": 0,
  "ClipboardHeightInches": 0
}
```

- Muốn đổi phím tắt: sửa giá trị dạng `"Ctrl+Alt+X"` (các phần hợp lệ: `Ctrl`, `Alt`, `Shift`, `Win`, cộng với 1 phím ở cuối, ví dụ `"Ctrl+Shift+D"`).
- Muốn đổi nơi lưu ảnh: điền đường dẫn thư mục vào `SaveFolder`, ví dụ `"D:\\Screenshots"` (trong JSON, dấu `\` phải viết thành `\\`). Nếu thư mục chưa tồn tại, app sẽ tự tạo; để trống (`""`) nghĩa là lưu vào thư mục tạm `%TEMP%` của Windows.
- **Sau khi sửa file**, không cần khởi động lại app — chỉ cần chuột phải vào icon tray → **"Nạp lại setting"** để áp dụng ngay.
- Nếu 1 hotkey không hợp lệ (gõ sai) hoặc bị app khác chiếm mất, QuickShot sẽ tự dùng lại phím mặc định cho chức năng đó và báo qua balloon ở khay hệ thống.

### Tự động chỉnh kích thước ảnh khi dán vào Excel/SharePoint

Nếu bạn hay dán ảnh vào Excel Online/SharePoint và phải kéo tay Width/Height cho đều mỗi lần, điền `ClipboardWidthInches`/`ClipboardHeightInches` (đơn vị **inch**, khớp với đơn vị Format Picture của Excel) vào `settings.json`, ví dụ:

```json
"ClipboardWidthInches": 3.0,
"ClipboardHeightInches": 2.0
```

rồi **"Nạp lại setting"**. Từ đó, ảnh đưa vào Clipboard sẽ luôn ở đúng kích thước này (méo tỷ lệ nếu vùng chụp không cùng tỷ lệ khung — đổi lại luôn khớp Width/Height cố định), nên dán vào Excel là đúng sẵn, không cần kéo lại. File PNG lưu đĩa vẫn giữ nguyên độ phân giải gốc, không bị ảnh hưởng.

Để `0` (mặc định) ở 1 trong 2 giá trị nghĩa là tắt tính năng này, ảnh clipboard giữ nguyên kích thước gốc như trước.

## Lưu ý

- Nếu chụp cửa sổ (`Ctrl+Shift+W`) mà không có cửa sổ nào đang active hợp lệ (ví dụ đang ở màn hình desktop, hoặc cửa sổ đang thu nhỏ), app sẽ báo lỗi qua balloon và không chụp gì cả.
- File ảnh được đặt tên theo thời điểm chụp, ví dụ: `QuickShot_20260826_153012_045.png`.

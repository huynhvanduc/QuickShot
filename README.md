# QuickShot — Hướng dẫn sử dụng

Công cụ chụp màn hình nhỏ gọn, chạy nền ở khay hệ thống (system tray).

## Phím tắt mặc định

| Phím tắt | Chức năng |
|---|---|
| `Ctrl+Alt+R` | Kéo chuột chọn 1 vùng màn hình, lưu lại để chụp nhiều lần |
| `Ctrl+Alt+S` | Chụp lại đúng vùng đã lưu ở trên |
| `Ctrl+Alt+F` | Chụp toàn màn hình (màn hình đang chứa con trỏ chuột, nếu có nhiều màn hình) |
| `Ctrl+Alt+W` | Chụp đúng cửa sổ đang active |

Mỗi lần chụp thành công:
- Ảnh được **copy vào Clipboard** → dán (Ctrl+V) ngay vào bất kỳ đâu.
- Ảnh được **lưu thành file PNG** vào thư mục đã cấu hình (mặc định `%TEMP%`).
- Có **hiệu ứng ảnh thu nhỏ bay về góc dưới-phải màn hình**, giữ khoảng 1.5 giây rồi tự mờ dần biến mất. **Click vào ảnh nhỏ đó** trong lúc còn hiện để mở file ảnh vừa lưu.

Cũng có thể dùng các chức năng trên qua **menu chuột phải** vào icon QuickShot ở khay hệ thống.

## Cách dùng cơ bản

1. Mở `QuickShot.exe` — app sẽ chạy nền, hiện icon ở khay hệ thống (góc dưới-phải màn hình, có thể cần bấm mũi tên "^" để thấy icon ẩn).
2. Nhấn `Ctrl+Alt+R`, kéo chuột để chọn vùng cần chụp, thả chuột để xác nhận (nhấn `Esc` nếu muốn hủy).
3. Từ giờ, mỗi lần muốn chụp lại đúng vùng đó, chỉ cần nhấn `Ctrl+Alt+S` — không cần chọn lại vùng.
4. Muốn chụp toàn màn hình hoặc 1 cửa sổ cụ thể, dùng `Ctrl+Alt+F` / `Ctrl+Alt+W` bất kỳ lúc nào, không cần định nghĩa vùng trước.
5. Thoát app: chuột phải vào icon tray → **Thoát**.

## Tùy chỉnh phím tắt và nơi lưu ảnh

App đọc cấu hình từ file **`settings.json`** nằm cùng thư mục với `QuickShot.exe`. Nếu chưa có file này, app sẽ tự tạo với giá trị mặc định khi khởi động lần đầu.

Ví dụ nội dung file:

```json
{
  "DefineRegionHotkey": "Ctrl+Alt+R",
  "CaptureRegionHotkey": "Ctrl+Alt+S",
  "FullScreenHotkey": "Ctrl+Alt+F",
  "ActiveWindowHotkey": "Ctrl+Alt+W",
  "SaveFolder": ""
}
```

- Muốn đổi phím tắt: sửa giá trị dạng `"Ctrl+Alt+X"` (các phần hợp lệ: `Ctrl`, `Alt`, `Shift`, `Win`, cộng với 1 phím ở cuối, ví dụ `"Ctrl+Shift+D"`).
- Muốn đổi nơi lưu ảnh: điền đường dẫn thư mục vào `SaveFolder`, ví dụ `"D:\\Screenshots"`. Để trống (`""`) nghĩa là lưu vào thư mục tạm `%TEMP%` của Windows.
- **Sau khi sửa file**, không cần khởi động lại app — chỉ cần chuột phải vào icon tray → **"Nạp lại setting"** để áp dụng ngay.
- Nếu 1 hotkey không hợp lệ (gõ sai) hoặc bị app khác chiếm mất, QuickShot sẽ tự dùng lại phím mặc định cho chức năng đó và báo qua balloon ở khay hệ thống.

## Lưu ý

- Nếu chụp cửa sổ (`Ctrl+Alt+W`) mà không có cửa sổ nào đang active hợp lệ (ví dụ đang ở màn hình desktop, hoặc cửa sổ đang thu nhỏ), app sẽ báo lỗi qua balloon và không chụp gì cả.
- File ảnh được đặt tên theo thời điểm chụp, ví dụ: `QuickShot_20260826_153012_045.png`.

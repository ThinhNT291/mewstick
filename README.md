# Mewstick (.NET / WPF)

Bản viết lại của Mewstick từ Tauri sang **.NET 8 + WPF**, giữ nguyên các tính năng cốt lõi:
danh sách task, cửa sổ "thêm nhanh" (quick add) dạng popup frameless, tray icon, phím tắt toàn cục,
single-instance.

## Chạy thử

```bash
cd Mewstick
dotnet restore
dotnet run --project Mewstick
```

(Chỉ chạy được trên Windows vì dùng WPF + Win32 API cho hotkey/tray.)

## Kiến trúc

```
Mewstick/
  Mewstick.sln
  Mewstick/
    App.xaml(.cs)              - khởi động, single-instance, wiring tray + hotkey
    MainWindow.xaml(.cs)       - danh sách task chính
    Views/QuickAddWindow       - popup thêm nhanh (frameless, always-on-top)
    Models/TaskItem.cs         - model task (ObservableObject)
    ViewModels/MainViewModel   - CRUD task, MVVM (CommunityToolkit.Mvvm)
    Services/DataService       - lưu/đọc JSON tại %AppData%\Mewstick\tasks.json
    Services/HotkeyService     - đăng ký phím tắt toàn cục (RegisterHotKey)
    Services/TrayIconService   - icon khay hệ thống + menu ngữ cảnh
```

Mặc định: **Ctrl+Alt+N** mở popup thêm nhanh gần vị trí con trỏ chuột (giống Todoist/TickTick).
Nút X trên cửa sổ chính chỉ ẩn app (vẫn chạy nền qua tray) — thoát thật sự qua menu tray → "Thoát".

## Roadmap

- [x] **Giai đoạn 1-7 (MVP, đã dựng ở đây):** khung dự án, lưu JSON, task list CRUD,
      quick add popup, tray icon, global hotkey, single instance.
- [ ] **Giai đoạn 8 — Polish:**
  - Chọn màu cho từng ghi chú (bảng màu như Microsoft Sticky Notes)
  - Due date / reminder + thông báo Windows (Notezilla)
  - Ô tìm kiếm/lọc task (Google Keep)
  - Kéo-thả sắp xếp lại thứ tự
  - Icon riêng cho app + tray (thay `SystemIcons.Application`)
  - Cấu hình hotkey tùy chỉnh trong Settings thay vì cố định Ctrl+Alt+N
  - Khởi động cùng Windows (registry Run key hoặc Task Scheduler)
- [ ] **Giai đoạn 9 — Nâng cấp lưu trữ:** chuyển từ JSON sang SQLite
      (Microsoft.Data.Sqlite) nếu cần tìm kiếm nhanh trên nhiều task hoặc đồng bộ.
- [ ] **Giai đoạn 10 — Đóng gói:** publish self-contained hoặc dùng MSIX/Inno Setup
      để tạo installer cho Windows.

## Ghi chú kỹ thuật

- Icon app hiện đang dùng tạm `SystemIcons.Application` — bỏ comment dòng
  `ApplicationIcon` trong `Mewstick.csproj` và thêm file `Resources/app.ico` khi có icon thật.
- `TaskItem` dùng `[ObservableProperty]` của CommunityToolkit.Mvvm nên checkbox
  `IsDone` cập nhật UI ngay khi tick, nhưng việc ghi xuống JSON hiện chỉ chạy khi
  đóng app hoặc thêm/xóa task — nếu muốn lưu ngay khi tick checkbox, subscribe
  `PropertyChanged` của từng `TaskItem` trong `MainViewModel` và gọi `Persist()`.

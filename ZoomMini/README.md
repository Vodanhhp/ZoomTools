# Công cụ Zoom (ZoomMini)

Ứng dụng Windows Forms nhỏ gọn, viết bằng C# và tương thích Visual Studio 2019.

## Mở và chạy

1. Mở `ZoomMini.sln` bằng Visual Studio 2019.
2. Chọn cấu hình Debug.
3. Chọn **Build > Build Solution**.
4. Nhấn **F5** để chạy.

Yêu cầu: .NET Framework 4.7.2 Developer Pack/Targeting Pack.

## Giao diện và nhận diện Zoom

- Cửa sổ không dùng thanh tiêu đề Windows, có thể kéo bằng vùng trên cùng.
- Luôn nổi trên các cửa sổ khác khi đang mở; nút thu nhỏ đưa ứng dụng xuống taskbar.
- Giao diện nền đen/xanh đậm, nút xanh Zoom khi được bật.
- Kiểm tra tiến trình `Zoom.exe` mỗi giây. Khi chưa phát hiện Zoom, các nút điều khiển bị vô hiệu hóa.
- Việc phát hiện tiến trình chỉ xác nhận Zoom đang chạy, không phải kết nối Zoom SDK/API.

## Phím tắt

- MIC: gửi `Alt + A` để bật/tắt micro cá nhân.
- MUTE ALL: gửi `Alt + M` khi nhấn chuột xuống.
- CAMERA: gửi `Alt + V` để bật/tắt camera.

Trong Zoom Workplace, cần bật **Global Shortcut** cho các phím tắt tương ứng. MUTE ALL cần quyền Host hoặc Co-host và còn phụ thuộc thiết lập cuộc họp.

## Giới hạn trạng thái

Màu MIC và CAMERA chỉ phản ánh số lần bấm nút trong ứng dụng này, không đọc trạng thái micro/camera thực tế từ Zoom. Nếu đổi trạng thái trực tiếp trong Zoom hoặc bằng thiết bị khác, màu có thể không còn khớp.

Ứng dụng không tự đưa Zoom lên trước, không tự nhấn Enter và không sửa cấu hình Zoom. Hãy kiểm tra thực tế trên phiên bản Zoom đang sử dụng.

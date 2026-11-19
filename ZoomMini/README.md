# Công cụ Zoom (ZoomMini)

Ứng dụng Windows Forms nhỏ gọn, viết mới độc lập bằng C# và tương thích Visual Studio 2019.

## Mở và chạy

1. Mở `ZoomMini.sln` bằng Visual Studio 2019.
2. Chọn cấu hình Debug.
3. Chọn **Build > Build Solution**.
4. Nhấn **F5** để chạy.

Yêu cầu: .NET Framework 4.7.2 Developer Pack/Targeting Pack.

## Phím tắt

- MIC: gửi `Alt + A` để bật/tắt micro cá nhân.
- MUTE ALL: gửi `Alt + M` khi nhấn chuột xuống. Nút chỉ sáng trong lúc nhấn.
- CAMERA: gửi `Alt + V` để bật/tắt camera.

Trong Zoom Workplace, cần bật tùy chọn **Global Shortcut** cho các phím tắt tương ứng. MUTE ALL cần quyền Host hoặc Co-host và kết quả còn phụ thuộc quyền, thiết lập của cuộc họp.

## Lưu ý về màu trạng thái

Màu MIC và CAMERA chỉ phản ánh số lần bạn bấm các nút trong ứng dụng này. Ứng dụng không đọc được trạng thái micro/camera thực tế từ Zoom. Nếu bạn đổi trạng thái trực tiếp trong Zoom hoặc bằng thiết bị khác, màu trên ứng dụng có thể không còn khớp.

Ứng dụng gửi tổ hợp phím Windows Alt + A/M/V; không tự đưa Zoom lên trước, không tự nhấn Enter và không sửa cấu hình Zoom. Hãy kiểm tra thực tế trên phiên bản Zoom đang sử dụng.

## Phạm vi hiện tại

Hai liên kết **Cài đặt** và **Help** hiện mới hiển thị thông báo hướng dẫn ngắn. Chưa có chức năng cấu hình riêng trong ứng dụng.

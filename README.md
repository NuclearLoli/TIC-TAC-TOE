# 🎮 Cờ Caro Arena (Gomoku Online) - .NET 9 WPF

> Trò chơi Cờ Caro bàn cờ vô hạn với giao diện Chess.com Dark Theme, hệ thống Multiplayer thời gian thực (SignalR), bảng xếp hạng Elo, hệ thống bạn bè và tùy biến ảnh đại diện.

---

## ✨ Tính Năng Nổi Bật

* **🎨 Giao Diện Chess.com Dark Theme**:
  * Tông màu tối dịu mắt `#262421` kết hợp viền xanh lá cờ vua `#81B64C`.
  * Typography chuẩn `Segoe UI`, công nghệ rendering chống rách hình và sub-pixel layout rounding.
* **♾️ Bàn Cờ Vô Hạn (Infinite Board)**:
  * Zoom chuột mượt mà (0.4x đến 3.0x), Pan kéo thả tự do, tự động mở rộng vùng chơi khi đặt cờ.
  * Hỗ trợ 2 luật chơi chuẩn: **Chặn 2 đầu** (Việt Nam) và **Tự do**.
* **⚔️ Chế Độ Chơi Đa Dạng**:
  * **Đấu với Máy (AI)**: Thuật toán Alpha-Beta Pruning 3 cấp độ (Dễ, Trung bình, Khó).
  * **Đấu 2 Người Offline (Pass & Play)**: Chơi cùng bạn bè trên một máy tính.
  * **Đấu Trực Tuyến Thời Gian Thực (Online Multiplayer)**: Tạo phòng có mã, ghép đấu tự động, bộ đếm thời gian lượt chơi và tổng trận.
* **👑 Hệ Thống Tài Khoản & Xếp Hạng (Chess.com Style)**:
  * Đăng ký / Đăng nhập an toàn với mã xác thực OTP qua Email (Gmail SMTP).
  * Điểm xếp hạng **Elo Rating**, chuỗi thắng (Win Streak), kỷ lục Elo (Peak Elo) và tỷ lệ thắng.
  * Phân bậc cờ thủ: *Đại Kiện Tướng*, *Kiện Tướng*, *Tinh Anh*, *Nghiệp Dư*, *Tập Sự*.
* **🖼️ Tùy Biến Avatar & Hồ Sơ**:
  * Bộ sưu tập 10 icon danh giá (👑 Vua Cờ, ⚔️ Hiệp Sĩ, 🥷 Ninja, 🤖 Robot, 🐉 Hỏa Long...).
  * **Tự do tải ảnh từ máy tính**: Tự động căn giữa, cắt vuông 1:1 và nén tối ưu (256x256 circular avatar).
  * Chọn cờ quốc gia (Việt Nam 🇻🇳, Nhật Bản 🇯🇵, Mỹ 🇺🇸...).
* **👥 Hệ Thống Bạn Bè & Thách Đấu Trực Tiếp**:
  * Xem trạng thái bạn bè theo thời gian thực (🟢 Online, 🟠 Đang chơi, ⚫ Offline).
  * Gửi lời mời thách đấu (Live Challenge Popup) vào phòng ngay lập tức.
  * Tìm kiếm người chơi theo username hoặc email.

---

## 🛠️ Công Nghệ Sử Dụng

* **Frontend Client**: .NET 9, WPF (Windows Presentation Foundation), CommunityToolkit.Mvvm.
* **Backend Server**: ASP.NET Core 9 Minimal APIs, SignalR Core WebSockets, Entity Framework Core SQLite.
* **Architecture**: Clean Architecture (Core, Data, Server, Wpf Client).
* **Testing**: xUnit, FluentAssertions, Moq (26/26 Unit Tests).

---

## 🚀 Hướng Dẫn Cài Đặt & Chạy Dự Án

### Yêu Cầu Môi Trường
* Hệ điều hành: Windows 10/11 (64-bit).
* SDK: [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

### 1. Khởi Chạy Server
```bash
# Di chuyển vào thư mục Server
cd src/CaroGame.Server

# Cấu hình file bí mật cho Email OTP (Tùy chọn)
# Tạo file appsettings.Local.json và điền App Password Gmail nếu muốn dùng gửi mã OTP qua Email

# Khởi chạy máy chủ
dotnet run
```
Máy chủ sẽ lắng nghe tại `http://localhost:5000` (hoặc mở rộng mạng LAN/Ngrok).

### 2. Khởi Chạy Client WPF
```bash
# Di chuyển vào thư mục Client
cd src/CaroGame.Wpf

# Khởi chạy giao diện game
dotnet run
```

---

## 🔒 Bảo Mật & Thông Tin Cấu Hình

* Mọi thông tin nhạy cảm (như mật khẩu ứng dụng Gmail, database người chơi) đều được bảo vệ trong file `appsettings.Local.json` và cơ sở dữ liệu `*.db`, đã được loại trừ khỏi Git qua `.gitignore`.
* Mật khẩu người chơi được mã hóa an toàn qua thuật toán PBKDF2/SHA256 kèm Salt ngẫu nhiên.
* Token xác thực được tạo ngẫu nhiên bằng bộ sinh số an toàn mật mã (`RandomNumberGenerator`).

---

## 📦 Bản Phát Hành (Releases)

Bạn có thể tải file chạy trực tiếp (Portable, không cần cài đặt .NET) tại mục **[Releases](https://github.com/)** của repository này.

---
© 2026 Caro Arena Online. Developed with .NET 9 & WPF.

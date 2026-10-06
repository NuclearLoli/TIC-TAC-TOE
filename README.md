# 🎮 Cờ Caro Arena (Infinite Gomoku Online) - .NET 9 WPF

<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 9" />
  <img src="https://img.shields.io/badge/WPF-Desktop-0078D7?style=for-the-badge&logo=windows&logoColor=white" alt="WPF" />
  <img src="https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/SignalR-Real--Time-512BD4?style=for-the-badge&logo=socketdotio&logoColor=white" alt="SignalR" />
  <img src="https://img.shields.io/badge/SQLite-Database-003B57?style=for-the-badge&logo=sqlite&logoColor=white" alt="SQLite" />
  <img src="https://img.shields.io/badge/Tests-33%2F33%20Passed-success?style=for-the-badge&logo=checkmarx&logoColor=white" alt="Tests" />
  <img src="https://img.shields.io/badge/License-MIT-blue?style=for-the-badge" alt="License MIT" />
</p>

> **Cờ Caro Arena** là trò chơi Cờ Caro bàn cờ vô hạn đỉnh cao được xây dựng trên nền tảng **.NET 9 WPF** hiện đại theo phong cách **Chess.com Dark Theme**. Dự án tích hợp hệ thống thi đấu trực tuyến thời gian thực (SignalR WebSockets), trí tuệ nhân tạo Minimax Alpha-Beta Pruning, bảng xếp hạng Elo chuẩn quốc tế, mạng xã hội bạn bè và hệ thống tùy biến hồ sơ cá nhân sâu sắc.

---

## 🌟 Tính Năng Nổi Bật

### ♾️ 1. Bàn Cờ Vô Hạn (Infinite Pan & Zoom Canvas)
* **Kéo thả & Thu phóng tự do:** Hỗ trợ thu phóng mượt mà từ `0.4x` đến `3.0x` bằng con lăn chuột, kéo bàn cờ bằng chuột giữa hoặc chuột phải.
* **Tự động mở rộng:** Bàn cờ tự động nở rộng theo vị trí đánh mà không bị giới hạn bởi kích thước cố định `15x15` hay `19x19`.
* **Hai luật chơi chuẩn thi đấu:**
  * **Luật Chặn 2 đầu (Việt Nam):** Hàng 5 quân liên tiếp bị chặn cả 2 đầu bởi quân đối phương sẽ không tính là thắng.
  * **Luật Tự do (Quốc tế):** Đạt 5 quân liên tiếp (hoặc nhiều hơn) theo bất kỳ hướng nào là giành chiến thắng ngay lập tức.

### ⚔️ 2. Chế Độ Chơi Đa Dạng
* 🤖 **Đấu với Robot (AI Minimax Engine):**
  * Tích hợp thuật toán Minimax kết hợp Alpha-Beta Pruning và bảng mẫu đe dọa (Threat Pattern Table).
  * 3 cấp độ thông minh: **Dễ**, **Vừa**, **Khó** (tìm kiếm sâu, phản xạ chặn nước đôi, nước 4 nhạy bén).
* 👥 **Đấu 2 Người Offline (Pass & Play):** Chơi trực tiếp 2 người luân phiên trên cùng một máy tính mà không cần kết nối mạng.
* ⚡ **Đấu Trực Tuyến Thời Gian Thực (SignalR Online PvP):**
  * Ghép phòng nhanh bằng mã phòng 6 số hoặc tạo phòng riêng tư.
  * Đồng hồ đếm ngược lượt đánh (30s) và đồng hồ tổng trận đấu.
  * Tính năng xin hòa (Draw), đầu hàng (Resign) và yêu cầu đấu lại (Rematch).
  * Khung chat thời gian thực ngay trong bàn cờ.

### 🏆 3. Hệ Thống Xếp Hạng & Điểm Elo (Chess.com Style)
* **Điểm xếp hạng Elo chuẩn:** Thuật toán K-factor quốc tế tính điểm dựa trên chênh lệch trình độ giữa 2 kỳ thủ.
* **Bảng Vàng Cao Thủ (Leaderboard):** Bảng xếp hạng Top 30 người chơi có điểm Elo cao nhất toàn server.
* **Phân bậc đẳng cấp danh giá:**
  * 👑 *Đại Kiện Tướng* (Elo ≥ 1800)
  * 💎 *Kiện Tướng* (Elo ≥ 1500)
  * 🥇 *Tinh Anh* (Elo ≥ 1300)
  * 🥈 *Nghiệp Dư* (Elo ≥ 1100)
  * 🥉 *Tập Sự* (Elo < 1100)
* Thống kê chi tiết: Tỉ lệ thắng (Win Rate), chuỗi thắng hiện tại, kỷ lục chuỗi thắng (Best Win Streak) và tổng thời gian thi đấu.

### 🎨 4. Hồ Sơ Kỳ Thủ & Tùy Biến Cá Nhân
* **Bộ sưu tập Avatar phong phú:** 10+ biểu tượng linh vật độc quyền (Vua Cờ 👑, Hiệp Sĩ ⚔️, Ninja 🥷, Robot 🤖, Hỏa Long 🐉, v.v.).
* **Tải ảnh đại diện cá nhân:** Tự do upload ảnh từ máy tính (hỗ trợ JPG/PNG), tự động cắt tròn 1:1 và nén tối ưu.
* **Khung viền Avatar (Avatar Frames):** Classic, Đồng, Bạc, Vàng, Kim Cương, Thách Đấu.
* **Danh hiệu Kỳ thủ (Title & Flair):** Lựa chọn danh hiệu danh dự (*Kiện Tướng*, *Chiến Thần*, *Bậc Thầy Caro*...) hoặc tự đặt danh hiệu riêng.
* **Quốc gia & Cờ hiệu:** Lựa chọn quốc kỳ đại diện (🇻🇳 Việt Nam, 🇯🇵 Nhật Bản, 🇰🇷 Hàn Quốc, 🇺🇸 Mỹ...).

### 🛡️ 5. Bảo Mật & Quản Lý Tài Khoản Chuẩn Mực
* **Mã hóa mật khẩu an toàn:** Sử dụng thuật toán PBKDF2 với muối ngẫu nhiên (Salt).
* **Bảo mật phiên đăng nhập Stateless HMAC-SHA256:**
  * Token được ký số an toàn bằng khóa bí mật lưu trong `server_secret.key`.
  * Phiên đăng nhập duy trì 30 ngày, bảo toàn trọn vẹn ngay cả khi khởi động lại máy chủ.
* **Xác thực OTP Email:** Gửi mã xác thực OTP 6 số qua email khi đăng ký tài khoản, khôi phục mật khẩu hoặc liên kết email.
* **Liên kết & Thay đổi Email:** Tính năng liên kết email mới kèm kiểm tra xác thực OTP trực tiếp trong trang Cài Đặt.
* **Bảo mật hiển thị mật khẩu:**
  * 100% các ô nhập mật khẩu được che dấu sao bảo mật: `●●●●●●`.
  * Tích hợp nút **Con Mắt (`👁️` / `🙈`)** cho phép ẩn/hiện mật khẩu tức thì và hỗ trợ phím **Enter** để gửi lệnh nhanh chóng.

### 🔊 6. Cài Đặt Âm Thanh & Giao Diện
* Tùy chỉnh thanh trượt âm lượng tổng (Master Volume).
* Tự do thay thế file âm thanh `.wav` / `.mp3` cho từng sự kiện: *Đánh cờ*, *Cảnh báo 4 ô*, *Thắng trận*, *Thua trận*, *Rút lại nước*, *Đếm ngược*.
* Font chữ tiếng Việt sắc nét: Nhúng trực tiếp font `Cecefontvn` cùng công nghệ ClearType và Sub-pixel Layout Rounding chống mờ.

---

## 🏗️ Kiến Trúc Dự Án (Clean Architecture)

```
Caro-game/
├── src/
│   ├── CaroGame.Core/            # Domain Models, Engine AI Minimax, Luật chơi, Network DTOs & Interfaces
│   ├── CaroGame.Data/            # SQLite Entity Framework Core, DbContext & Game Repository
│   ├── CaroGame.Server/          # ASP.NET Core 9 Minimal APIs, SignalR CaroHub, TokenService HMAC, Email OTP
│   └── CaroGame.Wpf/             # WPF Client (.NET 9), MVVM Pattern, Canvas vô hạn, Views & ViewModels
├── tests/
│   └── CaroGame.Core.Tests/      # 33 Unit Tests tự động (xUnit) kiểm thử AI, Luật chơi, Elo, DTOs
├── installer/
│   └── caro_installer.iss        # Kịch bản đóng gói Inno Setup tạo file cài đặt Windows Installer (.exe)
├── publish/                      # Thư mục phát hành độc lập (CaroClient, CaroServer, batch scripts)
├── .gitignore                    # Bộ lọc bảo mật toàn diện cho .NET 9
├── .gitattributes                # Chuẩn hóa mã hóa dòng và bảo vệ file binary (font, audio, db)
└── README.md                     # Tài liệu hướng dẫn dự án
```

---

## 🚀 Hướng Dẫn Cài Đặt & Chạy

### Yêu Cầu Môi Trường
* Hệ điều hành: **Windows 10 / 11** (64-bit).
* SDK: [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) trở lên.

---

### Cách 1: Khởi Chạy Nhanh Bằng File Batch (Khuyên dùng)
Trong thư mục `publish/` đã tích hợp sẵn các file thực thi một click:
1. Nhấp đúp vào `publish/START_SERVER.bat` để khởi động Caro Server.
2. Nhấp đúp vào `publish/START_CLIENT.bat` để mở giao diện game Caro Arena.

---

### Cách 2: Khởi Chạy Từ Mã Nguồn (Dành cho Lập trình viên)

#### 1. Biên dịch và kiểm tra Unit Tests:
```powershell
# Chạy toàn bộ 33 bài kiểm thử tự động
dotnet test

# Biên dịch toàn bộ solution
dotnet build --configuration Release
```

#### 2. Khởi chạy Server:
```powershell
dotnet run --project src/CaroGame.Server
```
*Máy chủ sẽ tự động chạy tại địa chỉ: `http://localhost:5000`.*

#### 3. Khởi chạy Client WPF:
```powershell
dotnet run --project src/CaroGame.Wpf
```

---

### 📧 Cấu Hình Email OTP (Tùy chọn)
Mặc định hệ thống hoạt động hoàn hảo ở chế độ Console OTP (mã OTP in ra log server khi test). Để kích hoạt gửi mã OTP thực tế qua Gmail:
1. Tạo file `src/CaroGame.Server/appsettings.Local.json`:
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-16-character-app-password"
  }
}
```
*(File này đã được bảo vệ trong `.gitignore`, không bao giờ bị lộ lên GitHub).*

---

## ⌨️ Phím Tắt Trong Trận Đấu

| Phím tắt | Chức năng |
| :--- | :--- |
| **Cuộn chuột (Wheel)** | Thu phóng bàn cờ (Zoom In / Zoom Out từ 0.4x đến 3.0x) |
| **Kéo chuột giữa / phải** | Kéo di chuyển toàn bộ bàn cờ vô hạn (Pan Canvas) |
| **Ctrl + Z** | Rút lại nước đi (Chế độ chơi với máy hoặc 2 người) |
| **R** | Khởi động lại ván đấu mới |
| **Enter** | Gửi tin nhắn trong khung chat hoặc Đăng nhập nhanh |
| **Esc** | Đóng hộp thoại hoặc quay lại menu |

---

## 🧪 Kiểm Thử Tự Động (Unit Tests)
Hệ thống được kiểm thử tự động toàn diện với **33 bài kiểm thử unit tests**:
* `AiEngineTests`: Đánh giá phản xạ thông minh của AI Minimax, chặn nước đôi, nước 4 và tìm kiếm nước thắng.
* `RuleEngineTests`: Kiểm thử chính xác luật *Chặn 2 đầu* Việt Nam và luật *Tự do*.
* `DynamicBoardTests`: Kiểm thử khả năng mở rộng bàn cờ vô hạn và thuật toán kiểm tra 5 quân liên tiếp.
* `EloAndAuthTests`: Kiểm thử thuật toán tính điểm Elo và mã hóa mật khẩu.
* `NetworkDtoTests`: Kiểm thử tuần tự hóa JSON cho toàn bộ gói tin thời gian thực và liên kết email.

---

## 📄 Giấy Phép (License)
Dự án được phân phối dưới giấy phép **MIT License**. Bạn có thể tự do sử dụng, chỉnh sửa và phát triển tiếp cho mục đích cá nhân hoặc thương mại.

---
<p align="center">
  <b>Cờ Caro Arena</b> - Đấu trí đỉnh cao, thăng hạng vinh quang! 🏆
</p>

# Caro 30x30 WPF

Game Caro viết bằng `C# WPF (.NET 9)` theo kiến trúc `MVVM`.

## Tính năng

- `PvP local`
- `PvC` với 3 mức AI: `Dễ`, `Vừa`, `Siêu khó`
- Bàn cờ `30x30`
- Lưu lịch sử trận đấu bằng `SQL Server + EF Core`
- Chọn ảnh `png/jpg` làm background

## Chạy project

```powershell
dotnet build "TIC TAC TOE.sln"
dotnet run --project "TIC TAC TOE/TIC TAC TOE.csproj"
```

## Publish file exe

```powershell
dotnet publish "TIC TAC TOE/TIC TAC TOE.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish/win-x64
```

File chạy nằm trong thư mục `publish/win-x64`.

## Database

App ưu tiên đọc chuỗi kết nối từ biến môi trường:

- `CARO_SQLSERVER_CONNECTION`

Nếu không có, app sẽ dùng mặc định:

- `(localdb)\MSSQLLocalDB`
- Database: `CaroGameDb`

## Gợi ý an toàn khi đưa lên GitHub

- Không commit chuỗi kết nối thật, mật khẩu, API key hoặc file `.env`
- Không commit `bin`, `obj`, `.vs`, file publish
- Dùng `Trusted_Connection` hoặc biến môi trường cho cấu hình riêng của máy
- Rà lại dữ liệu cá nhân trong lịch sử commit trước khi public repo

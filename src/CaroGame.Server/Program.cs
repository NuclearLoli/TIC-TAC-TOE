using CaroGame.Core.Network;
using CaroGame.Server.Data;
using CaroGame.Server.Hubs;
using CaroGame.Server.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
string localJsonBase = Path.Combine(AppContext.BaseDirectory, "appsettings.Local.json");
if (File.Exists(localJsonBase))
{
    builder.Configuration.AddJsonFile(localJsonBase, optional: true, reloadOnChange: true);
}

if (!args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase)) &&
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://0.0.0.0:5000");
}

// Database SQLite
string baseDbPath = Path.Combine(AppContext.BaseDirectory, "caro_server.db");
string contentDbPath = Path.Combine(builder.Environment.ContentRootPath, "caro_server.db");
string dbPath = File.Exists(baseDbPath) ? baseDbPath : (File.Exists(contentDbPath) ? contentDbPath : baseDbPath);
Console.WriteLine($"[Server] SQLite Database: {Path.GetFullPath(dbPath)}");
builder.Services.AddDbContext<ServerDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});

builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

// SignalR & CORS
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure Database exists on startup without duplicate column errors
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
    db.Database.EnsureCreated();

    try
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            conn.Open();
        }

        // Ensure secondary tables exist in case of legacy database
        using (var migCmd = conn.CreateCommand())
        {
            migCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS EmailVerifications (
                    Id TEXT PRIMARY KEY,
                    Email TEXT NOT NULL,
                    Code TEXT NOT NULL,
                    Purpose TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    ExpiresAt TEXT NOT NULL,
                    IsUsed INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS IX_EmailVerifications_Lookup ON EmailVerifications(Email, Code, Purpose);

                CREATE TABLE IF NOT EXISTS Friendships (
                    Id TEXT PRIMARY KEY,
                    RequesterId TEXT NOT NULL,
                    AddresseeId TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL DEFAULT ''
                );
                CREATE INDEX IF NOT EXISTS IX_Friendships_Pair ON Friendships(RequesterId, AddresseeId);
            ";
            migCmd.ExecuteNonQuery();
        }

        var existingFriendshipCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var fcmd = conn.CreateCommand())
        {
            fcmd.CommandText = "PRAGMA table_info(Friendships);";
            using var reader = fcmd.ExecuteReader();
            while (reader.Read())
            {
                existingFriendshipCols.Add(reader.GetString(1));
            }
        }
        if (!existingFriendshipCols.Contains("UpdatedAt"))
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE Friendships ADD COLUMN UpdatedAt TEXT DEFAULT '2025-01-01T00:00:00';";
            alterCmd.ExecuteNonQuery();
        }

        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info(Users);";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                existingColumns.Add(reader.GetString(1));
            }
        }

        void AddColumnIfNotExists(string name, string typeDef)
        {
            if (!existingColumns.Contains(name))
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = $"ALTER TABLE Users ADD COLUMN {name} {typeDef};";
                alterCmd.ExecuteNonQuery();
            }
        }

        AddColumnIfNotExists("Email", "TEXT DEFAULT ''");
        AddColumnIfNotExists("Avatar", "TEXT DEFAULT 'king'");
        AddColumnIfNotExists("Country", "TEXT DEFAULT 'VN'");
        AddColumnIfNotExists("Bio", "TEXT DEFAULT 'Đam mê cờ Caro!'");
        AddColumnIfNotExists("Wins", "INTEGER DEFAULT 0");
        AddColumnIfNotExists("Losses", "INTEGER DEFAULT 0");
        AddColumnIfNotExists("Draws", "INTEGER DEFAULT 0");
        AddColumnIfNotExists("PeakElo", "INTEGER DEFAULT 1000");
        AddColumnIfNotExists("LowestElo", "INTEGER DEFAULT 1000");
        AddColumnIfNotExists("WinStreak", "INTEGER DEFAULT 0");
        AddColumnIfNotExists("BestWinStreak", "INTEGER DEFAULT 0");
        AddColumnIfNotExists("LastLoginAt", "TEXT DEFAULT '2025-01-01T00:00:00'");
        AddColumnIfNotExists("Role", "TEXT DEFAULT 'Player'");
        AddColumnIfNotExists("Status", "TEXT DEFAULT 'Active'");
        AddColumnIfNotExists("IsEmailVerified", "INTEGER DEFAULT 1");
        AddColumnIfNotExists("Level", "INTEGER DEFAULT 1");
        AddColumnIfNotExists("ExperiencePoints", "INTEGER DEFAULT 0");
        AddColumnIfNotExists("Title", "TEXT DEFAULT 'Tân Thủ'");
        AddColumnIfNotExists("AvatarFrame", "TEXT DEFAULT 'classic'");
        AddColumnIfNotExists("TotalPlayTimeSeconds", "INTEGER DEFAULT 0");
        AddColumnIfNotExists("LastLoginIp", "TEXT");
        AddColumnIfNotExists("PasswordChangedAt", "TEXT");

        // Fill default email if missing
        using (var updEmail = conn.CreateCommand())
        {
            updEmail.CommandText = "UPDATE Users SET Email = LOWER(Username) || '@carogame.local' WHERE Email IS NULL OR Email = '';";
            updEmail.ExecuteNonQuery();
        }

        // Seed default account mailrac0212@gmail.com and support Testarossa (Password: Caro@0212)
        using (var seedCmd = conn.CreateCommand())
        {
            string hash = PasswordHasher.HashPassword("Caro@0212");
            seedCmd.CommandText = @"
                UPDATE Users SET Id = UPPER(Id);
                UPDATE Users SET Email = 'testarossa@carogame.local', PasswordHash = @hash WHERE LOWER(Username) = 'testarossa';
                UPDATE Users SET PasswordHash = @hash, Email = 'mailrac0212@gmail.com', Role = 'VIP', Title = 'Kiện Tướng', AvatarFrame = 'gold', Level = 5, ExperiencePoints = 420 WHERE LOWER(Username) = 'mailrac0212';
            ";
            var pHash = seedCmd.CreateParameter();
            pHash.ParameterName = "@hash";
            pHash.Value = hash;
            seedCmd.Parameters.Add(pHash);
            seedCmd.ExecuteNonQuery();
        }

        bool hasMailrac = db.Users.AsNoTracking().Any(u => u.Username.ToLower() == "mailrac0212" || u.Email.ToLower() == "mailrac0212@gmail.com");
        if (!hasMailrac)
        {
            var seedUser = new User
            {
                Username = "mailrac0212",
                Email = "mailrac0212@gmail.com",
                DisplayName = "Kỳ Thủ Caro",
                PasswordHash = PasswordHasher.HashPassword("Caro@0212"),
                Role = "VIP",
                Title = "Kiện Tướng",
                AvatarFrame = "gold",
                Level = 5,
                ExperiencePoints = 420,
                EloRating = 1200,
                PeakElo = 1200,
                LowestElo = 950,
                Avatar = "king",
                Country = "VN",
                Bio = "Đam mê cờ Caro!",
                Wins = 5,
                Losses = 1,
                Draws = 0,
                WinStreak = 3,
                BestWinStreak = 4,
                TotalPlayTimeSeconds = 3600,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            db.Users.Add(seedUser);
            db.SaveChanges();
            Console.WriteLine("[Server] Khởi tạo tài khoản mẫu: mailrac0212@gmail.com (Mật khẩu: Caro@0212)");
        }
        else
        {
            Console.WriteLine("[Server] Đã đồng bộ tài khoản mẫu mailrac0212 (Mật khẩu: Caro@0212)");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Lỗi kiểm tra cấu trúc bảng SQLite Users");
    }
}

app.UseCors();

app.MapGet("/", () => "Caro Game SignalR Server is running!");
app.MapGet("/health", () => Results.Ok(new { status = "online", timestamp = DateTime.UtcNow }));

// Check Account Availability (Inspired by Chess.com)
app.MapPost("/api/auth/check-availability", async (CheckAvailabilityRequestDto req, ServerDbContext db) =>
{
    string identifier = req.Identifier?.Trim() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(identifier))
    {
        return Results.BadRequest(new CheckAvailabilityResponseDto
        {
            Exists = false,
            Available = false,
            Message = "Vui lòng nhập tên người dùng hoặc địa chỉ Email."
        });
    }

    bool isEmail = identifier.Contains('@') && identifier.Contains('.');
    string lowerId = identifier.ToLowerInvariant();

    var existingUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u =>
        u.Username.ToLower() == lowerId || u.Email.ToLower() == lowerId);

    if (existingUser != null)
    {
        bool matchedByEmail = existingUser.Email.Equals(lowerId, StringComparison.OrdinalIgnoreCase);
        return Results.Ok(new CheckAvailabilityResponseDto
        {
            Exists = true,
            Available = false,
            IsEmail = matchedByEmail || isEmail,
            Message = matchedByEmail
                ? "Địa chỉ Email này đã được đăng ký tài khoản."
                : $"Tên tài khoản '{existingUser.Username}' đã được sử dụng."
        });
    }

    return Results.Ok(new CheckAvailabilityResponseDto
    {
        Exists = false,
        Available = true,
        IsEmail = isEmail,
        Message = isEmail
            ? "Email hợp lệ và sẵn sàng đăng ký!"
            : $"Tên tài khoản '{identifier}' khả dụng!"
    });
});

// OTP Endpoints
app.MapPost("/api/auth/send-otp", async (SendOtpRequestDto req, ServerDbContext db, IEmailService emailService) =>
{
    string email = req.Email?.Trim() ?? string.Empty;
    string purpose = string.IsNullOrWhiteSpace(req.Purpose) ? "Register" : req.Purpose.Trim();

    if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Địa chỉ email không hợp lệ." });
    }

    if (string.Equals(purpose, "Register", StringComparison.OrdinalIgnoreCase))
    {
        bool exists = await db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        if (exists)
        {
            return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Email này đã được sử dụng cho tài khoản khác." });
        }
    }
    else if (string.Equals(purpose, "ResetPassword", StringComparison.OrdinalIgnoreCase))
    {
        bool exists = await db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        if (!exists)
        {
            return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Không tìm thấy tài khoản nào gắn với email này." });
        }
    }

    // Generate 6-digit OTP
    string code = Random.Shared.Next(100000, 999999).ToString();

    // Invalidate old active OTPs for this email and purpose
    var oldOtps = await db.EmailVerifications
        .Where(v => v.Email.ToLower() == email.ToLower() && v.Purpose == purpose && !v.IsUsed)
        .ToListAsync();
    foreach (var o in oldOtps) o.IsUsed = true;

    var verification = new EmailVerification
    {
        Email = email,
        Code = code,
        Purpose = purpose,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddMinutes(5),
        IsUsed = false
    };
    db.EmailVerifications.Add(verification);
    await db.SaveChangesAsync();

    var sendResult = await emailService.SendOtpEmailAsync(email, code, purpose);
    if (!sendResult.Success)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = sendResult.Message });
    }

    return Results.Ok(new AuthResponseDto { Success = true, Message = $"Mã OTP 6 số đã được gửi đến {email}. Vui lòng kiểm tra hộp thư đến!" });
});

app.MapPost("/api/auth/verify-otp", async (VerifyOtpRequestDto req, ServerDbContext db) =>
{
    string email = req.Email?.Trim() ?? string.Empty;
    string code = req.Code?.Trim() ?? string.Empty;
    string purpose = string.IsNullOrWhiteSpace(req.Purpose) ? "Register" : req.Purpose.Trim();

    var verification = await db.EmailVerifications
        .Where(v => v.Email.ToLower() == email.ToLower() && v.Purpose == purpose && !v.IsUsed && v.ExpiresAt > DateTime.UtcNow)
        .OrderByDescending(v => v.CreatedAt)
        .FirstOrDefaultAsync();

    if (verification == null || verification.Code != code)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Mã xác thực không chính xác hoặc đã hết hạn." });
    }

    return Results.Ok(new AuthResponseDto { Success = true, Message = "Mã xác thực hợp lệ!" });
});

app.MapPost("/api/auth/reset-password", async (ResetPasswordRequestDto req, ServerDbContext db) =>
{
    string email = req.Email?.Trim() ?? string.Empty;
    string code = req.OtpCode?.Trim() ?? string.Empty;
    string newPassword = req.NewPassword ?? string.Empty;

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code) || newPassword.Length < 6)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Thông tin không hợp lệ hoặc mật khẩu mới quá ngắn (tối thiểu 6 ký tự)." });
    }

    var verification = await db.EmailVerifications
        .Where(v => v.Email.ToLower() == email.ToLower() && v.Purpose == "ResetPassword" && !v.IsUsed && v.ExpiresAt > DateTime.UtcNow)
        .OrderByDescending(v => v.CreatedAt)
        .FirstOrDefaultAsync();

    if (verification == null || verification.Code != code)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Mã OTP không chính xác hoặc đã hết hạn." });
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    if (user == null)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Không tìm thấy tài khoản với email này." });
    }

    user.PasswordHash = PasswordHasher.HashPassword(newPassword);
    verification.IsUsed = true;
    await db.SaveChangesAsync();

    return Results.Ok(new AuthResponseDto { Success = true, Message = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập ngay." });
});

// Authentication Endpoints
app.MapPost("/api/auth/register", async (RegisterRequestDto req, ServerDbContext db, TokenService tokenService) =>
{
    string username = req.Username?.Trim() ?? string.Empty;
    string email = req.Email?.Trim() ?? string.Empty;
    string password = req.Password ?? string.Empty;
    string displayName = string.IsNullOrWhiteSpace(req.DisplayName) ? username : req.DisplayName.Trim();

    if (username.Length < 3 || username.Length > 30)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Tên đăng nhập phải từ 3 đến 30 ký tự." });
    }

    if (string.IsNullOrWhiteSpace(email))
    {
        email = $"{username.ToLower()}@carogame.local";
    }
    else if (!email.Contains('@') || !email.Contains('.'))
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Vui lòng nhập địa chỉ Email hợp lệ." });
    }

    if (password.Length < 6)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Mật khẩu phải từ 6 ký tự trở lên." });
    }

    bool usernameExists = await db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
    if (usernameExists)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Tên đăng nhập này đã được sử dụng." });
    }

    bool emailExists = await db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    if (emailExists)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Địa chỉ Email này đã được đăng ký tài khoản khác." });
    }

    if (!string.IsNullOrWhiteSpace(req.OtpCode))
    {
        var verification = await db.EmailVerifications
            .Where(v => v.Email.ToLower() == email.ToLower() && v.Purpose == "Register" && !v.IsUsed && v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync();

        if (verification == null || verification.Code != req.OtpCode.Trim())
        {
            return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Mã xác thực OTP không chính xác hoặc đã hết hạn (5 phút)." });
        }

        verification.IsUsed = true;
    }

    var user = new User
    {
        Username = username,
        Email = email,
        PasswordHash = PasswordHasher.HashPassword(password),
        DisplayName = displayName,
        EloRating = 1000,
        CreatedAt = DateTime.UtcNow,
        LastLoginAt = DateTime.UtcNow
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    string token = tokenService.CreateToken(user.Id);

    return Results.Ok(new AuthResponseDto
    {
        Success = true,
        Message = "Đăng ký tài khoản thành công!",
        Token = token,
        User = MapProfile(user)
    });
});

app.MapPost("/api/auth/login", async (LoginRequestDto req, ServerDbContext db, TokenService tokenService) =>
{
    string identifier = (string.IsNullOrWhiteSpace(req.UsernameOrEmail) ? req.Username : req.UsernameOrEmail)?.Trim().ToLower() ?? string.Empty;
    string password = req.Password ?? string.Empty;

    var user = await db.Users.FirstOrDefaultAsync(u =>
        u.Username.ToLower() == identifier ||
        (u.Email != null && u.Email.ToLower() == identifier));

    if (user == null || !PasswordHasher.VerifyPassword(password, user.PasswordHash))
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Tên đăng nhập / Email hoặc mật khẩu không chính xác." });
    }

    try
    {
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Không thể cập nhật LastLoginAt khi người dùng đăng nhập");
    }

    string token = tokenService.CreateToken(user.Id);

    return Results.Ok(new AuthResponseDto
    {
        Success = true,
        Message = "Đăng nhập thành công!",
        Token = token,
        User = MapProfile(user)
    });
});

app.MapGet("/api/auth/profile", async Task<IResult> (HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return JsonUnauthorized();

    var user = await db.Users.FindAsync(userId.Value);
    if (user == null) return JsonNotFound();

    return Results.Ok(MapProfile(user));
});

// Public profile lookup
app.MapGet("/api/profile/{idOrUsername}", async Task<IResult> (string idOrUsername, ServerDbContext db) =>
{
    idOrUsername = idOrUsername.Trim();
    User? user = null;
    if (Guid.TryParse(idOrUsername, out var guid))
    {
        user = await db.Users.FindAsync(guid);
    }
    if (user == null)
    {
        user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == idOrUsername.ToLower());
    }

    if (user == null) return JsonNotFound();
    return Results.Ok(MapProfile(user));
});

// Update Profile Customization
app.MapPost("/api/profile/update", async Task<IResult> (UpdateProfileRequestDto req, HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return JsonUnauthorized();

    var user = await db.Users.FindAsync(userId.Value);
    if (user == null) return JsonNotFound();

    if (!string.IsNullOrWhiteSpace(req.DisplayName))
    {
        string name = req.DisplayName.Trim();
        if (name.Length >= 2 && name.Length <= 40)
        {
            user.DisplayName = name;
        }
    }

    if (!string.IsNullOrWhiteSpace(req.Avatar))
    {
        var trimmed = req.Avatar.Trim();
        if (trimmed.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            if (trimmed.Length <= 500_000)
            {
                user.Avatar = trimmed;
            }
        }
        else
        {
            user.Avatar = trimmed.ToLowerInvariant();
        }
    }

    if (!string.IsNullOrWhiteSpace(req.Country))
    {
        user.Country = req.Country.Trim().ToUpper();
    }

    if (req.Bio != null)
    {
        user.Bio = req.Bio.Length > 200 ? req.Bio[..200] : req.Bio;
    }

    if (!string.IsNullOrWhiteSpace(req.Title))
    {
        user.Title = req.Title.Trim();
    }

    if (!string.IsNullOrWhiteSpace(req.AvatarFrame))
    {
        user.AvatarFrame = req.AvatarFrame.Trim().ToLowerInvariant();
    }

    await db.SaveChangesAsync();

    return Results.Ok(new AuthResponseDto
    {
        Success = true,
        Message = "Cập nhật hồ sơ thành công!",
        User = MapProfile(user)
    });
});

// Change Password
app.MapPost("/api/profile/change-password", async Task<IResult> (ChangePasswordRequestDto req, HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return JsonUnauthorized();

    var user = await db.Users.FindAsync(userId.Value);
    if (user == null) return JsonNotFound();

    if (!PasswordHasher.VerifyPassword(req.OldPassword, user.PasswordHash))
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Mật khẩu hiện tại không chính xác." });
    }

    if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Mật khẩu mới phải từ 6 ký tự trở lên." });
    }

    user.PasswordHash = PasswordHasher.HashPassword(req.NewPassword);
    await db.SaveChangesAsync();

    return Results.Ok(new AuthResponseDto { Success = true, Message = "Đổi mật khẩu thành công!" });
});

// Friends Endpoints
app.MapGet("/api/friends", async Task<IResult> (HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return JsonUnauthorized();

    var uid = userId.Value;
    var friendships = await db.Friendships
        .Where(f => f.RequesterId == uid || f.AddresseeId == uid)
        .ToListAsync();

    var friendList = new List<FriendDto>();

    foreach (var f in friendships)
    {
        bool isRequester = f.RequesterId == uid;
        var otherUserId = isRequester ? f.AddresseeId : f.RequesterId;
        var otherUser = await db.Users.FindAsync(otherUserId);
        if (otherUser == null) continue;

        string displayStatus = f.Status;
        if (f.Status == "Pending")
        {
            displayStatus = isRequester ? "Sent" : "Pending";
        }

        bool isOnline = CaroHub.UserToConnection.ContainsKey(otherUserId);

        friendList.Add(new FriendDto
        {
            FriendshipId = f.Id,
            FriendId = otherUser.Id,
            Username = otherUser.Username,
            DisplayName = otherUser.DisplayName,
            Avatar = otherUser.Avatar,
            Country = otherUser.Country,
            Bio = otherUser.Bio,
            EloRating = otherUser.EloRating,
            Wins = otherUser.Wins,
            Losses = otherUser.Losses,
            IsOnline = isOnline,
            Status = displayStatus,
            CreatedAt = f.CreatedAt
        });
    }

    return Results.Ok(friendList.OrderByDescending(f => f.IsOnline).ThenByDescending(f => f.EloRating));
});

app.MapPost("/api/friends/request", async Task<IResult> (SendFriendRequestDto req, HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return JsonUnauthorized();

    string target = req.TargetUsername?.Trim().ToLower() ?? string.Empty;
    var targetUser = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == target || u.Email.ToLower() == target);
    if (targetUser == null)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Không tìm thấy người chơi với tên đăng nhập hoặc email này." });
    }

    if (targetUser.Id == userId.Value)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Bạn không thể gửi lời mời kết bạn cho chính mình." });
    }

    var existing = await db.Friendships.FirstOrDefaultAsync(f =>
        (f.RequesterId == userId.Value && f.AddresseeId == targetUser.Id) ||
        (f.RequesterId == targetUser.Id && f.AddresseeId == userId.Value));

    if (existing != null)
    {
        if (existing.Status == "Accepted")
        {
            return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Hai bạn đã là bạn bè của nhau rồi!" });
        }
        if (existing.Status == "Pending")
        {
            return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Lời mời kết bạn đang chờ phản hồi." });
        }
        existing.Status = "Pending";
        existing.RequesterId = userId.Value;
        existing.AddresseeId = targetUser.Id;
        existing.UpdatedAt = DateTime.UtcNow;
    }
    else
    {
        db.Friendships.Add(new Friendship
        {
            RequesterId = userId.Value,
            AddresseeId = targetUser.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    await db.SaveChangesAsync();
    return Results.Ok(new AuthResponseDto { Success = true, Message = $"Đã gửi lời mời kết bạn đến {targetUser.DisplayName}!" });
});

app.MapPost("/api/friends/respond", async Task<IResult> (FriendRequestActionDto req, HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return JsonUnauthorized();

    var friendship = await db.Friendships.FindAsync(req.FriendshipId);
    if (friendship == null || friendship.AddresseeId != userId.Value)
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Lời mời kết bạn không hợp lệ." });
    }

    if (req.Accept)
    {
        friendship.Status = "Accepted";
        friendship.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(new AuthResponseDto { Success = true, Message = "Đã chấp nhận lời mời kết bạn!" });
    }
    else
    {
        db.Friendships.Remove(friendship);
        await db.SaveChangesAsync();
        return Results.Ok(new AuthResponseDto { Success = true, Message = "Đã từ chối lời mời kết bạn." });
    }
});

app.MapDelete("/api/friends/{friendId:guid}", async Task<IResult> (Guid friendId, HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return JsonUnauthorized();

    var friendship = await db.Friendships.FirstOrDefaultAsync(f =>
        (f.RequesterId == userId.Value && f.AddresseeId == friendId) ||
        (f.RequesterId == friendId && f.AddresseeId == userId.Value));

    if (friendship != null)
    {
        db.Friendships.Remove(friendship);
        await db.SaveChangesAsync();
    }

    return Results.Ok(new AuthResponseDto { Success = true, Message = "Đã hủy kết bạn." });
});

app.MapGet("/api/users/search", async (string? q, HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
    {
        return Results.Ok(new List<UserProfileDto>());
    }

    string query = q.Trim().ToLower();
    var users = await db.Users
        .Where(u => u.Id != (userId ?? Guid.Empty) && (u.Username.ToLower().Contains(query) || u.DisplayName.ToLower().Contains(query)))
        .Take(15)
        .ToListAsync();

    return Results.Ok(users.Select(MapProfile).ToList());
});

app.MapGet("/api/leaderboard", async (ServerDbContext db) =>
{
    var topPlayers = await db.Users
        .OrderByDescending(u => u.EloRating)
        .Take(30)
        .ToListAsync();

    return Results.Ok(topPlayers.Select(MapProfile).ToList());
});

// SignalR Hub
app.MapHub<CaroHub>("/carohub");

app.Run();

// Helper functions
static UserProfileDto MapProfile(User user) => new()
{
    Id = user.Id,
    Username = user.Username,
    Email = user.Email,
    DisplayName = user.DisplayName,
    Role = user.Role,
    Status = user.Status,
    IsEmailVerified = user.IsEmailVerified,
    Level = user.Level,
    ExperiencePoints = user.ExperiencePoints,
    Title = user.Title,
    AvatarFrame = user.AvatarFrame,
    Avatar = user.Avatar,
    Country = user.Country,
    Bio = user.Bio,
    EloRating = user.EloRating,
    PeakElo = user.PeakElo,
    LowestElo = user.LowestElo,
    Wins = user.Wins,
    Losses = user.Losses,
    Draws = user.Draws,
    WinStreak = user.WinStreak,
    BestWinStreak = user.BestWinStreak,
    TotalPlayTimeSeconds = user.TotalPlayTimeSeconds,
    CreatedAt = user.CreatedAt
};

static Guid? GetAuthUserId(HttpContext http, TokenService tokenService)
{
    string? authHeader = http.Request.Headers["Authorization"].FirstOrDefault();
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return null;
    return tokenService.ValidateToken(authHeader["Bearer ".Length..].Trim());
}

static IResult JsonUnauthorized(string message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.") =>
    Results.Json(new AuthResponseDto { Success = false, Message = message }, statusCode: 401);

static IResult JsonNotFound(string message = "Không tìm thấy dữ liệu yêu cầu.") =>
    Results.Json(new AuthResponseDto { Success = false, Message = message }, statusCode: 404);


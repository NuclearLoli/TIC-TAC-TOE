using CaroGame.Core.Network;
using CaroGame.Server.Data;
using CaroGame.Server.Hubs;
using CaroGame.Server.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

if (!args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase)) &&
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://0.0.0.0:5000");
}

// Database SQLite
string dbPath = Path.Combine(builder.Environment.ContentRootPath, "caro_server.db");
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

// Ensure Database exists on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
    db.Database.EnsureCreated();

    // Auto-migrate new profile columns if existing DB
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN Avatar TEXT DEFAULT 'king';"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN Country TEXT DEFAULT 'VN';"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN Bio TEXT DEFAULT 'Đam mê cờ Caro!';"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN PeakElo INTEGER DEFAULT 1000;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN WinStreak INTEGER DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN BestWinStreak INTEGER DEFAULT 0;"); } catch { }
}

app.UseCors();

app.MapGet("/", () => "Caro Game SignalR Server is running!");
app.MapGet("/health", () => Results.Ok(new { status = "online", timestamp = DateTime.UtcNow }));

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
        u.Email.ToLower() == identifier);

    if (user == null || !PasswordHasher.VerifyPassword(password, user.PasswordHash))
    {
        return Results.BadRequest(new AuthResponseDto { Success = false, Message = "Tên đăng nhập / Email hoặc mật khẩu không chính xác." });
    }

    user.LastLoginAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    string token = tokenService.CreateToken(user.Id);

    return Results.Ok(new AuthResponseDto
    {
        Success = true,
        Message = "Đăng nhập thành công!",
        Token = token,
        User = MapProfile(user)
    });
});

app.MapGet("/api/auth/profile", async (HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return Results.Unauthorized();

    var user = await db.Users.FindAsync(userId.Value);
    if (user == null) return Results.NotFound();

    return Results.Ok(MapProfile(user));
});

// Public profile lookup
app.MapGet("/api/profile/{idOrUsername}", async (string idOrUsername, ServerDbContext db) =>
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

    if (user == null) return Results.NotFound();
    return Results.Ok(MapProfile(user));
});

// Update Profile Customization
app.MapPost("/api/profile/update", async (UpdateProfileRequestDto req, HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return Results.Unauthorized();

    var user = await db.Users.FindAsync(userId.Value);
    if (user == null) return Results.NotFound();

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

    await db.SaveChangesAsync();

    return Results.Ok(new AuthResponseDto
    {
        Success = true,
        Message = "Cập nhật hồ sơ thành công!",
        User = MapProfile(user)
    });
});

// Change Password
app.MapPost("/api/profile/change-password", async (ChangePasswordRequestDto req, HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return Results.Unauthorized();

    var user = await db.Users.FindAsync(userId.Value);
    if (user == null) return Results.NotFound();

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
app.MapGet("/api/friends", async (HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return Results.Unauthorized();

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

app.MapPost("/api/friends/request", async (SendFriendRequestDto req, HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return Results.Unauthorized();

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

app.MapPost("/api/friends/respond", async (FriendRequestActionDto req, HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return Results.Unauthorized();

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

app.MapDelete("/api/friends/{friendId:guid}", async (Guid friendId, HttpContext http, ServerDbContext db, TokenService tokenService) =>
{
    var userId = GetAuthUserId(http, tokenService);
    if (!userId.HasValue) return Results.Unauthorized();

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
    Avatar = user.Avatar,
    Country = user.Country,
    Bio = user.Bio,
    EloRating = user.EloRating,
    PeakElo = user.PeakElo,
    Wins = user.Wins,
    Losses = user.Losses,
    Draws = user.Draws,
    WinStreak = user.WinStreak,
    BestWinStreak = user.BestWinStreak,
    CreatedAt = user.CreatedAt
};

static Guid? GetAuthUserId(HttpContext http, TokenService tokenService)
{
    string? authHeader = http.Request.Headers["Authorization"].FirstOrDefault();
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return null;
    return tokenService.ValidateToken(authHeader["Bearer ".Length..].Trim());
}

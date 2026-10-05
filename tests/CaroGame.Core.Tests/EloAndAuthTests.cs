using CaroGame.Server.Data;
using CaroGame.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CaroGame.Core.Tests;

public class EloAndAuthTests
{
    [Fact]
    public void PasswordHasher_ShouldVerifyCorrectPassword()
    {
        string rawPassword = "SecurePassword123!";
        string hash = PasswordHasher.HashPassword(rawPassword);

        Assert.NotEmpty(hash);
        Assert.Contains(":", hash);

        bool isValid = PasswordHasher.VerifyPassword(rawPassword, hash);
        Assert.True(isValid);

        bool isInvalid = PasswordHasher.VerifyPassword("WrongPassword", hash);
        Assert.False(isInvalid);
    }

    [Fact]
    public void EloCalculator_EqualRatings_WinnerGains16Points()
    {
        int eloX = 1000;
        int eloO = 1000;

        // Player X wins (scoreX = 1.0)
        var (changeX, changeO, newEloX, newEloO) = EloCalculator.Calculate(eloX, eloO, 1.0);

        Assert.Equal(16, changeX);
        Assert.Equal(-16, changeO);
        Assert.Equal(1016, newEloX);
        Assert.Equal(984, newEloO);
    }

    [Fact]
    public void EloCalculator_EqualRatings_DrawChangesZero()
    {
        int eloX = 1000;
        int eloO = 1000;

        // Draw (scoreX = 0.5)
        var (changeX, changeO, newEloX, newEloO) = EloCalculator.Calculate(eloX, eloO, 0.5);

        Assert.Equal(0, changeX);
        Assert.Equal(0, changeO);
        Assert.Equal(1000, newEloX);
        Assert.Equal(1000, newEloO);
    }

    [Fact]
    public void EloCalculator_UnderdogWins_GainsMorePoints()
    {
        int eloX = 1000; // Underdog
        int eloO = 1300; // Strong player

        var (changeX, changeO, newEloX, newEloO) = EloCalculator.Calculate(eloX, eloO, 1.0);

        // Underdog winning against +300 rating difference gains much more than 16 points (~27 points)
        Assert.True(changeX > 20);
        Assert.True(changeO < -20);
        Assert.Equal(eloX + changeX, newEloX);
        Assert.Equal(eloO + changeO, newEloO);
    }

    [Fact]
    public void TokenService_CreateAndValidateToken()
    {
        var tokenService = new TokenService();
        var userId = Guid.NewGuid();

        string token = tokenService.CreateToken(userId);
        Assert.NotEmpty(token);

        var validated = tokenService.ValidateToken(token);
        Assert.NotNull(validated);
        Assert.Equal(userId, validated.Value);

        var invalid = tokenService.ValidateToken("invalid_token_12345");
        Assert.Null(invalid);
    }

    [Fact]
    public async Task ServerDbContext_Leaderboard_OrdersByEloDescending()
    {
        string dbName = $"test_lb_{Guid.NewGuid():N}.db";
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite($"Data Source={dbName}")
            .Options;

        using var db = new ServerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.Users.AddRange(
            new User { Username = "user1", PasswordHash = "h", DisplayName = "User 1", EloRating = 1200 },
            new User { Username = "user2", PasswordHash = "h", DisplayName = "User 2", EloRating = 1500 },
            new User { Username = "user3", PasswordHash = "h", DisplayName = "User 3", EloRating = 1100 }
        );
        await db.SaveChangesAsync();

        var top = await db.Users
            .OrderByDescending(u => u.EloRating)
            .Take(20)
            .ToListAsync();

        Assert.Equal(3, top.Count);
        Assert.Equal("user2", top[0].Username);
        Assert.Equal(1500, top[0].EloRating);
        Assert.Equal("user1", top[1].Username);
        Assert.Equal(1200, top[1].EloRating);
        Assert.Equal("user3", top[2].Username);
        Assert.Equal(1100, top[2].EloRating);

        await db.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task ServerDbContext_User_CanLookupByEmailOrUsername()
    {
        string dbName = $"test_email_auth_{Guid.NewGuid():N}.db";
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite($"Data Source={dbName}")
            .Options;

        using var db = new ServerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new User
        {
            Username = "grandmaster_caro",
            Email = "master@chess-caro.com",
            DisplayName = "Grandmaster Caro",
            PasswordHash = PasswordHasher.HashPassword("SecretPass999!")
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Query by Username
        string searchUsername = "grandmaster_caro";
        var byUsername = await db.Users.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == searchUsername.ToLower() ||
            u.Email.ToLower() == searchUsername.ToLower());
        Assert.NotNull(byUsername);
        Assert.Equal(user.Id, byUsername.Id);
        Assert.Equal("master@chess-caro.com", byUsername.Email);

        // Query by Email
        string searchEmail = "MASTER@CHESS-CARO.COM";
        var byEmail = await db.Users.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == searchEmail.ToLower() ||
            u.Email.ToLower() == searchEmail.ToLower());
        Assert.NotNull(byEmail);
        Assert.Equal(user.Id, byEmail.Id);
        Assert.Equal("grandmaster_caro", byEmail.Username);

        await db.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task ServerDbContext_LegacyMigration_Succeeds()
    {
        string tempDb = $"legacy_test_{Guid.NewGuid():N}.db";
        try
        {
            var options = new DbContextOptionsBuilder<ServerDbContext>()
                .UseSqlite($"Data Source={tempDb}")
                .Options;

            // 1. Simulate legacy database with only old Users table
            using (var db = new ServerDbContext(options))
            {
                var conn = db.Database.GetDbConnection();
                await conn.OpenAsync();
                using var legacyCmd = conn.CreateCommand();
                legacyCmd.CommandText = @"
                    CREATE TABLE Users (
                        Id TEXT PRIMARY KEY,
                        Username TEXT NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        DisplayName TEXT NOT NULL,
                        EloRating INTEGER NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        LastLogin TEXT
                    );
                    INSERT INTO Users (Id, Username, PasswordHash, DisplayName, EloRating, CreatedAt)
                    VALUES ('a0000000-0000-0000-0000-000000000001', 'legacyuser', 'hash123', 'Old Player', 1200, '2025-01-01');
                ";
                await legacyCmd.ExecuteNonQueryAsync();

                // 2. Perform the exact migration logic from Program.cs
                using var migCmd = conn.CreateCommand();
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
                await migCmd.ExecuteNonQueryAsync();

                var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using var pragmaCmd = conn.CreateCommand();
                pragmaCmd.CommandText = "PRAGMA table_info(Users);";
                using var reader = await pragmaCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    existingCols.Add(reader.GetString(1));
                }

                void AddCol(string col, string typeDef)
                {
                    if (!existingCols.Contains(col))
                    {
                        using var c = conn.CreateCommand();
                        c.CommandText = $"ALTER TABLE Users ADD COLUMN {col} {typeDef};";
                        c.ExecuteNonQuery();
                    }
                }

                AddCol("Email", "TEXT DEFAULT ''");
                AddCol("Avatar", "TEXT DEFAULT 'king'");
                AddCol("Country", "TEXT DEFAULT 'VN'");
                AddCol("Bio", "TEXT DEFAULT 'Đam mê cờ Caro!'");
                AddCol("Wins", "INTEGER DEFAULT 0");
                AddCol("Losses", "INTEGER DEFAULT 0");
                AddCol("Draws", "INTEGER DEFAULT 0");
                AddCol("PeakElo", "INTEGER DEFAULT 1000");
                AddCol("WinStreak", "INTEGER DEFAULT 0");
                AddCol("BestWinStreak", "INTEGER DEFAULT 0");
                AddCol("LastLoginAt", "TEXT DEFAULT '2025-01-01T00:00:00'");

                using var updCmd = conn.CreateCommand();
                updCmd.CommandText = "UPDATE Users SET Email = LOWER(Username) || '@carogame.local' WHERE Email IS NULL OR Email = '';";
                await updCmd.ExecuteNonQueryAsync();
            }

            // 3. Verify ServerDbContext can query the migrated database without EF Core errors
            using (var db = new ServerDbContext(options))
            {
                var users = await db.Users.ToListAsync();
                Assert.Single(users);
                Assert.Equal("legacyuser", users[0].Username);
                Assert.Equal("legacyuser@carogame.local", users[0].Email);
                Assert.Equal("king", users[0].Avatar);
                Assert.Equal(0, users[0].Wins);

                // Ensure EmailVerifications and Friendships are also queryable
                var verifications = await db.EmailVerifications.ToListAsync();
                Assert.Empty(verifications);
                var friendships = await db.Friendships.ToListAsync();
                Assert.Empty(friendships);
            }
        }
        finally
        {
            if (System.IO.File.Exists(tempDb))
            {
                try { System.IO.File.Delete(tempDb); } catch { }
            }
        }
    }
}

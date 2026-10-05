using System.Net.Http.Json;
using CaroGame.Core.Enums;
using CaroGame.Core.Network;
using CaroGame.Server.Data;
using CaroGame.Server.Hubs;
using CaroGame.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaroGame.Core.Tests;

public class OnlineMultiplayerIntegrationTests
{
    [Fact]
    public async Task TwoClients_CanCreateRoom_Join_AndExchangeMovesAndChat()
    {
        int port = Random.Shared.Next(5100, 5900);
        string serverUrl = $"http://127.0.0.1:{port}/carohub";
        string dbName = $"test_{Guid.NewGuid():N}.db";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Services.AddDbContext<ServerDbContext>(options => options.UseSqlite($"Data Source={dbName}"));
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddSignalR();
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            db.Database.EnsureCreated();
        }

        app.UseCors();
        app.MapHub<CaroHub>("/carohub");

        await app.StartAsync();

        string roomCode = "TEST-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var client1 = new HubConnectionBuilder().WithUrl(serverUrl).Build();
        var client2 = new HubConnectionBuilder().WithUrl(serverUrl).Build();

        try
        {
            await client1.StartAsync();
            await client2.StartAsync();

            var client1GameStartTcs = new TaskCompletionSource<GameStartDto>();
            var client2GameStartTcs = new TaskCompletionSource<GameStartDto>();
            var client2MoveReceivedTcs = new TaskCompletionSource<NetworkMoveDto>();
            var client2ChatReceivedTcs = new TaskCompletionSource<ChatMessageDto>();

            client1.On<GameStartDto>("GameStarted", dto => client1GameStartTcs.TrySetResult(dto));
            client2.On<GameStartDto>("GameStarted", dto => client2GameStartTcs.TrySetResult(dto));
            client2.On<NetworkMoveDto>("MoveReceived", dto => client2MoveReceivedTcs.TrySetResult(dto));
            client2.On<ChatMessageDto>("ChatMessageReceived", dto => client2ChatReceivedTcs.TrySetResult(dto));

            // Client 1 creates room
            await client1.InvokeAsync("CreateRoom", roomCode, (int)RuleType.BlockedBothEnds, 30, true);

            // Client 2 joins room
            await client2.InvokeAsync("JoinRoom", roomCode);

            // Both should receive GameStarted within 4 seconds
            var timeout = Task.Delay(4000);
            var finishedStart1 = await Task.WhenAny(client1GameStartTcs.Task, timeout);
            var finishedStart2 = await Task.WhenAny(client2GameStartTcs.Task, timeout);

            Assert.Same(client1GameStartTcs.Task, finishedStart1);
            Assert.Same(client2GameStartTcs.Task, finishedStart2);

            var startDto1 = await client1GameStartTcs.Task;
            var startDto2 = await client2GameStartTcs.Task;

            Assert.Equal(CellState.X, startDto1.YourRole);
            Assert.Equal(CellState.O, startDto2.YourRole);
            Assert.Equal(roomCode, startDto1.RoomCode);

            // Client 1 makes move at (10, 10)
            await client1.InvokeAsync("SendMove", roomCode, 10, 10, (int)CellState.X, 1);

            var finishedMove = await Task.WhenAny(client2MoveReceivedTcs.Task, timeout);
            Assert.Same(client2MoveReceivedTcs.Task, finishedMove);

            var moveDto = await client2MoveReceivedTcs.Task;
            Assert.Equal(10, moveDto.Row);
            Assert.Equal(10, moveDto.Col);
            Assert.Equal(CellState.X, moveDto.Player);
            Assert.Equal(1, moveDto.TurnNumber);

            // Client 1 sends Chat message
            await client1.InvokeAsync("SendChatMessage", roomCode, "Xin chào đối thủ!");

            var finishedChat = await Task.WhenAny(client2ChatReceivedTcs.Task, timeout);
            Assert.Same(client2ChatReceivedTcs.Task, finishedChat);

            var chatDto = await client2ChatReceivedTcs.Task;
            Assert.Equal("Xin chào đối thủ!", chatDto.Message);
        }
        finally
        {
            await client1.DisposeAsync();
            await client2.DisposeAsync();
            await app.StopAsync();
            await app.DisposeAsync();

            try { File.Delete(dbName); } catch { }
        }
    }

    [Fact]
    public async Task LeaderboardEndpoint_ReturnsTopPlayersOrdered()
    {
        int port = Random.Shared.Next(5100, 5900);
        string serverUrl = $"http://127.0.0.1:{port}";
        string dbName = $"test_api_{Guid.NewGuid():N}.db";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(serverUrl);
        builder.Services.AddDbContext<ServerDbContext>(options => options.UseSqlite($"Data Source={dbName}"));
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            db.Database.EnsureCreated();
            db.Users.AddRange(
                new User { Username = "top1", DisplayName = "Cao Thủ 1", EloRating = 1600 },
                new User { Username = "top2", DisplayName = "Cao Thủ 2", EloRating = 1450 }
            );
            db.SaveChanges();
        }

        app.UseCors();
        app.MapGet("/api/leaderboard", async (ServerDbContext db) =>
        {
            var top = await db.Users
                .OrderByDescending(u => u.EloRating)
                .Take(20)
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    DisplayName = u.DisplayName,
                    EloRating = u.EloRating,
                    Wins = u.Wins,
                    Losses = u.Losses,
                    Draws = u.Draws
                })
                .ToListAsync();
            return top;
        });

        await app.StartAsync();

        try
        {
            using var http = new HttpClient();
            var list = await http.GetFromJsonAsync<List<UserProfileDto>>($"{serverUrl}/api/leaderboard");

            Assert.NotNull(list);
            Assert.Equal(2, list.Count);
            Assert.Equal("top1", list[0].Username);
            Assert.Equal(1600, list[0].EloRating);
            Assert.Equal("top2", list[1].Username);
            Assert.Equal(1450, list[1].EloRating);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
            try { File.Delete(dbName); } catch { }
        }
    }

    [Fact]
    public async Task FullMatchLifecycle_WithAuthentication_Chat_GameOver_EloUpdate_AndRematch()
    {
        int port = Random.Shared.Next(5100, 5900);
        string serverUrl = $"http://127.0.0.1:{port}";
        string hubUrl = $"{serverUrl}/carohub";
        string dbName = $"test_full_{Guid.NewGuid():N}.db";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(serverUrl);
        builder.Services.AddDbContext<ServerDbContext>(options => options.UseSqlite($"Data Source={dbName}"));
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddSignalR();
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            db.Database.EnsureCreated();

            var alice = new User
            {
                Username = "alice",
                PasswordHash = PasswordHasher.HashPassword("Pass123!"),
                DisplayName = "Alice Champion",
                EloRating = 1000
            };
            var bob = new User
            {
                Username = "bob",
                PasswordHash = PasswordHasher.HashPassword("Pass123!"),
                DisplayName = "Bob Challenger",
                EloRating = 1000
            };
            db.Users.AddRange(alice, bob);
            db.SaveChanges();
        }

        app.UseCors();
        app.MapGet("/api/leaderboard", async (ServerDbContext db) =>
        {
            var top = await db.Users
                .OrderByDescending(u => u.EloRating)
                .Take(20)
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    DisplayName = u.DisplayName,
                    EloRating = u.EloRating,
                    Wins = u.Wins,
                    Losses = u.Losses,
                    Draws = u.Draws
                })
                .ToListAsync();
            return top;
        });
        app.MapHub<CaroHub>("/carohub");

        await app.StartAsync();

        var tokenService = app.Services.GetRequiredService<TokenService>();
        Guid aliceId, bobId;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            aliceId = (await db.Users.FirstAsync(u => u.Username == "alice")).Id;
            bobId = (await db.Users.FirstAsync(u => u.Username == "bob")).Id;
        }

        string aliceToken = tokenService.CreateToken(aliceId);
        string bobToken = tokenService.CreateToken(bobId);

        var clientAlice = new HubConnectionBuilder().WithUrl(hubUrl).Build();
        var clientBob = new HubConnectionBuilder().WithUrl(hubUrl).Build();

        try
        {
            await clientAlice.StartAsync();
            await clientBob.StartAsync();

            var aliceAuthTcs = new TaskCompletionSource<UserProfileDto>();
            var bobAuthTcs = new TaskCompletionSource<UserProfileDto>();
            var aliceGameStartTcs = new TaskCompletionSource<GameStartDto>();
            var bobGameStartTcs = new TaskCompletionSource<GameStartDto>();
            var bobChatTcs = new TaskCompletionSource<ChatMessageDto>();
            var aliceMatchFinishTcs = new TaskCompletionSource<MatchFinishDto>();
            var bobMatchFinishTcs = new TaskCompletionSource<MatchFinishDto>();
            var aliceRematchReqTcs = new TaskCompletionSource<bool>();
            var aliceRematchStartTcs = new TaskCompletionSource<GameStartDto>();
            var bobRematchStartTcs = new TaskCompletionSource<GameStartDto>();
            var bobOpponentLeftTcs = new TaskCompletionSource<string>();

            clientAlice.On<UserProfileDto>("Authenticated", dto => aliceAuthTcs.TrySetResult(dto));
            clientBob.On<UserProfileDto>("Authenticated", dto => bobAuthTcs.TrySetResult(dto));
            clientAlice.On<GameStartDto>("GameStarted", dto => aliceGameStartTcs.TrySetResult(dto));
            clientBob.On<GameStartDto>("GameStarted", dto => bobGameStartTcs.TrySetResult(dto));
            clientBob.On<ChatMessageDto>("ChatMessageReceived", dto => bobChatTcs.TrySetResult(dto));
            clientAlice.On<MatchFinishDto>("MatchFinished", dto => aliceMatchFinishTcs.TrySetResult(dto));
            clientBob.On<MatchFinishDto>("MatchFinished", dto => bobMatchFinishTcs.TrySetResult(dto));
            clientAlice.On("RematchRequested", () => aliceRematchReqTcs.TrySetResult(true));
            clientAlice.On<GameStartDto>("RematchStarted", dto => aliceRematchStartTcs.TrySetResult(dto));
            clientBob.On<GameStartDto>("RematchStarted", dto => bobRematchStartTcs.TrySetResult(dto));
            clientBob.On<string>("OpponentLeft", reason => bobOpponentLeftTcs.TrySetResult(reason));

            // Authenticate both clients
            await clientAlice.InvokeAsync("Authenticate", aliceToken);
            await clientBob.InvokeAsync("Authenticate", bobToken);

            var aProfile = await aliceAuthTcs.Task;
            var bProfile = await bobAuthTcs.Task;
            Assert.Equal("Alice Champion", aProfile.DisplayName);
            Assert.Equal("Bob Challenger", bProfile.DisplayName);

            // Alice creates ranked room
            string roomCode = "MATCH-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
            await clientAlice.InvokeAsync("CreateRoom", roomCode, (int)RuleType.BlockedBothEnds, 30, true);

            // Bob joins room
            await clientBob.InvokeAsync("JoinRoom", roomCode);

            // Verify GameStarted
            var aStart = await aliceGameStartTcs.Task;
            var bStart = await bobGameStartTcs.Task;
            Assert.Equal(CellState.X, aStart.YourRole);
            Assert.Equal(CellState.O, bStart.YourRole);
            Assert.Equal("Alice Champion", aStart.HostName);
            Assert.Equal("Bob Challenger", aStart.GuestName);
            Assert.True(aStart.IsRanked);

            // Chat exchange
            await clientAlice.InvokeAsync("SendChatMessage", roomCode, "Good luck Bob!");
            var chatMsg = await bobChatTcs.Task;
            Assert.Equal("Good luck Bob!", chatMsg.Message);
            Assert.Equal("Alice Champion", chatMsg.SenderName);

            // Game over reported (Alice wins)
            await clientAlice.InvokeAsync("ReportGameOver", roomCode, (int)CellState.X, "Chiến thắng bằng 5 quân cờ");

            var aFinish = await aliceMatchFinishTcs.Task;
            var bFinish = await bobMatchFinishTcs.Task;
            Assert.Equal(CellState.X, aFinish.Winner);
            Assert.Equal("Alice Champion", aFinish.WinnerName);
            Assert.Equal(16, aFinish.EloChangeX);
            Assert.Equal(-16, aFinish.EloChangeO);
            Assert.Equal(1016, aFinish.NewEloX);
            Assert.Equal(984, aFinish.NewEloO);

            // Verify Leaderboard updated in database
            using (var http = new HttpClient())
            {
                var leaderboard = await http.GetFromJsonAsync<List<UserProfileDto>>($"{serverUrl}/api/leaderboard");
                Assert.NotNull(leaderboard);
                Assert.Equal("Alice Champion", leaderboard[0].DisplayName);
                Assert.Equal(1016, leaderboard[0].EloRating);
                Assert.Equal(1, leaderboard[0].Wins);
                Assert.Equal("Bob Challenger", leaderboard[1].DisplayName);
                Assert.Equal(984, leaderboard[1].EloRating);
                Assert.Equal(1, leaderboard[1].Losses);
            }

            // Rematch flow: Bob requests rematch
            await clientBob.InvokeAsync("RequestRematch", roomCode);
            await aliceRematchReqTcs.Task;

            // Alice accepts rematch
            await clientAlice.InvokeAsync("AcceptRematch", roomCode);
            var aRematch = await aliceRematchStartTcs.Task;
            var bRematch = await bobRematchStartTcs.Task;

            // Roles are inverted in rematch
            Assert.Equal(CellState.O, aRematch.YourRole);
            Assert.Equal(CellState.X, bRematch.YourRole);

            // Leave room / Disconnect
            await clientAlice.InvokeAsync("LeaveRoom", roomCode);
            var leftReason = await bobOpponentLeftTcs.Task;
            Assert.Contains("rời", leftReason);
        }
        finally
        {
            await clientAlice.DisposeAsync();
            await clientBob.DisposeAsync();
            await app.StopAsync();
            await app.DisposeAsync();
            try { File.Delete(dbName); } catch { }
        }
    }
}

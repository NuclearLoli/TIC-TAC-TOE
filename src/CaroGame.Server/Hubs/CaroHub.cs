using System.Collections.Concurrent;
using CaroGame.Core.Enums;
using CaroGame.Core.Network;
using CaroGame.Server.Data;
using CaroGame.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CaroGame.Server.Hubs;

public class PlayerSession
{
    public string ConnectionId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string DisplayName { get; set; } = "Người chơi";
    public int EloRating { get; set; } = 1000;
    public string Avatar { get; set; } = "king";
}

public class GameRoom
{
    public string RoomCode { get; set; } = string.Empty;
    public PlayerSession Host { get; set; } = new();
    public PlayerSession? Guest { get; set; }
    public RuleType Rule { get; set; } = RuleType.BlockedBothEnds;
    public int TurnTimeLimitSeconds { get; set; } = 30;
    public bool IsRanked { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime MatchStartedAt { get; set; } = DateTime.UtcNow;
    public int MoveCount { get; set; }
    public bool MatchFinished { get; set; }
    public bool IsActive => !string.IsNullOrEmpty(Host.ConnectionId);
}

public class CaroHub : Hub
{
    private static readonly ConcurrentDictionary<string, GameRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> _connectionToRoom = new();
    private static readonly ConcurrentDictionary<string, PlayerSession> _authenticatedUsers = new();
    public static readonly ConcurrentDictionary<Guid, string> UserToConnection = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly TokenService _tokenService;

    public CaroHub(IServiceProvider serviceProvider, TokenService tokenService)
    {
        _serviceProvider = serviceProvider;
        _tokenService = tokenService;
    }

    public async Task Authenticate(string token)
    {
        var userId = _tokenService.ValidateToken(token);
        if (userId.HasValue)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            var user = await db.Users.FindAsync(userId.Value);
            if (user != null)
            {
                var session = new PlayerSession
                {
                    ConnectionId = Context.ConnectionId,
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    EloRating = user.EloRating,
                    Avatar = user.Avatar
                };
                _authenticatedUsers[Context.ConnectionId] = session;
                UserToConnection[user.Id] = Context.ConnectionId;

                await Clients.Caller.SendAsync("Authenticated", new UserProfileDto
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
                });
            }
        }
    }

    public async Task CreateRoom(string roomCode, int ruleValue, int turnTime, bool isRanked)
    {
        roomCode = roomCode.Trim().ToUpperInvariant();

        if (_rooms.TryGetValue(roomCode, out var existing) && existing.IsActive)
        {
            await Clients.Caller.SendAsync("ErrorOccurred", "Mã phòng này đã tồn tại, vui lòng chọn mã khác.");
            return;
        }

        var hostSession = _authenticatedUsers.TryGetValue(Context.ConnectionId, out var auth)
            ? auth
            : new PlayerSession { ConnectionId = Context.ConnectionId, DisplayName = "Chủ phòng (X)", EloRating = 1000 };

        var room = new GameRoom
        {
            RoomCode = roomCode,
            Host = hostSession,
            Rule = (RuleType)ruleValue,
            TurnTimeLimitSeconds = turnTime,
            IsRanked = isRanked
        };

        _rooms[roomCode] = room;
        _connectionToRoom[Context.ConnectionId] = roomCode;

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        await Clients.Caller.SendAsync("RoomCreated", roomCode);
    }

    public async Task JoinRoom(string roomCode)
    {
        roomCode = roomCode.Trim().ToUpperInvariant();

        if (!_rooms.TryGetValue(roomCode, out var room) || !room.IsActive)
        {
            await Clients.Caller.SendAsync("ErrorOccurred", "Phòng không tồn tại hoặc đã bị hủy.");
            return;
        }

        if (room.Guest != null && !string.IsNullOrEmpty(room.Guest.ConnectionId))
        {
            await Clients.Caller.SendAsync("ErrorOccurred", "Phòng đã đủ 2 người chơi.");
            return;
        }

        var guestSession = _authenticatedUsers.TryGetValue(Context.ConnectionId, out var auth)
            ? auth
            : new PlayerSession { ConnectionId = Context.ConnectionId, DisplayName = "Khách (O)", EloRating = 1000 };

        room.Guest = guestSession;
        room.MatchStartedAt = DateTime.UtcNow;
        room.MoveCount = 0;
        room.MatchFinished = false;

        _connectionToRoom[Context.ConnectionId] = roomCode;
        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

        // Báo cho Host biết ván đấu bắt đầu (cầm X)
        await Clients.Client(room.Host.ConnectionId).SendAsync("GameStarted", new GameStartDto
        {
            RoomCode = roomCode,
            Rule = room.Rule,
            TurnTimeLimitSeconds = room.TurnTimeLimitSeconds,
            IsRanked = room.IsRanked,
            YourRole = CellState.X,
            FirstPlayer = CellState.X,
            HostName = room.Host.DisplayName,
            HostElo = room.Host.EloRating,
            HostAvatar = room.Host.Avatar,
            GuestName = room.Guest.DisplayName,
            GuestElo = room.Guest.EloRating,
            GuestAvatar = room.Guest.Avatar
        });

        // Báo cho Guest biết ván đấu bắt đầu (cầm O)
        await Clients.Client(room.Guest.ConnectionId).SendAsync("GameStarted", new GameStartDto
        {
            RoomCode = roomCode,
            Rule = room.Rule,
            TurnTimeLimitSeconds = room.TurnTimeLimitSeconds,
            IsRanked = room.IsRanked,
            YourRole = CellState.O,
            FirstPlayer = CellState.X,
            HostName = room.Host.DisplayName,
            HostElo = room.Host.EloRating,
            HostAvatar = room.Host.Avatar,
            GuestName = room.Guest.DisplayName,
            GuestElo = room.Guest.EloRating,
            GuestAvatar = room.Guest.Avatar
        });
    }

    public async Task SendMove(string roomCode, int row, int col, int playerVal, int turnNumber)
    {
        roomCode = roomCode.Trim().ToUpperInvariant();

        if (_rooms.TryGetValue(roomCode, out var room))
        {
            room.MoveCount = Math.Max(room.MoveCount, turnNumber);

            var moveDto = new NetworkMoveDto
            {
                RoomCode = roomCode,
                Row = row,
                Col = col,
                Player = (CellState)playerVal,
                TurnNumber = turnNumber
            };

            await Clients.OthersInGroup(roomCode).SendAsync("MoveReceived", moveDto);
        }
    }

    public async Task SendChatMessage(string roomCode, string message)
    {
        roomCode = roomCode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(message) || message.Length > 200) return;

        if (_rooms.TryGetValue(roomCode, out var room))
        {
            string senderName = Context.ConnectionId == room.Host.ConnectionId
                ? room.Host.DisplayName
                : (room.Guest?.DisplayName ?? "Khách");

            await Clients.Group(roomCode).SendAsync("ChatMessageReceived", new ChatMessageDto
            {
                SenderName = senderName,
                Message = message.Trim(),
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public async Task ReportGameOver(string roomCode, int winnerVal, string reason)
    {
        roomCode = roomCode.Trim().ToUpperInvariant();

        if (_rooms.TryGetValue(roomCode, out var room) && !room.MatchFinished && room.Guest != null)
        {
            room.MatchFinished = true;
            var winner = (CellState)winnerVal;

            int changeX = 0, changeO = 0;
            int newEloX = room.Host.EloRating, newEloO = room.Guest.EloRating;

            // Xử lý Elo nếu là trận Ranked
            if (room.IsRanked)
            {
                double scoreX = winner == CellState.X ? 1.0 : (winner == CellState.O ? 0.0 : 0.5);
                (changeX, changeO, newEloX, newEloO) = EloCalculator.Calculate(room.Host.EloRating, room.Guest.EloRating, scoreX);

                room.Host.EloRating = newEloX;
                room.Guest.EloRating = newEloO;

                // Lưu vào cơ sở dữ liệu
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                    if (room.Host.UserId.HasValue)
                    {
                        var uHost = await db.Users.FindAsync(room.Host.UserId.Value);
                        if (uHost != null)
                        {
                            uHost.EloRating = newEloX;
                            if (newEloX > uHost.PeakElo) uHost.PeakElo = newEloX;
                            if (winner == CellState.X)
                            {
                                uHost.Wins++;
                                uHost.WinStreak++;
                                if (uHost.WinStreak > uHost.BestWinStreak) uHost.BestWinStreak = uHost.WinStreak;
                            }
                            else if (winner == CellState.O)
                            {
                                uHost.Losses++;
                                uHost.WinStreak = 0;
                            }
                            else
                            {
                                uHost.Draws++;
                            }
                        }
                    }

                    if (room.Guest.UserId.HasValue)
                    {
                        var uGuest = await db.Users.FindAsync(room.Guest.UserId.Value);
                        if (uGuest != null)
                        {
                            uGuest.EloRating = newEloO;
                            if (newEloO > uGuest.PeakElo) uGuest.PeakElo = newEloO;
                            if (winner == CellState.O)
                            {
                                uGuest.Wins++;
                                uGuest.WinStreak++;
                                if (uGuest.WinStreak > uGuest.BestWinStreak) uGuest.BestWinStreak = uGuest.WinStreak;
                            }
                            else if (winner == CellState.X)
                            {
                                uGuest.Losses++;
                                uGuest.WinStreak = 0;
                            }
                            else
                            {
                                uGuest.Draws++;
                            }
                        }
                    }

                    db.Matches.Add(new OnlineMatch
                    {
                        PlayerXId = room.Host.UserId ?? Guid.Empty,
                        PlayerOId = room.Guest.UserId ?? Guid.Empty,
                        WinnerId = winner == CellState.X ? room.Host.UserId : (winner == CellState.O ? room.Guest.UserId : null),
                        EloChangeX = changeX,
                        EloChangeO = changeO,
                        Rule = room.Rule,
                        IsRanked = room.IsRanked,
                        TotalMoves = room.MoveCount,
                        DurationSeconds = (int)(DateTime.UtcNow - room.MatchStartedAt).TotalSeconds,
                        PlayedAt = DateTime.UtcNow
                    });

                    await db.SaveChangesAsync();
                }
                catch
                {
                    // Tránh crash nếu ghi log DB lỗi
                }
            }

            string winnerName = winner == CellState.X ? room.Host.DisplayName : (winner == CellState.O ? room.Guest.DisplayName : "Hòa");

            await Clients.Group(roomCode).SendAsync("MatchFinished", new MatchFinishDto
            {
                RoomCode = roomCode,
                Winner = winner,
                WinnerName = winnerName,
                EloChangeX = changeX,
                EloChangeO = changeO,
                NewEloX = newEloX,
                NewEloO = newEloO,
                Reason = reason
            });
        }
    }

    public async Task RequestRematch(string roomCode)
    {
        roomCode = roomCode.Trim().ToUpperInvariant();
        await Clients.OthersInGroup(roomCode).SendAsync("RematchRequested");
    }

    public async Task AcceptRematch(string roomCode)
    {
        roomCode = roomCode.Trim().ToUpperInvariant();

        if (_rooms.TryGetValue(roomCode, out var room) && room.Guest != null)
        {
            // Đổi vai cho ván mới
            var oldHost = room.Host;
            var oldGuest = room.Guest;
            room.Host = oldGuest;
            room.Guest = oldHost;
            room.MatchStartedAt = DateTime.UtcNow;
            room.MoveCount = 0;
            room.MatchFinished = false;

            await Clients.Client(room.Host.ConnectionId).SendAsync("RematchStarted", new GameStartDto
            {
                RoomCode = roomCode,
                Rule = room.Rule,
                TurnTimeLimitSeconds = room.TurnTimeLimitSeconds,
                IsRanked = room.IsRanked,
                YourRole = CellState.X,
                FirstPlayer = CellState.X,
                HostName = room.Host.DisplayName,
                HostElo = room.Host.EloRating,
                HostAvatar = room.Host.Avatar,
                GuestName = room.Guest.DisplayName,
                GuestElo = room.Guest.EloRating,
                GuestAvatar = room.Guest.Avatar
            });

            await Clients.Client(room.Guest.ConnectionId).SendAsync("RematchStarted", new GameStartDto
            {
                RoomCode = roomCode,
                Rule = room.Rule,
                TurnTimeLimitSeconds = room.TurnTimeLimitSeconds,
                IsRanked = room.IsRanked,
                YourRole = CellState.O,
                FirstPlayer = CellState.X,
                HostName = room.Host.DisplayName,
                HostElo = room.Host.EloRating,
                HostAvatar = room.Host.Avatar,
                GuestName = room.Guest.DisplayName,
                GuestElo = room.Guest.EloRating,
                GuestAvatar = room.Guest.Avatar
            });
        }
    }

    public async Task LeaveRoom(string roomCode)
    {
        roomCode = roomCode.Trim().ToUpperInvariant();

        if (_rooms.TryRemove(roomCode, out var room))
        {
            if (!room.MatchFinished && room.Guest != null)
            {
                // Xử lý người ở lại thắng
                var leaverConn = Context.ConnectionId;
                var winnerRole = leaverConn == room.Host.ConnectionId ? CellState.O : CellState.X;
                await ReportGameOver(roomCode, (int)winnerRole, "Đối thủ đã rời khỏi phòng đấu.");
            }
            await Clients.OthersInGroup(roomCode).SendAsync("OpponentLeft", "Đối thủ đã rời khỏi phòng đấu.");
        }

        _connectionToRoom.TryRemove(Context.ConnectionId, out _);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
    }

    public async Task ChallengeFriend(Guid friendId, string roomCode)
    {
        roomCode = roomCode.Trim().ToUpperInvariant();
        if (_authenticatedUsers.TryGetValue(Context.ConnectionId, out var caller) && caller.UserId.HasValue)
        {
            if (UserToConnection.TryGetValue(friendId, out var targetConnId))
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
                var callerUser = await db.Users.FindAsync(caller.UserId.Value);

                await Clients.Client(targetConnId).SendAsync("ReceiveFriendChallenge", new ChallengeFriendDto
                {
                    ChallengerId = caller.UserId.Value,
                    ChallengerName = callerUser?.DisplayName ?? caller.DisplayName,
                    ChallengerAvatar = callerUser?.Avatar ?? "king",
                    ChallengerElo = callerUser?.EloRating ?? caller.EloRating,
                    RoomCode = roomCode
                });
            }
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_authenticatedUsers.TryRemove(Context.ConnectionId, out var session) && session.UserId.HasValue)
        {
            UserToConnection.TryRemove(session.UserId.Value, out _);
        }

        if (_connectionToRoom.TryRemove(Context.ConnectionId, out var roomCode))
        {
            if (_rooms.TryRemove(roomCode, out var room))
            {
                if (!room.MatchFinished && room.Guest != null)
                {
                    var leaverConn = Context.ConnectionId;
                    var winnerRole = leaverConn == room.Host.ConnectionId ? CellState.O : CellState.X;
                    await ReportGameOver(roomCode, (int)winnerRole, "Đối thủ đã mất kết nối Internet.");
                }
                await Clients.OthersInGroup(roomCode).SendAsync("OpponentLeft", "Đối thủ đã mất kết nối Internet.");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}

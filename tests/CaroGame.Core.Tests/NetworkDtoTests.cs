using System.Text.Json;
using CaroGame.Core.Enums;
using CaroGame.Core.Network;
using Xunit;

namespace CaroGame.Core.Tests;

public class NetworkDtoTests
{
    [Fact]
    public void GameStartDto_SerializationAndDeserialization_ShouldMatch()
    {
        var dto = new GameStartDto
        {
            RoomCode = "CARO-9999",
            Rule = RuleType.BlockedBothEnds,
            TurnTimeLimitSeconds = 30,
            YourRole = CellState.X,
            FirstPlayer = CellState.X
        };

        string json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<GameStartDto>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("CARO-9999", deserialized.RoomCode);
        Assert.Equal(RuleType.BlockedBothEnds, deserialized.Rule);
        Assert.Equal(30, deserialized.TurnTimeLimitSeconds);
        Assert.Equal(CellState.X, deserialized.YourRole);
        Assert.Equal(CellState.X, deserialized.FirstPlayer);
    }

    [Fact]
    public void NetworkMoveDto_SerializationAndDeserialization_ShouldMatch()
    {
        var dto = new NetworkMoveDto
        {
            RoomCode = "CARO-1234",
            Row = 5,
            Col = -3,
            Player = CellState.O,
            TurnNumber = 7
        };

        string json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<NetworkMoveDto>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("CARO-1234", deserialized.RoomCode);
        Assert.Equal(5, deserialized.Row);
        Assert.Equal(-3, deserialized.Col);
        Assert.Equal(CellState.O, deserialized.Player);
        Assert.Equal(7, deserialized.TurnNumber);
    }
}

namespace CaroGame.Server.Services;

public static class EloCalculator
{
    private const int KFactor = 32;
    private const int MinElo = 100;

    public static (int changeX, int changeO, int newEloX, int newEloO) Calculate(int eloX, int eloO, double scoreX)
    {
        double expectedX = 1.0 / (1.0 + Math.Pow(10, (eloO - eloX) / 400.0));
        double expectedO = 1.0 - expectedX;

        double scoreO = 1.0 - scoreX;

        int changeX = (int)Math.Round(KFactor * (scoreX - expectedX));
        int changeO = (int)Math.Round(KFactor * (scoreO - expectedO));

        int newEloX = Math.Max(MinElo, eloX + changeX);
        int newEloO = Math.Max(MinElo, eloO + changeO);

        return (changeX, changeO, newEloX, newEloO);
    }
}

namespace CaroGame.Wpf.Services;

public interface INavigationService
{
    void NavigateToMenu();
    void NavigateToGamePlay();
    void NavigateToSettings();
    void NavigateToHistory();
    void NavigateToReplay(int gameId);
    void NavigateToOnlineLobby();
    void NavigateToAuth(bool returnToMenu = false, bool isRegister = false);
    void NavigateToLeaderboard();
    void NavigateToProfile(Guid? targetUserId = null);
    void NavigateToFriends();
}

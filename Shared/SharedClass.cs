
public class Player
{
    public Player(string name)
    {
        Name = name;
    }

    public event EventHandler<EContentChange> ContentUpdated;

    public string Name { get; set; }
    public string Message { get; set; }
    public int AvatarCode { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public EDirection Dir { get; set; }

    public void UpdateMessage(string message)
    {
        Message = message;
        ContentUpdated?.Invoke(this, EContentChange.Message);
    }

    public void UpdatePosition(EDirection dir, int x, int y)
    {
        Dir = dir;
        X = x;
        Y = y;
        ContentUpdated?.Invoke(this, EContentChange.Movement);
    }
}

public interface IHubClient
{
    Task OnPong();
    Task OnPlayerAction(PlayerActionEventArgs args);
    Task OnPlayersChanged(PlayersChangedEventArgs args);
}

public interface IHubHost
{
    string Ping();
    Task<Player> Join(string name);
    Task Move(EDirection direction);
    Task Speak(string message);
}

public enum EDirection
{
    Down = 0,
    Up = 3,
    Left = 1,
    Right = 2
}

public enum EContentChange
{
    Unknown = 0,
    Movement = 1,
    Message = 2,
}

public enum EConnectionChange
{
    Disconnected = 0,
    Connected = 1,
}

public record PlayersChangedEventArgs
{
    public Player Player { get; }
    public EConnectionChange ConnectionChange { get; }

    public PlayersChangedEventArgs(Player player, EConnectionChange connectionChange)
    {
        Player = player;
        ConnectionChange = connectionChange;
    }

}

public record PlayerActionEventArgs
{
    public Player Player { get; }
    public EContentChange ChangeType { get; }
    public PlayerActionEventArgs(Player player, EContentChange changeType)

    {
        Player = player;
        ChangeType = changeType;
    }
}
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace AccuChat.Shared;

public class GameServer
{
    public HubConnection _conn;
    private Player _me;
    private readonly ILogger<GameServer> _log;
    private Dictionary<string, Player> _players = new();
    public ICollection<Player> Players => _players.Values;
    public event EventHandler<PlayersChangedEventArgs> PlayersChanged;

    public GameServer(ILogger<GameServer> log)
    {
        _log = log;
        _log.LogDebug("HubClient constructed");
        _conn = new HubConnectionBuilder()
            .WithAutomaticReconnect()
            .WithUrl("https://accuchat.loca.lt/hub", o => o.Headers.Add("Bypass-Tunnel-Reminder", "anything"))
            //.WithUrl("https://qrpg2mb5-7243.uks1.devtunnels.ms/hub")
            .Build();
    }

    public async Task StartAsync()
    {
        _conn.Closed += async e => _log.LogWarning("Connection closed");
        _conn.Reconnecting += async e => _log.LogInformation("Connection closed");
        _conn.Reconnected += async e => _log.LogInformation("Reconnected");

        _conn.On(nameof(IHubClient.OnPong), OnPong);
        _conn.On<PlayerActionEventArgs>(nameof(IHubClient.OnPlayerAction), OnPlayerUpdated);
        _conn.On<PlayersChangedEventArgs>(nameof(IHubClient.OnPlayersChanged), OnPlayersChanged);
        //_conn.On<PlayerCard>(nameof(OnPlayerEnteredRoom), OnPlayerEnteredRoom);

        await _conn.StartAsync();

    }

    public Task<string> Ping() => _conn.InvokeAsync<string>(nameof(IHubHost.Ping));
    public async Task Join(string name)
    {
        _me = await _conn.InvokeAsync<Player>(nameof(IHubHost.Join), name);
    }

    public async Task Move(EDirection direction)
    {
        await _conn.SendAsync(nameof(IHubHost.Move), direction);
    }

    public async Task Speak(string message)
    {
        await _conn.SendAsync(nameof(IHubHost.Speak), message);
    }

    private void OnPong()
    {
        _log.LogInformation("PONG from server");
    }

    private void OnPlayersChanged(PlayersChangedEventArgs args)
    {
        _log.LogInformation("Received players change {args}", args);

        var p = _GetOrAddPlayer(args.Player);
        switch (args.ConnectionChange)
        {
            case EConnectionChange.Disconnected:
                _players.Remove(p.Name);
                PlayersChanged?.Invoke(this, new(p, EConnectionChange.Disconnected));
                break;
            case EConnectionChange.Connected:
                break;
            default:
                break;
        }

    }

    private void OnPlayerUpdated(PlayerActionEventArgs args)
    {
        _log.LogInformation("Received updated {args}", args);

        var p = _GetOrAddPlayer(args.Player);
        switch (args.ChangeType)
        {
            case EContentChange.Unknown:
                break;
            case EContentChange.Movement:
                p.UpdatePosition(args.Player.Dir, args.Player.X, args.Player.Y);
                break;
            case EContentChange.Message:
                p.UpdateMessage(args.Player.Message);
                break;
            default:
                break;
        }

        p.AvatarCode = args.Player.AvatarCode;
        p.Name = args.Player.Name;

    }
    private Player _GetOrAddPlayer(Player newPlayer)
    {
        if (_players.ContainsKey(newPlayer.Name) == false)
        {
            _players.Add(newPlayer.Name, newPlayer);
            PlayersChanged?.Invoke(this, new(newPlayer, EConnectionChange.Connected));
        }

        return _players[newPlayer.Name];
    }
}

using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AccuChat.Server;

public class GameStore
{
	public ConcurrentDictionary<string, PlayerCard> CardsByConnection = new();
	public ICollection<PlayerCard> Cards => CardsByConnection.Values;
	public List<(int x, int y)> Blockers;
	private ILogger<GameStore> _log;
	
	public GameStore(ILogger<GameStore> log, IWebHostEnvironment env)
	{
		_log = log;
		var file = @$"{env.ContentRootPath}\ldktmap\simplified\Level_0\data.json";
		_log.LogWarning(file);
        var str = File.ReadAllText(file);
		var json = JsonConvert.DeserializeObject<JObject>(str);
		Blockers = json.SelectToken("entities.Entity")
		.Select(e => (x: e["x"].Value<int>()/32, y: e["y"].Value<int>()/32))
		.ToList();
	}
}

public class ServerHub : Hub<IHubClient>, IHubHost
{
	private ILogger _log;
	private GameStore _store;
	
	PlayerCard Card => _store.CardsByConnection[Context.ConnectionId];
	Player Player => Card.Player;
	
	public ServerHub(ILogger<ServerHub> log, GameStore store)
	{
		_log = log;
		_store = store;
	}

	public override Task OnConnectedAsync()
	{
		return base.OnConnectedAsync();
	}

	public override Task OnDisconnectedAsync(Exception exception)
	{
		_store.CardsByConnection.Remove(Context.ConnectionId, out var card);
		Clients.All.OnPlayersChanged(new(card.Player, EConnectionChange.Disconnected));
		return base.OnDisconnectedAsync(exception);
	}

	public string Ping() => "Pong";

	public async Task<Player> Join(string name)
	{
		var count = 1;
		var finalName = name;
		while (_store.Cards.Any(x => x.Player.Name.Equals(finalName, StringComparison.InvariantCultureIgnoreCase)))
		{
			finalName = $"{name}_{count++}";
		}
		
		var player = new Player(finalName)
		{
			AvatarCode = Randomize(96),
		};
		player.UpdatePosition(EDirection.Down, 8, 6);

		if(_store.CardsByConnection.TryAdd(Context.ConnectionId,  new PlayerCard(player)) == false)
			return null;

		_log.LogInformation($"{finalName} joined {Context.ConnectionId}");
		
		await Clients.All.OnPlayersChanged(new (player, EConnectionChange.Connected));
		
		return player;
	}

	public async Task Move(EDirection direction)
	{
		var x = Player.X;
		var y = Player.Y;
		var step = 1;

		if (Card.FunState == EFunState.Drunk && TakeAChance(30))
			direction = (EDirection)Randomize(4);	
		if(Card.FunState == EFunState.Caffeine)
			step = 2;	
		
		switch (direction)
		{
			case EDirection.Down: 
				y += step;
				break;
			case EDirection.Up:
				y -= step;
				break;
			case EDirection.Left: 
				x -= step;
				break;
			case EDirection.Right:
				x += step;
				break;
		}

		//block map borders
		x = Math.Clamp(x, 0, 27);
		y = Math.Clamp(y, 0, 18);
		
		//block other players or objects
		if (_store.Cards.Any(c => c.Player.X == x && c.Player.Y == y)
			|| _store.Blockers.Any(b => b.x == x && b.y == y))
		{
			x = Player.X;
			y = Player.Y;
		}	
		
		Player.UpdatePosition(direction, x, y);
		await Clients.All.OnPlayerAction(new(Player, EContentChange.Movement));

		_ = HandleFunState(Card);
	}

	public async Task Speak(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return;
		}
		message = message.Substring(0, Math.Min(96, message.Length));
		
		if (Card.FunState == EFunState.Caffeine)
		{
			message = message.ToUpper();
		}
		
		await SendMessage(Clients.All, Card, message);
	}
	
	private static async Task SendMessage(IHubClient clients, PlayerCard card, string message, int delay = 0)
	{
		card.Player.Message = message;
		await clients.OnPlayerAction(new(card.Player, EContentChange.Message));
		await Task.Delay(delay);
	}

	private async Task HandleFunState(PlayerCard card)
	{
		if (card.FunState != EFunState.None)
		{
			return;
		}
		
		var clients = Clients.All;
		
		if (card.Player is { X: 24, Y: 5 })
		{
			card.FunState = EFunState.Drunk;
			
			await SendMessage(clients, card, "🍺 Found a Beer! ╰(*°▽°*)╯", 2500);
			await SendMessage(clients, card, "Takes a Sip ", 1500);
			await SendMessage(clients, card, card.Player.Name +  " is now Drunk! 🥴");

			var stop = Stopwatch.StartNew();
			while (stop.Elapsed.TotalSeconds < 60)
			{
				await Task.Delay(1000);
				if (TakeAChance(3))
				{
					var drunkMessages = new[] { "**.BLURP!.**", "🥴", "＼（〇_ｏ）／", "i'mn juszt Fihne! dun't bohter ne", "neeVer goign driiiink aghain!" };
					await SendMessage(clients, card, drunkMessages[Randomize(drunkMessages.Length)]);
				}
			}
		}
		else if (card.Player is {X: 10, Y: 1})
		{
			card.FunState = EFunState.Caffeine;

			await SendMessage(clients, card, "☕ enjoys some coffee.", 2500);
			await SendMessage(clients, card, "(⓿_⓿) is now in full power mode ⚡⚡⚡", 2500);
			
			await Task.Delay(60*1000);
		}
		else if (card.Player is { X: 27, Y: 15 })
		{
			await SendMessage(clients, card, "🎤 founds a mic. Karaoke time! 🎷", 2500);

			for (int i = 0; i < 3; i++)
			{
				var songs = new[] { "Is this the real life? Is this just fantasy?",
					"You may say I'm a dreamer, but I'm not the only one.",
					"Hey Jude, don't make it bad. Take a sad song and make it better.",
					"And I-eee-iii will always love you. I will always love you",
					"Welcome to the Hotel California, such a lovely place. Such a lovely face.",
					"Cause this is thriller, thriller night. And no one's gonna save you from the beast about to strike.",
					"With the lights out, it's less dangerous. Here we are now, entertain us."};

				var chorus = $"🎵{songs[Randomize(songs.Length)]}🎶";
				await SendMessage(clients, card, chorus, 8000);
			}
		}


			card.FunState = EFunState.None;
	}

	int Randomize(int max) => Randomize(0, max);
	int Randomize(int min, int max) => Random.Shared.Next(min, max);
	private bool TakeAChance(int value) => Randomize(100) <= value;
}

public class PlayerCard
{
	public PlayerCard(Player player)
	{
		Player = player;
	}
	public Player Player { get; }
	public DateTime LastAction { get; set; } = DateTime.Now;
	public EFunState FunState { get; set; } = EFunState.None;
	
}

public enum EFunState{
	None,
	Caffeine,
	Drunk
}
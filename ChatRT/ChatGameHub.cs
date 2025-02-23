using Microsoft.AspNetCore.SignalR;

// ============================================================================================
// Class: Player
// ============================================================================================
    
public class Player
{
    private const int STEP = 1;
    private const int FINISH = 100;
    public string Id { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
    public int Count { get; set; } = 0;
    public bool IsWin => Count >= FINISH;
    public Player(string id, string icon, string name) => (Id, Icon, Name) = (id, icon, name);
    public void Run() => Count += STEP;
}

// ============================================================================================
// Class: Game
// ============================================================================================

public class Game
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public Player? PlayerA { get; set; }
    public Player? PlayerB { get; set; }
    public bool IsWaiting { get; set; } = false;
    // public bool IsStarted { get; set; } = false; // new line
    public bool IsEmpty => PlayerA == null && PlayerB == null;
    public bool IsFull  => PlayerA != null && PlayerB != null;
    // public bool CanStart => !IsWaiting && IsFull; // new line - Game can only start if it's not waiting and is full

    public string? AddPlayer(Player player)
    {
        if (PlayerA == null)
        {
            PlayerA = player;
            IsWaiting = true;
            return "A";
        }
        else if (PlayerB == null)
        {
            PlayerB = player;
            IsWaiting = false;
            return "B";
        }

        return null;
    }
    // public bool StartGame(Player player)
    // {
    //     // Ensure that only PlayerA (the game creator) can start the game
    //     if (PlayerA == player && CanStart)
    //     {
    //         IsStarted = true;
    //         IsWaiting = false; // No longer waiting
    //         return true; // Game has started successfully
    //     }

    //     return false; // Either not PlayerA or game cannot be started
    // }
}

public class ChatGameHub : Hub
{
    // -------------------------------------------------------------
    // General fields fro Chat and game
    // -----------------------------------------------------------
    private static int userCount = 0;
    private static List<string> names = new List<string>();
    private static List<Game> games = new List<Game>();
    private static Dictionary<string, string> messages = new Dictionary<string, string>();

    //------------------------------------------------------------
    // Chat Functions
    //------------------------------------------------------------

    public async Task SendText(string name, string message)
    {
        var messageId = Guid.NewGuid().ToString();
        messages[messageId] = message;
        await Clients.Caller.SendAsync("ReceiveText", name, message, "caller", messageId);
        await Clients.Others.SendAsync("ReceiveText", name, message, "others", messageId);
    }

    public async Task SendImage(string name, string url)
    {
        await Clients.Caller.SendAsync("ReceiveImage", name, url, "caller");
        await Clients.Others.SendAsync("ReceiveImage", name, url, "others");
    }

    public async Task SendYouTube(string name, string id)
    {
        await Clients.Caller.SendAsync("ReceiveYouTube", name, id, "caller");
        await Clients.Others.SendAsync("ReceiveYouTube", name, id, "others");
    }

    public async Task SendFile(string name, string url, string filename)
    {
        await Clients.Caller.SendAsync("ReceiveFile", name, url, filename, "caller");
        await Clients.Others.SendAsync("ReceiveFile", name, url, filename, "others");
    }

    public async Task EditMessage(string messageId, string newMessage)
    {
        if (messages.ContainsKey(messageId))
        {
            messages[messageId] = newMessage;
            await Clients.All.SendAsync("MessageEdited", messageId, newMessage);
        }
    }

    public async Task DeleteMessage(string messageId, string name)
    {
        // if (messages.ContainsKey(messageId))
        // {
            messages.Remove(messageId); // Ensure the message is removed from the dictionary
            await Clients.All.SendAsync("MessageDeleted", messageId, name);
        // }
        //     else
        // {
        //     // Log or handle the case where the message ID is not found
        //     await Clients.Caller.SendAsync("Error", "Message ID not found.");
        // }
    }
    //------------------------------------------------------------
    // Game Functions
    //------------------------------------------------------------

    public string Create()
    {
        var game = new Game();
        games.Add(game);
        return game.Id;
    }

    public async Task Run(string letter)
    {
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();
        
        var game = games.Find(g => g.Id == gameId);
        if (game == null)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }

        var player = letter == "A" ? game.PlayerA : game.PlayerB;
        if (player == null) return;

        player.Run();
        await Clients.Group(gameId).SendAsync("Move", letter, player.Count);

        if (player.IsWin)
        {
            await Clients.Group(gameId).SendAsync("Win", letter);
        }
    }

    //------------------------------------------------------------
    // Connection Management
    //------------------------------------------------------------

    public override async Task OnConnectedAsync()
    {
        
        string page = Context.GetHttpContext()!.Request.Query["page"].ToString();
        string name = Context.GetHttpContext()!.Request.Query["name"].ToString();
        
        userCount++;
        names.Add(name);

        await Clients.All.SendAsync("UpdateStatus", userCount, $"<b>{name}</b> joined", names);

        switch (page)
        {
            case "list": await ListConnected(); break;
            case "game": await GameConnected(); break;
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string page = Context.GetHttpContext()!.Request.Query["page"].ToString();
        string name = Context.GetHttpContext()!.Request.Query["name"].ToString();

        userCount--;
        names.Remove(name);

        await Clients.All.SendAsync("UpdateStatus", userCount, $"<b>{name}</b> left", names);

        switch (page)
        {
            case "list": await ListDisconnected(); break;
            case "game": await GameDisconnected(); break;
        }

        await base.OnDisconnectedAsync(exception);
    }

    //------------------------------------------------------------
    // Game Connection Handling
    // -----------------------------------------------------------

    private async Task ListConnected()
    {
        await Clients.Caller.SendAsync("UpdateList", games.FindAll(g => g.IsWaiting));
    }

    private async Task GameConnected()
    {
        string id = Context.ConnectionId;
        string icon = Context.GetHttpContext()!.Request.Query["icon"].ToString();
        string name = Context.GetHttpContext()!.Request.Query["name"].ToString();
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();
        
        var game = games.Find(g => g.Id == gameId);
        if (game == null || game.IsFull)
        {
            await Clients.Caller.SendAsync("Reject");
            return;
        }

        var player = new Player(id, icon, name);
        var letter = game.AddPlayer(player);

        await Groups.AddToGroupAsync(id, gameId);
        await Clients.Group(gameId).SendAsync("Ready", letter, game);
        await Clients.All.SendAsync("UpdateList", games.FindAll(g => g.IsWaiting));

        if (game.IsFull)
        {
            await Clients.Group(gameId).SendAsync("Start");
        }
    }

    private async Task ListDisconnected()
    {
        await Task.CompletedTask;
    }

    private async Task GameDisconnected()
    {
        string id = Context.ConnectionId;
        string gameId = Context.GetHttpContext()!.Request.Query["gameId"].ToString();
        
        var game = games.Find(g => g.Id == gameId);
        if (game == null)
        {
            return;
        }

        if (game.PlayerA?.Id == id)
        {
            game.PlayerA = null;
            await Clients.Group(gameId).SendAsync("Left", "A");
        }
        else if (game.PlayerB?.Id == id)
        {
            game.PlayerB = null;
            await Clients.Group(gameId).SendAsync("Left", "B");
        }

        if (game.IsEmpty)
        {
            games.Remove(game);
            await Clients.All.SendAsync("UpdateList", games.FindAll(g => g.IsWaiting));
        }
    }

    // End of ChatGameHub

}
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class GameHub : Hub
{
    // Store all active game sessions
    private static readonly ConcurrentDictionary<string, GameSession> GameSessions = new();

    // Predefined win patterns for bitwise check
    private static readonly int[] WinPatterns = new int[]
    {
        0b000000111, // Bottom row
        0b000111000, // Middle row
        0b111000000, // Top row
        0b001001001, // Left column
        0b010010010, // Middle column
        0b100100100, // Right column
        0b100010001, // TL-BR diagonal
        0b001010100  // TR-BL diagonal
    };


    public async Task<string> CreateGame()
    {
        string connectionId = Context.ConnectionId;

        // Prevent creating a game if the player is already in one
        bool alreadyInGame = GameSessions.Values.Any(session =>
            session.PlayerX == connectionId || session.PlayerO == connectionId);

        if (alreadyInGame)
        {
            await Clients.Caller.SendAsync("Error", "You are already in a game.");
            return string.Empty;
        }

        // Generate a unique game ID
        string gameId = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

        var session = new GameSession
        {
            PlayerX = connectionId
        };

        GameSessions[gameId] = session;
        await Groups.AddToGroupAsync(connectionId, gameId);
        await Clients.Caller.SendAsync("GameCreated", gameId);

        return gameId;
    }


    public async Task JoinGame(string gameId)
    {
        string connectionId = Context.ConnectionId;

        // Prevent joining if the user is already in another game
        bool alreadyInGame = GameSessions.Values.Any(session =>
            session.PlayerX == connectionId || session.PlayerO == connectionId);

        if (alreadyInGame)
        {
            await Clients.Caller.SendAsync("Error", "You are already in a game.");
            return;
        }

        if (!GameSessions.TryGetValue(gameId, out var session))
        {
            await Clients.Caller.SendAsync("Error", "Game ID not found.");
            return;
        }

        if (session.PlayerO != null)
        {
            await Clients.Caller.SendAsync("Error", "Game is full.");
            return;
        }

        session.PlayerO = connectionId;
        await Groups.AddToGroupAsync(connectionId, gameId);

        await Clients.Caller.SendAsync("GameJoined", gameId);
        await Clients.Group(gameId).SendAsync("GameStarted", gameId, session.PlayerX, session.PlayerO);
    }

    public async Task LeaveGame(string gameId)
    {
        if (!GameSessions.TryGetValue(gameId, out var session)) return;

        bool playerWasInGame = false;

        if (session.PlayerX == Context.ConnectionId)
        {
            session.PlayerX = null;
            playerWasInGame = true;
        }
        else if (session.PlayerO == Context.ConnectionId)
        {
            session.PlayerO = null;
            playerWasInGame = true;
        }

        if (playerWasInGame)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerLeft", Context.ConnectionId);
        }

        // Remove game for both if either player leaves
        GameSessions.TryRemove(gameId, out _);
    }

    public Task<List<string>> GetOpenGames()
    {
        var openGames = GameSessions
            .Where(kvp => kvp.Value.PlayerX != null && kvp.Value.PlayerO == null && !kvp.Value.IsGameOver)
            .Select(kvp => kvp.Key)
            .ToList();

        return Task.FromResult(openGames);
    }

    public async Task MakeMove(string gameId, int index)
    {
        if (!GameSessions.TryGetValue(gameId, out var session))
            return;

        // 🔐 Prevent moves if both players are not present
        if (session.PlayerX == null || session.PlayerO == null)
        {
            await Clients.Caller.SendAsync("Error", "Wait for another player to join before making a move.");
            return;
        }

        if (session.IsGameOver || index < 0 || index > 8)
        {
            await Clients.Caller.SendAsync("Error", "Invalid move.");
            return;
        }

        var connectionId = Context.ConnectionId;

        bool isX = connectionId == session.PlayerX;
        bool isO = connectionId == session.PlayerO;

        if (!isX && !isO)
        {
            await Clients.Caller.SendAsync("Error", "You are not a player in this game.");
            return;
        }

        if ((session.IsXTurn && !isX) || (!session.IsXTurn && !isO))
        {
            await Clients.Caller.SendAsync("Error", "It's not your turn.");
            return;
        }

        int moveBit = 1 << index;

        if ((session.PlayerXState & moveBit) != 0 || (session.PlayerOState & moveBit) != 0)
        {
            await Clients.Caller.SendAsync("Error", "Cell already taken.");
            return;
        }

        // Apply the move
        if (isX)
            session.PlayerXState |= moveBit;
        else
            session.PlayerOState |= moveBit;

        // Check win
        int playerState = isX ? session.PlayerXState : session.PlayerOState;
        bool hasWon = WinPatterns.Any(pattern => (playerState & pattern) == pattern);

        if (hasWon)
        {
            session.IsGameOver = true;
            await Clients.Group(gameId).SendAsync("UpdateBoard", session.PlayerXState, session.PlayerOState);
            await Clients.Group(gameId).SendAsync("GameOver", isX ? "X" : "O");
            return;
        }

        // Check draw
        if ((session.PlayerXState | session.PlayerOState) == 0b111111111)
        {
            session.IsGameOver = true;
            await Clients.Group(gameId).SendAsync("UpdateBoard", session.PlayerXState, session.PlayerOState);
            await Clients.Group(gameId).SendAsync("GameOver", "Draw");
            return;
        }

        // Toggle turn and update board
        session.IsXTurn = !session.IsXTurn;
        await Clients.Group(gameId).SendAsync("UpdateBoard", session.PlayerXState, session.PlayerOState);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var game = GameSessions.FirstOrDefault(g =>
            g.Value.PlayerX == Context.ConnectionId || g.Value.PlayerO == Context.ConnectionId);

        if (!string.IsNullOrEmpty(game.Key))
        {
            GameSessions.TryRemove(game.Key, out _);
            await Clients.Group(game.Key).SendAsync("PlayerDisconnected", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task RequestRematch(string gameId)
    {
        if (!GameSessions.TryGetValue(gameId, out var session))
            return;

        string caller = Context.ConnectionId;
        bool isX = caller == session.PlayerX;
        bool isO = caller == session.PlayerO;

        if (!isX && !isO) return;

        if (isX) session.RematchRequestedByX = true;
        if (isO) session.RematchRequestedByO = true;

        int voteCount = (session.RematchRequestedByX ? 1 : 0) + (session.RematchRequestedByO ? 1 : 0);
        await Clients.Group(gameId).SendAsync("RematchVoteUpdate", voteCount);

        if (voteCount == 2)
        {
            session.ResetBoardAndSwapPlayers();
            await Clients.Group(gameId).SendAsync("StartRematch", session.PlayerX, session.PlayerO);
            await Clients.Group(gameId).SendAsync("UpdateBoard", session.PlayerXState, session.PlayerOState);
        }
    }

    private bool HasPlayerWon(int state)
    {
        return WinPatterns.Any(pattern => (state & pattern) == pattern);
    }
}

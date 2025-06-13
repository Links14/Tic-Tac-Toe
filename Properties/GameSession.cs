public class GameSession
{
    // Connection ID of Player X
    public string? PlayerX { get; set; }

    // Connection ID of Player O
    public string? PlayerO { get; set; }

    // Bitboard state for Player X (each bit represents a move on the 3x3 board)
    public int PlayerXState { get; set; } = 0;

    // Bitboard state for Player O
    public int PlayerOState { get; set; } = 0;

    // Indicates whether it is Player X's turn
    public bool IsXTurn { get; set; } = true;

    // Whether the game is over due to win or draw
    public bool IsGameOver { get; set; } = false;

    // Optional: stores the winner's connection ID, or null if no one yet
    public string? Winner { get; set; }

    public bool RematchRequestedByX { get; set; } = false;
    public bool RematchRequestedByO { get; set; } = false;

    public void ResetBoardAndSwapPlayers()
    {
        var oldX = PlayerX;
        PlayerX = PlayerO;
        PlayerO = oldX;

        PlayerXState = 0;
        PlayerOState = 0;
        IsXTurn = true;
        IsGameOver = false;
        RematchRequestedByX = false;
        RematchRequestedByO = false;
    }

    // Returns the player connection ID whose turn it is
    public string? GetCurrentPlayer() =>
        IsXTurn ? PlayerX : PlayerO;

    // Returns the bitboard for the given player
    public int GetPlayerState(string connectionId)
    {
        if (connectionId == PlayerX) return PlayerXState;
        if (connectionId == PlayerO) return PlayerOState;
        return 0;
    }

    // Updates the bitboard for a given player
    public void UpdatePlayerState(string connectionId, int newState)
    {
        if (connectionId == PlayerX)
            PlayerXState = newState;
        else if (connectionId == PlayerO)
            PlayerOState = newState;
    }
}

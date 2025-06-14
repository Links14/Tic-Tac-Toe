using Microsoft.AspNetCore.Mvc;
using TicTacToe.Services;

namespace TicTacToe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        [HttpGet("open")]
        public IActionResult GetOpenGames()
        {
            var openGames = GameSessionManager
                .GetAllSessions()
                .Where(kvp => kvp.Value.PlayerX != null && kvp.Value.PlayerO == null && !kvp.Value.IsGameOver)
                .Select(kvp => kvp.Key)
                .ToList();

            return Ok(openGames);
        }
    }
}

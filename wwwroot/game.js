let gameStarted = false;

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/GameHub")
    .build();

let currentPlayer = null;
let gameId = null;
let board = Array(9).fill(null);

// Update UI buttons
function setControlsEnabled(enabled) {
    document.getElementById("createGameButton").disabled = !enabled;
    document.getElementById("joinGameButton").disabled = !enabled;
    document.getElementById("leaveGameButton").disabled = enabled;
}

// Render X/O on each cell
function renderBoard() {
    const cells = document.querySelectorAll(".cell");
    cells.forEach((cell, index) => {
        cell.innerText = board[index] || "";
    });
}

// Event: Game Started
connection.on("GameStarted", (gameId, xPlayer, oPlayer) => {
    gameStarted = true;
    showToast("Both players connected. Game start!", "success");
});

// Event: Game successfully created (Player X)
connection.on("GameCreated", id => {
    gameId = id;
    currentPlayer = 'X';
    setControlsEnabled(false);
    document.getElementById("gameInfo").innerText = `Game created. You are Player X. Game ID: ${gameId}`;
    document.getElementById("gameBoard").classList.remove("hidden");
});

// Event: Successfully joined a game (Player O)
connection.on("GameJoined", id => {
    gameId = id;
    currentPlayer = 'O';
    setControlsEnabled(false);
    document.getElementById("gameInfo").innerText = `Game joined. You are Player O. Game ID: ${gameId}`;
    document.getElementById("gameBoard").classList.remove("hidden");
});

// Event: Game Finished
connection.on("GameOver", winner => {
    const status = winner === "Draw" ? "It's a draw!" : `${winner} wins!`;
    document.getElementById("status").innerText = status;
    document.getElementById("rematchButton").classList.remove("hidden");
});

// Event: Opponent left
connection.on("PlayerLeft", connectionId => {
    const status = document.getElementById("status");
    status.innerText = "Opponent has left the game.";

    // Hide and reset rematch
    document.getElementById("rematchButton").classList.add("hidden");
    const rematchStatus = document.getElementById("rematchStatus");
    rematchStatus.classList.add("hidden");
    rematchStatus.innerText = "Rematch Votes: 0/2";

    // Clear board after short delay (optional)
    setTimeout(() => {
        status.innerText = "";
        gameId = null;
        currentPlayer = null;
        board = Array(9).fill(null);
        renderBoard();
        document.getElementById("gameBoard").classList.add("hidden");
        document.getElementById("gameInfo").innerText = "";
        setControlsEnabled(true);
    }, 1000);
});

// Event: Display Existing Games
connection.start()
    .then(() => {
        console.log("Connected to SignalR Hub");
        refreshGames();
    })
    .catch(err => console.error("SignalR connection failed: ", err));

// Event: Update board (bitwise states)
connection.on("UpdateBoard", (xBoard, oBoard) => {
    board = Array(9).fill(null);
    for (let i = 0; i < 9; i++) {
        const mask = 1 << i;
        if ((xBoard & mask) !== 0) {
            board[i] = 'X';
        } else if ((oBoard & mask) !== 0) {
            board[i] = 'O';
        }
    }
    renderBoard();
});

// Event: Update Rematch Votes
connection.on("RematchVoteUpdate", voteCount => {
    const status = document.getElementById("rematchStatus");
    status.classList.remove("hidden");
    status.innerText = `Rematch Votes: ${voteCount}/2`;
});

// Event: Start Rematch
connection.on("StartRematch", (newX, newO) => {
    currentPlayer = connection.connectionId === newX ? 'X' : 'O';
    board = Array(9).fill(null);
    renderBoard();
    document.getElementById("status").innerText = "Rematch started!";
    document.getElementById("rematchButton").classList.add("hidden");
    document.getElementById("rematchStatus").classList.add("hidden");

    document.getElementById("gameInfo").innerText =
        `You are Player ${currentPlayer}. Game ID: ${gameId}`;
});

// Event: Error
connection.on("Error", message => {
    showToast(message, "danger");
});

connection.onclose(() => {
    showToast("You’ve been disconnected from the server.", "warning");
});

connection.onreconnected(() => {
    showToast("Reconnected to the server.", "success");
});


// Create a game
function createGame() {
    connection.invoke("CreateGame")
        .then(id => {
            if (!id) return; // creation rejected
            gameId = id;
            currentPlayer = 'X';
            setControlsEnabled(false);
            document.getElementById("gameInfo").innerText = `Game created. You are Player X. Game ID: ${gameId}`;
            document.getElementById("gameBoard").classList.remove("hidden");
        })
        .catch(err => console.error(err.toString()));
}

// Join a game
function joinGame() {
    const input = document.getElementById("gameIdInput").value.trim();
    if (!input) return;
    connection.invoke("JoinGame", input)
        .catch(err => console.error(err.toString()));
}

// Leave a game
function leaveGame() {
    if (!gameId) return;

    connection.invoke("LeaveGame", gameId)
        .then(() => {
            gameId = null;
            currentPlayer = null;
            board = Array(9).fill(null);
            renderBoard();

            // Reset UI
            document.getElementById("gameInfo").innerText = "You have left the game.";
            document.getElementById("status").innerText = "";
            document.getElementById("gameBoard").classList.add("hidden");

            // Hide and reset rematch button and status
            const rematchBtn = document.getElementById("rematchButton");
            const rematchStatus = document.getElementById("rematchStatus");

            rematchBtn.classList.add("hidden");
            rematchStatus.classList.add("hidden");
            rematchStatus.innerText = "Rematch Votes: 0/2";

            setControlsEnabled(true);
        })
        .catch(err => console.error(err.toString()));
}


// Make a move
function makeMove(index) {
    if (!currentPlayer || board[index] || !gameStarted) {
        if (!gameStarted) {
            showToast("Wait for both players before playing.", "info");
        }
        return;
    }

    connection.invoke("MakeMove", gameId, index)
        .catch(err => console.error(err.toString()));
}


// Request a rematch
function requestRematch() {
    connection.invoke("RequestRematch", gameId)
        .catch(err => console.error(err.toString()));
}

// Alerts handler
function showToast(message, type = "danger", duration = 2000) {
    const container = document.getElementById("toastContainer");

    const toast = document.createElement("div");
    toast.className = `alert alert-${type} alert-dismissible fade show`;
    toast.setAttribute("role", "alert");
    toast.innerText = message;

    // Add a close button (optional)
    const closeBtn = document.createElement("button");
    closeBtn.type = "button";
    closeBtn.className = "btn-close";
    closeBtn.setAttribute("data-bs-dismiss", "alert");
    toast.appendChild(closeBtn);

    container.appendChild(toast);

    // Auto-dismiss after `duration` ms
    setTimeout(() => {
        toast.classList.remove("show");
        toast.classList.add("hide");
        setTimeout(() => toast.remove(), 500);
    }, duration);
}

// Display existing GameIDs
function refreshGames() {
    connection.invoke("GetOpenGames")
        .then(gameIds => {
            const menu = document.getElementById("gameDropdownMenu");
            menu.innerHTML = "";

            if (!gameIds || gameIds.length === 0) {
                menu.innerHTML = `<li><span class="dropdown-item text-muted">(No open games)</span></li>`;
                return;
            }

            gameIds.forEach(id => {
                const item = document.createElement("li");
                const option = document.createElement("button");
                option.className = "dropdown-item";
                option.textContent = id;
                option.onclick = () => {
                    document.getElementById("gameIdInput").value = id;
                };
                item.appendChild(option);
                menu.appendChild(item);
            });
        })
        .catch(err => {
            console.error("Failed to load open games:", err);
            const menu = document.getElementById("gameDropdownMenu");
            menu.innerHTML = `<li><span class="dropdown-item text-danger">Error loading games</span></li>`;
        });
}

// Start connection
connection.start()
    .then(() => console.log("Connected to SignalR Hub"))
    .catch(err => console.error("SignalR connection failed: ", err));

document.addEventListener('DOMContentLoaded', () => {
    const dropdownToggle = document.querySelector('[data-bs-toggle="dropdown"]');
    if (dropdownToggle) {
        dropdownToggle.addEventListener('show.bs.dropdown', refreshGames);
    }
});

window.addEventListener("beforeunload", () => {
    if (gameId) {
        navigator.sendBeacon("/leave", JSON.stringify({ gameId })); // Optional for future API
        connection.invoke("LeaveGame", gameId); // fire-and-forget
    }
});


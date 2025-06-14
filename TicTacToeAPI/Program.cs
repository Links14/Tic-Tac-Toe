using TicTacToe.Hubs;

var builder = WebApplication.CreateBuilder(args);

// CORS Policy (Cross-Origin Resource Sharing Policy), allows frontend to access backend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins("https://localhost:25565")   // frontend port
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Needed for SignalR
    });
});

builder.Services.AddControllers();
builder.Services.AddSignalR();  // Add SignalR service
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors("AllowedOrigins");

app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/GameHub"); // Maps SignalR hub

app.Run();

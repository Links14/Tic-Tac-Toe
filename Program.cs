var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR(); // ✅ Add SignalR service

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseStaticFiles(); // ✅ Serve static frontend (like index.html)
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/GameHub"); // ✅ Map the SignalR hub


app.Run();

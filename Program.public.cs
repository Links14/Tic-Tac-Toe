var builder = WebApplication.CreateBuilder(args);

// CORS Policy (Cross-Origin Resource Sharing Policy)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:5555/")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Needed for SignalR
    });
});

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

// Program.cs
using CardGameServer.Game;
using CardGameServer.Network;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<GameEngine>();
builder.Services.AddSingleton<ConnectionManager>();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure SignalR
app.MapHub<GameHub>("/gameHub");

app.Run();
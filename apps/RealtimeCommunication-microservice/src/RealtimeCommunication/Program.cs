using RealtimeCommunication.Messaging;
using RealtimeCommunication.Messaging.Handlers;
using RealtimeCommunication.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Messaging
builder.Services.AddMessageClient(
    builder.Configuration);

builder.Services.AddMessageHandlers(
    typeof(MessagePostedHandler).Assembly);

builder.Services.AddScoped<IRealtimeSessionService, RealtimeSessionService>();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();
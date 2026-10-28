using RealtimeCommunication.Messaging;
using RealtimeCommunication.Messaging.Handlers;
using RealtimeCommunication.Services;
using EasyNetQ;
using EasyNetQ.Persistent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Messaging
builder.Services.AddMessageClient(
    builder.Configuration);

// Monitor RabbitMQ connectivity.
builder.Services.AddHostedService<RabbitMqConnectionMonitor>();

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

app.MapGet("/health/ready", (IBus bus) =>
{
    var status = bus.Advanced.GetConnectionStatus(
        PersistentConnectionType.Consumer);

    return status.State == PersistentConnectionState.Connected
        ? Results.Ok("RabbitMQ connected")
        : Results.StatusCode(503);
});

app.Run();
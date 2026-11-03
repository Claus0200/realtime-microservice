
using RealtimeCommunication.Messaging;
using RealtimeCommunication.Messaging.Handlers;
using RealtimeCommunication.Services;
using EasyNetQ;
using EasyNetQ.Persistent;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;

var builder = WebApplication.CreateBuilder(args);

// Load the RabbitMQ connection string from Vault.
var vaultAddress = Environment.GetEnvironmentVariable("VAULT_ADDR")
    ?? throw new InvalidOperationException("VAULT_ADDR is missing.");

var roleId = Environment.GetEnvironmentVariable("VAULT_ROLE_ID")
    ?? throw new InvalidOperationException("VAULT_ROLE_ID is missing.");

var secretId = Environment.GetEnvironmentVariable("VAULT_SECRET_ID")
    ?? throw new InvalidOperationException("VAULT_SECRET_ID is missing.");

var vault = new VaultClient(new VaultClientSettings(
    vaultAddress,
    new AppRoleAuthMethodInfo(roleId, secretId)));

var secret = await vault.V1.Secrets.KeyValue.V2.ReadSecretAsync(
    path: "bizcord",
    mountPoint: "secret");

var connectionString = secret.Data.Data
    .TryGetValue("RabbitMqConnectionString", out var value)
    ? value?.ToString()
    : null;

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("RabbitMQ secret is missing.");

builder.Configuration["ConnectionStrings:RabbitMq"] = connectionString;

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

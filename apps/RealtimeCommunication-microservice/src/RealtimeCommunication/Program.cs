
using EasyNetQ;
using EasyNetQ.Persistent;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RealtimeCommunication.Messaging;
using RealtimeCommunication.Messaging.Handlers;
using RealtimeCommunication.Services;
using StackExchange.Redis;
using System.Text;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using RealtimeCommunication.Repositories;

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
builder.Services.AddSingleton<IUserBanSagaRepository, InMemoryUserBanSagaRepository>();

builder.Services.AddSingleton<UserBanSagaLocks>();

builder.Services.AddSingleton<LiveKitTokenService>();

builder.Services.AddSingleton(new RoomServiceClient(
    builder.Configuration["LiveKit:InternalUrl"]
        ?? throw new InvalidOperationException("LiveKit URL missing"),

    builder.Configuration["LiveKit:ApiKey"]
        ?? throw new InvalidOperationException("LiveKit API key missing"),

    builder.Configuration["LiveKit:ApiSecret"]
        ?? throw new InvalidOperationException("LiveKit secret missing")
));

builder.Services.AddSingleton<IVoiceMediaAdmin, LiveKitVoiceMediaAdmin>();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration["Redis:Connection"]
            ?? throw new InvalidOperationException(
                "Redis connection missing")));

builder.Services.AddSingleton<IVoiceAccessStore, RedisVoiceAccessStore>();

// JWT authentication for voice service.
var jwtSigningKey = builder.Configuration["Voice:JwtSigningKey"]
    ?? throw new InvalidOperationException(
        "JWT signing key is missing");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

// Serve static files for the WebRTC client.
app.UseStaticFiles();

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

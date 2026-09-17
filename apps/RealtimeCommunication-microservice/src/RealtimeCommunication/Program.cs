using RealtimeCommunication;
using RealtimeCommunication.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Messaging
builder.Services.AddMessageClient(builder.Configuration);
builder.Services.AddHostedService<MessageSubscriber>();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

using System.Text.Json.Nodes;
using Bizcord.ApiGateway.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Ocelot merges these source files into a generated ocelot.json at startup.
builder.Configuration.AddOcelot("Configuration", builder.Environment);
builder.Services.AddJwtTokenService(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApiGatewayServices(builder.Configuration);

// Used to retrieve Swagger documents from downstream services.
builder.Services.AddHttpClient();

var app = builder.Build();

// Gateway Swagger document
app.UseSwagger();

// Swagger UI with multiple service definitions
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "API Gateway");

    options.SwaggerEndpoint(
        "/openapi/realtime.json",
        "Realtime Communication");

    options.RoutePrefix = "swagger";
});

// Proxy Realtime Communication's Swagger document through the gateway.
app.MapGet("/openapi/realtime.json",
    async (IHttpClientFactory httpClientFactory) =>
    {
        var client = httpClientFactory.CreateClient();

        HttpResponseMessage response;

        try
        {
            response = await client.GetAsync(
                "http://realtime-communication:8080/swagger/v1/swagger.json");
        }
        catch (HttpRequestException)
        {
            return Results.Problem(
                "Unable to reach Realtime Communication.",
                statusCode: 502);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return Results.Problem(
                    "Unable to retrieve Realtime Communication's Swagger document.",
                    statusCode: 502);
            }

            var json = await response.Content.ReadAsStringAsync();
            var document = JsonNode.Parse(json);

            if (document is null)
            {
                return Results.Problem(
                    "Invalid Swagger document.",
                    statusCode: 502);
            }

            // Force Swagger "Try it out" to use the gateway.
            // For example: http://localhost:9081/api/realtime-sessions
            document["servers"] = new JsonArray(
                new JsonObject
                {
                    ["url"] = "/"
                });

            // Ensure OpenAPI components exist.
            if (document["components"] is not JsonObject)
            {
                document["components"] = new JsonObject();
            }

            var components = (JsonObject)document["components"]!;

            // Ensure security schemes exist.
            if (components["securitySchemes"] is not JsonObject)
            {
                components["securitySchemes"] = new JsonObject();
            }

            var securitySchemes =
                (JsonObject)components["securitySchemes"]!;

            // Add JWT Bearer authentication to Swagger.
            securitySchemes["Bearer"] = new JsonObject
            {
                ["type"] = "http",
                ["scheme"] = "bearer",
                ["bearerFormat"] = "JWT"
            };

            // Display JWT authorization for downstream endpoints.
            document["security"] = new JsonArray(
                new JsonObject
                {
                    ["Bearer"] = new JsonArray()
                });

            return Results.Json(document);
        }
    });

app.MapGet("/health", () => Results.Ok("Healthy"));

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Execute the faucet controller before Ocelot's terminal proxy pipeline.
#pragma warning disable ASP0014
app.UseEndpoints(endpoints => endpoints.MapControllers());
#pragma warning restore ASP0014

// These exercise headers must come from validated token claims,
// not directly from the client.
app.Use(async (context, next) =>
{
    context.Request.Headers.Remove("X-UserId");
    context.Request.Headers.Remove("X-Role");

    await next(context);
});

// Ocelot API Gateway
await app.UseOcelot();

await app.RunAsync();

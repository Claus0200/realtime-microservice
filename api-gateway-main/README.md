# API Gateway

A standalone Ocelot gateway for the System Integration API gateway exercises. Clone it next to your team's existing microservice repository and run both locally. Each team works in its own clone.

Based on [`apps/api-gateway` from Bizcord-F2026/bizcord-microservices](https://github.com/Bizcord-F2026/bizcord-microservices/tree/54c6ac46d3292ec6215675660a47ba78ef4225c0/apps/api-gateway), originally written by Patrick Stolc. This extraction has fresh history and targets .NET 10, Ocelot 25.0.2, JwtBearer 10.0.13 and Swashbuckle 10.3.0.

## Start with Docker Compose

Install Docker with Compose (Docker Desktop on macOS/Windows), then:

```sh
git clone https://github.com/patrickstolc/api-gateway.git
cd api-gateway
docker compose up --build
```

Open [Swagger](http://localhost:8080/swagger). The gateway is available at `http://localhost:8080`; Swagger describes the gateway's own token faucet, not every downstream microservice endpoint.

Start your team's microservice separately using its existing setup. The gateway does not start your microservice, database or message broker.

To stop, press Ctrl+C and run `docker compose down`. After editing route configuration, run `docker compose up --build` again (stop the previous foreground run first). Configuration files are copied into the image and merged when the gateway starts.

## Configure your own routes — Task 01

Edit or replace `src/Configuration/ocelot.example.json`, or add `src/Configuration/ocelot.<your-service>.json`. Keep `ocelot.global.json` for the gateway's global configuration. Do not edit the generated `src/Configuration/ocelot.json`.

The starter contains one **unprotected example**, not a bundled microservice:

```json
{
  "Routes": [
    {
      "Key": "local-example-replace-me",
      "UpstreamPathTemplate": "/example/{id}",
      "UpstreamHttpMethod": ["GET"],
      "DownstreamPathTemplate": "/api/example/{id}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "host.docker.internal", "Port": 5001 }
      ]
    }
  ]
}
```

Replace the methods, paths and port with a real endpoint from your microservice. For example, `GET http://localhost:8080/example/42` would reach `http://host.docker.internal:5001/api/example/42` from inside the gateway container. Until your service exposes that endpoint, the example will not return a successful downstream response.

| Where the gateway runs | Downstream host | Downstream port |
| --- | --- | --- |
| Docker, with your service running on/published to your computer | `host.docker.internal` | Your service's **host/published HTTP port** |
| Directly with the .NET CLI | `localhost` | Your service's local HTTP port |

Inside the gateway container, `localhost` means the gateway container itself. The Compose file includes the `host-gateway` mapping needed for `host.docker.internal` on Linux Docker Engine. Ensure your microservice's HTTP listener/published port is reachable from Docker; on Linux a host-only loopback listener may need to listen on a reachable host interface.

Use your service's HTTP endpoint for this local exercise. Its HTTPS development certificate may not be trusted inside the gateway container. No certificate-validation bypass is configured.

Test an endpoint directly and through the gateway, including its method, path and response. Commit your work locally; you do not need to push changes back to this teaching repository.

## Authentication and authorization — Task 02

Continue in the same clone. Configure authentication, role/scope requirements and claim transformations on your own routes using the [Ocelot authentication documentation](https://ocelot.readthedocs.io/en/latest/features/authentication.html), [authorization documentation](https://ocelot.readthedocs.io/en/latest/features/authorization.html) and [claims transformation documentation](https://ocelot.readthedocs.io/en/latest/features/claimstransformation.html).

The registered authentication scheme is `Bearer`. Ocelot 25 route configuration uses the plural `AuthenticationProviderKeys` array. Incoming claim names are preserved, so `sub`, `role` and `scope` can be used in your route configuration.

The starter removes client-supplied `X-UserId` and `X-Role` headers before Ocelot applies claim transformations. If you choose different trusted identity-header names, remove those incoming headers too before populating them from validated claims.

### Get a local test token

The gateway's existing `POST /faucet` endpoint accepts a dictionary of claims and returns a signed JWT in the response's `value` field:

```sh
curl -sS -X POST http://localhost:8080/faucet \
  -H 'Content-Type: application/json' \
  -d '{"claims":{"sub":"student-123","role":"member","scope":"channel.read"}}'
```

Copy `value` into the `Authorization` header when calling a route you have protected:

```sh
curl -i http://localhost:8080/your-protected-route \
  -H 'Authorization: Bearer PASTE_VALUE_HERE'
```

You can also call the faucet in Swagger. Tokens expire after one hour; request a new token when needed. Test requests without a token, with an invalid token, and with claims that both satisfy and fail your authorization requirement. Verify the user identity received by your microservice comes from the token rather than client-supplied identity data.

The faucet accepts whatever claims you request; it is a **local teaching tool, not a login or identity provider**. `appsettings.Development.json` contains a clearly named demo signing key shared by local clones. The Compose port is bound to your computer's loopback interface. Do not deploy this faucet or use the demo key for real authentication.

Authentication only protects requests that pass through protected gateway routes. Direct calls to your microservice bypass the gateway; a real deployment needs to prevent such bypasses before trusting forwarded identity headers.

## Optional: run with the .NET CLI

Install the .NET 10 SDK, change your downstream host to `localhost`, then run from the repository root:

```sh
dotnet run --project src/api-gateway.csproj --launch-profile http
```

The included profile selects Development, supplies the local demo key, and listens at `http://localhost:8080`. Stop and rerun after changing route files. Avoid running this and Docker Compose at the same time on port 8080.

## Validation

With Python 3 and the .NET 10 SDK installed:

```sh
python3 tests/smoke.py
```

The smoke test publishes a clean copy, starts a temporary downstream HTTP service and gateway, then checks routing, Swagger, the faucet, authentication, scope/role authorization, identity-header replacement, path transformation, token expiry and configuration changes after restart. It changes only temporary files and stops its own servers. The student starter routes remain unprotected.

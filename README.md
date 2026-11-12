# Bizcord — Realtime Communication

.NET 10 microservice with an Ocelot API gateway, RabbitMQ messaging, LiveKit voice chat, Redis voice-access blocking, and Vault-managed configuration.

## Requirements

- Docker Desktop with Docker Compose v2
- Git
- .NET SDK 10 (for local builds and tests)
- PowerShell (commands below use PowerShell syntax)
- A browser with microphone access
- Vault AppRole credentials for the project

## Setup

### 1. Clone the repository

```powershell
git clone https://github.com/Claus0200/realtime-microservice.git
cd realtime-microservice
```

Run all commands from the repository root, where `compose.yaml` is located.

### 2. Configure `.env`

Create a `.env` file in the repository root with the following variables. Obtain the AppRole IDs and secrets from the project administrator.

```dotenv
VAULT_ROLE_ID=<realtime-role-id>
VAULT_SECRET_ID=<realtime-secret-id>
GATEWAY_VAULT_ROLE_ID=<gateway-role-id>
GATEWAY_VAULT_SECRET_ID=<gateway-secret-id>
RABBITMQ_VAULT_ROLE_ID=<rabbitmq-role-id>
RABBITMQ_VAULT_SECRET_ID=<rabbitmq-secret-id>
KEYCLOAK_VAULT_ROLE_ID=<keycloak-role-id>
KEYCLOAK_VAULT_SECRET_ID=<keycloak-secret-id>
VOICE_JWT_SIGNING_KEY=<gateway-jwt-signing-key>
```

Vault is initialized from `vault/seed.tar.gz` when its Docker volume is empty. The API Gateway reads its JWT signing key from the `AuthenticationProviderKey` field at `secret/gateway` in Vault. `VOICE_JWT_SIGNING_KEY` **must match this value**, or voice requests return HTTP 401.

### 3. Start the services

```powershell
docker compose config --quiet
docker compose up -d --build
docker compose ps
```

`vault-seed` and `vault-config-init` are one-time initialization containers; exiting successfully is normal.

Check application and infrastructure health:

```powershell
curl.exe -i http://localhost:9080/health
curl.exe -i http://localhost:9080/health/ready
curl.exe -i http://localhost:9081/health
docker compose exec voice-redis redis-cli PING
docker compose exec rabbitmq rabbitmq-diagnostics -q ping
```

Expected: HTTP 200 for the health endpoints, `PONG` from Redis, and `Ping succeeded` from RabbitMQ.

## Local interfaces

| URL | Service |
| --- | --- |
| http://localhost:9081/swagger | API Gateway Swagger; JWT generation and authenticated routes |
| http://localhost:9080/swagger | RealtimeCommunication Swagger; direct development Saga endpoints |
| http://localhost:9080/voice-test/index.html | Browser voice client |
| http://localhost:15672 | RabbitMQ Management |
| http://localhost:8200 | Vault UI |
| http://localhost:9082 | Keycloak |
| ws://localhost:7880 | LiveKit WebSocket endpoint |

RabbitMQ Management uses username `bizcord` and the `RabbitMqPassword` value in Vault at `secret/bizcord`.

## Manual test data

| Identity | GUID |
| --- | --- |
| Voice channel | `11111111-1111-4111-8111-111111111111` |
| Alice (user to terminate) | `22222222-2222-4222-8222-222222222222` |
| Bob (unaffected user) | `33333333-3333-4333-8333-333333333333` |

These fixed IDs can be used directly in the Development voice API.

## Testing with Swagger and LiveKit

### 1. Generate JWTs

Open **http://localhost:9081/swagger** and select **API Gateway** in the Swagger definition dropdown.

Expand **`POST /Faucet`** → **Try it out**, enter the following body, and click **Execute**.

**Alice:**

```json
{
  "claims": {
    "sub": "22222222-2222-4222-8222-222222222222",
    "role": "member",
    "scope": "channel.read"
  }
}
```

**Bob:** Repeat the request with:

```json
{
  "claims": {
    "sub": "33333333-3333-4333-8333-333333333333",
    "role": "member",
    "scope": "channel.read"
  }
}
```

Each request returns HTTP 200 and a JWT in the `value` field. Copy the complete `value` separately for Alice and Bob. Tokens expire after one hour.

### 2. Test authenticated voice joining

In **http://localhost:9081/swagger**:

1. Change the definition dropdown to **Realtime Communication**.
2. Click **Authorize**, paste Alice's Faucet JWT, and confirm.
3. Expand **`POST /api/voice/{channelId}/join`** → **Try it out**.
4. Enter `11111111-1111-4111-8111-111111111111` as `channelId`.
5. Click **Execute**.

Expected: **HTTP 200** with `roomName`, `serverUrl`, and `token`. The room name is `voice-11111111111141118111111111111111`, and the server URL is `ws://localhost:7880`.

Repeat using Bob's JWT (switch tokens using **Authorize**). Both users should receive a LiveKit token for the same room. The `token` from `/join` is for LiveKit; the browser test page requires the **Faucet JWT** instead.

### 3. Connect two users

1. Open **http://localhost:9080/voice-test/index.html** in two browser windows.
2. In both windows, use channel ID `11111111-1111-4111-8111-111111111111`.
3. In the first window, click **Join voice** and paste **Alice's Faucet JWT**.
4. In the second window, click **Join voice** and paste **Bob's Faucet JWT**.
5. Allow microphone access and test speaking, mute/unmute, and leaving the room.

Expected: Both windows display **Connected** and **Participants: 2**. The page's **User ID** field is not used for authentication; identity comes from the JWT `sub` claim.

### 4. Terminate Alice's voice session

Keep both users connected. Open **http://localhost:9080/swagger** (the direct backend, **not** port 9081).

1. Expand **`POST /internal/voice-saga-test/terminate/{userId}`** → **Try it out**.
2. Enter `22222222-2222-4222-8222-222222222222`.
3. Click **Execute**.

Expected: **HTTP 202 Accepted** with generated `sagaId` and `userId`. The command is processed asynchronously through RabbitMQ. Alice should be disconnected; Bob should stay connected.

Check Redis:

```powershell
docker compose exec voice-redis redis-cli EXISTS voice:blocked:22222222222242228222222222222222
```

Expected: `1` (blocked). Check handler logs:

```powershell
docker compose logs --tail=100 realtime-communication
```

Look for `Disconnected user ... from 1 rooms`. If it reports `0 rooms`, the command was processed but no active participant was disconnected.

### 5. Verify that Alice cannot rejoin

In **http://localhost:9081/swagger**, select **Realtime Communication**, authorize with **Alice's Faucet JWT**, and execute `POST /api/voice/{channelId}/join` with the same channel GUID.

Expected: **HTTP 403 Forbidden**. Repeat with Bob's JWT; expected: **HTTP 200**.

### 6. Restore Alice's voice access

In **http://localhost:9080/swagger**:

1. Expand **`POST /internal/voice-saga-test/restore/{userId}`** → **Try it out**.
2. Enter `22222222-2222-4222-8222-222222222222`.
3. Click **Execute**.

Expected: **HTTP 202 Accepted**. Check Redis:

```powershell
docker compose exec voice-redis redis-cli EXISTS voice:blocked:22222222222242228222222222222222
```

Expected: `0` (unblocked). In Gateway Swagger, Alice's authenticated `/join` request should return **HTTP 200** again. Alice can then reconnect through the browser client.

### 7. Test LiveKit failure handling

1. Stop LiveKit: `docker compose stop livekit`.
2. In **http://localhost:9080/swagger**, execute **`POST /internal/voice-saga-test/terminate/{userId}`** with Bob's GUID: `33333333-3333-4333-8333-333333333333`.
3. Expected HTTP response: **202 Accepted** (command queued).
4. Check `docker compose logs --since=2m realtime-communication` for `Failed to terminate voice sessions`. The handler publishes `VoiceSessionTerminationFailed`.
5. Check `docker compose exec voice-redis redis-cli EXISTS voice:blocked:33333333333343338333333333333333`; expected: `1`.
6. Restart LiveKit: `docker compose start livekit`.
7. In direct Swagger, execute **`POST /internal/voice-saga-test/restore/{userId}`** with Bob's GUID.
8. Check the same Redis key; expected: `0`.

### 8. Other endpoint checks

| Test | Endpoint | Expected |
| --- | --- | --- |
| No JWT | Gateway `POST /api/voice/{channelId}/join` | 401 |
| Invalid JWT | Gateway `POST /api/voice/{channelId}/join` | 401 |
| Empty channel GUID (all zeroes) | Gateway `POST /api/voice/{channelId}/join` with valid JWT | 400 |
| Blocked user | Gateway `POST /api/voice/{channelId}/join` | 403 |
| RabbitMQ publishing | Gateway `POST /api/messages/ping` with `channel.read` scope | 202 |

The `/api/realtime-sessions` CRUD routes are defined but currently throw `NotImplementedException` and cannot be used as successful functional tests.

## RabbitMQ and automated tests

Open **http://localhost:15672** → **Queues and Streams** to inspect subscriptions and consumers for `TerminateUserVoiceSessions`, `RestoreUserVoiceAccess`, and their result events.

PowerShell diagnostics:

```powershell
docker compose exec rabbitmq rabbitmqctl list_queues name consumers messages
docker compose logs realtime-communication | Select-String 'Registering message subscription'
```

Build the solution and run the existing MessagePostedHandler unit tests:

```powershell
dotnet restore .\Bizcord.slnx
dotnet build .\Bizcord.slnx
dotnet test .\apps\RealtimeCommunication-microservice\tests\RealtimeCommunication.Tests\RealtimeCommunication.Tests.csproj --filter 'FullyQualifiedName~RealtimeCommunication.Tests.MessagePostedHandlerTests'
```

The separate RabbitMQ integration test uses a hard-coded `bizcord/bizcord` connection string; it may fail with the Vault-managed RabbitMQ password. VoIP Saga handlers can be tested manually with the endpoints above.

## Troubleshooting

| Problem | Check |
| --- | --- |
| JWT request returns **401** | Gateway's `AuthenticationProviderKey` in Vault must equal `VOICE_JWT_SIGNING_KEY` in `.env`; regenerate expired tokens. |
| Saga test returns **404** | Use `http://localhost:9080/swagger`, not port 9081. |
| Voice join returns **403** | User is blocked in Redis; run the restore test endpoint. |
| Voice browser displays **`Error:`** with no detail | Open browser DevTools → Network and check the HTTP status; a 403 may have an empty response body. |
| Backend is unhealthy | Run `docker compose logs --tail=150 realtime-communication`. |
| RabbitMQ not ready | Check `docker compose logs --tail=100 rabbitmq` and `/health/ready`. |
| Browser cannot connect to voice | Check `docker compose logs --tail=100 livekit`, browser microphone permissions, and DevTools Console. |

## Stop and restart

```powershell
# Stop containers, keeping data volumes
docker compose down

# Start again
docker compose up -d --build

# Rebuild after source changes
docker compose up -d --build realtime-communication

# Recreate after environment changes
docker compose up -d --force-recreate realtime-communication
```

For local testing, `Voice__AllowSagaTestCommands` is set to `"true"` in `compose.yaml`. Set it to `"false"` and recreate `realtime-communication` when development tests are finished. The test endpoints are unauthenticated and intended only for local development.

The VoIP termination and restoration handlers can be tested independently; the complete automatic cross-service user-ban Saga is not yet implemented. The current Saga repository also stores state in memory.

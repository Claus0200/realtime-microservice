# Task 1 – API Gateway

The implementation covers routing and authorization using Ocelot and JWT authentication.

## 1. Overview

The Bizcord project uses an API Gateway as the central entry point for incoming API requests.

The gateway is responsible for:

- Routing incoming requests to the appropriate microservice.
- Validating JWT access tokens.
- Enforcing authorization rules based on roles and scopes.
- Keeping internal microservice addresses hidden from external clients.

Ocelot is used as the API Gateway framework.

## 2. Implementation location

The API Gateway is located in the following project:

```text
api-gateway-main/
```

Relevant components:

| Component | Location |
|---|---|
| API Gateway project | `api-gateway-main/` |
| Ocelot routing configuration | `api-gateway-main/src/Configuration/ocelot.realtimecommunication.json` |
| Authentication configuration | `api-gateway-main/src/Program.cs` |
| RealtimeCommunication API | `realtime_microservice/` |
| JWT Token authentication | `api-gateway-main/src/Application/Services/JwtTokenService.cs` |

The routing configuration defines how incoming requests are forwarded to RealtimeCommunication.

### Routing

The downstream service address is:

```text
http://realtime-communication:8080
```

Docker Compose service discovery allows Ocelot to communicate with the microservice using its service name rather than a hardcoded container IP address.

The RealtimeCommunication API includes the following route:

```http
/api/realtime-sessions
```

The API supports the following operations:

| HTTP method | Operation |
|---|---|
| GET | Retrieve realtime sessions |
| GET | Retrieve a specific session |
| POST | Create a realtime session |
| PUT | Update a realtime session |
| DELETE | Delete a realtime session |

The corresponding Ocelot configuration maps incoming gateway requests to the downstream RealtimeCommunication API.

## 3. Authorization

The API Gateway uses JWT Bearer authentication to validate incoming access tokens.

Keycloak is used as the identity provider.

### Authentication flow

1. A client authenticates with Keycloak.
2. Keycloak issues a JWT access token.
3. The client sends the token in the HTTP Authorization header.
4. Ocelot validates the token.
5. The gateway checks the required authorization policy.
6. If authorization succeeds, the request is forwarded to RealtimeCommunication.
7. If authentication or authorization fails, the request is rejected.

The expected HTTP header is:

```http
Authorization: Bearer <access_token>
```

### JWT configuration

The authentication configuration uses the following settings:

| Setting | Value |
|---|---|
| Authentication scheme | Bearer |
| Authority | Keycloak realm URL |
| Audience | `realtime-communication` |
| Require HTTPS metadata | `false` for local development |
| Inbound claim mapping | Disabled |

The JWT contains claims used to identify the user and determine their permissions.

The relevant claims include:

- `sub`: Identifies the authenticated user.
- `role`: Identifies the user's role.
- `scope`: Contains the permissions granted to the access token.

### Authorization rules

The API Gateway enforces authorization using scopes and roles.

The following rules are configured:

| HTTP method | Required permission |
|---|---|
| GET | `channel.read` scope |
| POST | `member` role |
| PUT | `member` role |
| DELETE | `admin` role |

This ensures that users cannot perform operations beyond their assigned permissions.

## 4. Running the implementation

### Prerequisites

- Docker Desktop
- Docker Compose
- Access to the Bizcord repository
- A configured Keycloak realm and client
- A valid JWT access token for testing

### Step 1 – Start the Docker environment

Navigate to the repository root and run:

```bash
docker compose up --build -d
```

This starts the API Gateway, RealtimeCommunication and the required infrastructure.

### Step 2 – Verify running containers

Run:

```bash
docker compose ps
```

Verify that the API Gateway and RealtimeCommunication containers are running.

## 5. Testing routing and authorization

The API Gateway is accessible through:

```text
http://localhost:9081
```

The following examples demonstrate how to verify routing and authorization.

### Test 1 – Request without an access token

```bash
curl -i http://localhost:9081/api/realtime-sessions
```

**Expected result:** `401 Unauthorized`, because no valid access token was supplied.

### Test 2 – Request an access token

The project provides a token faucet endpoint for generating test JWTs.

```bash
curl -X POST \
  http://localhost:9081/faucet \
  -H "Content-Type: application/json" \
  -d '{
    "claims": {
      "sub": "test-user-123",
      "scope": "channel.read",
      "role": "member"
    }
  '
```

**Expected result:** The faucet returns a signed JWT access token.

Copy the returned token. It will be used in the following authorization tests.

For convenience, store the token in an environment variable:

```bash
TOKEN="<access_token>"
```

### Test 3 – Request with an authorized access token

```bash
curl -i \
  -H "Authorization: Bearer <access_token>" \
  http://localhost:9081/api/realtime-sessions
```

**Expected result:** The request passes gateway authentication and authorization and is forwarded to RealtimeCommunication.

The final response depends on the microservice implementation and its current data.

### Test 4 – Request with insufficient permissions

Use a valid token that does not contain the required scope or role.

For example, a user without the `admin` role attempting to delete a session:

```bash
curl -i -X DELETE \
  -H "Authorization: Bearer <access_token>" \
  http://localhost:9081/api/realtime-sessions/<session-id>
```

**Expected result:** `403 Forbidden` when the gateway authorization policy rejects the request.

### Test 5 – Verify successful authorization

Repeat the DELETE request using a valid token with the required `admin` role.

**Expected result:** The request passes gateway authorization and reaches the downstream service. The final HTTP response depends on whether the requested operation succeeds.

## 6. Stopping the environment

To stop the Docker environment:

```bash
docker compose down
```

To also remove persistent volumes:

```bash
docker compose down -v
```

Be aware that removing volumes can delete locally stored development data.
# Bizcord project setup

Use PowerShell on Windows with Git, Docker Desktop running Linux containers, and Docker Compose v2. Install the .NET 10 SDK for the Saga tests. Run commands from the repository root after cloning. Port 9081 is the gateway. Port 9080 is the backend used for health and local development checks.

## Initialize the project

```powershell
git clone https://github.com/Claus0200/realtime-microservice.git
cd realtime-microservice
notepad .env
```

Save the following entries as `.env` beside `compose.yaml`. Replace each placeholder with the value supplied by the project maintainer. The four AppRoles must belong to the seeded Vault. The voice signing key must match `AuthenticationProviderKey` at `secret/gateway` because both the gateway and backend validate the user token.

```dotenv
VAULT_ROLE_ID=<realtime-role-id>
VAULT_SECRET_ID=<realtime-secret-id>
GATEWAY_VAULT_ROLE_ID=<gateway-role-id>
GATEWAY_VAULT_SECRET_ID=<gateway-secret-id>
RABBITMQ_VAULT_ROLE_ID=<rabbitmq-role-id>
RABBITMQ_VAULT_SECRET_ID=<rabbitmq-secret-id>
KEYCLOAK_VAULT_ROLE_ID=<keycloak-role-id>
KEYCLOAK_VAULT_SECRET_ID=<keycloak-secret-id>
VOICE_JWT_SIGNING_KEY=<gateway-signing-key>
```

Start Vault first. On a fresh volume, `vault-seed` restores the repository's Vault snapshot. Wait for the Vault API to respond and check its seal status.

```powershell
docker compose config --quiet
docker compose up -d vault
docker compose exec vault vault status
```

If the status shows `Sealed true`, run `docker compose exec vault vault operator unseal` and enter a maintainer-provided unseal key at the hidden prompt. Repeat with distinct shares until the threshold is reached and `Sealed` is `false`. AppRole credentials are used after unsealing. The same check applies after restarting Vault.

```powershell
docker compose up -d --build
docker compose ps -a
$gateway = 'http://localhost:9081'
$backend = 'http://localhost:9080'
curl.exe -i "$gateway/health"
curl.exe -i "$backend/health/ready"
docker compose exec voice-redis redis-cli PING
```

Expect both `vault-seed` and `vault-config-init` to show `Exited (0)`, both HTTP checks to return **200**, and Redis to return **PONG**. Readiness specifically checks the RabbitMQ consumer connection. If startup stops at an init container, inspect `docker compose logs vault-seed vault-config-init` before testing routes.

## Verify the assignment tasks

1. [API Gateway](01-API-gateway.md)
2. [Reliability](02-Reliability.md)
3. [Security](03-Security.md)
4. [Data Management](04-Data-management.md)
5. [Maintainability and Decoupling](05-Maintainability-and-decoupling.md)

## Shut down

Run `docker compose down` to stop the stack and keep its named volumes. To start it again, follow the Vault status and startup steps above.

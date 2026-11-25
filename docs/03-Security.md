# Task 3 · Security (week 45)

Complete [project setup](00-Project-setup.md), then open PowerShell in the repository root. Run the blocks on this page in the same window.

## Service-to-service authorization

The gateway's internal ping route authenticates service callers with Keycloak. Obtain the client ID and secret for a service-account-enabled confidential client in the `bizcord` realm from the maintainer. Its access token needs audience `realtime-communication` and a top-level `role` claim of `internal-service`. A role present only under `realm_access.roles` does not satisfy this route.

```powershell
$gateway = 'http://localhost:9081'
$userId = [guid]::NewGuid().ToString()
function Get-TestJwt([string]$scope) {
    $body = @{ claims = @{ sub = $userId; role = 'member'; scope = $scope } } | ConvertTo-Json
    (Invoke-RestMethod -Method Post -Uri "$gateway/Faucet" -ContentType 'application/json' -Body $body).value
}
$jwt = Get-TestJwt 'channel.read'

$clientId = Read-Host 'Keycloak service client ID'
$client = Get-Credential -UserName $clientId -Message 'Enter the Keycloak client secret as the password'
$form = @{ grant_type = 'client_credentials'; client_id = $clientId; client_secret = $client.GetNetworkCredential().Password }
$serviceJwt = (Invoke-RestMethod -Method Post -Uri 'http://localhost:9082/realms/bizcord/protocol/openid-connect/token' -ContentType 'application/x-www-form-urlencoded' -Body $form).access_token
Remove-Variable form,client

curl.exe -i -X POST "$gateway/internal/messages/ping" -H "Authorization: Bearer $jwt"
curl.exe -i -X POST "$gateway/internal/messages/ping" -H "Authorization: Bearer $serviceJwt"
```

Expect **401** for the Faucet token and **202** for the authorized service token. A valid service token with the correct audience but without the required role should receive **403**. This policy is enforced at the gateway. The same downstream ping action is accessible directly on the local development backend.

## Verify Vault integration

Check the seal status, init job, and broker without printing credentials.

```powershell
docker compose exec vault vault status
docker compose logs vault-config-init
docker compose exec rabbitmq rabbitmq-diagnostics -q ping
```

Look for `Sealed false`, `RabbitMQ and Keycloak configuration generated successfully.`, and `Ping succeeded`. The init job logs that message after both AppRole logins, secret reads, and config writes succeed. The successful gateway ping also exercises the gateway signing key and the backend broker connection string loaded from Vault at startup.

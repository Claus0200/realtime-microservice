# Task 5 · Maintainability and Decoupling (week 47)

Complete [project setup](00-Project-setup.md), then open PowerShell in the repository root. Run the blocks on this page in the same window.

## GraphQL

Query the current user's voice access through the gateway. Hot Chocolate takes the user ID from the JWT subject and reads the block flag from Redis.

```powershell
$gateway = 'http://localhost:9081'
$backend = 'http://localhost:9080'
$userId = [guid]::NewGuid().ToString()
function Get-TestJwt([string]$scope) {
    $body = @{ claims = @{ sub = $userId; role = 'member'; scope = $scope } } | ConvertTo-Json
    (Invoke-RestMethod -Method Post -Uri "$gateway/Faucet" -ContentType 'application/json' -Body $body).value
}
$jwt = Get-TestJwt 'channel.read'
$auth = @{ Authorization = "Bearer $jwt" }
$query = @{ query = '{ myVoiceAccess { userId isBlocked } }' } | ConvertTo-Json
function Get-VoiceAccess {
    $reply = Invoke-RestMethod -Method Post -Uri "$gateway/graphql" -Headers $auth -ContentType 'application/json' -Body $query
    if ($reply.errors) { throw ($reply.errors | ConvertTo-Json -Depth 5) }
    $reply.data.myVoiceAccess
}
Get-VoiceAccess
Invoke-RestMethod -Method Post -Uri "$backend/internal/voice-saga-test/terminate/$userId"
Get-VoiceAccess
Invoke-RestMethod -Method Post -Uri "$backend/internal/voice-saga-test/restore/$userId"
Get-VoiceAccess
```

Run each line individually. Expect the same user ID and `isBlocked` to move from **false → true → false**. Each command endpoint returns **202** before its handler finishes. Repeat the query until the expected value appears before sending the next command. The two internal endpoints require the local Development profile and `Voice__AllowSagaTestCommands=true`, both set by Compose. The session-list and session-by-ID resolvers currently call unimplemented service methods.

## Fluentd logging sidecar

Generate a fresh log with a known Saga ID, then find that exact ID in the collector's output:

```powershell
$backend = 'http://localhost:9080'
$testUser = [guid]::NewGuid().ToString()
$run = Invoke-RestMethod -Method Post -Uri "$backend/internal/user-ban-saga-test/start/$testUser"
docker compose logs --since=2m fluentd | Select-String -SimpleMatch $run.sagaId
```

Expect a record tagged `bizcord.realtime` containing `Starting user ban saga` and the ID. Repeat the log command if forwarding is still in progress. This ties an actual backend request to the sidecar output. Docker forwards the realtime container's console logs to Fluentd on port 24224, and Fluentd writes matching records to its own console.

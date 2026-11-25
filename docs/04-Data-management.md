# Task 4 · Data Management (week 46)

Complete [project setup](00-Project-setup.md), then open PowerShell in the repository root. Run the blocks on this page in the same window.

The [workflow](SagaWorkflow.md) coordinates the user ban, channel removal, and voice termination. This repository implements orchestration with its own message client over EasyNetQ/RabbitMQ. Saga state is in memory and is lost on backend restart.

Start a workflow and inspect its state:

```powershell
$backend = 'http://localhost:9080'
$userId = [guid]::NewGuid().ToString()
$run = Invoke-RestMethod -Method Post -Uri "$backend/internal/user-ban-saga-test/start/$userId"
$run
Invoke-RestMethod "$backend/internal/user-ban-saga-test/status/$($run.sagaId)"
```

Start returns **202** with a new `sagaId`. If status initially returns **404**, repeat the GET after the message is consumed. With only this Compose stack, expect `phase = BanRequested` and `isCompleted = false`. The orchestrator is waiting for the user-management participant. User management and channel management run outside this repository.

Run the orchestrator tests to verify the later steps without those services:

```powershell
dotnet test ./apps/RealtimeCommunication-microservice/tests/RealtimeCommunication.Tests/RealtimeCommunication.Tests.csproj --filter 'FullyQualifiedName~UserBanSagaOrchestratorTests'
```

Expect six passing tests. They cover completion, ban failure, channel failure, voice failure, duplicate delivery, and failed compensation. The voice-failure test checks that the orchestrator restores channels, unbans the user, and restores voice access. For a live check of the voice participant, follow the [voice-access query](05-Maintainability-and-decoupling.md#graphql).

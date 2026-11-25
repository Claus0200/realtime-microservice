# Task 2 – Reliability

This section describes how to test the reliability measures implemented

## Test setup procedure

1. Start the Docker environment:

   ```bash
   docker compose up -d
   ```

2. Verify that the containers are running:

   ```bash
   docker compose ps
   ```

## Timeout

To test this, it requires an jwt token for authentication

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

Verify that the RealtimeCommunication service and RabbitMQ are running

1. Send a ping request while RabbitMQ is available. Use the API's exposed base URL; for example, if the request is routed through the API Gateway:

   ```bash
   curl.exe -i -X POST http://localhost:9081/api/messages/ping
   ```

2. Verify that the request succeeds with `202 Accepted`. This confirms that the message was published successfully.

3. Stop RabbitMQ while leaving the RealtimeCommunication service running:

   ```bash
   docker compose stop rabbitmq
   ```

4. Send the ping request again:

   ```bash
   curl.exe -i -X POST http://localhost:9081/api/messages/ping
    -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
   ```

5. Observe the HTTP response and how long the request takes.

## Circuit breaker

The API Gateway uses the Circuit Breaker pattern through Ocelot's Quality of Service (QoS) functionality.

The purpose of this pattern is to prevent the API Gateway from repeatedly sending requests to a downstream service that is unavailable or responding too slowly.

The circuit breaker monitors requests to downstream services and can temporarily stop forwarding requests when too many failures occur.

### Configuration

The circuit breaker is configured in `api-gateway-main/src/Configuration/ocelot.global.json`:

```json
"QoSOptions": {
  "MinimumThroughput": 3,
  "BreakDuration": 10000,
  "Timeout": 3000
}
```

### Test

1. Generate a valid Bearer token using the Faucet endpoint.

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

2. Send a request through the API Gateway to an endpoint handled by the RealtimeCommunication service:

   ```bash
   curl.exe -i -X POST http://localhost:9081/api/messages/ping -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
   ```

3. Verify that the request works while the downstream service is running. The endpoint should return `202 Accepted` if the message is published successfully.

4. Stop the RealtimeCommunication service to simulate a service failure:

   ```bash
   docker compose stop realtime-communication
   ```

5. Send multiple requests to the same endpoint using the API Gateway. Make sure to send at least three requests, as the configured `MinimumThroughput` is 3.

6. Observe the responses and API Gateway logs. The circuit breaker should detect the downstream failures and temporarily stop forwarding requests.

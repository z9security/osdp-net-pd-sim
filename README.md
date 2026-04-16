# OSDP PD Simulator

HTTP-controllable OSDP Peripheral Device (PD) simulator built on [OSDP.Net](https://github.com/bytedreamer/OSDP.Net). Designed for E2E integration testing of access control systems.

## Architecture

This is one component in a three-part E2E test stack:

1. **Host server** (e.g., real-bs Rails app) -- speaks Z9 Open Community Protocol over TCP, sends config and receives events
2. **Aporta** (.NET controller) -- connects to host via protobuf TCP, connects to this PD sim via OSDP over TCP
3. **This project** (OSDP PD Sim) -- simulates a card reader/door hardware, controllable via HTTP REST API

```
 Host (real-bs)           Aporta              OSDP PD Sim
 +-----------+     protobuf/TCP      +----------+    OSDP/TCP    +----------+
 | Rails app | <------ 9723 -------> | .NET     | <--- 9843 ---> | This     |
 | port 3000 |                       | controller|               | project  |
 +-----------+                       +----------+                | port 5230|
                                                                 +----------+
                                                          HTTP control: 5230
```

## What It Does

- Listens for OSDP connections on a configurable TCP port (default 9843)
- Responds to OSDP polls, ID reports, and capability queries
- Exposes an HTTP API for tests to inject card reads, keypad entries, and input status changes
- Tracks received OSDP commands (output control, LED, buzzer) for test assertions

## HTTP API

### Control Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/card-read` | Simulate a card read |
| `POST` | `/keypad` | Simulate a keypad PIN entry |
| `POST` | `/input-status` | Set input status (door contact, REX) |
| `GET`  | `/status` | Get device connection status |
| `GET`  | `/commands` | Get list of received OSDP commands |
| `POST` | `/reset` | Clear received commands log |

### Examples

```bash
# Simulate a 26-bit Wiegand card read
curl -X POST http://localhost:5230/card-read \
  -H "Content-Type: application/json" \
  -d '{"cardNumber": 12345, "bitCount": 26, "readerNumber": 0}'

# Simulate keypad PIN entry
curl -X POST http://localhost:5230/keypad \
  -H "Content-Type: application/json" \
  -d '{"digits": "1234", "readerNumber": 0}'

# Set door contact to active (closed) and REX inactive
curl -X POST http://localhost:5230/input-status \
  -H "Content-Type: application/json" \
  -d '{"inputs": [true, false]}'

# Check connection status
curl http://localhost:5230/status

# Get received commands (output control, LED, buzzer)
curl http://localhost:5230/commands
```

## Prerequisites

- .NET 9.0 SDK
- OSDP.Net 5.0.44 (restored automatically)

## Build & Run

```bash
cd OsdpPdSim
dotnet restore
dotnet build
dotnet run
```

The simulator starts with:
- HTTP API on port 5230
- OSDP TCP listener on port 9843

### Configuration

Environment variables or `appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `Osdp:TcpPort` | `9843` | OSDP TCP listener port |
| `Osdp:Address` | `0` | OSDP device address |
| `Urls` | `http://localhost:5230` | HTTP API listen URL |

## License

MIT

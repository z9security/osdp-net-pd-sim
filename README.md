# OSDP PD Simulator

HTTP-controllable OSDP Peripheral Device (PD) simulator built on [OSDP.Net](https://github.com/bytedreamer/OSDP.Net). Useful for integration testing any access control system that speaks OSDP.

## What It Does

- Listens for OSDP connections on a configurable TCP port (default 9843)
- Responds to OSDP polls, ID reports, and capability queries
- Exposes an HTTP API to inject card reads, keypad entries, and input status changes
- Tracks received OSDP commands (output control, LED, buzzer) for test assertions

## HTTP API

### Control Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/card-read` | Simulate a raw card read (cardNumber + bitCount) |
| `POST` | `/card-read-wiegand26` | Simulate a 26-bit Wiegand card read (facilityCode + cardNumber) |
| `POST` | `/keypad` | Simulate a keypad PIN entry |
| `POST` | `/input-status` | Set input status (door contact, REX) |
| `GET`  | `/status` | Get device connection status |
| `GET`  | `/commands` | Get list of received OSDP commands |
| `POST` | `/reset` | Clear received commands log |

### Examples

```bash
# Simulate a 26-bit Wiegand card read (facility code 100, card 12345)
curl -X POST http://localhost:5230/card-read-wiegand26 \
  -H "Content-Type: application/json" \
  -d '{"facilityCode": 100, "cardNumber": 12345, "readerNumber": 0}'

# Simulate a raw card read (bit-level)
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

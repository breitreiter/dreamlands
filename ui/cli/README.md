# Dreamlands CLI

HTTP client for integration testing against GameServer. Sends commands and prints the raw JSON responses, making it easy to verify server behavior from the terminal.

The CLI auto-starts GameServer if it isn't already running.

## Usage

```
dreamlands-cli [options] <command> [args...]
```

## Options

| Flag | Description |
|------|-------------|
| `--url <url>` | Server URL (default: `http://localhost:5064`) |
| `--game-id <id>` | Use a specific game ID instead of the saved session |

## Commands

| Command | Description |
|---------|-------------|
| `new` | Create a new game and save the session |
| `status` | Get current game state |
| `move <direction>` | Move north/south/east/west |
| `choose <index>` | Pick an encounter choice by index |
| `enter-dungeon` | Enter dungeon at current location |
| `end-encounter` | End current encounter |
| `end-dungeon` | End dungeon (after completion or flee) |
| `enter-settlement` | Enter settlement at current location |
| `leave-settlement` | Leave current settlement |
| `market` | Get market stock |
| `market-order <json>` | Submit a buy/sell order as JSON |

## Session

Running `new` saves the game ID and server URL to `.session.json` in the build output directory. Subsequent commands reuse this session automatically, so you don't need to pass `--game-id` each time.

## Development Testing

This CLI serves as the primary integration testing tool during development. Claude Code uses it directly to verify features as they're built — after implementing a server endpoint or game mechanic, it spins up a game via `new`, exercises the new functionality through a sequence of commands, and inspects the JSON responses to confirm correct behavior. This tight loop of "write code, hit it with the CLI, read the output" has been the main way new features get validated before any manual testing happens.

## Examples

```bash
# Start a new game (auto-starts server if needed)
dotnet run --project ui/cli -- new

# Explore
dotnet run --project ui/cli -- status
dotnet run --project ui/cli -- move north
dotnet run --project ui/cli -- move east

# Encounter
dotnet run --project ui/cli -- choose 0
dotnet run --project ui/cli -- end-encounter

# Settlement trading
dotnet run --project ui/cli -- enter-settlement
dotnet run --project ui/cli -- market
dotnet run --project ui/cli -- market-order '{"buy":[{"itemId":"rope","quantity":1}]}'
dotnet run --project ui/cli -- leave-settlement

# Use a different server
dotnet run --project ui/cli -- --url http://localhost:9000 status
```

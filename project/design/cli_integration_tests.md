# CLI Smoke & Integration Tests

## Goal

Automated bash-script tests that exercise the CLI→GameServer round-trip. The CLI is already a thin HTTP client that prints JSON — shell scripts are the natural test harness. No xUnit/NUnit needed for this layer.

## Test Harness

A single `test/smoke.sh` script at repo root that:

1. Builds the server and CLI
2. Starts GameServer in the background with test flags
3. Runs a sequence of CLI commands, asserting on JSON output via `jq`
4. Tears down the server on exit (trap)

### Server Lifecycle

```bash
# Build once
dotnet build server/GameServer
dotnet build ui/cli

# Start server with determinism flags against test world
dotnet run --no-build --project server/GameServer -- \
  --map worlds/test/map.json \
  --bundle worlds/test/encounters.bundle.json \
  --no-encounters --no-camp \
  --urls http://localhost:5064 &
SERVER_PID=$!

# Wait for server ready (poll /api/game/new would work, or just sleep 3)
for i in $(seq 1 20); do
  curl -sf http://localhost:5064/api/game/healthcheck && break
  sleep 0.5
done

# Cleanup on exit
trap "kill $SERVER_PID 2>/dev/null; wait $SERVER_PID 2>/dev/null" EXIT
```

**Note:** The server doesn't have a `/healthcheck` endpoint yet. Two options:
- Add a trivial `app.MapGet("/api/game/healthcheck", () => Results.Ok())` (preferred)
- Poll `POST /api/game/new` and discard the result (wasteful but works today)

### Assertion Helper

```bash
CLI="dotnet run --no-build --project ui/cli --"
SESSION=""  # set after `new`

run_cli() {
  $CLI --url http://localhost:5064 ${SESSION:+--game-id $SESSION} "$@" 2>/dev/null
}

assert_eq() {
  local label="$1" expected="$2" actual="$3"
  if [[ "$expected" != "$actual" ]]; then
    echo "FAIL: $label — expected '$expected', got '$actual'"
    FAILURES=$((FAILURES + 1))
  else
    echo "  ok: $label"
  fi
}

assert_json() {
  local label="$1" jq_expr="$2" expected="$3" json="$4"
  local actual
  actual=$(echo "$json" | jq -r "$jq_expr")
  assert_eq "$label" "$expected" "$actual"
}
```

## Smoke Test Suite (Happy Path)

Linear playthrough exercising the core game loop. Run with `--no-encounters --no-camp` so movement doesn't trigger random encounters or end-of-day, giving us a predictable sequence.

### 1. Create Game

```bash
RESULT=$(run_cli new)
SESSION=$(echo "$RESULT" | jq -r '.gameId')
assert_json "gameId is 12 hex chars" '.gameId | length' '12' "$RESULT"
assert_json "mode is exploring" '.state.mode' 'exploring' "$RESULT"
assert_json "has exits" '.state.exits | length > 0' 'true' "$RESULT"
```

### 2. Get Status

```bash
RESULT=$(run_cli status)
assert_json "mode still exploring" '.mode' 'exploring' "$RESULT"
assert_json "health > 0" '.status.health > 0' 'true' "$RESULT"
assert_json "day is 1" '.status.day' '1' "$RESULT"
assert_json "has node info" '.node.terrain' '' "$RESULT"  # non-null
```

### 3. Movement

Pick a valid exit from the status response and move there.

```bash
DIR=$(echo "$RESULT" | jq -r '.exits[0].direction')
OLD_X=$(echo "$RESULT" | jq -r '.node.x')
OLD_Y=$(echo "$RESULT" | jq -r '.node.y')

RESULT=$(run_cli move "$DIR")
assert_json "mode is exploring" '.mode' 'exploring' "$RESULT"

NEW_X=$(echo "$RESULT" | jq -r '.node.x')
NEW_Y=$(echo "$RESULT" | jq -r '.node.y')
# Position must have changed
if [[ "$OLD_X,$OLD_Y" == "$NEW_X,$NEW_Y" ]]; then
  echo "FAIL: position didn't change after move"
fi
```

### 4. Walk to Settlement

The test world's starting city is a settlement. The player starts there. So we can test settlement entry immediately after `new` (before moving), or walk back to it.

Better approach: after `new`, enter the settlement at the starting node directly.

```bash
# Reset — create fresh game for settlement tests
RESULT=$(run_cli new)
SESSION=$(echo "$RESULT" | jq -r '.gameId')

RESULT=$(run_cli enter-settlement)
assert_json "mode is at_settlement" '.mode' 'at_settlement' "$RESULT"
assert_json "has settlement info" '.settlement.name' '' "$RESULT"  # non-null
```

### 5. Market

```bash
RESULT=$(run_cli market)
assert_json "has stock" '.stock | length > 0' 'true' "$RESULT"
```

### 6. Equip / Unequip / Discard

Starting gear depends on balance data. Check the inventory first, then act on what's there.

```bash
RESULT=$(run_cli status)
ITEM=$(echo "$RESULT" | jq -r '.inventory.pack[0].id // empty')

if [[ -n "$ITEM" ]]; then
  RESULT=$(run_cli equip "$ITEM")
  assert_json "item equipped" '.mode' 'at_settlement' "$RESULT"

  RESULT=$(run_cli unequip weapon)
  assert_json "weapon unequipped" '.mode' 'at_settlement' "$RESULT"

  RESULT=$(run_cli discard "$ITEM")
  assert_json "item discarded" '.mode' 'at_settlement' "$RESULT"
fi
```

### 7. Leave Settlement

```bash
RESULT=$(run_cli leave-settlement)
assert_json "mode is exploring" '.mode' 'exploring' "$RESULT"
```

### 8. Camp (separate run without --no-camp)

Camp tests need a second server instance without `--no-camp`, or a dedicated sub-test. Since camp triggers after a full day of movement (4 time periods), this test moves repeatedly until camp triggers.

This is worth splitting into a separate test script (`test/smoke-camp.sh`) that runs the server without `--no-camp` but still with `--no-encounters`.

```bash
# Move until camp triggers
for i in $(seq 1 10); do
  DIR=$(run_cli status | jq -r '.exits[0].direction')
  RESULT=$(run_cli move "$DIR")
  MODE=$(echo "$RESULT" | jq -r '.mode')
  [[ "$MODE" == "camp" ]] && break
done

assert_eq "reached camp" "camp" "$MODE"

RESULT=$(run_cli camp)
assert_json "camp resolved" '.mode' 'camp_resolved' "$RESULT"
```

## Error Case Tests

### Invalid Game ID

```bash
RESULT=$(run_cli --game-id nonexistent status 2>&1) || true
# Should get HTTP 404 or error message
echo "$RESULT" | grep -qi "not found\|error\|404" || echo "FAIL: no error for bad game ID"
```

### Wrong Mode Actions

```bash
# While exploring, try to leave settlement
RESULT=$(run_cli leave-settlement 2>&1) || true
echo "$RESULT" | grep -qi "error\|invalid\|cannot" || echo "FAIL: no error for wrong-mode action"

# While at settlement, try to move
run_cli enter-settlement >/dev/null
RESULT=$(run_cli move north 2>&1) || true
echo "$RESULT" | grep -qi "error\|invalid\|cannot" || echo "FAIL: no error for move while in settlement"
```

### Invalid Move Direction

```bash
# Find a direction that ISN'T in exits
RESULT=$(run_cli status)
EXITS=$(echo "$RESULT" | jq -r '.exits[].direction')
for DIR in north south east west; do
  if ! echo "$EXITS" | grep -q "$DIR"; then
    RESULT=$(run_cli move "$DIR" 2>&1) || true
    echo "$RESULT" | grep -qi "error\|invalid\|cannot\|blocked" || echo "FAIL: no error for blocked direction"
    break
  fi
done
```

## Random Action Fuzzer

A separate `test/fuzz.sh` script that creates a game and takes random valid actions for N iterations, asserting only that the server never returns a 500 or malformed response. No scripted sequence — just a mode-aware loop that picks a legal action for the current state.

### Default Mode: Immortal

Run with `--no-encounters --no-camp`. The player can never die or get stuck in a complex flow, so the fuzzer only needs to handle `exploring` and `at_settlement` modes. This makes it trivially simple while still exercising movement across the whole map, settlement entry/exit, and inventory operations.

### Full Mode: Mortal

Run without suppression flags. The fuzzer now hits encounters, camp, outcomes, and potentially game over. This exercises the full state machine but games will eventually end (player death, or the fuzzer gets stuck in a mode it can't escape). That's fine — start a new game and keep going.

### Core Loop

```bash
MAX_ACTIONS=500
ACTIONS=0
ERRORS=0

new_game() {
  RESULT=$(run_cli new)
  SESSION=$(echo "$RESULT" | jq -r '.gameId')
}

random_pick() {
  local arr=("$@")
  echo "${arr[$((RANDOM % ${#arr[@]}))]}"
}

new_game

while [[ $ACTIONS -lt $MAX_ACTIONS ]]; do
  RESULT=$(run_cli status)
  HTTP_STATUS=$?
  ACTIONS=$((ACTIONS + 1))

  if [[ $HTTP_STATUS -ne 0 ]]; then
    echo "FAIL [$ACTIONS]: status returned non-zero exit"
    ERRORS=$((ERRORS + 1))
    new_game
    continue
  fi

  MODE=$(echo "$RESULT" | jq -r '.mode')

  case "$MODE" in
    exploring)
      # 80% move, 10% enter settlement (if at one), 10% status-only
      ROLL=$((RANDOM % 10))
      if [[ $ROLL -lt 8 ]]; then
        DIR=$(echo "$RESULT" | jq -r '[.exits[].direction] | .[(now * 1000 | floor) % length]')
        run_cli move "$DIR" >/dev/null || ERRORS=$((ERRORS + 1))
      elif [[ $ROLL -lt 9 ]]; then
        POI=$(echo "$RESULT" | jq -r '.node.poi.kind // empty')
        if [[ "$POI" == "settlement" ]]; then
          run_cli enter-settlement >/dev/null || ERRORS=$((ERRORS + 1))
        fi
      fi
      ;;

    at_settlement)
      # 40% leave, 20% market, 20% equip random item, 20% unequip random slot
      ROLL=$((RANDOM % 5))
      if [[ $ROLL -lt 2 ]]; then
        run_cli leave-settlement >/dev/null || ERRORS=$((ERRORS + 1))
      elif [[ $ROLL -lt 3 ]]; then
        run_cli market >/dev/null || ERRORS=$((ERRORS + 1))
      elif [[ $ROLL -lt 4 ]]; then
        ITEM=$(echo "$RESULT" | jq -r '.inventory.pack[0].id // empty')
        [[ -n "$ITEM" ]] && run_cli equip "$ITEM" >/dev/null || true
      else
        SLOT=$(random_pick weapon armor boots)
        run_cli unequip "$SLOT" >/dev/null || true
      fi
      ;;

    encounter)
      # Pick a random valid choice
      COUNT=$(echo "$RESULT" | jq '.encounter.choices | length')
      if [[ "$COUNT" -gt 0 ]]; then
        run_cli choose $((RANDOM % COUNT)) >/dev/null || ERRORS=$((ERRORS + 1))
      fi
      ;;

    outcome)
      NEXT=$(echo "$RESULT" | jq -r '.outcome.nextAction')
      if [[ "$NEXT" == "end_dungeon" ]]; then
        run_cli end-dungeon >/dev/null || ERRORS=$((ERRORS + 1))
      else
        run_cli end-encounter >/dev/null || ERRORS=$((ERRORS + 1))
      fi
      ;;

    camp)
      run_cli camp >/dev/null || ERRORS=$((ERRORS + 1))
      ;;

    camp_resolved)
      # Move to continue — pick any exit
      DIR=$(echo "$RESULT" | jq -r '.exits[0].direction')
      run_cli move "$DIR" >/dev/null || ERRORS=$((ERRORS + 1))
      ;;

    game_over)
      echo "  [game over at action $ACTIONS, starting new game]"
      new_game
      ;;

    *)
      echo "FAIL [$ACTIONS]: unknown mode '$MODE'"
      ERRORS=$((ERRORS + 1))
      new_game
      ;;
  esac
done

echo ""
echo "Fuzzer complete: $ACTIONS actions, $ERRORS errors"
exit $([[ $ERRORS -eq 0 ]] && echo 0 || echo 1)
```

### What It Catches

- **500s and crashes**: any server error is a test failure
- **Mode transition bugs**: the fuzzer will hit every mode transition that's reachable from random play — stuck states, missing handlers, modes that don't accept any action
- **Edge cases in inventory**: equipping when pack is empty, unequipping empty slots, equipping at settlement vs exploring
- **Map boundary behavior**: random walks will eventually hit every edge and corner of the test world

### What It Doesn't Catch

- Specific outcome correctness (it doesn't check that health changed by the right amount)
- Rare sequences that require specific multi-step setups
- Market order edge cases (the fuzzer doesn't construct buy/sell JSON — add later if needed)

## Prerequisites

1. **Test world must be pre-built**: `worlds/test/map.json` and `worlds/test/encounters.bundle.json` must exist. The test script should check and fail fast if missing.

2. **Healthcheck endpoint**: Add `app.MapGet("/api/game/healthcheck", () => Results.Ok())` to GameServer so the test script can reliably wait for startup.

3. **`jq` required**: For JSON assertions. Available on all CI runners and most dev machines.

4. **Encounter bundle for test world**: `worlds/test/build.sh` has a TODO for wiring up the encounter bundler. This needs to be done before encounter-dependent tests can work. For the initial smoke suite, `--no-encounters` sidesteps this.

## File Layout

```
test/
  smoke.sh          # Main happy-path suite (--no-encounters --no-camp)
  smoke-camp.sh     # Camp-specific tests (--no-encounters, no --no-camp)
  smoke-errors.sh   # Error case tests
  fuzz.sh           # Random action fuzzer (default: --no-encounters --no-camp)
  lib.sh            # Shared helpers (run_cli, assert_eq, assert_json, server lifecycle)
```

`fuzz.sh` accepts flags to control the run:
- `--actions N` — number of actions per run (default 500)
- `--mortal` — skip `--no-encounters` and `--no-camp`, letting the player die

## CI Considerations

- Needs .NET 8 SDK and `jq`
- No GUI, no browser — pure CLI
- Test world (`worlds/test/`) must be pre-generated; don't run mapgen in CI (slow, needs SkiaSharp native libs)
- Check `worlds/test/map.json` into git (it already is)
- Encounter bundle needs to be checked in or generated in CI via `encounter-tool bundle`
- Tests are inherently sequential (single server, stateful game sessions) — keep them fast by using `--no-build` after initial build
- Exit code: non-zero if any assertions fail

## What This Doesn't Cover

- **Encounter flow**: Requires encounters to be loaded and triggered. A future `smoke-encounters.sh` would run without `--no-encounters` against the test world's encounter bundle and exercise `choose` + outcome flow.
- **Dungeon flow**: Requires walking to a dungeon node. Could be scripted if the test world has a known dungeon location.
- **Market transactions**: `market-order` with buy/sell JSON. Add once the happy path is stable.
- **Concurrent games**: Multiple game IDs against the same server. Low priority.

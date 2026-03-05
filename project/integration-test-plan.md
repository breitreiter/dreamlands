# Integration Test Plan

Manual integration tests exercising core game loops via the CLI client against a running GameServer.

## Prerequisites

1. Build the encounter bundle for the test world:
   ```bash
   dotnet run --project text/encounter-tool/EncounterCli -- bundle text/encounters --out worlds/test/
   ```
2. Generate the test world (if not already present):
   ```bash
   worlds/test/build.sh
   ```
3. Start the server pointing at the test world:
   ```bash
   dotnet run --project server/GameServer -- --map worlds/test/map.json --bundle worlds/test/encounters.bundle.json
   ```
4. Create a new game:
   ```bash
   dotnet run --project ui/cli -- new
   ```

All subsequent commands assume the CLI session file exists from step 4.

---

## 1. Resolving Encounters

**Goal**: Walk until an encounter triggers, make choices, and return to exploring.

### 1a. Trigger an overworld encounter

Move repeatedly until the server returns `encounter` mode. The test world is small, so encounters should appear within a handful of moves.

```bash
# Move in any valid direction until an encounter fires
dotnet run --project ui/cli -- move north
dotnet run --project ui/cli -- move north
# ... repeat, changing direction as needed based on available exits
```

**Verify**: Response switches to `encounter` mode with title, body text, and at least one choice.

### 1b. Make a choice

```bash
dotnet run --project ui/cli -- choose 0
```

**Verify**: Response is `outcome` mode with result text. May include skill check results and mechanic effects (gold, health, items, etc.).

### 1c. End the encounter

If the encounter completes (no further choices), the server returns `exploring` or `camp`. If it navigates to a follow-up encounter, repeat `choose` until finished.

```bash
dotnet run --project ui/cli -- end-encounter
```

**Verify**: Back to `exploring` mode. Player state reflects any mechanic changes from the encounter.

### 1d. Multi-choice encounter

If the encounter has multiple visible choices, try choosing a non-zero index:

```bash
dotnet run --project ui/cli -- choose 1
```

**Verify**: Different branch resolves correctly. Conditional branches (skill checks, item gates) produce appropriate pass/fail text.

### 1e. Camp phase after encounter

If an encounter finishes at night (time = Night), the server should enter `camp` mode.

```bash
dotnet run --project ui/cli -- camp
```

**Verify**: `camp_resolved` response with event log. Day increments, time resets to Morning.

---

## 2. Moving on the Grid

**Goal**: Confirm movement in all four directions, boundary behavior, and time advancement.

### 2a. Check current position and exits

```bash
dotnet run --project ui/cli -- status
```

**Verify**: Response includes `node` (coordinates, terrain, region) and `exits` (list of valid directions).

### 2b. Move in each cardinal direction

From any starting position, move in each direction listed in `exits`:

```bash
dotnet run --project ui/cli -- move north
dotnet run --project ui/cli -- move south
dotnet run --project ui/cli -- move east
dotnet run --project ui/cli -- move west
```

**Verify** for each:
- Node coordinates change by exactly 1 in the expected axis
- New exits are returned
- `visitedNodes` count increments
- Time advances: Morning → Afternoon → Evening → Night → Morning (with day increment)

### 2c. Invalid direction

Try moving in a direction not in the current exits list.

**Verify**: Server returns an error (400 or descriptive message), player position unchanged.

### 2d. Arriving at a settlement

Move toward a settlement node (check `status` for POI info on nearby nodes, or just explore until one appears).

**Verify**: Node response includes settlement info. Conditions are cleared on arrival.

### 2e. Day rollover and camp

Move four times (Morning → Afternoon → Evening → Night → Morning) without resting.

**Verify**: After the fourth move, if `--no-camp` is not set, the response should be `camp` mode. Resolve with `camp` command.

---

## 3. Inventory and Discarding Items

**Goal**: View inventory, equip/unequip gear, discard items.

### 3a. Check inventory

```bash
dotnet run --project ui/cli -- status
```

**Verify**: Response includes `inventory` with `pack`, `haversack`, and `equipment` sections. New game starts with items defined in BalanceData (starting gear).

### 3b. Equip an item

```bash
dotnet run --project ui/cli -- equip hatchet
```

**Verify**: Item moves from pack to equipment slot. Equipment section shows the item in the `weapon` slot. Skill modifiers from equipment are reflected in character stats.

### 3c. Unequip a slot

```bash
dotnet run --project ui/cli -- unequip weapon
```

**Verify**: Item returns to pack. Equipment slot is empty. Skill modifiers removed.

### 3d. Discard an item

```bash
dotnet run --project ui/cli -- discard bodkin
```

**Verify**: Item removed from pack entirely. Inventory count decreases. Cannot discard equipped items (try and verify error).

### 3e. Discard nonexistent item

```bash
dotnet run --project ui/cli -- discard unicorn_horn
```

**Verify**: Error response, inventory unchanged.

---

## 4. Market: Buying Food and Equipment

**Goal**: Visit a settlement, browse the market, buy food and equipment, sell items.

### 4a. Navigate to a settlement

Move until arriving at a node with a settlement POI. Use `status` to confirm.

### 4b. View market stock

```bash
dotnet run --project ui/cli -- market
```

**Verify**: Response includes:
- `stock` array with items, prices, quantities
- `sellPrices` for items in the player's inventory
- Item types include food, equipment, and supplies
- Prices vary by tier (higher tier = better/pricier gear)

### 4c. Buy food

```bash
dotnet run --project ui/cli -- market-order '{"buys":[{"itemId":"rations","quantity":2}],"sells":[]}'
```

**Verify**:
- Gold decreases by the buy price × quantity
- Items appear in haversack (food goes to haversack)
- Market stock quantity decreases
- Result includes per-line success messages

### 4d. Buy equipment

```bash
dotnet run --project ui/cli -- market-order '{"buys":[{"itemId":"rope","quantity":1}],"sells":[]}'
```

**Verify**:
- Gold decreases appropriately
- Item appears in pack (non-food goes to pack)
- Can subsequently equip purchased equipment

### 4e. Sell an item

```bash
dotnet run --project ui/cli -- market-order '{"buys":[],"sells":[{"itemDefId":"bodkin"}]}'
```

**Verify**:
- Item removed from inventory
- Gold increases by sell price (check against `sellPrices` from market query)
- Mercantile skill affects sell price

### 4f. Buy with insufficient gold

Attempt to buy an item that costs more gold than the player has.

**Verify**: Order fails with descriptive error. Gold and inventory unchanged.

### 4g. Buy and sell in one order

```bash
dotnet run --project ui/cli -- market-order '{"buys":[{"itemId":"rations","quantity":1}],"sells":[{"itemDefId":"hatchet"}]}'
```

**Verify**: Both operations resolve. Sells process before buys (so gold from selling can fund purchases).

### 4h. Market not available outside settlements

Move away from the settlement and try:

```bash
dotnet run --project ui/cli -- market
```

**Verify**: Error response — market requires being at a settlement.

---

## Notes

- **Item IDs**: Actual item IDs depend on what BalanceData defines as starting gear and what the market generates. Check `status` output to see real IDs before running equip/discard/sell commands.
- **Server restart**: The server must be restarted after any C# code changes.
- **Fresh game**: Start a new game (`cli new`) if state gets into an unrecoverable position (dead, stuck in dungeon, etc.).
- **`--no-encounters` flag**: Use this on the server to isolate movement tests from encounter triggers.
- **`--no-camp` flag**: Use this to isolate movement/market tests from camp interruptions.

# Factual parity fixtures

Self-contained regression set for the `factual` stage. Run with:

```
dotnet run --project EncounterCli -- factual --parity            # all 6 scenes, 1 run each
dotnet run --project EncounterCli -- factual --parity 3 --slug intro-aldgate
```

## Provenance

The six `<slug>.enc` + `<slug>.color.md` pairs and the `known-good/` outputs are copied
verbatim from the `../factual` prototype repo (`/home/joseph/repos/factual`,
commit `e816b4b`): `inputs/` for the pairs, `runs/*.factual.md` + `eval/*.report.gold.md`
for the known-good. They are vendored here so the gate does not depend on a sibling repo.

`known-good/` is human-reference only (the prototype's whole-scene reports + hand golds).
The harness does NOT diff against it — the gold is freely resequenced and won't map to
per-beat output. The gate asserts the documented invariants instead (see `FactualEval.cs`):

- em-dash leakage 0 (every scene; validates the HouseStyle post-process);
- quoted dialogue present in the speech scenes (harvest-gift, hermit-sallow-fen, the-plaza);
- intro-aldgate never names the script-gated signet ring, and keeps the "weight on your
  hand" beat;
- record-in-stone reproduces both oath-fragments (Alain / Matthis) rather than summarizing;
- wrenbury-market doesn't misparse "canteen" -> "casket" or lose "battlefield relic market".

If a scene's expected behavior changes, update the invariants in `FactualEval.cs`, not the
fixtures.

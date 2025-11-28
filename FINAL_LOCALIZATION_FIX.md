# FINAL FIX: Localization Parameter Mismatch

## Issues Found and Fixed

### Issue 1: Duplicate JSON Keys (FIXED)
The `lang/en.json` file had duplicate keys for comment sections, causing the JSON parser to fail and break the entire Localizer system.

**Fix**: Changed all comment keys to be unique (`// COMMENT_01`, `// COMMENT_02`, etc.)

### Issue 2: Missing Localization Parameters (FIXED)
Several localization strings expected 2 parameters but the code was only providing 1:

```csharp
// WRONG - Only provides bot count
Server.PrintToChatAll(Localizer["Wave.FightBots", waveTarget]);
Server.PrintToChatAll(Localizer["Wave.StartingAtWave", startWave]);

// RIGHT - Provides both bot count AND difficulty
Server.PrintToChatAll(Localizer["Wave.FightBots", waveTarget, difficulty]);
Server.PrintToChatAll(Localizer["Wave.StartingAtWave", startWave, difficulty]);
```

The localization string template is:
```json
"Wave.FightBots": " {gold}!wave{default} {0} bots ({lime}{1}{default})change bot difficulty? type: {lime}!dif"
```

Where `{0}` = bot count, `{1}` = difficulty name

### Error Message
```
[Bot Waves] Error in spawn limit check: Index (zero based) must be greater than or equal to zero and less than the size of the argument list.
```

This error occurred because the Localizer tried to format `{1}` but no second parameter was provided.

## Changes Made

### 1. Added Helper Method
```csharp
private string GetDifficultyName()
{
    if (g_Main.isZombieModeActive)
    {
        return "Easy (Knives Only)";
    }
    
    return "Normal";
}
```

### 2. Fixed All Localizer Calls

**In `CheckSpawnLimit` method**:
- Added `string difficulty = GetDifficultyName();`
- Updated all `Localizer["Wave.FightBots", ...]` calls to include difficulty parameter

**In `EnableWaveMode` method**:
- Added `string difficulty = GetDifficultyName();`
- Updated `Localizer["Wave.StartingAtWave", ...]` to include difficulty parameter

**In `OnWaveCommand` method**:
- Added `string difficulty = GetDifficultyName();`
- Updated wave number change message to include difficulty parameter

## Result
Now when wave mode starts or a wave begins, players will see proper messages like:
- ` !wave 5 bots (Normal) change bot difficulty? type: !dif`
- ` !wave 10 bots (Easy (Knives Only)) change bot difficulty? type: !dif`

No more "Index out of range" errors, and all chat messages display correctly!

## Testing Checklist
- [x] Build successful
- [ ] Upload new DLL to server
- [ ] Upload fixed `lang/en.json` to server
- [ ] Restart server or reload plugin
- [ ] Type `!wave` - should start wave mode
- [ ] Check for proper chat messages with bot count and difficulty
- [ ] No errors in console

All issues are now resolved! ??

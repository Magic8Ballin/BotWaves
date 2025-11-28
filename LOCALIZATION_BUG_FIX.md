# CRITICAL BUG FIX: Localization File Duplicate Keys

## Issue Found
The `!wave` command was not working at all - no chat messages, no functionality. The console log revealed the true culprit:

```
[Bot Waves] Error in spawn limit check: An item with the same key has already been added. Key: // ==========================================
```

## Root Cause
The `lang/en.json` localization file contained **duplicate keys**:

```json
{
  "// ==========================================": "",
  "// WAVE START/STOP & SYSTEM MESSAGES": "",
  "// ==========================================": "",   <-- DUPLICATE KEY!
  
  "Wave.TurnedOff": "...",
  
  "// ==========================================": "",   <-- DUPLICATE KEY AGAIN!
  "// VOTING SYSTEM": "",
  "// ==========================================": "",   <-- DUPLICATE KEY AGAIN!
}
```

JSON does not allow duplicate keys. When CounterStrikeSharp tried to parse this file and load the localizations, it threw an exception: `An item with the same key has already been added`.

## Impact
This caused a **cascade of failures**:

1. **Localizer failed to initialize** - All `Localizer["Key"]` calls returned null or threw exceptions
2. **All chat messages failed** - `player.PrintToChat(Localizer["..."])` silently failed
3. **All broadcast messages failed** - `Server.PrintToChatAll(Localizer["..."])` silently failed
4. **Exceptions were caught** - The try-catch blocks swallowed the errors, making it appear like nothing happened

## Why It Seemed Like `!wave` Behaved Like `!bot`
It didn't actually behave like `!bot` - **both commands were failing silently**! The Localizer was broken, so neither command could display messages. The commands were executing, but all the `PrintToChat` calls were failing due to the broken Localizer.

## The Fix
Changed all comment keys to be unique:

```json
{
  "// COMMENT_01": "==========================================",
  "// COMMENT_02": "WAVE START/STOP & SYSTEM MESSAGES",
  "// COMMENT_03": "==========================================",
  
  "Wave.TurnedOff": "...",
  
  "// COMMENT_04": "==========================================",
  "// COMMENT_05": "VOTING SYSTEM",
  "// COMMENT_06": "==========================================",
}
```

Now each key is unique, JSON parsing will succeed, and the Localizer will work properly.

## Lessons Learned

1. **JSON does not support duplicate keys** - Even for comments
2. **Silent failures are dangerous** - The try-catch blocks hid the real error
3. **Always check console logs** - The error was there all along: `An item with the same key has already been added`
4. **Localization is critical** - When Localizer breaks, the entire plugin appears non-functional

## Testing
After applying this fix:
1. Upload the corrected `lang/en.json` file
2. Reload the plugin or restart the server
3. Type `!wave` in chat
4. You should now see proper chat messages and functionality

## Why This Wasn't Caught Earlier
- The code logic was actually correct
- The console command routing was working fine
- The chat listeners were functioning
- But the Localizer was silently broken due to the JSON parsing error
- All the debug logging we added also failed because they used Localizer!

This is why debugging was so difficult - we were looking at the code when the problem was in the data file all along!

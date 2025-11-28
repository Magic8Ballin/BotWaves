# Center Message System - Quick Guide

## Overview
The Bot Waves plugin now supports **center screen messages** that you can toggle on/off by simply editing the `lang/en.json` file - no code changes needed!

## How It Works

### The `.Center` Suffix
Messages can be displayed in two ways:
- **Regular Chat** - Normal chat messages (default)
- **Center Screen** - Large text in the middle of the screen (add `.Center` suffix)

### Example

In your `lang/en.json`:

```json
{
  "Wave.FightBots": " {gold}!wave{default} {0} bots ({lime}{1}{default})",
  "Wave.FightBots.Center": "FIGHT {0} BOTS - {1} DIFFICULTY"
}
```

**How the plugin decides:**
1. Plugin looks for `Wave.FightBots.Center` first
2. If `.Center` key exists ? displays message in **center screen**
3. If `.Center` key doesn't exist ? displays message in **regular chat**

## Testing & Toggling

### To make a message show in CENTER:
1. Add a `.Center` variant to `lang/en.json`
2. Reload the plugin (`css_plugins reload BotWaves`)
3. Test in-game

### To make a message show in CHAT:
1. Remove the `.Center` key from `lang/en.json`
2. Reload the plugin
3. Test in-game

### You can have BOTH:
- Keep the regular key for chat fallback
- Add `.Center` key for center screen
- Plugin automatically uses `.Center` when available

## Recommended Center Messages

Based on typical player experience, these work great as center messages:

### High Priority (Dramatic Moments)
- ? `Wave.FightBots.Center` - Wave start
- ? `Wave.YouWonNext.Center` - Wave victory
- ? `Wave.YouLostTryAgain.Center` - Wave defeat
- ? `Wave.DifficultyChanged.Center` - Difficulty changes
- ? `Wave.NoMoreRespawns.Center` - Final bot respawn

### Medium Priority (Game State)
- `Wave.StartingAtWave.Center` - Wave mode activation
- `Wave.ZombieEnabled.Center` - Easy mode toggle
- `Wave.DifficultyReduced.Center` - Auto difficulty reduction

### Keep in Chat (Detailed Info)
- ? `Wave.Stats.Kills` - Kill statistics (too detailed)
- ? `Wave.Vote.Enable` - Vote progress (needs chat history)
- ? `Wave.Help.Vote` - Help commands (needs to be readable)
- ? `Wave.RespawnsLeft` - Respawn count (frequent updates)

## Color Formatting

**Note:** Center screen messages don't support color tags like `{gold}`, `{lime}`, etc.
- Chat messages: Use color tags for styling
- Center messages: Use plain text, ALL CAPS for emphasis

Example:
```json
{
  "Wave.FightBots": " {gold}!wave{default} Fight {0} bots!",
  "Wave.FightBots.Center": "FIGHT {0} BOTS - LET'S GO!"
}
```

## Quick Reference

| Message Type | When to Use | Example |
|-------------|-------------|---------|
| **Chat** | Detailed info, help text, vote progress | Wave.Vote.Enable |
| **Center** | Dramatic moments, wave start/end, important announcements | Wave.FightBots.Center |

## Need Help?

Just experiment! You can toggle messages between chat and center by:
1. Edit `lang/en.json`
2. Run `css_plugins reload BotWaves` in console
3. Test immediately

No code compilation needed! ??

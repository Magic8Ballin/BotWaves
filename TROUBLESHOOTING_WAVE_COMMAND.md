# Troubleshooting Guide: !wave Command Not Working

## Issue
When typing `!wave` in chat, nothing happens - no messages, no wave mode starting.

## Debug Steps

### Step 1: Check Server Console Output
The plugin now has extensive debug logging. When you type `!wave`, you should see output like this in the server console:

```
[Bot Waves] OnPlayerSay: Player 'YourName' said: '!wave'
[Bot Waves] OnPlayerSay: Detected wave command, calling HandleWaveVote
[Bot Waves] OnWaveCommand ENTRY - Player: YourName
[Bot Waves] OnWaveCommand called by YourName, ArgCount: 1, Arg: ''
[Bot Waves] No arguments provided, calling HandleWaveVote
[Bot Waves] HandleWaveVote called by YourName
[Bot Waves] Human player count: 1, Wave mode active: False
[Bot Waves] Single player - enabling wave mode
[Bot Waves] EnableWaveMode CALLED - StartWave: 1, UsedOverride: False
```

### Step 2: What to Check Based on Console Output

#### If you see NO console output at all:
1. **Plugin not loaded** - Type `css_plugins list` in server console to verify "Bot Waves" is listed
2. **Plugin crashed on load** - Check for error messages earlier in the console log
3. **Wrong command** - Make sure you're typing `!wave` or `wave` (not `/wave` or other variants)

#### If you see "OnPlayerSay" but NOT "OnWaveCommand":
- The chat listener is working but the console command isn't being triggered
- This suggests CounterStrikeSharp isn't routing `!wave` to `css_wave` correctly
- **Workaround**: Try typing `css_wave` in console instead

#### If you see "OnWaveCommand" but NOT "HandleWaveVote called":
- The console command is being triggered but HandleWaveVote isn't being called
- Check if there's an exception being caught
- Look for error messages in console

#### If you see "HandleWaveVote called" but nothing after:
- The method is being executed but something is failing
- Check the human player count value
- Look for exceptions in the try-catch block

#### If you see "EnableWaveMode CALLED" but wave mode doesn't start:
- EnableWaveMode is running but something is failing during setup
- Check for server command execution errors
- Verify the Localizer is working (localization files loaded)

### Step 3: Common Causes

#### 1. Plugin Not Fully Loaded
**Symptom**: No console output at all
**Solution**: 
- Restart the server
- Check that `lang/en.json` exists in the plugin folder
- Verify the plugin DLL is in the correct location

#### 2. Player Count Detection Issue
**Symptom**: Console shows "Human player count: 0" even though you're on the server
**Solution**:
- Make sure you're not a bot or HLTV
- Check that you're fully connected (not still loading)
- Try rejoining the server

#### 3. Localizer Not Loaded
**Symptom**: EnableWaveMode runs but no chat messages appear
**Solution**:
- Verify `lang/en.json` exists and is valid JSON
- Check console for localization loading errors
- Try reloading the plugin with `css_plugins reload BotWaves`

#### 4. Command Routing Issue
**Symptom**: Typing `!wave` does nothing, but `css_wave` in console works
**Solution**:
- This is a CounterStrikeSharp routing issue
- Use `css_wave` in console as a workaround
- Update CounterStrikeSharp to latest version

### Step 4: Manual Testing

Try these commands in order:

1. **Test console command directly**:
   ```
   css_wave
   ```
   This should trigger wave voting/toggle

2. **Test with argument**:
   ```
   css_wave 5
   ```
   This should try to start wave mode at wave 5 (may require voting or password)

3. **Test help command**:
   ```
   css_wave help
   ```
   This should show help messages

### Step 5: Check Config

Open `Config/config.json` and verify these settings:

```json
{
  "EnableBotQuotaToggle": true,
  "MaxPlayersWithoutPassword": 5,
  "WaveVoteThreshold": 0.5,
  "ShowWaveStartMessages": true
}
```

Make sure:
- `MaxPlayersWithoutPassword` is >= the number of players on server (or use password override)
- `WaveVoteThreshold` is not set to 1.0 (would require 100% votes)
- Messages are not disabled

## Quick Fix Checklist

- [ ] Plugin shows in `css_plugins list`
- [ ] No errors in console on plugin load
- [ ] `lang/en.json` file exists
- [ ] You're the only player on the server (for immediate toggle)
- [ ] You've tried both `!wave` and `css_wave`
- [ ] Server console shows debug output when you type the command
- [ ] Config values are reasonable (vote threshold, max players, etc.)

## Still Not Working?

If you've tried everything above and it still doesn't work:

1. **Copy the ENTIRE console output** from server startup to when you type `!wave`
2. **Share your config.json file**
3. **Confirm your CounterStrikeSharp version**: Type `css_version` in console
4. **Confirm the plugin version**: Should show "2.5.0" in `css_plugins list`

Include all this information when reporting the issue.

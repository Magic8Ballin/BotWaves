# BOTWAVES(1)

## NAME

BotWaves - cooperative survival mode for Counter-Strike 2

## DESCRIPTION

BotWaves is a CounterStrikeSharp plugin that creates a wave-based survival mode. Humans play as Terrorists and fight waves of CT bots. Kill all bots to advance. Each wave adds more bots.

## COMMANDS

**!wave**
: Vote to toggle wave mode. Solo players toggle instantly.

**!wave** *n*
: Vote with specific starting bot count.

**!wave** *n* *password*
: Admin override - start immediately with *n* bots.

**!dif**, **!diff**
: Toggle between Easy and Hard difficulty.

Commands also work without the `!` prefix in chat.

## DIFFICULTY

**Easy**
: Bots use knives only, don't shoot.

**Hard**
: Expert bots with full weapons.

## CONFIGURATION

Config file: `plugins/BotWaves/config.json`

| Option | Default | Description |
|--------|---------|-------------|
| MaxPlayersAllowed | 4 | Max players for wave mode |
| VoteThreshold | 0.51 | Vote percentage to toggle |
| AdminPassword | "" | Password for admin override |
| MinimumBotsPerWave | 1 | Minimum bots after reduction |
| MaxFailuresBeforeReduction | 3 | Losses before bot count drops |
| DebugMode | false | Enable verbose logging |

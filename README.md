# [CS2] Bot Wave Survival (2.0.1)

A cooperative survival mode where 1-4 players fight increasingly difficult waves of bots.

![bot-quote](https://github.com/user-attachments/assets/c88a8ba3-dfaf-4265-9e22-1a4174370d8d)

---

## 🎮 Features

- **Dynamic Wave Scaling**: Wave difficulty scales with team size
  - 1 player: +1 bot per wave
  - 2 players: +2 bots per wave
  - 3 players: +3 bots per wave
  - 4 players: +4 bots per wave
- **Dynamic Round Time**: Round time automatically adjusts based on wave number
  - Waves 1-10: 40 seconds base time
  - Waves 11+: +3 seconds per additional bot (e.g., Wave 20 = 70 seconds)
- **Auto-Respawn System**: When map spawn limits are hit, bots will respawn to reach the target wave count
- **Admin Override**: Password system to allow unlimited players (password: "glove")
  - Use `!wave [number] glove` to start with 5+ players
  - Once override is used, unlimited players can join without disabling wave mode
  - Override persists until wave mode is manually disabled or map changes
- **Auto-Disable on Population**: When a 5th player joins during normal mode (no override), wave mode automatically disables with a thank you message
- **Helpful Command Guide**: Automatic !wave command instructions shown at the start of every round when 1-4 players are present
- **Comprehensive Configuration**: Over 20 configurable settings for wave mode, round times, bot spawning, messages, and debugging

---

## 📦 Dependencies
[![Metamod:Source](https://img.shields.io/badge/Metamod:Source-2.x-2d2d2d?logo=sourceengine)](https://www.sourcemm.net/downloads.php?branch=dev)

[![CounterStrikeSharp](https://img.shields.io/badge/CounterStrikeSharp-83358F)](https://github.com/roflmuffin/CounterStrikeSharp)

---

## 📥 Installation

1. Download latest release
2. Extract to `csgo` directory
3. Configure `Bot-Quota-GoldKingZ\config\config.json`
4. Restart server

---

## ⚙️ Configuration

> [!NOTE]
> Located In ..\Bot-Quota-GoldKingZ\config\config.json                                           
>

| Property | Description | Values | Required |  
|----------|-------------|--------|----------|  
| `DisablePluginOnWarmUp` | Disable Plugin On WarmUp | `true`/`false` | - |  
| `AddBotsWhenXOrLessPlayersInServer` | Add Bots When X Or Less Players In The Server | `Integer` (e.g., `5`) | - |  
| `IncludeCountingSpecPlayers` | Include Counting In `AddBotsWhenXOrLessPlayersInServer` Spectator Players | `true`/`false` | `AddBotsWhenXOrLessPlayersInServer=x` |  
| `HowManyBotsShouldAdd` | How Many Bots Should Add When `AddBotsWhenXOrLessPlayersInServer` Pass | `Integer` (e.g., `10`) | `AddBotsWhenXOrLessPlayersInServer=x` |    
| `BotAddMode` | Add Bots By Mode |`String` (e.g., `fill`)<br> `normal`-The Number Of Bots On The Server Equals HowManyBotsShouldAdd<br>`fill`-The Server Is Filled With Bots Until There Are At Least HowManyBotsShouldAdd Players On The Server (Humans + Bots). Human Players Joining Cause An Existing Bot To Be Kicked, Human Players Leaving Might Cause A Bot To Be Added<br>`match`-The Number Of Bots On The Server Equals The Number Of Human Players Times HowManyBotsShouldAdd | - |  
| `ExecConfigWhenBotsAdded` | Custom Cfg When Bots Added | `String` (e.g., `Bot-Quota-GoldKingZ/WhenBotsAdded.cfg`) | - |  
| `ExecConfigWhenBotsKicked` | Custom Cfg When Bots Kicked | `String` (e.g., `Bot-Quota-GoldKingZ/WhenBotsKicked.cfg`) | - |  
| `EnableDebug` | Debug mode | `true`/`false` | - |  


---

## 🌍 Language

> [!NOTE]
> Located In ..\Bot-Quota-GoldKingZ\lang\en.json                                           
>

<details>
<summary>🖼️ Preview Colors In Game (Click to expand 🔽)</summary>

![Color Preview](https://github.com/oqyh/cs2-Game-Manager/assets/48490385/3df7caa9-34a7-47da-94aa-8d682f59e85d)
</details>

```json
{
	//==========================
	//        Colors
	//==========================
	//{Yellow} {Gold} {Silver} {Blue} {DarkBlue} {BlueGrey} {Magenta} {LightRed}
	//{LightBlue} {Olive} {Lime} {Red} {Purple} {Grey}
	//{Default} {White} {Darkred} {Green} {LightYellow}
	//==========================
	//        Other
	//==========================
	//{nextline} = Print On Next Line
	//{0} = How Many Bots Added/Kicked
	//{1} = Players In The Server
	//==========================

	"PrintChatToAll.LessPlayers": "{green}Gold KingZ {grey}| {grey}Server Has Less Players {lime}Adding {0} Bots",
	"PrintChatToAll.KickBots": "{green}Gold KingZ {grey}| {grey}Server Has More Players {darkred}Kicking All Bots"
}
```

---

## 📜 Changelog

<details>
<summary>📋 View Version History (Click to expand 🔽)</summary>

### [2.0.1]
- **Update**: Help message feature now implemented

### [2.0.0]
- **Major Update**: Wave system rework
- Implemented dynamic wave scaling based on player count
- Added mid-round join protection
- Introduced auto-respawn system for bots
- Milestone celebrations at specific wave intervals
- Admin override password for 5+ players

### [1.0.4]
- Fix Cfg Flood

### [1.0.3]
- Fix Some Bugs
- Removed CheckPlayersByTimer
- Added In config.json info on each what it do

### [1.0.2]
- Fix Bug Counting
- Added Some Debugs Info

### [1.0.1]
- Fix Bug

### [1.0.0]
- Initial Release

</details>

---

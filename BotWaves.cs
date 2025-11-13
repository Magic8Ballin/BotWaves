using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Timers;

namespace BotWaves;

[MinimumApiVersion(80)]
public class BotWaves : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "Bot Waves";
    public override string ModuleVersion => "2.0.1";
    public override string ModuleAuthor => "Gold KingZ & Magic8Ball";
    public override string ModuleDescription => "Bot Wave survival mode for 1-4 players";

    public static BotWaves? Instance { get; private set; }
    public Globals g_Main = new();
    public ConfigGen Config { get; set; } = new();

    public void OnConfigParsed(ConfigGen config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        try
        {
            Instance = this;

            // Register listeners for map events
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnMapEnd>(OnMapEnd);

            // Register event handlers
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            
            // Start help message timer (every 60 seconds)
            if (Config.ShowHelpMessages)
            {
                g_Main.HelpMessageTimer = AddTimer(60.0f, ShowHelpMessagesIfNeeded, TimerFlags.REPEAT);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Waves] ERROR during load: {ex.Message}");
            throw;
        }
    }

    public override void Unload(bool hotReload)
    {
        // Kill help message timer
        g_Main.HelpMessageTimer?.Kill();
        g_Main.HelpMessageTimer = null;
  
        if (g_Main.isWaveModeActive)
        {
            DisableWaveMode();
        }
        Instance = null;
    }

    private void OnMapStart(string mapName)
    {
        // Reset wave mode on map change and ensure all cvars are cleaned up
        if (g_Main.isWaveModeActive)
        {
            DisableWaveMode();
        }
    }

    private void OnMapEnd()
    {
        // Clean up wave mode and ensure all cvars are cleaned up
        if (g_Main.isWaveModeActive)
        {
            DisableWaveMode();
        }
    }

    [ConsoleCommand("css_wave", "Enable/disable Bot Wave mode")]
    [CommandHelper(minArgs: 0, usage: "[number|off|disable] [password]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnWaveCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid) return;

        try
        {
            string arg = commandInfo.ArgCount > 1 ? commandInfo.GetArg(1).ToLower() : "1";

            if (arg == "off" || arg == "disable" || arg == "0")
            {
                DisableWaveMode();
                player.PrintToChat(Localizer["Wave.TurnedOff"]);
                return;
            }

            if (!int.TryParse(arg, out int waveNumber) || waveNumber <= 0)
            {
                player.PrintToChat(Localizer["Wave.PleaseUseNumber"]);
                return;
            }

            int humanPlayerCount = GetHumanPlayerCount();

            // Check for password override
            bool hasOverride = false;
            if (commandInfo.ArgCount > 2)
            {
                string password = commandInfo.GetArg(2);
                if (password == Config.AdminPasswordOverride)
                {
                    hasOverride = true;
                    player.PrintToChat(Localizer["Wave.SpecialCodeAccepted"]);
                }
            }

            if (humanPlayerCount > Config.MaxPlayersWithoutPassword && !hasOverride)
            {
                player.PrintToChat(Localizer["Wave.OnlyFourPlayers"]);
                return;
            }

            EnableWaveMode(waveNumber, hasOverride);

            // Show appropriate message based on override usage
            if (hasOverride && humanPlayerCount > Config.MaxPlayersWithoutPassword)
            {
                Server.PrintToChatAll(Localizer["Wave.StartingWithOverride", waveNumber, humanPlayerCount]);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Waves] Error in wave command: {ex.Message}");
        }
    }

    private void EnableWaveMode(int startWave, bool usedOverride = false)
    {
        g_Main.isWaveModeActive = true;
        g_Main.currentWaveBotCount = startWave;
        g_Main.waveModeJustActivated = true;
        g_Main.waveStartedWithOverride = usedOverride;
        g_Main.consecutiveWaveFailures = 0;
        g_Main.playersAssignedToTeam.Clear();

        // Reset respawn system to prevent leftover state
        g_Main.autoRespawnEnabled = false;
        g_Main.respawnsNeeded = 0;
        g_Main.respawnsUsed = 0;
        Server.ExecuteCommand("mp_respawn_on_death_ct 0");

        // Save current server cvar values
        SaveServerCvar("mp_autoteambalance");
        SaveServerCvar("mp_limitteams");
        SaveServerCvar("mp_teambalance_enabled");
        SaveServerCvar("mp_force_pick_time");
        SaveServerCvar("mp_roundtime");
        SaveServerCvar("mp_warmuptime");
        SaveServerCvar("mp_do_warmup_period");
        SaveServerCvar("mp_forcecamera");

        // Save and disable Skill Auto Balance plugin (if enabled in config)
        if (Config.DisableSkillAutoBalanceInWaveMode)
        {
            SaveServerCvar("css_skill_autobalance_minplayers");
            Server.ExecuteCommand("css_skill_autobalance_minplayers 30");
        }

        // Disable all auto-balancing mechanisms
        Server.ExecuteCommand("mp_autoteambalance 0");
        Server.ExecuteCommand("mp_limitteams 0");
        Server.ExecuteCommand("mp_teambalance_enabled 0");
        Server.ExecuteCommand("mp_force_pick_time 0");

        // Disable warmup to prevent round restarts on player joins
        Server.ExecuteCommand("mp_warmuptime 0");
        Server.ExecuteCommand("mp_do_warmup_period 0");

        // Force spectators to only watch their own team (Terrorists)
        Server.ExecuteCommand("mp_forcecamera 1");

        // Kick all existing bots
        Server.ExecuteCommand("bot_kick");

        // Restart game to immediately start wave mode
        Server.ExecuteCommand("mp_restartgame 1");

        Server.PrintToChatAll(Localizer["Wave.StartingAtWave", startWave]);
    }

    private void DisableWaveMode()
    {
        g_Main.isWaveModeActive = false;
        g_Main.currentWaveBotCount = 1;
        g_Main.autoRespawnEnabled = false;
        g_Main.respawnsNeeded = 0;
        g_Main.respawnsUsed = 0;
        g_Main.waveModeJustActivated = false;
        g_Main.waveStartedWithOverride = false;
        g_Main.consecutiveWaveFailures = 0;
        g_Main.playersAssignedToTeam.Clear();

        // Kill any active timers
        g_Main.BotSpawnTimer?.Kill();
        g_Main.BotSpawnTimer = null;

        // Disable auto-respawn if it was on
        Server.ExecuteCommand("mp_respawn_on_death_ct 0");
        Server.ExecuteCommand("mp_respawn_on_death_t 0");

        // Kick all bots
        Server.ExecuteCommand("bot_kick");

        // Restore all saved cvar values
        RestoreAllCvars();
    }

    private void SaveServerCvar(string cvarName)
    {
        if (!Config.SaveServerCvars)
        {
            return;
        }

        try
        {
            string defaultValue = cvarName switch
            {
                "mp_autoteambalance" => "1",
                "mp_limitteams" => "2",
                "mp_teambalance_enabled" => "1",
                "mp_force_pick_time" => "15",
                "mp_roundtime" => "1.92",
                "mp_warmuptime" => "60",
                "mp_do_warmup_period" => "1",
                "mp_forcecamera" => "0",
                "css_skill_autobalance_minplayers" => "5",
                _ => "1"
            };

            g_Main.savedCvars[cvarName] = defaultValue;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Waves] Error saving cvar {cvarName}: {ex.Message}");
        }
    }

    private void RestoreAllCvars()
    {
        if (!Config.RestoreCvarsOnDisable)
        {
            return;
        }

        foreach (var kvp in g_Main.savedCvars)
        {
            try
            {
                Server.ExecuteCommand($"{kvp.Key} {kvp.Value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bot Waves] Error restoring {kvp.Key}: {ex.Message}");
            }
        }

        g_Main.savedCvars.Clear();
    }

    private void CheckSpawnLimit(int waveTarget)
    {
        try
        {
            var aliveCTBots = Utilities.GetPlayers().Count(p => p != null && p.IsValid && p.IsBot && !p.IsHLTV && p.Team == g_Main.botTeam);

            if (aliveCTBots < waveTarget)
            {
                // Hit spawn limit! Enable auto-respawn
                if (Config.EnableAutoRespawn)
                {
                    g_Main.respawnsNeeded = waveTarget - aliveCTBots;
                    g_Main.respawnsUsed = 0;
                    g_Main.autoRespawnEnabled = true;

                    Server.ExecuteCommand("mp_respawn_on_death_ct 1");

                    if (Config.ShowWaveStartMessages)
                    {
                        Server.PrintToChatAll(Localizer["Wave.FightBots", waveTarget, waveTarget]);
                        Server.PrintToChatAll(Localizer["Wave.SpawnLimitReached", aliveCTBots, g_Main.respawnsNeeded]);
                    }
                }
                else
                {
                    if (Config.ShowWaveStartMessages)
                    {
                        Server.PrintToChatAll(Localizer["Wave.FightBots", aliveCTBots, aliveCTBots]);
                    }
                }
            }
            else
            {
                // Normal wave, all bots spawned
                if (Config.ShowWaveStartMessages)
                {
                    Server.PrintToChatAll(Localizer["Wave.FightBots", waveTarget, waveTarget]);
                }
            }

            // Clear the just activated flag - bots are confirmed spawned and wave is truly running
            if (g_Main.waveModeJustActivated)
            {
                g_Main.waveModeJustActivated = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Waves] Error in spawn limit check: {ex.Message}");
        }
    }

    private void AddBots(int count)
    {
        string teamCmd = g_Main.botTeam == CsTeam.Terrorist ? "t" : "ct";

        for (int i = 0; i < count; i++)
        {
            Server.ExecuteCommand($"bot_add_{teamCmd}");
        }
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        try
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
                return HookResult.Continue;

            Server.NextFrame(() =>
            {
                try
                {
                    if (!player.IsValid) return;

                    int humanCount = GetHumanPlayerCount();

                    // Check if wave mode should be disabled due to too many players
                    if (g_Main.isWaveModeActive &&
        humanCount > Config.MaxPlayersWithoutPassword &&
                !g_Main.waveStartedWithOverride &&
            Config.DisableWaveOnFifthPlayer)
                    {
                        Server.PrintToChatAll(Localizer["Wave.FifthPlayerJoined"]);
                        Server.PrintToChatAll(Localizer["Wave.FifthPlayerJoinedThanks"]);
                        DisableWaveMode();
                    }

                    // Handle new player joining during active wave
                    if (g_Main.isWaveModeActive && !g_Main.waveModeJustActivated)
                    {
                        // Assign to T team immediately but they'll be dead until next round
                        if (player.IsValid && !player.IsBot)
                        {
                            player.ChangeTeam(g_Main.humanTeam);
                            g_Main.playersAssignedToTeam.Add(player.SteamID);

                            // Notify the player
                            player.PrintToChat(" {lime}[Bot Waves]{default} You've joined during an active wave. You'll spawn at the start of the next round!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Bot Waves] Error in player connect: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Waves] Error in OnPlayerConnectFull: {ex.Message}");
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        try
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
                return HookResult.Continue;

            // Check after a short delay if server is now empty
            AddTimer(0.5f, () =>
         {
             try
             {
                 if (!g_Main.isWaveModeActive) return;

                 int remainingHumans = GetHumanPlayerCount();

                 if (remainingHumans == 0)
                 {
                     DisableWaveMode();
                 }
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[Bot Waves] Error in disconnect timer: {ex.Message}");
             }
         });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Waves] Error in OnPlayerDisconnect: {ex.Message}");
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        try
        {
            if (!g_Main.isWaveModeActive) return HookResult.Continue;

            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsHLTV)
                return HookResult.Continue;

            int newTeam = @event.Team;
            int humanTeamNum = (int)g_Main.humanTeam;
            int botTeamNum = (int)g_Main.botTeam;

            // Handle human players - force to T side
            if (!player.IsBot)
            {
                // Allow spectator switches
                if (newTeam == (int)CsTeam.Spectator)
                {
                    return HookResult.Continue;
                }

                if (newTeam != humanTeamNum && newTeam != (int)CsTeam.None && newTeam != (int)CsTeam.Spectator)
                {
                    Server.NextFrame(() =>
                        {
                            if (player.IsValid && !player.IsBot)
                            {
                                player.ChangeTeam(g_Main.humanTeam);
                                g_Main.playersAssignedToTeam.Add(player.SteamID);
                            }
                        });
                }
                else if (newTeam == humanTeamNum)
                {
                    g_Main.playersAssignedToTeam.Add(player.SteamID);
                }
            }
            // Handle bots - force to CT side
            else if (player.IsBot)
            {
                if (newTeam != botTeamNum && newTeam != (int)CsTeam.None)
                {
                    Server.NextFrame(() =>
                 {
                     if (player.IsValid && player.IsBot)
                     {
                         player.ChangeTeam(g_Main.botTeam);
                     }
                 });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Waves] Error in OnPlayerTeam: {ex.Message}");
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        try
        {
            if (!g_Main.isWaveModeActive) return HookResult.Continue;

            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
                return HookResult.Continue;

            // Only correct team if player is on wrong team AND not already tracked as assigned
            if (player.Team != g_Main.humanTeam && !g_Main.playersAssignedToTeam.Contains(player.SteamID))
            {
                Server.NextFrame(() =>
                    {
                        if (player.IsValid && !player.IsBot)
                        {
                            player.ChangeTeam(g_Main.humanTeam);
                            g_Main.playersAssignedToTeam.Add(player.SteamID);
                        }
                    });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Waves] Error in OnPlayerSpawn: {ex.Message}");
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        try
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;

            // Track kills if wave mode is active
            if (g_Main.isWaveModeActive && victim != null && victim.IsValid && attacker != null && attacker.IsValid)
            {
                // Only count bot kills by human players
                if (victim.IsBot && !attacker.IsBot && victim.Team == g_Main.botTeam && attacker.Team == g_Main.humanTeam)
                {
                    if (!g_Main.roundKills.ContainsKey(attacker.SteamID))
                    {
                        g_Main.roundKills[attacker.SteamID] = 0;
                    }
                    g_Main.roundKills[attacker.SteamID]++;
                }
            }

            // Handle auto-respawn system for bots
            if (!g_Main.isWaveModeActive || !g_Main.autoRespawnEnabled) return HookResult.Continue;

            if (victim == null || !victim.IsValid || !victim.IsBot) return HookResult.Continue;

            if (victim.Team != g_Main.botTeam) return HookResult.Continue;

            g_Main.respawnsUsed++;
            int respawnsRemaining = g_Main.respawnsNeeded - g_Main.respawnsUsed;

            if (g_Main.respawnsUsed >= g_Main.respawnsNeeded)
            {
                Server.ExecuteCommand("mp_respawn_on_death_ct 0");
                g_Main.autoRespawnEnabled = false;

                if (Config.ShowRespawnMessages)
                {
                    Server.PrintToChatAll(Localizer["Wave.NoMoreRespawns"]);
                }
            }
            else
            {
                if (Config.ShowRespawnMessages && (g_Main.respawnsUsed % Config.ShowRespawnEveryXDeaths == 0 || respawnsRemaining <= 3))
                {
                    Server.PrintToChatAll(Localizer["Wave.RespawnsLeft", respawnsRemaining]);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Waves] Error in OnPlayerDeath: {ex.Message}");
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        try
        {
            if (!g_Main.isWaveModeActive) return HookResult.Continue;

       // Mark that we're now in an active round
            g_Main.isRoundActive = true;

     // CRITICAL: Always disable respawn at the start of every wave to prevent leftover state
  Server.ExecuteCommand("mp_respawn_on_death_ct 0");

      // Reset auto-respawn tracking variables
          g_Main.autoRespawnEnabled = false;
            g_Main.respawnsNeeded = 0;
       g_Main.respawnsUsed = 0;

  // Kill any existing spawn timer
            g_Main.BotSpawnTimer?.Kill();
         g_Main.BotSpawnTimer = null;

            Server.NextFrame(() => DoRoundStart());
        }
        catch (Exception ex)
        {
   Console.WriteLine($"[Bot Waves] Error in OnRoundStart: {ex.Message}");
      }

        return HookResult.Continue;
    }

    private void DoRoundStart()
    {
        try
        {
            // Reset kill tracking for new round
            g_Main.roundKills.Clear();
            g_Main.roundParticipants.Clear();

            SetRoundTime(g_Main.currentWaveBotCount);

            // Force all humans to T side
            var humans = Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV).ToList();

            foreach (var human in humans)
            {
                if (IsSpectator(human))
                {
                    continue;
                }

                // Mark as participant and initialize kill count
                g_Main.roundParticipants.Add(human.SteamID);
                if (!g_Main.roundKills.ContainsKey(human.SteamID))
                {
                    g_Main.roundKills[human.SteamID] = 0;
                }

                if (human.Team != g_Main.humanTeam)
                {
                    human.ChangeTeam(g_Main.humanTeam);
                    g_Main.playersAssignedToTeam.Add(human.SteamID);
                }
                else
                {
                    g_Main.playersAssignedToTeam.Add(human.SteamID);
                }
            }

            // Store player count for wave increment calculation
            int humanPlayerCount = humans.Where(p => !IsSpectator(p)).Count();
            g_Main.humanPlayerCountAtRoundStart = humanPlayerCount;

            // Spawn bots
            var existingBots = Utilities.GetPlayers().Count(p => p != null && p.IsValid && p.IsBot && !p.IsHLTV);
            int waveTarget = g_Main.currentWaveBotCount;

            if (existingBots < waveTarget)
            {
                int toSpawn = waveTarget - existingBots;
                AddBots(toSpawn);
            }

            AddTimer(Config.SpawnLimitCheckDelay, () => CheckSpawnLimit(waveTarget));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Waves] Error in round start: {ex.Message}");
        }
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        try
        {
            // Mark that the round is no longer active
            g_Main.isRoundActive = false;

            if (!g_Main.isWaveModeActive)
            {
                return HookResult.Continue;
            }

            if (g_Main.waveModeJustActivated)
            {
                return HookResult.Continue;
            }

            // ===== DISPLAY KILL STATISTICS =====
            if (g_Main.roundParticipants.Count > 0)
            {
                // Get all participants and their kill counts, sorted by kills (descending)
                var sortedStats = g_Main.roundParticipants
                      .Select(steamId => new
                      {
                          SteamId = steamId,
                          Kills = g_Main.roundKills.ContainsKey(steamId) ? g_Main.roundKills[steamId] : 0
                      })
                .OrderByDescending(x => x.Kills)
                          .ToList();

                // Find player names and print stats
                foreach (var stat in sortedStats)
                {
                    var player = Utilities.GetPlayers().FirstOrDefault(p =>
        p != null && p.IsValid && !p.IsBot && p.SteamID == stat.SteamId);

                    if (player != null)
                    {
                        string localizedMessage = stat.Kills == 1
                    ? Localizer["Wave.Stats.Kills", player.PlayerName, stat.Kills]
                         : Localizer["Wave.Stats.KillsPlural", player.PlayerName, stat.Kills];

                        Server.PrintToChatAll(localizedMessage);
                    }
                }
            }

            CsTeam winner = (CsTeam)@event.Winner;

            if (winner == g_Main.humanTeam)
            {
                // RESET failure counter on win
                g_Main.consecutiveWaveFailures = 0;

                // Use stored player count from round start
                int humanPlayersOnTeam = g_Main.humanPlayerCountAtRoundStart;

                int increment = Math.Max(Config.MinimumWaveIncrement, humanPlayersOnTeam);

                g_Main.currentWaveBotCount += increment;

                if (Config.ShowWaveEndMessages)
                {
                    Server.PrintToChatAll(Localizer["Wave.YouWonNext", g_Main.currentWaveBotCount]);
                }
            }
            else if (winner == g_Main.botTeam)
            {
                // Increment failure counter
                g_Main.consecutiveWaveFailures++;

                // Show failure counter in RED text
                Server.PrintToChatAll(Localizer["Wave.FailureCounter", g_Main.consecutiveWaveFailures, Globals.MaxFailuresBeforeReduction]);

                // Check if we need to reduce difficulty
                if (g_Main.consecutiveWaveFailures >= Globals.MaxFailuresBeforeReduction)
                {
                    int oldBotCount = g_Main.currentWaveBotCount;

                    // Calculate 10% reduction (minimum 1 bot)
                    double reductionPercent = oldBotCount * 0.10;
                    int botsToRemove = Math.Max(1, (int)Math.Round(reductionPercent));

                    // Apply reduction
                    g_Main.currentWaveBotCount -= botsToRemove;

                    // Ensure we never go below 1 bot
                    if (g_Main.currentWaveBotCount < 1)
                    {
                        g_Main.currentWaveBotCount = 1;
                    }

                    // Reset failure counter after reduction
                    g_Main.consecutiveWaveFailures = 0;

                    // Show difficulty reduction message
                    Server.PrintToChatAll(Localizer["Wave.DifficultyReduced", g_Main.currentWaveBotCount, oldBotCount, g_Main.currentWaveBotCount, botsToRemove]);
                }

                if (Config.ShowWaveEndMessages)
                {
                    Server.PrintToChatAll(Localizer["Wave.YouLostTryAgain", g_Main.currentWaveBotCount]);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Waves] Error in OnRoundEnd: {ex.Message}");
        }

        return HookResult.Continue;
    }

    private int GetHumanPlayerCount()
    {
        return Utilities.GetPlayers().Count(p =>
     p != null &&
  p.IsValid &&
       !p.IsBot &&
    !p.IsHLTV &&
       p.Connected == PlayerConnectedState.PlayerConnected
        );
    }

    private bool IsSpectator(CCSPlayerController player)
    {
        return player.Team == CsTeam.Spectator;
    }

    // ===== Round Time Management =====

    private int CalculateRoundTimeSeconds(int waveNumber)
    {
        if (!Config.EnableDynamicRoundTime)
        {
            return Config.BaseRoundTimeSeconds;
        }

        if (waveNumber <= Config.WaveThresholdForTimeIncrease)
        {
            return Config.BaseRoundTimeSeconds;
        }

        int botsAboveThreshold = waveNumber - Config.WaveThresholdForTimeIncrease;
        int additionalTime = botsAboveThreshold * Config.RoundTimeIncrementPerBot;
        int totalSeconds = Config.BaseRoundTimeSeconds + additionalTime;

        return totalSeconds;
    }

    private float SecondsToRoundTimeMinutes(int seconds)
    {
        return seconds / 60.0f;
    }

    private void SetRoundTime(int waveNumber)
    {
        if (!Config.EnableDynamicRoundTime)
        {
            return;
        }

        int roundTimeSeconds = CalculateRoundTimeSeconds(waveNumber);
        float roundTimeMinutes = SecondsToRoundTimeMinutes(roundTimeSeconds);
        string roundTimeStr = roundTimeMinutes.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        Server.ExecuteCommand($"mp_roundtime {roundTimeStr}");
    }

    private void ShowHelpMessagesIfNeeded()
    {
   try
        {
      // Only show messages if wave mode is NOT active and we have 1-4 players
            if (g_Main.isWaveModeActive || !Config.ShowHelpMessages)
   return;

            int humanCount = GetHumanPlayerCount();

   if (humanCount >= 1 && humanCount <= Config.MaxPlayersWithoutPassword)
            {
        Server.PrintToChatAll(Localizer["Wave.HelpStart"]);
          Server.PrintToChatAll(Localizer["Wave.HelpCustomWave"]);
            Server.PrintToChatAll(Localizer["Wave.HelpTurnOff"]);
  }
        }
    catch (Exception ex)
   {
         Console.WriteLine($"[Bot Waves] Error in ShowHelpMessagesIfNeeded: {ex.Message}");
        }
    }
}

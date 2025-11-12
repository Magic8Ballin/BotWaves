using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Timers;
using Bot_Quota_GoldKingZ;

namespace Bot_Quota_GoldKingZ;

[MinimumApiVersion(80)]
public class BotQuotaGoldKingZ : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "Bot Wave";
    public override string ModuleVersion => "2.0.1";
    public override string ModuleAuthor => "Gold KingZ";
    public override string ModuleDescription => "Bot Wave survival mode for 1-4 players";
    
    public static BotQuotaGoldKingZ? Instance { get; private set; }
    public Globals g_Main = new();
    public ConfigGen Config { get; set; } = new();

    public void OnConfigParsed(ConfigGen config)
    {
        Config = config;
  Console.WriteLine("[Bot Wave] Configuration loaded successfully!");
        Console.WriteLine($"[Bot Wave] Dynamic Round Time: {(Config.EnableDynamicRoundTime ? "Enabled" : "Disabled")}");
        Console.WriteLine($"[Bot Wave] Base Round Time: {Config.BaseRoundTimeSeconds} seconds");
        Console.WriteLine($"[Bot Wave] Round Time Increment: +{Config.RoundTimeIncrementPerBot}s per bot (after wave {Config.WaveThresholdForTimeIncrease})");
    }

    public override void Load(bool hotReload)
    {
      try
      {
       Console.WriteLine("========================================");
   Console.WriteLine("[Bot Wave] Loading plugin...");
            Console.WriteLine("========================================");
            
   Instance = this;
            
   // Register listeners for map events
       RegisterListener<Listeners.OnMapStart>(OnMapStart);
 RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
       
     // Register event handlers
    RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    RegisterEventHandler<EventRoundStart>(OnRoundStart);
   RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
   RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
      RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
         RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        
   Console.WriteLine("[Bot Wave] Plugin loaded successfully!");
  Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Wave] ERROR during load: {ex.Message}");
            throw;
        }
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine("[Bot Wave] Plugin unloading...");
        if (g_Main.isWaveModeActive)
        {
         DisableWaveMode();
    }
        Instance = null;
    }

    private void OnMapStart(string mapName)
 {
        Console.WriteLine($"[Bot Wave] Map started: {mapName}");
     
        // Reset wave mode on map change
        if (g_Main.isWaveModeActive)
        {
            Console.WriteLine("[Bot Wave] Map changed - disabling wave mode");
  g_Main.isWaveModeActive = false;
     g_Main.currentWaveBotCount = 1;
        }
    }

    private void OnMapEnd()
    {
  Console.WriteLine("[Bot Wave] Map ending");

        // Clean up wave mode
      if (g_Main.isWaveModeActive)
   {
            g_Main.isWaveModeActive = false;
g_Main.currentWaveBotCount = 1;
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
           Console.WriteLine($"[Bot Wave] {player.PlayerName} used password override for player limit");
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
 Console.WriteLine($"[Bot Wave] Error in wave command: {ex.Message}");
        }
    }

    private void EnableWaveMode(int startWave, bool usedOverride = false)
    {
     Console.WriteLine($"[Bot Wave] Enabling wave mode, starting at wave {startWave}");
 Console.WriteLine($"[Bot Wave] Human team: {g_Main.humanTeam}, Bot team: {g_Main.botTeam}");
        Console.WriteLine($"[Bot Wave] Override used: {usedOverride}");
 
     g_Main.isWaveModeActive = true;
        g_Main.currentWaveBotCount = startWave;
        g_Main.waveModeJustActivated = true;
        g_Main.waveStartedWithOverride = usedOverride;
g_Main.playersAssignedToTeam.Clear();
        
        // Save current server cvar values
 SaveServerCvar("mp_autoteambalance");
    SaveServerCvar("mp_limitteams");
        SaveServerCvar("mp_teambalance_enabled");
        SaveServerCvar("mp_force_pick_time");
  SaveServerCvar("mp_roundtime");
   
      Console.WriteLine($"[Bot Wave] Saved {g_Main.savedCvars.Count} cvar values");
        
        // Disable all auto-balancing mechanisms
        Server.ExecuteCommand("mp_autoteambalance 0");
        Server.ExecuteCommand("mp_limitteams 0");
  Server.ExecuteCommand("mp_teambalance_enabled 0");
     Server.ExecuteCommand("mp_force_pick_time 0");
        
        // Kick all existing bots
        Console.WriteLine("[Bot Wave] Kicking all existing bots");
  Server.ExecuteCommand("bot_kick");
     
        // Mark all players for team assignment on next spawn
        Console.WriteLine("[Bot Wave] Marking all players for team assignment on next spawn");
   var allPlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV).ToList();
        foreach (var playerToAssign in allPlayers)
        {
       if (IsSpectator(playerToAssign))
            {
            Console.WriteLine($"[Bot Wave] {playerToAssign.PlayerName} is spectator, will stay in spec");
                continue;
 }
         
            Console.WriteLine($"[Bot Wave] {playerToAssign.PlayerName} marked for T team assignment");
    }
        
  // Restart game to immediately start wave mode
        Console.WriteLine("[Bot Wave] Restarting game to begin wave mode");
        Server.ExecuteCommand("mp_restartgame 1");

        Server.PrintToChatAll(Localizer["Wave.StartingAtWave", startWave]);
    }

    private void DisableWaveMode()
    {
        Console.WriteLine("[Bot Wave] Disabling wave mode");
        
        g_Main.isWaveModeActive = false;
 g_Main.currentWaveBotCount = 1;
  g_Main.autoRespawnEnabled = false;
    g_Main.respawnsNeeded = 0;
        g_Main.respawnsUsed = 0;
 g_Main.waveModeJustActivated = false;
        g_Main.waveStartedWithOverride = false;
        g_Main.playersAssignedToTeam.Clear();

  // Kill any active timers
     g_Main.BotSpawnTimer?.Kill();
        g_Main.BotSpawnTimer = null;

        // Disable auto-respawn if it was on
        Server.ExecuteCommand("mp_respawn_on_death_ct 0");
        Server.ExecuteCommand("mp_respawn_on_death_t 0");
    
      // Kick all bots
  var bots = Utilities.GetPlayers().Where(p => p != null && p.IsValid && p.IsBot && !p.IsHLTV).ToList();
      Console.WriteLine($"[Bot Wave] Found {bots.Count} bots to kick");
        
     foreach (var bot in bots)
   {
     if (bot != null && bot.IsValid && bot.IsBot)
       {
            Console.WriteLine($"[Bot Wave] Kicking bot: {bot.PlayerName}");
           Server.ExecuteCommand($"bot_kick {bot.PlayerName}");
   }
        }
        
        Server.ExecuteCommand("bot_kick");
        
        // Restore all saved cvar values
      RestoreAllCvars();

    Console.WriteLine("[Bot Wave] Wave mode disabled");
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
   _ => "1"
   };
  
            g_Main.savedCvars[cvarName] = defaultValue;
         
  if (Config.EnableDebugMode)
            {
            Console.WriteLine($"[Bot Wave] Will restore {cvarName} to default: {defaultValue}");
            }
        }
  catch (Exception ex)
  {
Console.WriteLine($"[Bot Wave] Error saving cvar {cvarName}: {ex.Message}");
        }
    }

    private void RestoreAllCvars()
    {
 if (!Config.RestoreCvarsOnDisable)
      {
            return;
        }

Console.WriteLine($"[Bot Wave] Restoring {g_Main.savedCvars.Count} saved cvars");
   
    foreach (var kvp in g_Main.savedCvars)
     {
         try
            {
            Server.ExecuteCommand($"{kvp.Key} {kvp.Value}");
   
     if (Config.EnableDebugMode)
  {
      Console.WriteLine($"[Bot Wave] Restored {kvp.Key} = {kvp.Value}");
                }
     }
  catch (Exception ex)
{
                Console.WriteLine($"[Bot Wave] Error restoring {kvp.Key}: {ex.Message}");
 }
        }
        
        g_Main.savedCvars.Clear();
    }

    private void CheckSpawnLimit(int waveTarget)
  {
        try
        {
  var aliveCTBots = Utilities.GetPlayers().Count(p => p != null && p.IsValid && p.IsBot && !p.IsHLTV && p.Team == g_Main.botTeam);
            
  if (Config.LogBotSpawns)
            {
          Console.WriteLine($"[Bot Wave] Spawn check: {aliveCTBots} CT bots spawned, wave target: {waveTarget}");
            }
  
            if (aliveCTBots < waveTarget)
         {
    // Hit spawn limit! Enable auto-respawn
      if (Config.EnableAutoRespawn)
         {
   g_Main.respawnsNeeded = waveTarget - aliveCTBots;
       g_Main.respawnsUsed = 0;
       g_Main.autoRespawnEnabled = true;
    
      Console.WriteLine($"[Bot Wave] Spawn limit hit! Need {g_Main.respawnsNeeded} respawns");
       Console.WriteLine($"[Bot Wave] Enabling mp_respawn_on_death_ct 1");
     
   Server.ExecuteCommand("mp_respawn_on_death_ct 1");
       
        if (Config.ShowWaveStartMessages)
 {
    Server.PrintToChatAll(Localizer["Wave.FightBots", waveTarget, waveTarget]);
       Server.PrintToChatAll(Localizer["Wave.SpawnLimitReached", aliveCTBots, g_Main.respawnsNeeded]);
          }
 }
         else
          {
      Console.WriteLine($"[Bot Wave] Spawn limit hit but auto-respawn is disabled in config");
   
          if (Config.ShowWaveStartMessages)
         {
  Server.PrintToChatAll(Localizer["Wave.FightBots", aliveCTBots, aliveCTBots]);
    }
    }
            }
       else
        {
              // Normal wave, all bots spawned
              if (Config.LogBotSpawns)
       {
               Console.WriteLine($"[Bot Wave] All {aliveCTBots} bots spawned successfully");
      }
                
       if (Config.ShowWaveStartMessages)
         {
 Server.PrintToChatAll(Localizer["Wave.FightBots", waveTarget, waveTarget]);
     }
   }
    
   // Clear the just activated flag - bots are confirmed spawned and wave is truly running
            if (g_Main.waveModeJustActivated)
            {
      Console.WriteLine("[Bot Wave] Wave fully started with bots spawned - clearing just activated flag NOW");
      g_Main.waveModeJustActivated = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Wave] Error in spawn limit check: {ex.Message}");
        }
    }

  private void AddBots(int count)
    {
        string teamCmd = g_Main.botTeam == CsTeam.Terrorist ? "t" : "ct";
        
        if (Config.LogBotSpawns)
        {
            Console.WriteLine($"[Bot Wave] Adding {count} bots to {teamCmd} team");
        }
        
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
           Console.WriteLine($"[Bot Wave] {player.PlayerName} is the {humanCount}th player. No override was used, disabling wave mode.");
       Server.PrintToChatAll(Localizer["Wave.FifthPlayerJoined"]);
DisableWaveMode();
   }
      else if (g_Main.isWaveModeActive && 
            humanCount > Config.MaxPlayersWithoutPassword && 
            g_Main.waveStartedWithOverride)
           {
    Console.WriteLine($"[Bot Wave] {player.PlayerName} joined ({humanCount} players). Override was used, allowing unlimited players.");
       }
    }
    catch (Exception ex)
      {
        Console.WriteLine($"[Bot Wave] Error in player connect: {ex.Message}");
           }
            });
 }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bot Wave] Error in OnPlayerConnectFull: {ex.Message}");
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
      if (newTeam != humanTeamNum && newTeam != (int)CsTeam.None && newTeam != (int)CsTeam.Spectator)
  {
         if (Config.LogTeamChanges)
           {
    Console.WriteLine($"[Bot Wave] Redirecting human {player.PlayerName} from team {newTeam} to team {humanTeamNum}");
              }
 
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
     
        if (Config.LogTeamChanges)
        {
  Console.WriteLine($"[Bot Wave] {player.PlayerName} joined {g_Main.humanTeam}, marked as assigned");
             }
         }
            }
            // Handle bots - force to CT side
          else if (player.IsBot)
            {
if (newTeam != botTeamNum && newTeam != (int)CsTeam.None)
        {
               if (Config.LogTeamChanges)
  {
       Console.WriteLine($"[Bot Wave] Redirecting bot {player.PlayerName} from team {newTeam} to team {botTeamNum}");
          }
     
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
            Console.WriteLine($"[Bot Wave] Error in OnPlayerTeam: {ex.Message}");
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
  if (Config.LogTeamChanges)
          {
         Console.WriteLine($"[Bot Wave] WARNING: {player.PlayerName} spawned on wrong team {player.Team}, correcting to {g_Main.humanTeam}");
 }
     
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
      Console.WriteLine($"[Bot Wave] Error in OnPlayerSpawn: {ex.Message}");
     }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        try
        {
       if (!g_Main.isWaveModeActive || !g_Main.autoRespawnEnabled) return HookResult.Continue;

 var victim = @event.Userid;
            if (victim == null || !victim.IsValid || !victim.IsBot) return HookResult.Continue;

    if (victim.Team != g_Main.botTeam) return HookResult.Continue;

    g_Main.respawnsUsed++;
            int respawnsRemaining = g_Main.respawnsNeeded - g_Main.respawnsUsed;
       
  if (Config.EnableDebugMode)
     {
   Console.WriteLine($"[Bot Wave] Bot death detected. Respawns used: {g_Main.respawnsUsed}/{g_Main.respawnsNeeded}, Remaining: {respawnsRemaining}");
      }

       if (g_Main.respawnsUsed >= g_Main.respawnsNeeded)
            {
  Console.WriteLine($"[Bot Wave] Respawn limit reached! Disabling mp_respawn_on_death_ct");
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
          Console.WriteLine($"[Bot Wave] Error in OnPlayerDeath: {ex.Message}");
    }

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        try
        {
       if (Config.LogRoundEvents)
            {
                Console.WriteLine($"[Bot Wave] OnRoundStart fired. Wave mode active: {g_Main.isWaveModeActive}, Just activated: {g_Main.waveModeJustActivated}");
}
        
      // Show help messages if wave mode is NOT active and we have 1-4 players
      if (!g_Main.isWaveModeActive && Config.ShowHelpMessages)
    {
           int humanCount = GetHumanPlayerCount();
      
 if (humanCount >= 1 && humanCount <= Config.MaxPlayersWithoutPassword)
            {
        Server.PrintToChatAll(Localizer["Wave.HelpStart"]);
    Server.PrintToChatAll(Localizer["Wave.HelpCustomWave"]);
  Server.PrintToChatAll(Localizer["Wave.HelpTurnOff"]);
     
          Console.WriteLine($"[Bot Wave] Showed help messages to {humanCount} players");
 }
   }
            
     if (!g_Main.isWaveModeActive) return HookResult.Continue;

         // Reset auto-respawn system
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
  Console.WriteLine($"[Bot Wave] Error in OnRoundStart: {ex.Message}");
        }

        return HookResult.Continue;
    }

    private void DoRoundStart()
    {
        try
    {
        if (Config.LogRoundEvents)
            {
   Console.WriteLine($"[Bot Wave] Processing round start. Wave: {g_Main.currentWaveBotCount}, Just activated: {g_Main.waveModeJustActivated}");
            }

            SetRoundTime(g_Main.currentWaveBotCount);

         // Force all humans to T side
       Console.WriteLine("[Bot Wave] FORCING all humans to T team (aggressive mode)");
    var humans = Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV).ToList();
       
            foreach (var human in humans)
     {
         if (IsSpectator(human))
                {
       Console.WriteLine($"[Bot Wave] {human.PlayerName} is spectator, leaving in spec");
    continue;
   }

    if (human.Team != g_Main.humanTeam)
                {
            Console.WriteLine($"[Bot Wave] !!! FORCING {human.PlayerName} from {human.Team} to {g_Main.humanTeam}");
human.ChangeTeam(g_Main.humanTeam);
    g_Main.playersAssignedToTeam.Add(human.SteamID);
         }
          else
                {
     g_Main.playersAssignedToTeam.Add(human.SteamID);
Console.WriteLine($"[Bot Wave] {human.PlayerName} already on {g_Main.humanTeam}");
     }
       }

      // Store player count for wave increment calculation
    int humanPlayerCount = humans.Where(p => !IsSpectator(p)).Count();
          g_Main.humanPlayerCountAtRoundStart = humanPlayerCount;
          Console.WriteLine($"[Bot Wave] *** STORED PLAYER COUNT FOR THIS WAVE: {humanPlayerCount} ***");

    // Spawn bots
    var existingBots = Utilities.GetPlayers().Count(p => p != null && p.IsValid && p.IsBot && !p.IsHLTV);
    int waveTarget = g_Main.currentWaveBotCount;

      if (Config.LogBotSpawns)
            {
                Console.WriteLine($"[Bot Wave] Existing bots: {existingBots}, Wave target: {waveTarget}");
    }

      if (existingBots < waveTarget)
            {
     int toSpawn = waveTarget - existingBots;
      
  if (Config.LogBotSpawns)
        {
            Console.WriteLine($"[Bot Wave] Attempting to spawn {toSpawn} bots");
     }
     
             AddBots(toSpawn);
        }

    AddTimer(Config.SpawnLimitCheckDelay, () => CheckSpawnLimit(waveTarget));
        }
        catch (Exception ex)
        {
        Console.WriteLine($"[Bot Wave] Error in round start: {ex.Message}");
        }
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
     try
        {
  if (!g_Main.isWaveModeActive)
          {
        Console.WriteLine("[Bot Wave] OnRoundEnd called but wave mode is NOT active");
          return HookResult.Continue;
  }

        if (g_Main.waveModeJustActivated)
          {
        Console.WriteLine("[Bot Wave] ===== IGNORING ROUND END - Wave mode just activated =====");
        return HookResult.Continue;
        }

     CsTeam winner = (CsTeam)@event.Winner;
            
 Console.WriteLine("===============================================================");
        Console.WriteLine("       BOT WAVE - ROUND END DEBUG REPORT");
        Console.WriteLine("===============================================================");
   Console.WriteLine($"[Bot Wave] Winner: {winner} ({(int)winner})");
Console.WriteLine($"[Bot Wave] g_Main.humanTeam: {g_Main.humanTeam} ({(int)g_Main.humanTeam})");
          Console.WriteLine($"[Bot Wave] g_Main.botTeam: {g_Main.botTeam} ({(int)g_Main.botTeam})");
       Console.WriteLine($"[Bot Wave] Current wave BEFORE increment: {g_Main.currentWaveBotCount}");

        if (winner == g_Main.humanTeam)
            {
     Console.WriteLine("[Bot Wave] >>> HUMANS WON! Calculating increment...");
        
             // Print to client consoles
     foreach (var client in Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV))
                {
 client.PrintToConsole("=======================================");
            client.PrintToConsole("  BOT WAVE - INCREMENT DEBUG");
               client.PrintToConsole("=======================================");
                client.PrintToConsole($"Winner: {winner}");
       client.PrintToConsole($"Current Wave: {g_Main.currentWaveBotCount}");
      }

  // Use stored player count from round start
         int humanPlayersOnTeam = g_Main.humanPlayerCountAtRoundStart;
         
            Console.WriteLine($"[Bot Wave] *** USING STORED PLAYER COUNT: {humanPlayersOnTeam} ***");
  Console.WriteLine($"[Bot Wave] (This was counted at round start when teams were stable)");
    
       foreach (var client in Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV))
                {
 client.PrintToConsole($"Using stored player count from round start: {humanPlayersOnTeam}");
             }

          Console.WriteLine($"[Bot Wave] ---------------------------------------");
           Console.WriteLine($"[Bot Wave] FINAL COUNT: {humanPlayersOnTeam} human players");
      Console.WriteLine($"[Bot Wave] Config.MinimumWaveIncrement: {Config.MinimumWaveIncrement}");
       
     int increment = Math.Max(Config.MinimumWaveIncrement, humanPlayersOnTeam);
     Console.WriteLine($"[Bot Wave] INCREMENT CALCULATION:");
    Console.WriteLine($"[Bot Wave]   Math.Max({Config.MinimumWaveIncrement}, {humanPlayersOnTeam}) = {increment}");
           
     foreach (var client in Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV))
              {
      client.PrintToConsole("---------------------------");
   client.PrintToConsole($"Min increment: {Config.MinimumWaveIncrement}");
          client.PrintToConsole($"Calculated: Math.Max({Config.MinimumWaveIncrement}, {humanPlayersOnTeam}) = {increment}");
         }
 
        int oldWave = g_Main.currentWaveBotCount;
    g_Main.currentWaveBotCount += increment;
         
                Console.WriteLine($"[Bot Wave] ---------------------------------------");
     Console.WriteLine($"[Bot Wave] WAVE UPDATE: {oldWave} + {increment} = {g_Main.currentWaveBotCount}");
     Console.WriteLine("===============================================================");

                foreach (var client in Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV))
       {
          client.PrintToConsole($"Wave: {oldWave} + {increment} = {g_Main.currentWaveBotCount}");
   client.PrintToConsole("=======================================");
    }

   if (Config.ShowWaveEndMessages)
             {
Server.PrintToChatAll(Localizer["Wave.YouWonNext", g_Main.currentWaveBotCount]);
        }
       }
            else if (winner == g_Main.botTeam)
            {
  Console.WriteLine($"[Bot Wave] >>> HUMANS LOST. Wave stays at: {g_Main.currentWaveBotCount}");
                Console.WriteLine("===============================================================");
     
      if (Config.ShowWaveEndMessages)
          {
 Server.PrintToChatAll(Localizer["Wave.YouLostTryAgain", g_Main.currentWaveBotCount]);
                }
    }
            else
    {
                Console.WriteLine($"[Bot Wave] >>> UNKNOWN WINNER: {winner} ({(int)winner})");
        Console.WriteLine("===============================================================");
            }
        }
        catch (Exception ex)
{
            Console.WriteLine($"[Bot Wave] !!!! ERROR in OnRoundEnd !!!!");
            Console.WriteLine($"[Bot Wave] Message: {ex.Message}");
          Console.WriteLine($"[Bot Wave] Stack trace: {ex.StackTrace}");
    
          foreach (var client in Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV))
            {
    client.PrintToConsole($"ERROR: {ex.Message}");
  }
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
        
        int minutes = roundTimeSeconds / 60;
   int seconds = roundTimeSeconds % 60;
        
  if (Config.EnableDebugMode || Config.LogRoundEvents)
        {
            Console.WriteLine($"[Bot Wave] Set round time for wave {waveNumber}: {roundTimeSeconds}s ({minutes}:{seconds:D2}) = {roundTimeStr} minutes");
 }
    }
}
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace BotWaves;

/// <summary>
/// Bot Waves - Cooperative survival mode where players fight waves of bots.
/// Humans play as Terrorists, Bots play as Counter-Terrorists.
/// </summary>
[MinimumApiVersion(80)]
public sealed class BotWaves : BasePlugin, IPluginConfig<ConfigGen>
{
    // ============================================================
    // PLUGIN METADATA
    // ============================================================
    
    public override string ModuleName => "Bot Waves";
    public override string ModuleVersion => "5.0.0";
    public override string ModuleAuthor => "Magic8Ball";
    public override string ModuleDescription => "Cooperative survival mode - fight waves of bots!";

    // ============================================================
    // STATE
    // ============================================================
    
    /// <summary>Plugin configuration loaded from JSON.</summary>
    public ConfigGen Config { get; set; } = new();
    
    /// <summary>Runtime state for wave mode.</summary>
    private readonly WaveState _state = new();

    // ============================================================
    // DEBUG LOGGING
    // All debug output goes through these methods for consistency.
    // Set Config.DebugMode = true to enable verbose logging.
    // ============================================================
    
    /// <summary>
    /// Logs a message to console. Always prints regardless of debug mode.
    /// Use for important events like plugin load/unload.
    /// </summary>
    private void Log(string message)
    {
        Console.WriteLine($"[BotWaves] {message}");
    }
    
    /// <summary>
    /// Logs a debug message to console if DebugMode is enabled.
    /// Use for detailed diagnostic information.
    /// </summary>
    private void Debug(string message)
    {
        if (Config.DebugMode)
        {
            Console.WriteLine($"[BotWaves DEBUG] {message}");
        }
    }
    
    /// <summary>
    /// Logs a categorized debug message. Categories help filter logs.
    /// Categories: LOAD, MAP, CMD, VOTE, WAVE, ROUND, SPAWN, TEAM, EVENT, CVAR
    /// </summary>
    private void Debug(string category, string message)
    {
        if (Config.DebugMode)
        {
            Console.WriteLine($"[BotWaves DEBUG] [{category}] {message}");
        }
    }
    
    /// <summary>
    /// Logs current state snapshot for debugging.
    /// </summary>
    private void DebugState()
    {
        if (Config.DebugMode)
        {
            Console.WriteLine($"[BotWaves DEBUG] [STATE] {_state.ToDebugString()}");
        }
    }

    // ============================================================
    // PLUGIN LIFECYCLE
    // ============================================================
    
    /// <summary>
    /// Called when config is loaded from JSON file.
    /// </summary>
    public void OnConfigParsed(ConfigGen config)
    {
        Config = config;
        
        // Validate config values
        Config.VoteThreshold = Math.Clamp(Config.VoteThreshold, 0.1f, 1.0f);
        Config.MaxPlayersAllowed = Math.Max(1, Config.MaxPlayersAllowed);
        Config.MinimumWaveIncrement = Math.Max(1, Config.MinimumWaveIncrement);
        Config.MinimumBotsPerWave = Math.Max(1, Config.MinimumBotsPerWave);
        Config.MaxFailuresBeforeReduction = Math.Max(1, Config.MaxFailuresBeforeReduction);
        Config.WaveReductionPercentage = Math.Clamp(Config.WaveReductionPercentage, 1, 100);
        Config.BaseRoundTimeSeconds = Math.Max(30, Config.BaseRoundTimeSeconds);
        Config.HelpMessageIntervalSeconds = Math.Max(10, Config.HelpMessageIntervalSeconds);
        
        if (Config.DebugMode)
        {
            Log("Config loaded with DebugMode ENABLED");
            Debug("CONFIG", $"MaxPlayersAllowed={Config.MaxPlayersAllowed}");
            Debug("CONFIG", $"VoteThreshold={Config.VoteThreshold}");
            Debug("CONFIG", $"AdminPassword={(string.IsNullOrEmpty(Config.AdminPassword) ? "(not set)" : "(set)")}");
        }
    }
    
    /// <summary>
    /// Called when plugin loads.
    /// </summary>
    public override void Load(bool hotReload)
    {
        Log($"Loading v{ModuleVersion}...");
        Debug("LOAD", $"HotReload={hotReload}");
        
        // Register map events
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        
        // Register game events
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        
        // Register chat commands
        AddCommandListener("say", OnPlayerChat);
        AddCommandListener("say_team", OnPlayerChat);
        
        // Start help message timer
        StartHelpTimer();
        
        Log("Loaded successfully!");
        DebugState();
    }
    
    /// <summary>
    /// Called when plugin unloads.
    /// </summary>
    public override void Unload(bool hotReload)
    {
        Debug("LOAD", $"Unloading, HotReload={hotReload}");
        
        // Clean up timers
        _state.KillAllTimers();
        
        // Disable wave mode if active
        if (_state.IsActive)
        {
            DisableWaveMode();
        }
        
        Log("Unloaded");
    }
    
    /// <summary>
    /// Called when a new map starts.
    /// </summary>
    private void OnMapStart(string mapName)
    {
        Debug("MAP", $"Map starting: {mapName}");
        
        // Reset wave mode on map change
        if (_state.IsActive)
        {
            Debug("MAP", "Wave mode was active, resetting");
            _state.Reset();
        }
        
        // Restart help timer
        StartHelpTimer();
        
        DebugState();
    }
    
    /// <summary>
    /// Called when map ends.
    /// </summary>
    private void OnMapEnd()
    {
        Debug("MAP", "Map ending");
        
        _state.KillAllTimers();
        
        if (_state.IsActive)
        {
            _state.Reset();
        }
    }

    // ============================================================
    // CONSOLE COMMANDS
    // ============================================================
    
    /// <summary>
    /// !wave command - Vote to toggle wave mode or start with specific bot count.
    /// Usage: !wave [botcount] [password]
    /// </summary>
    [ConsoleCommand("css_wave", "Toggle wave mode or set bot count")]
    [CommandHelper(minArgs: 0, usage: "[botcount] [password]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnWaveCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsValidHuman(player)) return;
        
        var arg1 = command.ArgCount > 1 ? command.GetArg(1).Trim() : "";
        var arg2 = command.ArgCount > 2 ? command.GetArg(2).Trim() : "";
        
        Debug("CMD", $"!wave from {player!.PlayerName}: arg1='{arg1}', arg2='{(string.IsNullOrEmpty(arg2) ? "" : "(password)")}'");
        
        // No arguments = vote to toggle
        if (string.IsNullOrEmpty(arg1))
        {
            HandleVote(player);
            return;
        }
        
        // Parse bot count
        if (!int.TryParse(arg1, out var botCount) || botCount <= 0)
        {
            player.PrintToChat(Localizer["Wave.InvalidNumber"]);
            return;
        }
        
        // Check for admin password
        var isAdmin = !string.IsNullOrEmpty(arg2) && 
                      !string.IsNullOrEmpty(Config.AdminPassword) &&
                      arg2 == Config.AdminPassword;
        
        Debug("CMD", $"BotCount={botCount}, IsAdmin={isAdmin}");
        
        // If wave mode active, just change bot count
        if (_state.IsActive)
        {
            _state.BotCount = botCount;
            _state.ConsecutiveFailures = 0;
            
            // PRE-SPAWN BOTS: Kick existing and add new count before restart
            Debug("WAVE", $"Changing bot count to {botCount}, pre-spawning before restart...");
            Server.ExecuteCommand("bot_kick");
            var humanCount = GetHumanPlayerCount();
            var totalQuota = humanCount + botCount;
            Server.ExecuteCommand("bot_join_team ct");
            Server.ExecuteCommand($"bot_quota {totalQuota}");
            
            // Small delay to let bots join before restart
            AddTimer(0.3f, () =>
            {
                if (_state.IsActive)
                {
                    DebugBotStatus("BEFORE BOT COUNT CHANGE RESTART");
                    Server.ExecuteCommand("mp_restartgame 1");
                }
            });
            
            Server.PrintToChatAll(Localizer["Wave.BotCountChanged", botCount]);
            Debug("WAVE", $"Bot count changed to {botCount}");
            return;
        }
        
        // Check player limit
        var playerCount = GetHumanPlayerCount();
        if (playerCount > Config.MaxPlayersAllowed && !isAdmin)
        {
            player.PrintToChat(Localizer["Wave.TooManyPlayers", Config.MaxPlayersAllowed]);
            return;
        }
        
        // Admin override - start immediately
        if (isAdmin)
        {
            EnableWaveMode(botCount, adminOverride: true);
            Server.PrintToChatAll(Localizer["Wave.AdminStart", botCount]);
            return;
        }
        
        // Otherwise just vote
        HandleVote(player);
    }
    
    /// <summary>
    /// !dif command - Toggle difficulty between Easy and Hard.
    /// </summary>
    [ConsoleCommand("css_dif", "Toggle difficulty")]
    [ConsoleCommand("css_diff", "Toggle difficulty")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnDifficultyCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsValidHuman(player)) return;
        
        Debug("CMD", $"!dif from {player!.PlayerName}");
        
        if (!_state.IsActive)
        {
            player.PrintToChat(Localizer["Wave.NeedWaveActive"]);
            return;
        }
        
        ToggleDifficulty();
    }

    // ============================================================
    // CHAT HANDLER
    // ============================================================
    
    /// <summary>
    /// Handles chat messages for wave and dif triggers (without ! prefix).
    /// Commands with ! prefix are handled by the console command system.
    /// </summary>
    private HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsValidHuman(player)) return HookResult.Continue;
        
        var msg = command.GetArg(1).Trim().ToLowerInvariant();
        
        // Skip commands with ! prefix - they're handled by console commands
        if (msg.StartsWith('!'))
        {
            return HookResult.Continue;
        }
        
        // wave (without !)
        if (msg == "wave")
        {
            HandleVote(player!);
            return HookResult.Continue;
        }
        
        // dif or diff (without !)
        if (msg is "dif" or "diff")
        {
            if (!_state.IsActive)
            {
                player!.PrintToChat(Localizer["Wave.NeedWaveActive"]);
            }
            else
            {
                ToggleDifficulty();
            }
            return HookResult.Continue;
        }
        
        return HookResult.Continue;
    }

    // ============================================================
    // VOTING SYSTEM
    // ============================================================
    
    /// <summary>
    /// Handles a player's vote to toggle wave mode.
    /// Solo players get instant toggle, multiplayer requires vote threshold.
    /// </summary>
    private void HandleVote(CCSPlayerController player)
    {
        var playerCount = GetHumanPlayerCount();
        Debug("VOTE", $"Vote from {player.PlayerName}, players={playerCount}");
        
        // Solo player = instant toggle
        if (playerCount == 1)
        {
            Debug("VOTE", "Solo player, instant toggle");
            if (_state.IsActive)
            {
                DisableWaveMode();
                player.PrintToChat(Localizer["Wave.Disabled"]);
            }
            else
            {
                EnableWaveMode(startBots: 1, adminOverride: false);
                Server.PrintToChatAll(Localizer["Wave.Enabled", 1]);
            }
            return;
        }
        
        // Check player limit when enabling
        if (!_state.IsActive && playerCount > Config.MaxPlayersAllowed)
        {
            player.PrintToChat(Localizer["Wave.TooManyPlayers", Config.MaxPlayersAllowed]);
            return;
        }
        
        // Check if already voted
        if (_state.Voters.Contains(player.SteamID))
        {
            player.PrintToChat(Localizer["Wave.AlreadyVoted"]);
            return;
        }
        
        // Add vote
        _state.Voters.Add(player.SteamID);
        
        // Calculate threshold
        var needed = Math.Max(2, (int)Math.Ceiling(playerCount * Config.VoteThreshold));
        var current = _state.Voters.Count;
        var action = _state.IsActive ? "disable" : "enable";
        
        Debug("VOTE", $"Votes: {current}/{needed} to {action}");
        
        // Broadcast vote
        Server.PrintToChatAll(Localizer["Wave.VoteCast", player.PlayerName, current, needed, action]);
        
        // Check if threshold met
        if (current >= needed)
        {
            Debug("VOTE", "Threshold met!");
            _state.Voters.Clear();
            
            if (_state.IsActive)
            {
                DisableWaveMode();
                Server.PrintToChatAll(Localizer["Wave.VotePassedDisable"]);
            }
            else
            {
                EnableWaveMode(startBots: 1, adminOverride: false);
                Server.PrintToChatAll(Localizer["Wave.VotePassedEnable"]);
            }
        }
    }

    // ============================================================
    // WAVE MODE CONTROL
    // ============================================================
    
    /// <summary>
    /// Enables wave mode with specified starting bot count.
    /// </summary>
    private void EnableWaveMode(int startBots, bool adminOverride)
    {
        Debug("WAVE", $"=== ENABLING WAVE MODE ===");
        Debug("WAVE", $"StartBots={startBots}, AdminOverride={adminOverride}");
        
        // Initialize state
        _state.Initialize(startBots, adminOverride);
        DebugState();
        
        // Kick all existing bots first - they may be on wrong team
        Debug("WAVE", "Kicking existing bots...");
        Server.ExecuteCommand("bot_quota 0");
        Server.ExecuteCommand("bot_kick");
        
        // Execute wave enabled config
        ExecuteConfig(Config.WaveEnabledConfig);
        
        // Set initial difficulty (Easy mode)
        ExecuteConfig(Config.DifficultyEasyConfig);
        
        // Move all humans to T team
        Debug("WAVE", "Moving humans to T team...");
        foreach (var player in Utilities.GetPlayers())
        {
            if (IsValidHuman(player) && player.Team != _state.HumanTeam)
            {
                Debug("WAVE", $"Moving {player.PlayerName} from {player.Team} to {_state.HumanTeam}");
                player.ChangeTeam(_state.HumanTeam);
            }
        }
        
        // PRE-SPAWN BOTS: Add bots BEFORE restart so they're on the server when round starts
        Debug("WAVE", $"Pre-spawning {startBots} bots before restart...");
        var humanCount = GetHumanPlayerCount();
        var totalQuota = humanCount + startBots;
        Server.ExecuteCommand("bot_join_team ct");
        Server.ExecuteCommand($"bot_quota {totalQuota}");
        
        // Schedule round restart - bots are already on server, will spawn with the round
        Debug("WAVE", "Scheduling round restart...");
        AddTimer(0.5f, () =>
        {
            if (_state.IsActive)
            {
                Debug("WAVE", "Executing mp_restartgame 1");
                DebugBotStatus("BEFORE RESTART");
                Server.ExecuteCommand("mp_restartgame 1");
            }
        });
        
        Debug("WAVE", "=== WAVE MODE ENABLED ===");
    }
    
    /// <summary>
    /// Disables wave mode and restores server settings.
    /// </summary>
    private void DisableWaveMode()
    {
        Debug("WAVE", "=== DISABLING WAVE MODE ===");
        
        // Re-enable normal gameplay
        Server.ExecuteCommand("mp_ignore_round_win_conditions 0");
        
        // Remove wave mode bots but keep 1 for normal gameplay
        Debug("WAVE", "Resetting bots for normal play...");
        Server.ExecuteCommand("bot_kick");
        
        // Execute wave disabled config to restore server settings
        ExecuteConfig(Config.WaveDisabledConfig);
        
        // Set bot quota to allow 1 bot for normal gameplay
        Server.ExecuteCommand("bot_quota 1");
        
        // Reset state
        _state.Reset();
        _state.KillAllTimers();
        
        Debug("WAVE", "=== WAVE MODE DISABLED ===");
        DebugState();
    }
    
    /// <summary>
    /// Toggles between Easy and Hard difficulty.
    /// </summary>
    private void ToggleDifficulty()
    {
        _state.IsEasyMode = !_state.IsEasyMode;
        Debug("WAVE", $"=== TOGGLING DIFFICULTY to {(_state.IsEasyMode ? "EASY" : "HARD")} ===");
        
        // Must kick bots and restart for difficulty to apply
        Server.ExecuteCommand("bot_quota 0");
        Server.ExecuteCommand("bot_kick");
        
        // Execute the appropriate difficulty config
        var difficultyConfig = _state.IsEasyMode ? Config.DifficultyEasyConfig : Config.DifficultyHardConfig;
        ExecuteConfig(difficultyConfig);
        
        var msg = _state.IsEasyMode ? Localizer["Wave.DifficultyEasy"] : Localizer["Wave.DifficultyHard"];
        Server.PrintToChatAll(msg);
        
        // PRE-SPAWN BOTS: Add bots BEFORE restart so they spawn with the round
        Debug("WAVE", $"Pre-spawning {_state.BotCount} bots before restart...");
        var humanCount = GetHumanPlayerCount();
        var totalQuota = humanCount + _state.BotCount;
        Server.ExecuteCommand("bot_join_team ct");
        Server.ExecuteCommand($"bot_quota {totalQuota}");
        
        // Small delay to let bots join before restart
        AddTimer(0.3f, () =>
        {
            if (_state.IsActive)
            {
                DebugBotStatus("BEFORE DIFFICULTY RESTART");
                Server.ExecuteCommand("mp_restartgame 1");
                Debug("WAVE", "Round restarting with new difficulty");
            }
        });
    }
    
    /// <summary>
    /// Executes a config file from the cfg folder.
    /// </summary>
    private void ExecuteConfig(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            Debug("CONFIG", "Config path is empty, skipping");
            return;
        }
        
        Debug("CONFIG", $"Executing config: {configPath}");
        Server.ExecuteCommand($"exec {configPath}");
    }

    // ============================================================
    // GAME EVENTS
    // ============================================================
    
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!IsValidHuman(player)) return HookResult.Continue;
        
        Debug("EVENT", $"Player connected: {player!.PlayerName}");
        
        Server.NextFrame(() =>
        {
            if (!player.IsValid) return;
            
            var count = GetHumanPlayerCount();
            Debug("EVENT", $"Player count now: {count}");
            
            // Check if we should disable wave mode due to player limit
            if (_state.IsActive && 
                !_state.StartedWithOverride && 
                Config.DisableOnPlayerLimitExceeded &&
                count > Config.MaxPlayersAllowed)
            {
                Debug("EVENT", "Player limit exceeded, disabling wave mode");
                Server.PrintToChatAll(Localizer["Wave.PlayerLimitExceeded"]);
                DisableWaveMode();
            }
        });
        
        return HookResult.Continue;
    }
    
    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is null || player.IsBot) return HookResult.Continue;
        
        Debug("EVENT", $"Player disconnected: {player.PlayerName}");
        
        // Remove their vote
        _state.Voters.Remove(player.SteamID);
        
        // Check if server is empty after delay
        AddTimer(Config.DisconnectCheckDelay, () =>
        {
            var remaining = GetHumanPlayerCount();
            Debug("EVENT", $"Post-disconnect check: {remaining} players");
            
            if (remaining == 0 && _state.IsActive)
            {
                Debug("EVENT", "Server empty, disabling wave mode");
                DisableWaveMode();
            }
        });
        
        return HookResult.Continue;
    }
    
    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        if (!_state.IsActive) return HookResult.Continue;
        
        var player = @event.Userid;
        if (player is null || !player.IsValid) return HookResult.Continue;
        
        var newTeam = (CsTeam)@event.Team;
        Debug("TEAM", $"{player.PlayerName} (Bot={player.IsBot}) -> {newTeam}");
        
        // Force humans to T team, bots to CT team
        Server.NextFrame(() =>
        {
            if (!player.IsValid) return;
            
            if (player.IsBot && newTeam != _state.BotTeam && newTeam != CsTeam.None)
            {
                Debug("TEAM", $"Forcing bot to {_state.BotTeam}");
                player.ChangeTeam(_state.BotTeam);
            }
            else if (!player.IsBot && newTeam == _state.BotTeam)
            {
                Debug("TEAM", $"Forcing human to {_state.HumanTeam}");
                player.ChangeTeam(_state.HumanTeam);
            }
        });
        
        return HookResult.Continue;
    }
    
    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Debug("ROUND", "=== ROUND START ===");
        DebugState();
        DebugBotStatus("ROUND START - INITIAL");
        
        if (!_state.IsActive) return HookResult.Continue;
        
        _state.IsRoundActive = true;
        _state.ResetRound();
        
        // Kill existing spawn timer
        if (_state.SpawnCheckTimer != null)
        {
            _state.SpawnCheckTimer.Kill();
            _state.SpawnCheckTimer = null;
        }
        
        Server.NextFrame(() => SetupRound());
        
        return HookResult.Continue;
    }
    
    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        var winner = (CsTeam)@event.Winner;
        Debug("ROUND", $"=== ROUND END: Winner={winner} ===");
        
        _state.IsRoundActive = false;
        
        // Disable respawns at round end
        if (_state.RespawnEnabled)
        {
            Debug("RESPAWN", "Round ended, disabling respawns");
            Server.ExecuteCommand("mp_respawn_on_death_ct 0");
            _state.RespawnEnabled = false;
        }
        
        if (!_state.IsActive) return HookResult.Continue;
        
        // Skip first round after activation
        if (_state.JustActivated)
        {
            Debug("ROUND", "Skipping first round (JustActivated)");
            _state.JustActivated = false;
            return HookResult.Continue;
        }
        
        // Process result
        if (winner == _state.HumanTeam)
        {
            HandleVictory();
        }
        else if (winner == _state.BotTeam)
        {
            HandleDefeat();
        }
        
        DebugState();
        return HookResult.Continue;
    }
    
    /// <summary>
    /// Handles bot deaths for respawn tracking.
    /// When respawns are enabled, tracks kills and disables respawns when enough kills achieved.
    /// </summary>
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (!_state.IsActive || !_state.IsRoundActive) return HookResult.Continue;
        
        var victim = @event.Userid;
        if (victim is null || !victim.IsValid || !victim.IsBot) return HookResult.Continue;
        
        // Only track CT bot deaths
        if (victim.Team != _state.BotTeam) return HookResult.Continue;
        
        // Track kills when respawn system is active
        if (_state.RespawnEnabled)
        {
            _state.CurrentKills++;
            Debug("RESPAWN", $"Bot killed: {victim.PlayerName}, Kills={_state.CurrentKills}/{_state.TotalKillsNeeded}, RespawnsRemaining={_state.RespawnsRemaining}");
            
            // Decrement respawns remaining
            if (_state.RespawnsRemaining > 0)
            {
                _state.RespawnsRemaining--;
                Debug("RESPAWN", $"Respawn used, remaining: {_state.RespawnsRemaining}");
                
                // If no more respawns, disable the cvar - remaining bots won't respawn
                if (_state.RespawnsRemaining <= 0)
                {
                    Debug("RESPAWN", "No respawns remaining, disabling mp_respawn_on_death_ct");
                    Server.ExecuteCommand("mp_respawn_on_death_ct 0");
                }
            }
        }
        else
        {
            Debug("RESPAWN", $"Bot died: {victim.PlayerName} (respawns not enabled)");
        }
        
        return HookResult.Continue;
    }
    
    /// <summary>
    /// Handles player spawns. In Easy mode, strips weapons from bots so they only have knives.
    /// </summary>
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (!_state.IsActive) return HookResult.Continue;
        
        var player = @event.Userid;
        if (player is null || !player.IsValid || !player.IsBot) return HookResult.Continue;
        
        // Only process bots on the bot team
        if (player.Team != _state.BotTeam) return HookResult.Continue;
        
        // In Easy mode, strip weapons so bots only have knives (if enabled in config)
        if (_state.IsEasyMode && Config.StripWeaponsOnEasyMode)
        {
            // Delay slightly to ensure weapons have been given
            AddTimer(0.1f, () =>
            {
                if (player.IsValid)
                {
                    StripBotWeapons(player);
                }
            });
        }
        
        return HookResult.Continue;
    }

    // ============================================================
    // ROUND LOGIC
    // ============================================================
    
    /// <summary>
    /// Sets up a new round with bots.
    /// </summary>
    private void SetupRound()
    {
        Debug("ROUND", "=== SETUP ROUND ===");
        Debug("ROUND", $"BotCount={_state.BotCount}, IsEasyMode={_state.IsEasyMode}");
        
        // Keep win conditions disabled during spawn
        Server.ExecuteCommand("mp_ignore_round_win_conditions 1");
        
        // Ensure bot settings are correct
        Server.ExecuteCommand("bot_join_team ct");
        Server.ExecuteCommand("bot_quota_mode fill");
        
        // Execute the appropriate difficulty config
        var difficultyConfig = _state.IsEasyMode ? Config.DifficultyEasyConfig : Config.DifficultyHardConfig;
        ExecuteConfig(difficultyConfig);
        
        // Set round time
        SetRoundTime(_state.BotCount);
        
        // Store player count for wave increment
        _state.PlayersAtRoundStart = GetHumanPlayerCount();
        Debug("ROUND", $"PlayersAtRoundStart={_state.PlayersAtRoundStart}");
        
        // Spawn bots
        SpawnBots(_state.BotCount);
        
        // Show message
        if (Config.ShowWaveStartMessages)
        {
            var diff = _state.IsEasyMode ? "Easy" : "Hard";
            Server.PrintToChatAll(Localizer["Wave.RoundStart", _state.BotCount, diff]);
        }
        
        // Schedule spawn check to re-enable win conditions and handle limited spawn points
        Debug("ROUND", $"Scheduling spawn check in {Config.SpawnCheckDelay}s");
        _state.SpawnCheckTimer = AddTimer(Config.SpawnCheckDelay, () =>
        {
            if (!_state.IsActive) return;
            
            Debug("SPAWN", "=== SPAWN CHECK ===");
            DebugBotStatus("SPAWN CHECK");
            
            // Log all bots currently on server
            var allBots = Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: true }).ToList();
            Debug("SPAWN", $"Total bots on server: {allBots.Count}");
            foreach (var bot in allBots)
            {
                Debug("SPAWN", $"  Bot: {bot.PlayerName}, Team={bot.Team}, IsValid={bot.IsValid}, PawnIsAlive={bot.PawnIsAlive}");
            }
            
            var actualBots = CountBots();
            var aliveBots = CountAliveBots();
            var expectedBots = _state.BotCount;
            Debug("SPAWN", $"CT bots: {actualBots}, Alive CT bots: {aliveBots}, Expected: {expectedBots}");
            
            // If no bots spawned at all, try again
            if (actualBots == 0)
            {
                Debug("SPAWN", "No bots found! Forcing bot spawn...");
                Server.ExecuteCommand("bot_join_team ct");
                Server.ExecuteCommand($"bot_quota {expectedBots}");
                // Re-check after another delay
                AddTimer(Config.SpawnCheckDelay, () =>
                {
                    DebugBotStatus("SPAWN RE-CHECK");
                    CheckAndEnableRespawns();
                });
            }
            else
            {
                // Check if we need respawns due to limited spawn points
                CheckAndEnableRespawns();
            }
            
            Debug("SPAWN", "Enabling win conditions");
            Server.ExecuteCommand("mp_ignore_round_win_conditions 0");
            _state.JustActivated = false;
            Debug("SPAWN", "=== SPAWN CHECK COMPLETE ===");
        });
        
        Debug("ROUND", "=== SETUP COMPLETE ===");
    }
    
    /// <summary>
    /// Checks if the map has limited spawn points and enables respawn system if needed.
    /// </summary>
    private void CheckAndEnableRespawns()
    {
        var actualBots = CountBots();
        var aliveBots = CountAliveBots();
        var expectedBots = _state.BotCount;
        
        Debug("RESPAWN", $"Checking spawn points: actual={actualBots}, alive={aliveBots}, expected={expectedBots}");
        DebugBotStatus("CHECK RESPAWNS");
        
        // CRITICAL: Check if bots exist but are DEAD (joined too late to spawn)
        if (actualBots > 0 && aliveBots == 0)
        {
            Debug("RESPAWN", $"*** PROBLEM DETECTED: {actualBots} bots exist but ALL ARE DEAD! Bots joined too late to spawn. ***");
        }
        else if (aliveBots < actualBots)
        {
            Debug("RESPAWN", $"*** WARNING: {actualBots - aliveBots} bots are dead out of {actualBots} total ***");
        }
        
        // If fewer bots spawned than expected, enable respawn system
        if (actualBots < expectedBots)
        {
            // Total kills needed = expected bot count
            // Respawns needed = expected - actual (how many extra kills beyond initial bots)
            _state.TotalKillsNeeded = expectedBots;
            _state.RespawnsRemaining = expectedBots - actualBots;
            _state.CurrentKills = 0;
            _state.RespawnEnabled = true;
            
            Debug("RESPAWN", $"Limited spawn points detected! TotalKillsNeeded={_state.TotalKillsNeeded}, RespawnsRemaining={_state.RespawnsRemaining}");
            Server.ExecuteCommand("mp_respawn_on_death_ct 1");
            
            if (Config.ShowWaveStartMessages)
            {
                Server.PrintToChatAll(Localizer["Wave.LimitedSpawns", actualBots, expectedBots, _state.TotalKillsNeeded]);
            }
        }
        else
        {
            // All bots spawned, no respawns needed
            _state.TotalKillsNeeded = expectedBots;
            _state.RespawnsRemaining = 0;
            _state.CurrentKills = 0;
            _state.RespawnEnabled = false;
            Debug("RESPAWN", "All bots spawned, respawn system not needed");
        }
    }
    
    /// <summary>
    /// Spawns the specified number of bots.
    /// Uses bot_quota with fill mode - quota is total players (humans + bots).
    /// </summary>
    private void SpawnBots(int count)
    {
        Debug("SPAWN", $"=== SPAWNING {count} BOTS ===");
        DebugBotStatus("BEFORE SPAWN BOTS");
        
        // Calculate max bots based on server slots
        var humanCount = GetHumanPlayerCount();
        var maxBots = Math.Max(1, Server.MaxPlayers - humanCount - 1);
        var actualBotCount = Math.Min(count, maxBots);
        
        Debug("SPAWN", $"Humans={humanCount}, MaxSlots={Server.MaxPlayers}, MaxBots={maxBots}, RequestedBots={actualBotCount}");
        
        if (actualBotCount < count)
        {
            Server.PrintToChatAll(Localizer["Wave.BotsCapped", actualBotCount, count]);
        }
        
        // Check existing bots first
        var existingBots = CountBots();
        Debug("SPAWN", $"Existing CT bots: {existingBots}");
        
        // With fill mode, bot_quota is TOTAL players (humans + bots)
        // So we need: quota = humans + desired_bots
        var totalQuota = humanCount + actualBotCount;
        Debug("SPAWN", $"Setting bot_quota to {totalQuota} (humans={humanCount} + bots={actualBotCount})");
        
        // If we have too many bots, kick them first
        if (existingBots > actualBotCount)
        {
            var toKick = existingBots - actualBotCount;
            Debug("SPAWN", $"Kicking {toKick} extra bots");
            for (int i = 0; i < toKick; i++)
            {
                Server.ExecuteCommand("bot_kick ct");
            }
        }
        
        // Set bot quota - fill mode will add bots to reach total
        Server.ExecuteCommand("bot_join_team ct");
        Server.ExecuteCommand($"bot_quota {totalQuota}");
        
        Debug("SPAWN", "=== SPAWN COMPLETE ===");
        
        // Add a delayed check to see bot status after spawn commands execute
        AddTimer(0.5f, () =>
        {
            DebugBotStatus("AFTER SPAWN BOTS (0.5s delay)");
        });
    }
    
    /// <summary>
    /// Sets round time based on bot count.
    /// </summary>
    private void SetRoundTime(int botCount)
    {
        if (!Config.EnableDynamicRoundTime) return;
        
        var seconds = Config.BaseRoundTimeSeconds;
        if (botCount > Config.BotThresholdForExtraTime)
        {
            seconds += (botCount - Config.BotThresholdForExtraTime) * Config.ExtraSecondsPerBot;
        }
        
        var minutes = seconds / 60.0f;
        Debug("ROUND", $"RoundTime={seconds}s ({minutes:F2}min) for {botCount} bots");
        Server.ExecuteCommand($"mp_roundtime {minutes:F2}");
    }
    
    /// <summary>
    /// Handles wave victory (humans win).
    /// </summary>
    private void HandleVictory()
    {
        Debug("WAVE", "=== VICTORY ===");
        
        _state.ConsecutiveFailures = 0;
        
        // Increment bot count
        var increment = Math.Max(Config.MinimumWaveIncrement, _state.PlayersAtRoundStart);
        var oldCount = _state.BotCount;
        _state.BotCount += increment;
        
        Debug("WAVE", $"BotCount: {oldCount} + {increment} = {_state.BotCount}");
        
        if (Config.ShowWaveEndMessages)
        {
            Server.PrintToChatAll(Localizer["Wave.Victory", increment]);
        }
        
        // PRE-SPAWN BOTS: Prepare bots for next round so they spawn with it
        // Need to kick existing bots and set quota to new count
        Debug("WAVE", $"Pre-spawning {_state.BotCount} bots for next round...");
        Server.ExecuteCommand("bot_kick");
        var humanCount = GetHumanPlayerCount();
        var totalQuota = humanCount + _state.BotCount;
        Server.ExecuteCommand("bot_join_team ct");
        Server.ExecuteCommand($"bot_quota {totalQuota}");
        DebugBotStatus("AFTER VICTORY PRE-SPAWN");
    }
    
    /// <summary>
    /// Handles wave defeat (bots win).
    /// </summary>
    private void HandleDefeat()
    {
        Debug("WAVE", "=== DEFEAT ===");
        
        _state.ConsecutiveFailures++;
        Debug("WAVE", $"ConsecutiveFailures={_state.ConsecutiveFailures}/{Config.MaxFailuresBeforeReduction}");
        
        Server.PrintToChatAll(Localizer["Wave.Defeat", _state.ConsecutiveFailures, Config.MaxFailuresBeforeReduction]);
        
        // Reduce difficulty if too many failures
        if (_state.ConsecutiveFailures >= Config.MaxFailuresBeforeReduction)
        {
            var oldCount = _state.BotCount;
            var reduction = Math.Max(1, (int)(oldCount * Config.WaveReductionPercentage / 100.0));
            _state.BotCount = Math.Max(Config.MinimumBotsPerWave, oldCount - reduction);
            _state.ConsecutiveFailures = 0;
            
            Debug("WAVE", $"Reducing: {oldCount} - {reduction} = {_state.BotCount}");
            Server.PrintToChatAll(Localizer["Wave.Reduced", oldCount, _state.BotCount]);
        }
        
        // PRE-SPAWN BOTS: Prepare bots for next round so they spawn with it
        Debug("WAVE", $"Pre-spawning {_state.BotCount} bots for next round...");
        Server.ExecuteCommand("bot_kick");
        var humanCount = GetHumanPlayerCount();
        var totalQuota = humanCount + _state.BotCount;
        Server.ExecuteCommand("bot_join_team ct");
        Server.ExecuteCommand($"bot_quota {totalQuota}");
        DebugBotStatus("AFTER DEFEAT PRE-SPAWN");
    }

    // ============================================================
    // HELP TIMER
    // ============================================================
    
    /// <summary>
    /// Starts the periodic help message timer.
    /// </summary>
    private void StartHelpTimer()
    {
        if (!Config.ShowHelpMessages) return;
        
        _state.HelpTimer?.Kill();
        _state.HelpTimer = AddTimer(Config.HelpMessageIntervalSeconds, () =>
        {
            if (_state.IsActive) return;
            
            var count = GetHumanPlayerCount();
            if (count >= 1 && count <= Config.MaxPlayersAllowed)
            {
                Server.PrintToChatAll(Localizer["Wave.Help"]);
            }
        }, TimerFlags.REPEAT);
    }

    // ============================================================
    // UTILITY METHODS
    // ============================================================
    
    /// <summary>
    /// Checks if player is a valid human (not bot, not HLTV).
    /// </summary>
    private static bool IsValidHuman(CCSPlayerController? player)
    {
        return player is { IsValid: true, IsBot: false, IsHLTV: false };
    }
    
    /// <summary>
    /// Gets count of connected human players.
    /// </summary>
    private static int GetHumanPlayerCount()
    {
        return Utilities.GetPlayers()
            .Count(p => p is { IsValid: true, IsBot: false, IsHLTV: false } &&
                        p.Connected == PlayerConnectedState.PlayerConnected);
    }
    
    /// <summary>
    /// Counts bots on the bot team.
    /// </summary>
    private int CountBots()
    {
        return Utilities.GetPlayers()
            .Count(p => p is { IsValid: true, IsBot: true } && p.Team == _state.BotTeam);
    }
    
    /// <summary>
    /// Counts alive bots on the bot team.
    /// </summary>
    private int CountAliveBots()
    {
        return Utilities.GetPlayers()
            .Count(p => p is { IsValid: true, IsBot: true, PawnIsAlive: true } && p.Team == _state.BotTeam);
    }
    
    /// <summary>
    /// Logs detailed status of all bots including alive/dead state.
    /// </summary>
    private void DebugBotStatus(string context)
    {
        if (!Config.DebugMode) return;
        
        var allBots = Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: true }).ToList();
        var ctBots = allBots.Where(p => p.Team == _state.BotTeam).ToList();
        var aliveCt = ctBots.Count(p => p.PawnIsAlive);
        var deadCt = ctBots.Count(p => !p.PawnIsAlive);
        
        Debug("BOTSTATUS", $"=== {context} ===");
        Debug("BOTSTATUS", $"Total bots: {allBots.Count}, CT bots: {ctBots.Count}, Alive: {aliveCt}, Dead: {deadCt}");
        
        foreach (var bot in ctBots)
        {
            var pawn = bot.PlayerPawn?.Value;
            var health = pawn?.Health ?? 0;
            var lifeState = pawn?.LifeState ?? 0;
            Debug("BOTSTATUS", $"  [{(bot.PawnIsAlive ? "ALIVE" : "DEAD ")}] {bot.PlayerName} - Team={bot.Team}, Health={health}, LifeState={lifeState}");
        }
        
        Debug("BOTSTATUS", $"=== END {context} ===");
    }
    
    /// <summary>
    /// Strips all weapons from a bot except their knife.
    /// Used in Easy mode to make bots knife-only.
    /// </summary>
    private void StripBotWeapons(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn?.Value;
        if (pawn?.WeaponServices?.MyWeapons is null) return;
        
        Debug("WEAPON", $"Stripping weapons from {player.PlayerName}");
        
        foreach (var weaponHandle in pawn.WeaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Value;
            if (weapon is null || !weapon.IsValid) continue;
            
            // Keep knives
            if (weapon.DesignerName.Contains("knife"))
            {
                continue;
            }
            
            // Remove everything else
            Debug("WEAPON", $"  Removing: {weapon.DesignerName}");
            weapon.Remove();
        }
    }
}

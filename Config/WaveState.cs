using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace BotWaves;

// Alias to avoid ambiguity with System.Threading.Timer
using CssTimer = CounterStrikeSharp.API.Modules.Timers.Timer;

/// <summary>
/// Centralized state management for Bot Waves plugin.
/// Keeps all runtime state in one place for easy debugging and reset.
/// </summary>
public sealed class WaveState
{
    // ============================================================
    // WAVE MODE STATE
    // ============================================================
    
    /// <summary>Is wave mode currently active?</summary>
    public bool IsActive { get; set; }
    
    /// <summary>Current number of bots to spawn each round.</summary>
    public int BotCount { get; set; } = 1;
    
    /// <summary>True for first round after activation (skip round end processing).</summary>
    public bool JustActivated { get; set; }
    
    /// <summary>Was wave mode started with admin password override?</summary>
    public bool StartedWithOverride { get; set; }
    
    /// <summary>Is a round currently in progress?</summary>
    public bool IsRoundActive { get; set; }
    
    // ============================================================
    // DIFFICULTY STATE
    // ============================================================
    
    /// <summary>True = Easy (knives only), False = Hard (guns).</summary>
    public bool IsEasyMode { get; set; } = true;
    
    /// <summary>How many times players failed the current wave in a row.</summary>
    public int ConsecutiveFailures { get; set; }
    
    // ============================================================
    // RESPAWN TRACKING (for maps with limited spawn points)
    // ============================================================
    
    /// <summary>Number of bot respawns remaining for this wave.</summary>
    public int RespawnsRemaining { get; set; }
    
    /// <summary>Is the respawn system currently active for this round?</summary>
    public bool RespawnEnabled { get; set; }
    
    /// <summary>Total kills needed to win the wave (used when respawns are active).</summary>
    public int TotalKillsNeeded { get; set; }
    
    /// <summary>Current kill count this round.</summary>
    public int CurrentKills { get; set; }
    
    // ============================================================
    // PLAYER TRACKING
    // ============================================================
    
    /// <summary>Number of human players when round started (for wave increment).</summary>
    public int PlayersAtRoundStart { get; set; }
    
    /// <summary>Players who have voted for wave mode toggle.</summary>
    public HashSet<ulong> Voters { get; } = new();
    
    // ============================================================
    // TEAM CONFIGURATION
    // ============================================================
    
    /// <summary>Team humans play on (Terrorist by default).</summary>
    public CsTeam HumanTeam { get; } = CsTeam.Terrorist;
    
    /// <summary>Team bots play on (CT by default).</summary>
    public CsTeam BotTeam { get; } = CsTeam.CounterTerrorist;
    
    // ============================================================
    // TIMERS
    // ============================================================
    
    /// <summary>Timer for periodic help messages when wave mode is inactive.</summary>
    public CssTimer? HelpTimer { get; set; }
    
    /// <summary>Timer for spawn check after round starts.</summary>
    public CssTimer? SpawnCheckTimer { get; set; }
    
    // ============================================================
    // METHODS
    // ============================================================
    
    /// <summary>
    /// Resets all state to defaults. Called when wave mode is disabled.
    /// </summary>
    public void Reset()
    {
        IsActive = false;
        BotCount = 1;
        JustActivated = false;
        StartedWithOverride = false;
        IsRoundActive = false;
        IsEasyMode = true;
        ConsecutiveFailures = 0;
        PlayersAtRoundStart = 0;
        RespawnsRemaining = 0;
        RespawnEnabled = false;
        TotalKillsNeeded = 0;
        CurrentKills = 0;
        Voters.Clear();
    }
    
    /// <summary>
    /// Initializes state for a new wave mode session.
    /// </summary>
    public void Initialize(int startBotCount, bool adminOverride)
    {
        IsActive = true;
        BotCount = startBotCount;
        JustActivated = true;
        StartedWithOverride = adminOverride;
        IsEasyMode = true;
        ConsecutiveFailures = 0;
        RespawnsRemaining = 0;
        RespawnEnabled = false;
        TotalKillsNeeded = 0;
        CurrentKills = 0;
        Voters.Clear();
    }
    
    /// <summary>
    /// Resets per-round state. Called at the start of each round.
    /// </summary>
    public void ResetRound()
    {
        RespawnsRemaining = 0;
        RespawnEnabled = false;
        TotalKillsNeeded = 0;
        CurrentKills = 0;
    }
    
    /// <summary>
    /// Kills all active timers safely.
    /// </summary>
    public void KillAllTimers()
    {
        HelpTimer?.Kill();
        HelpTimer = null;
        
        SpawnCheckTimer?.Kill();
        SpawnCheckTimer = null;
    }
    
    /// <summary>
    /// Returns a debug string with current state values.
    /// </summary>
    public string ToDebugString()
    {
        return $"IsActive={IsActive}, BotCount={BotCount}, IsEasyMode={IsEasyMode}, " +
               $"JustActivated={JustActivated}, IsRoundActive={IsRoundActive}, " +
               $"ConsecutiveFailures={ConsecutiveFailures}, PlayersAtRoundStart={PlayersAtRoundStart}, " +
               $"RespawnsRemaining={RespawnsRemaining}, RespawnEnabled={RespawnEnabled}, " +
               $"TotalKillsNeeded={TotalKillsNeeded}, CurrentKills={CurrentKills}, " +
               $"Voters={Voters.Count}, StartedWithOverride={StartedWithOverride}";
    }
}

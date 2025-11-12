using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Bot_Quota_GoldKingZ;

public class Globals
{
    // Timers
    public CounterStrikeSharp.API.Modules.Timers.Timer? BotSpawnTimer;
    
    // Wave mode state
    public bool isWaveModeActive = false;
    public int currentWaveBotCount = 1;
    public bool waveModeJustActivated = false; // True when wave mode was just enabled, becomes false after first round starts
    public bool waveStartedWithOverride = false; // Track if password override was used
    
    // Store the player count from round start to use for wave increment calculation
    public int humanPlayerCountAtRoundStart = 0;
    
    // Auto-respawn system (using mp_respawn_on_death_ct)
    public bool autoRespawnEnabled = false;
    public int respawnsNeeded = 0;
    public int respawnsUsed = 0;

    // Store original server cvar values to restore later
    public Dictionary<string, string> savedCvars = new Dictionary<string, string>();
    
    // Team assignments
    public CsTeam humanTeam = CsTeam.Terrorist;
    public CsTeam botTeam = CsTeam.CounterTerrorist;
    
    // Track players who joined mid-round and need to be moved at round start
    public HashSet<ulong> playersToMoveNextRound = new HashSet<ulong>();
    
    // Track players who have been properly assigned to their team (prevents unnecessary ChangeTeam calls)
    public HashSet<ulong> playersAssignedToTeam = new HashSet<ulong>();
    
    // Track if we're currently in a round (between round start and round end)
    public bool isRoundActive = false;
    
    // Track pending wave change (when changing wave mid-round)
    public int pendingWaveChange = -1; // -1 means no pending change
    
    // Credit and perk system
    public Dictionary<ulong, int> playerCredits = new Dictionary<ulong, int>();
    public Dictionary<ulong, PlayerPerks> playerPerks = new Dictionary<ulong, PlayerPerks>();
}

public class PlayerPerks
{
    public bool HasArmor { get; set; } = false;
    public bool HasSpeed { get; set; } = false;
    public int RespawnsAvailable { get; set; } = 0;
    public int FreezeBombsAvailable { get; set; } = 0;
}
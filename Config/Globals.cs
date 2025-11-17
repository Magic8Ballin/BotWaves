using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace BotWaves;

public class Globals
{
    // Timers
    public CounterStrikeSharp.API.Modules.Timers.Timer? _botSpawnTimer;
    public CounterStrikeSharp.API.Modules.Timers.Timer? _helpMessageTimer;
    
    // Wave mode state
    public bool isWaveModeActive = false;
    public bool isZombieModeActive = false; // Track if zombie mode is enabled
    public int currentWaveBotCount = 1;
    public bool waveModeJustActivated = false; // True when wave mode was just enabled, becomes false after first round starts
    public bool waveStartedWithOverride = false; // Track if password override was used
    
    // Bot difficulty tracking
    public int currentBotDifficulty = 3; // Default: Normal (3)
    
    // Store the player count from round start to use for wave increment calculation
    public int humanPlayerCountAtRoundStart = 0;
 
    // Wave failure and difficulty reduction system
    public int consecutiveWaveFailures = 0; // Track how many times current wave has been failed in a row

    // Auto-respawn system (using mp_respawn_on_death_ct)
    public bool autoRespawnEnabled = false;
    public int respawnsNeeded = 0;
    public int respawnsUsed = 0;

  // Store original server cvar values to restore later
    public Dictionary<string, string> savedCvars = new Dictionary<string, string>();
 
    // Team assignments
    public CsTeam humanTeam = CsTeam.Terrorist;
    public CsTeam botTeam = CsTeam.CounterTerrorist;
  
    // Track players who have been properly assigned to their team (prevents unnecessary ChangeTeam calls)
    public HashSet<ulong> playersAssignedToTeam = new HashSet<ulong>();
 
    // Track if we're currently in a round (between round start and round end)
    public bool isRoundActive = false;
    
    // Kill tracking for round statistics (SteamID -> kill count)
    public Dictionary<ulong, int> roundKills = new Dictionary<ulong, int>();
  // Track which players participated in the current round
    public HashSet<ulong> roundParticipants = new HashSet<ulong>();
  
    // Wave mode voting system
  public HashSet<ulong> waveVoteParticipants = new HashSet<ulong>();
}
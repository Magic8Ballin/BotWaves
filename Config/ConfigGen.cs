using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace BotWaves;

/// <summary>
/// Configuration for the Bot Waves plugin.
/// All settings are loaded from the plugin's config JSON file.
/// </summary>
public sealed class ConfigGen : BasePluginConfig
{
    // ============================================================
    // DEBUG SETTINGS
    // ============================================================
    
    /// <summary>
    /// Enable verbose debug logging to server console.
    /// Set to true when diagnosing issues, false for production.
    /// </summary>
    [JsonPropertyName("DebugMode")]
    public bool DebugMode { get; set; } = true;

    // ============================================================
    // ACCESS CONTROL
    // ============================================================
    
    /// <summary>
    /// Maximum players allowed before wave mode is restricted.
    /// Players beyond this limit need admin password to start waves.
    /// </summary>
    [JsonPropertyName("MaxPlayersAllowed")]
    public int MaxPlayersAllowed { get; set; } = 5;
    
    /// <summary>
    /// Password to bypass player limit restriction.
    /// Usage: !wave [botcount] [password]
    /// </summary>
    [JsonPropertyName("AdminPassword")]
    public string AdminPassword { get; set; } = "glove";
    
    /// <summary>
    /// If true, automatically disable wave mode when player count exceeds MaxPlayersAllowed.
    /// Only applies if wave wasn't started with admin override.
    /// </summary>
    [JsonPropertyName("DisableOnPlayerLimitExceeded")]
    public bool DisableOnPlayerLimitExceeded { get; set; } = true;

    // ============================================================
    // VOTING SETTINGS
    // ============================================================
    
    /// <summary>
    /// Percentage of players needed to vote for wave mode toggle (0.0 - 1.0).
    /// Example: 0.5 = 50% of players must vote.
    /// Solo players always get instant toggle.
    /// </summary>
    [JsonPropertyName("VoteThreshold")]
    public float VoteThreshold { get; set; } = 0.5f;

    // ============================================================
    // WAVE PROGRESSION
    // ============================================================
    
    /// <summary>
    /// Minimum number of bots to add after a wave victory.
    /// Actual increment = max(MinimumWaveIncrement, playerCount).
    /// </summary>
    [JsonPropertyName("MinimumWaveIncrement")]
    public int MinimumWaveIncrement { get; set; } = 1;
    
    /// <summary>
    /// Minimum bots per wave (floor for difficulty reduction).
    /// Wave count will never go below this value.
    /// </summary>
    [JsonPropertyName("MinimumBotsPerWave")]
    public int MinimumBotsPerWave { get; set; } = 1;
    
    /// <summary>
    /// Number of consecutive wave failures before reducing difficulty.
    /// </summary>
    [JsonPropertyName("MaxFailuresBeforeReduction")]
    public int MaxFailuresBeforeReduction { get; set; } = 7;
    
    /// <summary>
    /// Percentage of bots to remove when difficulty is reduced (1-100).
    /// </summary>
    [JsonPropertyName("WaveReductionPercentage")]
    public int WaveReductionPercentage { get; set; } = 10;

    // ============================================================
    // ROUND TIME SETTINGS
    // ============================================================
    
    /// <summary>
    /// Enable dynamic round time based on bot count.
    /// </summary>
    [JsonPropertyName("EnableDynamicRoundTime")]
    public bool EnableDynamicRoundTime { get; set; } = true;
    
    /// <summary>
    /// Base round time in seconds for small waves.
    /// </summary>
    [JsonPropertyName("BaseRoundTimeSeconds")]
    public int BaseRoundTimeSeconds { get; set; } = 40;
    
    /// <summary>
    /// Bot count threshold before adding extra time per bot.
    /// </summary>
    [JsonPropertyName("BotThresholdForExtraTime")]
    public int BotThresholdForExtraTime { get; set; } = 10;
    
    /// <summary>
    /// Extra seconds to add per bot above the threshold.
    /// </summary>
    [JsonPropertyName("ExtraSecondsPerBot")]
    public int ExtraSecondsPerBot { get; set; } = 3;

    // ============================================================
    // BOT SPAWN SETTINGS
    // ============================================================
    
    /// <summary>
    /// Delay in seconds before checking spawn count and enabling round win conditions.
    /// Allows time for bots to spawn before round can end.
    /// </summary>
    [JsonPropertyName("SpawnCheckDelay")]
    public float SpawnCheckDelay { get; set; } = 1.0f;

    // ============================================================
    // CHAT MESSAGE SETTINGS
    // ============================================================
    
    /// <summary>
    /// Show welcome messages to new players about Bot Waves mode.
    /// Only shown when 1-5 players are on the server and wave mode is inactive.
    /// </summary>
    [JsonPropertyName("ShowWelcomeMessages")]
    public bool ShowWelcomeMessages { get; set; } = true;
    
    /// <summary>
    /// Delay in seconds before showing the first welcome message after a player joins.
    /// </summary>
    [JsonPropertyName("WelcomeMessageDelaySeconds")]
    public float WelcomeMessageDelaySeconds { get; set; } = 5.0f;
    
    /// <summary>
    /// Interval in seconds between repeating welcome messages.
    /// </summary>
    [JsonPropertyName("WelcomeMessageRepeatSeconds")]
    public float WelcomeMessageRepeatSeconds { get; set; } = 90.0f;
    
    /// <summary>
    /// Show wave start messages (bot count, difficulty).
    /// </summary>
    [JsonPropertyName("ShowWaveStartMessages")]
    public bool ShowWaveStartMessages { get; set; } = true;
    
    /// <summary>
    /// Show wave end messages (win/lose).
    /// </summary>
    [JsonPropertyName("ShowWaveEndMessages")]
    public bool ShowWaveEndMessages { get; set; } = true;
    
    /// <summary>
    /// Show periodic help messages when wave mode is inactive.
    /// </summary>
    [JsonPropertyName("ShowHelpMessages")]
    public bool ShowHelpMessages { get; set; } = true;
    
    /// <summary>
    /// Interval in seconds between help messages.
    /// </summary>
    [JsonPropertyName("HelpMessageIntervalSeconds")]
    public int HelpMessageIntervalSeconds { get; set; } = 60;

    // ============================================================
    // THIRD-PARTY PLUGIN SETTINGS
    // ============================================================
    
    /// <summary>
    /// Disable the Skill Auto Balance plugin during wave mode.
    /// Sets css_skill_autobalance_minplayers to 30.
    /// </summary>
    [JsonPropertyName("DisableSkillAutoBalance")]
    public bool DisableSkillAutoBalance { get; set; } = true;

    // ============================================================
    // TIMING SETTINGS
    // ============================================================
    
    /// <summary>
    /// Delay before checking vote threshold after a player disconnects.
    /// </summary>
    [JsonPropertyName("DisconnectCheckDelay")]
    public float DisconnectCheckDelay { get; set; } = 0.5f;
}

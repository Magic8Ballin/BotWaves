using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace BotWaves;

public class ConfigGen : BasePluginConfig
{
    // ===== Wave Mode Settings =====
    [JsonPropertyName("MaxPlayersWithoutPassword")]
    public int MaxPlayersWithoutPassword { get; set; } = 5;
    
    [JsonPropertyName("AdminPasswordOverride")]
    public string AdminPasswordOverride { get; set; } = "glove";
    
    [JsonPropertyName("DisableWaveOnFifthPlayer")]
    public bool DisableWaveOnFifthPlayer { get; set; } = true;
    
    [JsonPropertyName("DisableSkillAutoBalanceInWaveMode")]
    public bool DisableSkillAutoBalanceInWaveMode { get; set; } = true;
    
    [JsonPropertyName("DefaultZombieMode")]
    public bool DefaultZombieMode { get; set; } = true;
    
    // ===== Wave Voting Settings =====
    [JsonPropertyName("WaveVoteThreshold")]
    public float WaveVoteThreshold { get; set; } = 0.5f; // 50% of players needed to vote
  
    // ===== Round Time Settings =====
    [JsonPropertyName("EnableDynamicRoundTime")]
    public bool EnableDynamicRoundTime { get; set; } = true;
    
    [JsonPropertyName("BaseRoundTimeSeconds")]
    public int BaseRoundTimeSeconds { get; set; } = 40;
    
    [JsonPropertyName("RoundTimeIncrementPerBot")]
    public int RoundTimeIncrementPerBot { get; set; } = 3;
  
    [JsonPropertyName("WaveThresholdForTimeIncrease")]
    public int WaveThresholdForTimeIncrease { get; set; } = 10;
    
    // ===== Bot Spawn Settings =====
    [JsonPropertyName("SpawnLimitCheckDelay")]
    public float SpawnLimitCheckDelay { get; set; } = 1.0f;
  
    [JsonPropertyName("BotDifficultyChangeDelay")]
    public float BotDifficultyChangeDelay { get; set; } = 0.15f;
    
    // ===== Wave Scaling Settings =====
    [JsonPropertyName("MinimumWaveIncrement")]
    public int MinimumWaveIncrement { get; set; } = 1;

    [JsonPropertyName("MinimumBotsPerWave")]
    public int MinimumBotsPerWave { get; set; } = 1;
    
    [JsonPropertyName("MaxFailuresBeforeReduction")]
    public int MaxFailuresBeforeReduction { get; set; } = 7; // Number of consecutive failures before reducing wave difficulty
    
    [JsonPropertyName("WaveReductionPercentage")]
    public int WaveReductionPercentage { get; set; } = 10; // Percentage of bots to remove on reduction (1-100)
    
    // ===== Respawn System Settings =====
    [JsonPropertyName("EnableAutoRespawn")]
    public bool EnableAutoRespawn { get; set; } = true;
 
    [JsonPropertyName("ShowRespawnMessages")]
    public bool ShowRespawnMessages { get; set; } = true;
    
  [JsonPropertyName("ShowRespawnEveryXDeaths")]
    public int ShowRespawnEveryXDeaths { get; set; } = 1;
  
  // ===== Chat Messages Settings =====
    [JsonPropertyName("ShowWaveStartMessages")]
    public bool ShowWaveStartMessages { get; set; } = true;
    
    [JsonPropertyName("ShowWaveEndMessages")]
    public bool ShowWaveEndMessages { get; set; } = true;
    
    [JsonPropertyName("ShowHelpMessages")]
    public bool ShowHelpMessages { get; set; } = true;
    
    [JsonPropertyName("HelpMessageInterval")]
    public int HelpMessageInterval { get; set; } = 60; // Seconds between help messages
    
    // ===== Bot Quota Toggle Settings =====
    [JsonPropertyName("EnableBotQuotaToggle")]
    public bool EnableBotQuotaToggle { get; set; } = true;
    
    [JsonPropertyName("BotQuotaNormal")]
    public int BotQuotaNormal { get; set; } = 2;
    
    [JsonPropertyName("BotQuotaDisabled")]
    public int BotQuotaDisabled { get; set; } = 0;
    
    // ===== Server Protection Settings =====
    [JsonPropertyName("SaveServerCvars")]
  public bool SaveServerCvars { get; set; } = true;

    [JsonPropertyName("RestoreCvarsOnDisable")]
    public bool RestoreCvarsOnDisable { get; set; } = true;
    
    // ===== Player Management Settings =====
    [JsonPropertyName("DisconnectCheckDelay")]
    public float DisconnectCheckDelay { get; set; } = 0.5f;
}

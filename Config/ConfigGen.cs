using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace BotWaves;

public class ConfigGen : BasePluginConfig
{
    // ===== Wave Mode Settings =====
    [JsonPropertyName("MaxPlayersWithoutPassword")]
    public int MaxPlayersWithoutPassword { get; set; } = 4;
    
    [JsonPropertyName("AdminPasswordOverride")]
    public string AdminPasswordOverride { get; set; } = "glove";
    
    [JsonPropertyName("DisableWaveOnFifthPlayer")]
    public bool DisableWaveOnFifthPlayer { get; set; } = true;
  
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
    
    // ===== Wave Scaling Settings =====
    [JsonPropertyName("EnableDynamicScaling")]
    public bool EnableDynamicScaling { get; set; } = true;
    
    [JsonPropertyName("MinimumWaveIncrement")]
    public int MinimumWaveIncrement { get; set; } = 1;
    
    // ===== Respawn System Settings =====
    [JsonPropertyName("EnableAutoRespawn")]
    public bool EnableAutoRespawn { get; set; } = true;
    
    [JsonPropertyName("ShowRespawnMessages")]
    public bool ShowRespawnMessages { get; set; } = true;
    
    [JsonPropertyName("ShowRespawnEveryXDeaths")]
    public int ShowRespawnEveryXDeaths { get; set; } = 5;
    
    // ===== Chat Messages Settings =====
    [JsonPropertyName("ShowWaveStartMessages")]
    public bool ShowWaveStartMessages { get; set; } = true;
    
    [JsonPropertyName("ShowWaveEndMessages")]
    public bool ShowWaveEndMessages { get; set; } = true;
    
    [JsonPropertyName("ShowHelpMessages")]
    public bool ShowHelpMessages { get; set; } = true;
    
    // ===== Server Protection Settings =====
    [JsonPropertyName("SaveServerCvars")]
    public bool SaveServerCvars { get; set; } = true;
    
    [JsonPropertyName("RestoreCvarsOnDisable")]
    public bool RestoreCvarsOnDisable { get; set; } = true;
    
    // ===== Debug Settings =====
    [JsonPropertyName("EnableDebugMode")]
    public bool EnableDebugMode { get; set; } = false;
    
    [JsonPropertyName("LogBotSpawns")]
    public bool LogBotSpawns { get; set; } = true;
    
    [JsonPropertyName("LogTeamChanges")]
    public bool LogTeamChanges { get; set; } = false;
    
    [JsonPropertyName("LogRoundEvents")]
    public bool LogRoundEvents { get; set; } = true;
}

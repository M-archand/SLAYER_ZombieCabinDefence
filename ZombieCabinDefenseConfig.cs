// Decompiled with JetBrains decompiler
// Type: ZombieCabinDefense.ZombieCabinDefenseConfig
// Assembly: SLAYER_ZombieCabinDefence, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 69087733-7F77-4F16-803C-6A7C967EABF2
// Assembly location: C:\Users\alexr\Desktop\SLAYER_ZombieCabinDefence.dll

using CounterStrikeSharp.API.Core;
using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable enable
namespace ZombieCabinDefense
{
  public class ZombieCabinDefenseConfig : BasePluginConfig
  {
    [JsonPropertyName("ZCD_PluginEnabled")]
    public bool ZCD_PluginEnabled { get; set; } = true;

    [JsonPropertyName("ZCD_HudText")]
    public bool ZCD_HudText { get; set; } = true;

    [JsonPropertyName("ZCD_NoBlock")]
    public bool ZCD_NoBlock { get; set; } = true;

    [JsonPropertyName("ZCD_Freeze")]
    public float ZCD_Freeze { get; set; } = 10f;

    [JsonPropertyName("ZCD_IncreaseHealth")]
    public int ZCD_IncreaseHealth { get; set; } = 1;

    [JsonPropertyName("ZCD_DecreaseHealth")]
    public int ZCD_DecreaseHealth { get; set; } = 2;

    [JsonPropertyName("ZCD_TimeBetweenNextWave")]
    public float ZCD_TimeBetweenNextWave { get; set; } = 30f;

    [JsonPropertyName("ZCD_ZombieSpawnMin")]
    public int ZCD_ZombieSpawnMin { get; set; } = 5;

    [JsonPropertyName("ZCD_ZombieSpawnMax")]
    public int ZCD_ZombieSpawnMax { get; set; } = 50;

    [JsonPropertyName("ZCD_ZombieIncreaseRate")]
    public int ZCD_ZombieIncreaseRate { get; set; } = 2;

    [JsonPropertyName("ZCD_HumanMax")]
    public int ZCD_HumanMax { get; set; } = 5;

    [JsonPropertyName("ZCD_KillZombies")]
    public int ZCD_KillZombies { get; set; } = 30;

    [JsonPropertyName("ZCD_IncrementHealthBoostBy")]
    public int ZCD_IncrementHealthBoostBy { get; set; } = 5;

    [JsonPropertyName("ZCD_IncrementZKillCountBy")]
    public int ZCD_IncrementZKillCountBy { get; set; } = 5;

    [JsonPropertyName("ZCD_AdminFlagToUseCMDs")]
    public string ZCD_AdminFlagToUseCMDs { get; set; } = "@css/root";

    [JsonPropertyName("ZCD_Waves")]
    public List<ZombieCabinDefenseSettings> ZCD_Waves { get; set; } = new List<ZombieCabinDefenseSettings>();

    [JsonPropertyName("ZCD_Zombies")]
    public List<ZombieCabinDefenseZombieSettings> ZCD_Zombies { get; set; } = new List<ZombieCabinDefenseZombieSettings>();
  }
}

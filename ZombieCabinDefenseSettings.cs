// Decompiled with JetBrains decompiler
// Type: ZombieCabinDefense.ZombieCabinDefenseSettings
// Assembly: SLAYER_ZombieCabinDefence, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 69087733-7F77-4F16-803C-6A7C967EABF2
// Assembly location: C:\Users\alexr\Desktop\SLAYER_ZombieCabinDefence.dll

using System.Text.Json.Serialization;

#nullable enable
namespace ZombieCabinDefense
{
  public class ZombieCabinDefenseSettings
  {
    [JsonPropertyName("WaveName")]
    public string WaveName { get; set; } = "Outbreak";

    [JsonPropertyName("ZWaves")]
    public int ZWaves { get; set; } = 15;

    [JsonPropertyName("ZKillCount")]
    public int ZKillCount { get; set; } = 15;

    [JsonPropertyName("ZHealthBoost")]
    public int ZHealthBoost { get; set; } = 0;

    [JsonPropertyName("ZRespawnTime")]
    public float ZRespawnTime { get; set; } = 5f;
  }
}

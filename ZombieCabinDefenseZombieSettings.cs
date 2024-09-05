// Decompiled with JetBrains decompiler
// Type: ZombieCabinDefense.ZombieCabinDefenseZombieSettings
// Assembly: SLAYER_ZombieCabinDefence, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 69087733-7F77-4F16-803C-6A7C967EABF2
// Assembly location: C:\Users\alexr\Desktop\SLAYER_ZombieCabinDefence.dll

using System.Text.Json.Serialization;

#nullable enable
namespace ZombieCabinDefense
{
  public class ZombieCabinDefenseZombieSettings
  {
    [JsonPropertyName("ZombieClassName")]
    public string ZombieClassName { get; set; } = "NormalZombie";

    [JsonPropertyName("ZombieModelPath")]
    public string ZombieModelPath { get; set; } = "";

    [JsonPropertyName("ZombieInWaves")]
    public string ZombieInWaves { get; set; } = "Outbreak";

    [JsonPropertyName("ZombieHealth")]
    public int ZombieHealth { get; set; } = 100;

    [JsonPropertyName("ZombieSpeed")]
    public float ZombieSpeed { get; set; } = 1.1f;

    [JsonPropertyName("ZombieGravity")]
    public float ZombieGravity { get; set; } = 0.9f;

    [JsonPropertyName("ZombieJump")]
    public float ZombieJump { get; set; } = 15f;

    [JsonPropertyName("ZombieFOV")]
    public int ZombieFOV { get; set; } = 110;
  }
}

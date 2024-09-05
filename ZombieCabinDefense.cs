// Decompiled with JetBrains decompiler
// Type: ZombieCabinDefense.ZombieCabinDefense
// Assembly: SLAYER_ZombieCabinDefence, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 69087733-7F77-4F16-803C-6A7C967EABF2
// Assembly location: C:\Users\alexr\Desktop\SLAYER_ZombieCabinDefence.dll

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable enable
namespace ZombieCabinDefense
{
  [RequiredMember]
  public class ZombieCabinDefense : BasePlugin, IPluginConfig<ZombieCabinDefenseConfig>
  {
    public int gZombiesKilled = 0;
    public int gZombieToSpawn = 1;
    public int gCurrentWaveDifficulty = 0;
    public int gCurrentWave = 0;
    public int gIncrementedHealthBoost = 0;
    public int gIncrementedZKillCount = 0;
    public float gNextWaveTime = 0.0f;
    public int[] gZombieID = new int[64];
    public int[] gPlayerZombieKilled = new int[64];
    public float[] gRespawnTime = new float[64];
    public bool[] IsPlayerZombie = new bool[64];
    public bool gShouldStartGame = false;
    public Timer? t_ZFreeze;
    public Timer? t_NextWave;
    public Timer? t_CheckPlayerLocation;
    public Timer[]? tRespawn = new Timer[64];
    public static string RespawnWindowsSig = new string((ReadOnlySpan<char>) GameData.GetSignature("CBasePlayerController_SetPawn"));
    public static string RespawnLinuxSig = new string((ReadOnlySpan<char>) GameData.GetSignature("CBasePlayerController_SetPawn"));
    public static MemoryFunctionVoid<CCSPlayerController, CCSPlayerPawn, bool, bool> CBasePlayerController_SetPawnFunc = new MemoryFunctionVoid<CCSPlayerController, CCSPlayerPawn, bool, bool>(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? ZombieCabinDefense.ZombieCabinDefense.RespawnLinuxSig : ZombieCabinDefense.ZombieCabinDefense.RespawnWindowsSig);
    public Action<IntPtr, float, RoundEndReason, IntPtr, uint> TerminateRoundWindows = new Action<IntPtr, float, RoundEndReason, IntPtr, uint>(ZombieCabinDefense.ZombieCabinDefense.TerminateRoundWindowsFunc.Invoke);
    public static MemoryFunctionVoid<IntPtr, float, RoundEndReason, IntPtr, uint> TerminateRoundWindowsFunc = new MemoryFunctionVoid<IntPtr, float, RoundEndReason, IntPtr, uint>(GameData.GetSignature("CCSGameRules_TerminateRound"));
    public static MemoryFunctionVoid<IntPtr, RoundEndReason, IntPtr, uint, float> TerminateRoundLinuxFunc = new MemoryFunctionVoid<IntPtr, RoundEndReason, IntPtr, uint, float>("55 48 89 E5 41 57 41 56 41 55 41 54 49 89 FC 53 48 81 EC ? ? ? ? 48 8D 05 ? ? ? ? F3 0F 11 85");
    public Action<IntPtr, RoundEndReason, IntPtr, uint, float> TerminateRoundLinux = new Action<IntPtr, RoundEndReason, IntPtr, uint, float>(ZombieCabinDefense.ZombieCabinDefense.TerminateRoundLinuxFunc.Invoke);
    private static readonly bool IsWindowsPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public override string ModuleName => "Zombie Cabin Defense";

    public override string ModuleVersion => "1.0";

    public override string ModuleAuthor => "SLAYER";

    public override string ModuleDescription
    {
      get => "Humans stick together to fight off zombie attacks on Cabin";
    }

    [RequiredMember]
    public ZombieCabinDefenseConfig Config { get; set; }

    public void OnConfigParsed(ZombieCabinDefenseConfig config) => this.Config = config;

    public ZombieCabinDefenseSettings GetWaveByName(string modeName, StringComparer comparer)
    {
      return this.Config.ZCD_Waves.FirstOrDefault<ZombieCabinDefenseSettings>((Func<ZombieCabinDefenseSettings, bool>) (mode => comparer.Equals(mode.WaveName, modeName)));
    }

    public ZombieCabinDefenseSettings GetWaveByIndex(int wave)
    {
      return this.Config.ZCD_Waves.ElementAt<ZombieCabinDefenseSettings>(wave);
    }

    public ZombieCabinDefenseZombieSettings GetZombieClassByName(
      string ClassName,
      StringComparer comparer)
    {
      return this.Config.ZCD_Zombies.FirstOrDefault<ZombieCabinDefenseZombieSettings>((Func<ZombieCabinDefenseZombieSettings, bool>) (mode => comparer.Equals(mode.ZombieClassName, ClassName)));
    }

    public ZombieCabinDefenseZombieSettings GetZombieByIndex(int Zombie)
    {
      return this.Config.ZCD_Zombies.ElementAt<ZombieCabinDefenseZombieSettings>(Zombie);
    }

    public int GetZombieClassIndexByName(string ClassName)
    {
      int classIndexByName = 0;
      foreach (ZombieCabinDefenseZombieSettings zcdZomby in this.Config.ZCD_Zombies)
      {
        if (zcdZomby.ZombieClassName == ClassName)
          return classIndexByName;
        ++classIndexByName;
      }
      return -1;
    }

    public override void Unload(bool hotReload) => this.ZCDEnd();

    public override void Load(bool hotReload)
    {
      this.AddCommand("css_startzombies", "Sets the certain zombie wave", new CommandInfo.CommandCallback(this.CMD_StartGame));
      this.AddCommand("css_restartzombies", "Sets the certain zombie wave", new CommandInfo.CommandCallback(this.CMD_StartGame));
      if (this.Config.ZCD_PluginEnabled)
      {
        this.gCurrentWaveDifficulty = 0;
        this.gCurrentWave = 0;
        this.gIncrementedHealthBoost = 0;
        this.gIncrementedZKillCount = 0;
        this.gZombieToSpawn = this.Config.ZCD_ZombieSpawnMin < 1 ? 1 : this.Config.ZCD_ZombieSpawnMin;
        Server.ExecuteCommand("bot_kick");
        Server.ExecuteCommand("mp_ignore_round_win_conditions 1");
        Server.ExecuteCommand("mp_autoteambalance 0");
        Server.ExecuteCommand("mp_limitteams 0");
      }
      this.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnMapStart>((CounterStrikeSharp.API.Core.Listeners.OnMapStart) (mapName =>
      {
        if (!this.Config.ZCD_PluginEnabled)
          return;
        this.gCurrentWaveDifficulty = 0;
        this.gCurrentWave = 0;
        this.gIncrementedHealthBoost = 0;
        this.gIncrementedZKillCount = 0;
        this.gZombieToSpawn = this.Config.ZCD_ZombieSpawnMin < 1 ? 1 : this.Config.ZCD_ZombieSpawnMin;
        Server.ExecuteCommand("bot_kick");
        Server.ExecuteCommand("mp_ignore_round_win_conditions 1");
        Server.ExecuteCommand("mp_autoteambalance 0");
        Server.ExecuteCommand("mp_limitteams 0");
      }));
      this.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnTick>((CounterStrikeSharp.API.Core.Listeners.OnTick) (() =>
      {
        if (!this.Config.ZCD_PluginEnabled || !this.gShouldStartGame)
          return;
        DefaultInterpolatedStringHandler interpolatedStringHandler;
        foreach (CCSPlayerController playerController1 in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player != (CEntityInstance) null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && !player.IsBot && player.TeamNum > (byte) 0)))
        {
          if (this.Config.ZCD_HudText && (double) this.gNextWaveTime <= 0.0 && playerController1.Pawn.Value.LifeState == (byte) 0 || playerController1.TeamNum == (byte) 1)
          {
            CCSPlayerController playerController2 = playerController1;
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(358, 2);
            interpolatedStringHandler.AppendLiteral("<font color='red'>☣ </font> <font class='fontSize-m' color='red'>Zombie Cabin Defense</font><font color='red'> ☣</font><br>");
            interpolatedStringHandler.AppendLiteral("<font color='green'>►</font> <font class='fontSize-m' color='gold'>Wave: ");
            interpolatedStringHandler.AppendFormatted<int>(this.gCurrentWave + 1);
            interpolatedStringHandler.AppendLiteral("</font> <font color='green'>◄</font><br>");
            interpolatedStringHandler.AppendLiteral("<font color='green'>►</font> <font class='fontSize-m' color='DodgerBlue'>Humans Left: ");
            interpolatedStringHandler.AppendFormatted<int>(this.GetHumanCount(true));
            interpolatedStringHandler.AppendLiteral("</font> <font color='green'>◄</font>");
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            playerController2.PrintToCenterHtml(stringAndClear);
          }
          else if (this.t_NextWave != null && (double) this.gNextWaveTime > 0.0)
          {
            CCSPlayerController playerController3 = playerController1;
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(417, 1);
            interpolatedStringHandler.AppendLiteral("<font color='red'>☣ </font> <font class='fontSize-m' color='red'>Zombie Cabin Defense</font><font color='red'> ☣</font><br>");
            interpolatedStringHandler.AppendLiteral("<font color='green'>►</font> <font class='fontSize-m' color='gold'>Next Wave will Start in:</font> <font class='fontSize-m' color='red'>");
            interpolatedStringHandler.AppendFormatted<float>(this.gNextWaveTime);
            interpolatedStringHandler.AppendLiteral("</font> <font color='green'>◄</font><br>");
            interpolatedStringHandler.AppendLiteral("<font color='green'>►</font> <font class='fontSize-m' color='lime'>You can now buy</font> <font color='green'>◄</font>");
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            playerController3.PrintToCenterHtml(stringAndClear);
          }
        }
      }));
      this.RegisterEventHandler<EventRoundStart>((BasePlugin.GameEventHandler<EventRoundStart>) ((@event, info) =>
      {
        if (!this.Config.ZCD_PluginEnabled || !this.gShouldStartGame)
          return HookResult.Continue;
        this.ResetZombies();
        this.RemoveObjectives();
        Server.ExecuteCommand("mp_autoteambalance 0");
        Server.ExecuteCommand("mp_limitteams 0");
        Server.ExecuteCommand("bot_knives_only");
        this.BeginWave(false);
        if (this.t_ZFreeze != null)
          this.t_ZFreeze?.Kill();
        if (this.t_NextWave != null)
          this.t_NextWave?.Kill();
        if (this.t_CheckPlayerLocation != null)
          this.t_CheckPlayerLocation?.Kill();
        return HookResult.Continue;
      }));
      this.RegisterEventHandler<EventRoundFreezeEnd>((BasePlugin.GameEventHandler<EventRoundFreezeEnd>) ((@event, info) =>
      {
        if (!this.Config.ZCD_PluginEnabled || !this.gShouldStartGame)
          return HookResult.Continue;
        this.RemoveObjectives();
        if (this.t_ZFreeze != null)
          this.t_ZFreeze?.Kill();
        if (this.t_CheckPlayerLocation != null)
          this.t_CheckPlayerLocation?.Kill();
        this.t_CheckPlayerLocation = this.AddTimer(1f, (Action) (() => this.CheckPlayerLocationTimer()), new TimerFlags?(TimerFlags.REPEAT));
        if ((double) this.Config.ZCD_Freeze > 0.0)
        {
          this.FreezeZombies();
          foreach (CCSPlayerController playerController in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player != (CEntityInstance) null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && !player.IsBot && player.TeamNum == (byte) 3)))
          {
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(60, 11);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
            interpolatedStringHandler.AppendLiteral("[");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
            interpolatedStringHandler.AppendLiteral("★ ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Lime);
            interpolatedStringHandler.AppendLiteral("Zombie Cabin Defense ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
            interpolatedStringHandler.AppendLiteral("★");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
            interpolatedStringHandler.AppendLiteral("] ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Green);
            interpolatedStringHandler.AppendLiteral("Zombies are ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
            interpolatedStringHandler.AppendLiteral("Frozen ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Green);
            interpolatedStringHandler.AppendLiteral("for ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
            interpolatedStringHandler.AppendFormatted<float>(this.Config.ZCD_Freeze);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Green);
            interpolatedStringHandler.AppendLiteral("seconds!");
            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
            playerController.PrintToChat(stringAndClear);
          }
          this.t_ZFreeze = this.AddTimer(this.Config.ZCD_Freeze, new Action(this.UnFreezeZombies));
        }
        return HookResult.Continue;
      }));
      this.RegisterEventHandler<EventPlayerDisconnect>((BasePlugin.GameEventHandler<EventPlayerDisconnect>) ((@event, info) =>
      {
        if (!this.Config.ZCD_PluginEnabled || (CEntityInstance) @event.Userid == (CEntityInstance) null || !@event.Userid.IsValid)
          return HookResult.Continue;
        CCSPlayerController userid = @event.Userid;
        if (this.tRespawn?[userid.Slot] != null)
          this.tRespawn?[userid.Slot]?.Kill();
        return HookResult.Continue;
      }));
      this.AddCommandListener("jointeam", (CommandInfo.CommandListenerCallback) ((player, commandInfo) => this.Config.ZCD_PluginEnabled && (CEntityInstance) player != (CEntityInstance) null && player.IsValid && commandInfo.ArgByIndex(1) != "0" && (commandInfo.ArgByIndex(1) == "1" || (!player.IsBot || !(commandInfo.ArgByIndex(1) == "3")) && (player.IsBot || !(commandInfo.ArgByIndex(1) == "2"))) ? HookResult.Continue : HookResult.Handled));
      this.RegisterEventHandler<EventPlayerTeam>((BasePlugin.GameEventHandler<EventPlayerTeam>) ((@event, info) =>
      {
        if (!this.Config.ZCD_PluginEnabled || @event.Disconnect || (CEntityInstance) @event.Userid == (CEntityInstance) null || !@event.Userid.IsValid || @event.Userid.Connected != PlayerConnectedState.PlayerConnected || @event.Userid.IsHLTV)
          return HookResult.Continue;
        CCSPlayerController player = @event.Userid;
        this.IsPlayerZombie[player.Slot] = player.IsBot;
        if (@event.Team != 1 && @event.Oldteam == 0 || @event.Oldteam == 1)
          this.AddTimer(0.1f, (Action) (() =>
          {
            CCSPlayerController player1 = player;
            CBasePlayerPawn cbasePlayerPawn = player.Pawn.Value;
            byte? nullable3 = cbasePlayerPawn != null ? new byte?(cbasePlayerPawn.LifeState) : new byte?();
            int? nullable4 = nullable3.HasValue ? new int?((int) nullable3.GetValueOrDefault()) : new int?();
            int num3 = 0;
            int num4 = nullable4.GetValueOrDefault() == num3 & nullable4.HasValue ? 1 : 0;
            this.AssignTeam(player1, num4 != 0);
          }));
        return HookResult.Continue;
      }));
      this.RegisterEventHandler<EventPlayerSpawn>((BasePlugin.GameEventHandler<EventPlayerSpawn>) ((@event, info) =>
      {
        if (!this.Config.ZCD_PluginEnabled || !this.gShouldStartGame || (CEntityInstance) @event.Userid == (CEntityInstance) null || !@event.Userid.IsValid || @event.Userid.TeamNum < (byte) 2 || @event.Userid.IsHLTV)
          return HookResult.Continue;
        CCSPlayerController player = @event.Userid;
        this.gZombieID[player.Slot] = -1;
        this.gPlayerZombieKilled[player.Slot] = 0;
        this.IsPlayerZombie[player.Slot] = player.IsBot;
        this.AddTimer(0.1f, (Action) (() =>
        {
          CCSPlayerController player2 = player;
          CBasePlayerPawn cbasePlayerPawn = player.Pawn.Value;
          byte? nullable7 = cbasePlayerPawn != null ? new byte?(cbasePlayerPawn.LifeState) : new byte?();
          int? nullable8 = nullable7.HasValue ? new int?((int) nullable7.GetValueOrDefault()) : new int?();
          int num7 = 0;
          int num8 = nullable8.GetValueOrDefault() == num7 & nullable8.HasValue ? 1 : 0;
          this.AssignTeam(player2, num8 != 0);
        }));
        if (this.IsPlayerZombie[player.Slot])
        {
          CCSPlayerController_InGameMoneyServices gameMoneyServices = player.InGameMoneyServices;
          if (gameMoneyServices != null)
            gameMoneyServices.Account = 0;
          if (this.Config.ZCD_NoBlock)
          {
            player.PlayerPawn.Value.Collision.CollisionGroup = (byte) 19;
            player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte) 19;
            Utilities.SetStateChanged((CBaseEntity) player, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged((CBaseEntity) player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
          }
          string[] zombiesToSpawn = this.GetZombiesToSpawn();
          Random random = new Random();
          if (zombiesToSpawn != null && ((IEnumerable<string>) zombiesToSpawn).Count<string>() > 0)
          {
            int index = random.Next(0, ((IEnumerable<string>) zombiesToSpawn).Count<string>());
            this.Zombify(player, this.GetZombieClassIndexByName(zombiesToSpawn[index]));
          }
          else
            Log.Error("[SLAYER Zombie Cabin Defense] No Zombie Found!");
          if (this.gIncrementedHealthBoost > 0)
            player.PlayerPawn.Value.Health += this.gIncrementedHealthBoost;
          else
            player.PlayerPawn.Value.Health += this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZHealthBoost;
        }
        else
        {
          player.PlayerPawn.Value.Collision.CollisionGroup = (byte) 8;
          player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte) 8;
          Utilities.SetStateChanged((CBaseEntity) player, "CCollisionProperty", "m_CollisionGroup");
          Utilities.SetStateChanged((CBaseEntity) player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
        }
        if (this.tRespawn[player.Slot] != null)
          this.tRespawn[player.Slot]?.Kill();
        return HookResult.Continue;
      }));
      this.RegisterEventHandler<EventPlayerDeath>((BasePlugin.GameEventHandler<EventPlayerDeath>) ((@event, info) =>
      {
        if (!this.Config.ZCD_PluginEnabled || !this.gShouldStartGame || (CEntityInstance) @event.Userid == (CEntityInstance) null || !@event.Userid.IsValid || (CEntityInstance) @event.Attacker == (CEntityInstance) null || !@event.Attacker.IsValid)
          return HookResult.Continue;
        CCSPlayerController userid = @event.Userid;
        CCSPlayerController attacker = @event.Attacker;
        if (this.IsPlayerZombie[userid.Slot])
        {
          ++this.gZombiesKilled;
          ++this.gPlayerZombieKilled[attacker.Slot];
          this.StartZombieRespawnTimer(userid);
          if (this.gZombiesKilled == this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZKillCount && this.gIncrementedZKillCount <= 0)
            this.HumansWin();
          else if (this.gIncrementedZKillCount > 0 && this.gZombiesKilled == this.gIncrementedZKillCount)
            this.HumansWin();
          if (this.gPlayerZombieKilled[attacker.Slot] == this.Config.ZCD_KillZombies)
          {
            this.gPlayerZombieKilled[attacker.Slot] = 0;
            attacker.GiveNamedItem("weapon_healthshot");
          }
        }
        else
        {
          CCSPlayerController_InGameMoneyServices gameMoneyServices = userid.InGameMoneyServices;
          if (gameMoneyServices != null)
            gameMoneyServices.Account = 0;
          Utilities.SetStateChanged((CBaseEntity) userid, "CCSPlayerController_InGameMoneyServices", "m_iAccount");
          if (this.GetHumanCount(true) <= 0)
          {
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(74, 11);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
            interpolatedStringHandler.AppendLiteral("[");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
            interpolatedStringHandler.AppendLiteral("★ ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Lime);
            interpolatedStringHandler.AppendLiteral("Zombie Cabin Defense ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
            interpolatedStringHandler.AppendLiteral("★");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
            interpolatedStringHandler.AppendLiteral("] ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
            interpolatedStringHandler.AppendLiteral("Zombie Wins ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Lime);
            interpolatedStringHandler.AppendLiteral("- ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
            interpolatedStringHandler.AppendLiteral("Reached Wave ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.Lime);
            interpolatedStringHandler.AppendFormatted<int>(this.gCurrentWave);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
            interpolatedStringHandler.AppendLiteral("(Restarting Round)");
            Server.PrintToChatAll(interpolatedStringHandler.ToStringAndClear());
            this.ZombiesWin();
          }
        }
        return HookResult.Continue;
      }));
      this.RegisterEventHandler<EventPlayerJump>((BasePlugin.GameEventHandler<EventPlayerJump>) ((@event, info) =>
      {
        if ((CEntityInstance) @event.Userid == (CEntityInstance) null || !@event.Userid.IsValid)
          return HookResult.Continue;
        CCSPlayerController userid = @event.Userid;
        if (!this.Config.ZCD_PluginEnabled || !this.gShouldStartGame || !this.IsPlayerZombie[userid.Slot])
          return HookResult.Continue;
        userid.PlayerPawn.Value.AbsVelocity.Z = this.GetZombieByIndex(this.gZombieID[userid.Slot]).ZombieJump;
        return HookResult.Continue;
      }));
    }

    private void AssignTeam(CCSPlayerController? player, bool spawn)
    {
      if ((CEntityInstance) player == (CEntityInstance) null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.IsHLTV)
        return;
      if (this.IsPlayerZombie[player.Slot])
      {
        if (player.TeamNum == (byte) 2)
          return;
        if (player.PawnIsAlive)
          player.SwitchTeam(CsTeam.Terrorist);
        else
          player.ChangeTeam(CsTeam.Terrorist);
        if (spawn)
          player.Respawn();
      }
      else if (this.GetHumanCount(false) <= this.Config.ZCD_HumanMax)
      {
        if (player.TeamNum != (byte) 3)
        {
          if (player.PawnIsAlive)
            player.SwitchTeam(CsTeam.CounterTerrorist);
          else
            player.ChangeTeam(CsTeam.CounterTerrorist);
          if (spawn)
            player.Respawn();
        }
      }
      else
      {
        player.ChangeTeam(CsTeam.Spectator);
        CCSPlayerController playerController = player;
        DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 8);
        interpolatedStringHandler.AppendLiteral(" ");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
        interpolatedStringHandler.AppendLiteral("[");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
        interpolatedStringHandler.AppendLiteral("★ ");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.Lime);
        interpolatedStringHandler.AppendLiteral("Zombie Cabin Defense ");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
        interpolatedStringHandler.AppendLiteral("★");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
        interpolatedStringHandler.AppendLiteral("] ");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.Blue);
        interpolatedStringHandler.AppendLiteral("Counter Terrorist ");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.Lime);
        interpolatedStringHandler.AppendLiteral("team is ");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
        interpolatedStringHandler.AppendLiteral("Full!");
        string stringAndClear = interpolatedStringHandler.ToStringAndClear();
        playerController.PrintToChat(stringAndClear);
      }
    }

    private void StartZombieRespawnTimer(CCSPlayerController? player)
    {
      if (this.GetAliveZombieCount() >= this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZKillCount - this.gZombiesKilled)
        return;
      if (this.tRespawn[player.Slot] != null)
        this.tRespawn[player.Slot].Kill();
      this.gRespawnTime[player.Slot] = this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZRespawnTime;
      this.tRespawn[player.Slot] = this.AddTimer(1f, (Action) (() => this.ZombieRespawn(player)), new TimerFlags?(TimerFlags.REPEAT));
    }

    private void CheckPlayerLocationTimer()
    {
      if (!this.Config.ZCD_PluginEnabled || !this.gShouldStartGame || this.GetHumanCount(true) <= 0)
      {
        this.t_CheckPlayerLocation.Kill();
      }
      else
      {
        foreach (CCSPlayerController playerController in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player != (CEntityInstance) null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && !this.IsPlayerZombie[player.Slot] && player.TeamNum == (byte) 3 && player.Pawn.Value.LifeState == (byte) 0)))
        {
          if (playerController.PlayerPawn.Value.InBuyZone)
          {
            if (playerController.PlayerPawn.Value.Health < 100)
              playerController.PlayerPawn.Value.Health += this.Config.ZCD_IncreaseHealth;
          }
          else if (playerController.PlayerPawn.Value.Health > 0)
            playerController.PlayerPawn.Value.Health -= this.Config.ZCD_DecreaseHealth;
          else
            playerController.CommitSuicide(false, true);
          Utilities.SetStateChanged((CBaseEntity) playerController.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
        }
      }
    }

    public static CCSGameRules GetGameRules()
    {
      return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First<CCSGameRulesProxy>().GameRules;
    }

    public void ZombieRespawn(CCSPlayerController player)
    {
      if ((CEntityInstance) player == (CEntityInstance) null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.IsHLTV || player.TeamNum != (byte) 2 || (double) this.gNextWaveTime > 0.0 || this.GetAliveZombieCount() >= this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZKillCount - this.gZombiesKilled)
      {
        this.tRespawn[player.Slot].Kill();
      }
      else
      {
        --this.gRespawnTime[player.Slot];
        if ((double) this.gRespawnTime[player.Slot] > 0.0)
          return;
        this.RespawnClient(player);
        this.tRespawn[player.Slot].Kill();
      }
    }

    private int GetZombieToKill()
    {
      return this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZKillCount - this.gZombiesKilled;
    }

    private int GetAliveZombieCount()
    {
      int aliveZombieCount = 0;
      foreach (CCSPlayerController playerController in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player != (CEntityInstance) null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > (byte) 1 && player.Pawn.Value.LifeState == (byte) 0)))
      {
        if (this.IsPlayerZombie[playerController.Slot])
          ++aliveZombieCount;
      }
      return aliveZombieCount;
    }

    private int GetHumanCount(bool alive)
    {
      int humanCount = 0;
      foreach (CCSPlayerController playerController in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player != (CEntityInstance) null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > (byte) 1)))
      {
        if (!this.IsPlayerZombie[playerController.Slot])
        {
          if (alive && playerController.Pawn.Value.LifeState == (byte) 0)
            ++humanCount;
          else if (!alive)
            ++humanCount;
        }
      }
      return humanCount;
    }

    private void RemoveObjectives()
    {
      foreach (CEntityInstance centityInstance in Utilities.GetAllEntities().Where<CEntityInstance>((Func<CEntityInstance, bool>) (entity => entity != (CEntityInstance) null && entity.IsValid)))
      {
        if (centityInstance.DesignerName == "func_bomb_target" || centityInstance.DesignerName == "func_hostage_rescue" || centityInstance.DesignerName == "c4" || centityInstance.DesignerName == "hostage_entity")
          centityInstance.Remove();
      }
    }

    private void FreezeZombies()
    {
      foreach (CCSPlayerController playerController in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player != (CEntityInstance) null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > (byte) 1 && player.Pawn.Value.LifeState == (byte) 0)))
      {
        if (this.IsPlayerZombie[playerController.Slot])
        {
          playerController.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_NONE;
          Schema.SetSchemaValue<int>(playerController.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 0);
          Utilities.SetStateChanged((CBaseEntity) playerController.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
          playerController.PlayerPawn.Value.TakesDamage = false;
        }
      }
    }

    private void UnFreezeZombies()
    {
      DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 8);
      interpolatedStringHandler.AppendLiteral(" ");
      interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
      interpolatedStringHandler.AppendLiteral("[");
      interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
      interpolatedStringHandler.AppendLiteral("★ ");
      interpolatedStringHandler.AppendFormatted<char>(ChatColors.Lime);
      interpolatedStringHandler.AppendLiteral("Zombie Cabin Defense ");
      interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
      interpolatedStringHandler.AppendLiteral("★");
      interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
      interpolatedStringHandler.AppendLiteral("] ");
      interpolatedStringHandler.AppendFormatted<char>(ChatColors.Green);
      interpolatedStringHandler.AppendLiteral("Zombies are ");
      interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
      interpolatedStringHandler.AppendLiteral("released ");
      interpolatedStringHandler.AppendFormatted<char>(ChatColors.Green);
      interpolatedStringHandler.AppendLiteral("Now!");
      this.PrinttoChatCT(interpolatedStringHandler.ToStringAndClear());
      foreach (CCSPlayerController playerController in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player != (CEntityInstance) null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > (byte) 1 && player.Pawn.Value.LifeState == (byte) 0)))
      {
        if (this.IsPlayerZombie[playerController.Slot])
        {
          playerController.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
          Schema.SetSchemaValue<int>(playerController.PlayerPawn.Value.Handle, "CBaseEntity", "m_nActualMoveType", 2);
          Utilities.SetStateChanged((CBaseEntity) playerController.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
          playerController.PlayerPawn.Value.TakesDamage = true;
        }
      }
    }

    public void BeginWave(bool respawn)
    {
      this.gZombiesKilled = 0;
      Server.ExecuteCommand("mp_buytime 0");
      this.gZombieToSpawn = this.CalculateZombiesForWave(this.gCurrentWave);
      if (this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZKillCount < this.gZombieToSpawn)
        this.gZombieToSpawn = this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZKillCount;
      this.AddTimer(0.5f, (Action) (() =>
      {
        DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 1);
        interpolatedStringHandler.AppendLiteral("bot_quota ");
        interpolatedStringHandler.AppendFormatted<int>(this.gZombieToSpawn);
        Server.ExecuteCommand(interpolatedStringHandler.ToStringAndClear());
      }));
      if (!respawn)
        return;
      this.AddTimer(1f, (Action) (() =>
      {
        foreach (CCSPlayerController client in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player != (CEntityInstance) null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum == (byte) 2 && player.IsBot)))
        {
          this.IsPlayerZombie[client.Slot] = true;
          this.RespawnClient(client);
        }
      }));
    }

    private void ZombiesWin()
    {
      this.gZombiesKilled = 0;
      this.gCurrentWave = 0;
      this.gCurrentWaveDifficulty = 0;
      this.gIncrementedZKillCount = 0;
      this.gIncrementedHealthBoost = 0;
      this.gShouldStartGame = false;
      if (this.t_CheckPlayerLocation != null)
        this.t_CheckPlayerLocation?.Kill();
      Server.ExecuteCommand("bot_kick");
      this.TerminateRound(3f, RoundEndReason.GameCommencing);
    }

    private void HumansWin()
    {
      this.gZombiesKilled = 0;
      ++this.gCurrentWave;
      if (this.gCurrentWave == this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZWaves && this.Config.ZCD_Waves.Count > this.gCurrentWaveDifficulty + 1)
        ++this.gCurrentWaveDifficulty;
      else if (this.Config.ZCD_Waves.Count == this.gCurrentWaveDifficulty + 1 && this.gCurrentWave >= this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZWaves)
      {
        if (this.gIncrementedHealthBoost <= 0)
          this.gIncrementedHealthBoost = this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZHealthBoost;
        else
          this.gIncrementedHealthBoost += this.Config.ZCD_IncrementHealthBoostBy;
        if (this.gIncrementedZKillCount <= 0)
          this.gIncrementedZKillCount = this.GetWaveByIndex(this.gCurrentWaveDifficulty).ZKillCount;
        else
          this.gIncrementedZKillCount += this.Config.ZCD_IncrementZKillCountBy;
      }
      this.RespawnDeadHumans();
      Server.ExecuteCommand("mp_buytime 99999");
      if (this.t_NextWave != null)
        this.t_NextWave?.Kill();
      this.gNextWaveTime = this.Config.ZCD_TimeBetweenNextWave;
      this.t_NextWave = this.AddTimer(1f, (Action) (() =>
      {
        --this.gNextWaveTime;
        if ((double) this.gNextWaveTime > 0.0)
          return;
        this.BeginWave(true);
        this.t_NextWave?.Kill();
      }), new TimerFlags?(TimerFlags.REPEAT));
    }

    private void Zombify(CCSPlayerController? player, int zombieid)
    {
      if ((CEntityInstance) player == (CEntityInstance) null || !player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.IsHLTV || player.TeamNum < (byte) 2 || player.Pawn.Value.LifeState > (byte) 0)
        return;
      this.gZombieID[player.Slot] = zombieid;
      this.AddTimer(0.3f, (Action) (() =>
      {
        if (player.PlayerPawn.Value.WeaponServices.MyWeapons.Count != 0)
          player.RemoveWeapons();
        player.GiveNamedItem("weapon_knife");
        foreach (CHandle<CBasePlayerWeapon> chandle in player.PlayerPawn.Value.WeaponServices.MyWeapons.Where<CHandle<CBasePlayerWeapon>>((Func<CHandle<CBasePlayerWeapon>, bool>) (weapon => weapon != null && weapon.IsValid && weapon.Value.IsValid)))
        {
          if (chandle.Value.DesignerName.Contains("weapon_knife"))
          {
            chandle.Value.RenderMode = RenderMode_t.kRenderTransAlpha;
            chandle.Value.Render = Color.FromArgb(0, (int) byte.MaxValue, (int) byte.MaxValue, (int) byte.MaxValue);
          }
        }
        player.PlayerPawn.Value.Health = this.GetZombieByIndex(zombieid).ZombieHealth;
        player.PlayerPawn.Value.VelocityModifier = this.GetZombieByIndex(zombieid).ZombieSpeed;
        player.PlayerPawn.Value.GravityScale *= this.GetZombieByIndex(zombieid).ZombieGravity;
        player.DesiredFOV = Convert.ToUInt32(this.GetZombieByIndex(zombieid).ZombieFOV);
        Utilities.SetStateChanged((CBaseEntity) player, "CBasePlayerController", "m_iDesiredFOV");
        if (!(this.GetZombieByIndex(zombieid).ZombieModelPath != ""))
          return;
        Server.PrecacheModel(this.GetZombieByIndex(zombieid).ZombieModelPath);
        player.PlayerPawn.Value.SetModel(this.GetZombieByIndex(zombieid).ZombieModelPath);
      }));
    }

    private void ResetZombies()
    {
      foreach (CCSPlayerController playerController in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player != (CEntityInstance) null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum > (byte) 1)))
        this.IsPlayerZombie[playerController.Slot] = playerController.IsBot;
    }

    private void ZCDEnd()
    {
      this.TerminateRound(3f, RoundEndReason.GameCommencing);
      Server.ExecuteCommand("bot_all_weapons");
      Server.ExecuteCommand("bot_kick");
      foreach (CCSPlayerController playerController in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player == (CEntityInstance) null || !player.IsValid || player.Connected > PlayerConnectedState.PlayerConnected)))
      {
        if (this.tRespawn[playerController.Slot] != null)
          this.tRespawn[playerController.Slot]?.Kill();
      }
      if (this.t_CheckPlayerLocation != null)
        this.t_CheckPlayerLocation?.Kill();
      if (this.t_NextWave == null)
        return;
      this.t_NextWave?.Kill();
    }

    private void CMD_StartGame(CCSPlayerController? player, CommandInfo commandInfo)
    {
      if (!this.Config.ZCD_PluginEnabled)
      {
        CCSPlayerController playerController = player;
        DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(68, 6);
        interpolatedStringHandler.AppendLiteral(" ");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
        interpolatedStringHandler.AppendLiteral("[");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
        interpolatedStringHandler.AppendLiteral("★ ");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.Lime);
        interpolatedStringHandler.AppendLiteral("Zombie Cabin Defense ");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
        interpolatedStringHandler.AppendLiteral("★");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
        interpolatedStringHandler.AppendLiteral("] ");
        interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
        interpolatedStringHandler.AppendLiteral("Zombie Cabin Defense Plugin is Disabled!");
        string stringAndClear = interpolatedStringHandler.ToStringAndClear();
        playerController.PrintToChat(stringAndClear);
      }
      else
      {
        int num;
        if ((CEntityInstance) player != (CEntityInstance) null)
          num = !AdminManager.PlayerHasPermissions(player, this.Config.ZCD_AdminFlagToUseCMDs) ? 1 : 0;
        else
          num = 0;
        if (num != 0)
        {
          CCSPlayerController playerController = player;
          DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(74, 6);
          interpolatedStringHandler.AppendLiteral(" ");
          interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
          interpolatedStringHandler.AppendLiteral("[");
          interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
          interpolatedStringHandler.AppendLiteral("★ ");
          interpolatedStringHandler.AppendFormatted<char>(ChatColors.Lime);
          interpolatedStringHandler.AppendLiteral("Zombie Cabin Defense ");
          interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
          interpolatedStringHandler.AppendLiteral("★");
          interpolatedStringHandler.AppendFormatted<char>(ChatColors.Gold);
          interpolatedStringHandler.AppendLiteral("] ");
          interpolatedStringHandler.AppendFormatted<char>(ChatColors.DarkRed);
          interpolatedStringHandler.AppendLiteral("You don't have permission to use this command!");
          string stringAndClear = interpolatedStringHandler.ToStringAndClear();
          playerController.PrintToChat(stringAndClear);
        }
        else
        {
          this.ZombiesWin();
          this.gShouldStartGame = true;
        }
      }
    }

    public static bool CanTarget(CCSPlayerController controller, CCSPlayerController target)
    {
      return target.IsBot || AdminManager.CanPlayerTarget(controller, target);
    }

    public void RespawnClient(CCSPlayerController client)
    {
      if (!client.IsValid || client.PawnIsAlive)
        return;
      CCSPlayerPawn ccsPlayerPawn = client.PlayerPawn.Value;
      ZombieCabinDefense.ZombieCabinDefense.CBasePlayerController_SetPawnFunc.Invoke(client, ccsPlayerPawn, true, false);
      VirtualFunction.CreateVoid<CCSPlayerController>(client.Handle, GameData.GetOffset("CCSPlayerController_Respawn"))(client);
    }

    private string[] GetZombiesToSpawn()
    {
      List<string> stringList = new List<string>();
      foreach (ZombieCabinDefenseZombieSettings zcdZomby in this.Config.ZCD_Zombies)
      {
        string[] source = zcdZomby.ZombieInWaves.Split(",");
        if (zcdZomby.ZombieInWaves != "" && source != null && ((IEnumerable<string>) source).Count<string>() > 0)
        {
          if (((IEnumerable<string>) source).Contains<string>(this.GetWaveByIndex(this.gCurrentWaveDifficulty).WaveName))
            stringList.Add(zcdZomby.ZombieClassName);
        }
        else if (zcdZomby.ZombieInWaves == "" || zcdZomby.ZombieInWaves == " ")
          stringList.Add(zcdZomby.ZombieClassName);
      }
      return stringList.ToArray();
    }

    private void RespawnDeadHumans()
    {
      foreach (CCSPlayerController client in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player != (CEntityInstance) null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && player.TeamNum == (byte) 3 && player.Pawn.Value.LifeState > (byte) 0)))
        this.RespawnClient(client);
    }

    private void PrinttoChatCT(string message)
    {
      foreach (CCSPlayerController playerController in Utilities.GetPlayers().Where<CCSPlayerController>((Func<CCSPlayerController, bool>) (player => (CEntityInstance) player != (CEntityInstance) null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && !player.IsHLTV && !player.IsBot && player.TeamNum == (byte) 3)))
        playerController.PrintToChat(message);
    }

    public int CalculateZombiesForWave(int currentWave)
    {
      if (currentWave <= 0)
        return this.Config.ZCD_ZombieSpawnMin;
      int zombieIncreaseRate = this.Config.ZCD_ZombieIncreaseRate;
      int num = 1;
      return Math.Min(this.Config.ZCD_ZombieSpawnMin + (int) Math.Floor(Math.Log((double) (currentWave + 1)) * (double) zombieIncreaseRate * (double) num), this.Config.ZCD_ZombieSpawnMax);
    }

    public void TerminateRound(float delay, RoundEndReason roundEndReason)
    {
      CCSGameRules gameRules = ZombieCabinDefense.ZombieCabinDefense.GetGameRules();
      if (ZombieCabinDefense.ZombieCabinDefense.IsWindowsPlatform)
        this.TerminateRoundWindows(gameRules.Handle, delay, roundEndReason, IntPtr.Zero, 0U);
      else
        this.TerminateRoundLinux(gameRules.Handle, roundEndReason, IntPtr.Zero, 0U, delay);
    }

    [Obsolete("Constructors of types with required members are not supported in this version of your compiler.", true)]
    [CompilerFeatureRequired("RequiredMembers")]
    public ZombieCabinDefense()
    {
    }
  }
}

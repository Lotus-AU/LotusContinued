using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Hazel;
using Lotus.API.Odyssey;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Interfaces;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Networking.RPC;
using VentLib.Networking.RPC.Interfaces;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Lotus.Roles;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Roles.Managers.Interfaces;
using System.Collections.Generic;
using Lotus.API.Player;

namespace Lotus.Utilities;

public static class Utils
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Utils));

    public static string GetNameWithRole(this NetworkedPlayerInfo player)
    {
        return GetPlayerById(player.PlayerId)?.GetNameWithRole() ?? "";
    }

    public static Color? ConvertHexToColor(string hex)
    {
        if (!ColorUtility.TryParseHtmlString(hex, out Color c)) return null;
        return c;
    }

    public static bool HasTasks(NetworkedPlayerInfo p)
    {
        CustomRole? primaryDefinition = p.GetPrimaryRole();
        if (primaryDefinition == null) return false;
        if (primaryDefinition.RealRole.IsImpostor() || primaryDefinition.GetType() != IRoleManager.Current.FallbackRole().GetType()) return false;
        if (primaryDefinition is not ITaskHolderRole taskHolder) return false;
        return taskHolder.TasksApplyToTotal() && taskHolder.HasTasks();
    }

    public static void Teleport(CustomNetworkTransform nt, Vector2 location)
    {
        Vector2 currentLocation = nt.lastPosSent;
        if (Game.State is not GameState.InLobby) Hooks.PlayerHooks.PlayerTeleportedHook.Propagate(new PlayerTeleportedHookEvent(nt.myPlayer, currentLocation, location));
        float delay = 0f;
        if (nt.myPlayer.petting)
        {
            if (AmongUsClient.Instance.AmHost) nt.myPlayer.MyPhysics.CancelPet();
            else
            {
                delay = NetUtils.DeriveDelay(0.1f);
                nt.myPlayer.MyPhysics.CancelPet();
                RpcV3.Immediate(nt.myPlayer.MyPhysics.NetId, RpcCalls.CancelPet, SendOption.None).Send();
            }
        }
        if (delay <= 0f) TeleportDeferred(nt, location).Send();
        else Async.Schedule(() => TeleportDeferred(nt, location).Send(), delay);
    }

    public static MonoRpc TeleportDeferred(CustomNetworkTransform transform, Vector2 location)
    {
        if (AmongUsClient.Instance.AmHost) transform.SnapTo(location, (ushort)(transform.lastSequenceId + 328));
        return RpcV3.Immediate(transform.NetId, (byte)RpcCalls.SnapTo, SendOption.None).Write(location).Write((ushort)(transform.lastSequenceId + 8));
    }

    public static string GetSubRolesText(byte id, bool disableColor = false)
    {
        PlayerControl player = GetPlayerById(id)!;
        return player.NameModel().GetComponentHolder<SubroleHolder>().Render(player, GameState.Roaming);
    }

    public static PlayerControl? GetPlayerById(byte playerId) => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(pc => pc.PlayerId == playerId);

    public static Optional<PlayerControl> PlayerById(byte playerId) => PlayerControl.AllPlayerControls.ToArray().FirstOrOptional(pc => pc.PlayerId == playerId);

    public static Optional<PlayerControl> PlayerByClientId(int clientId)
    {
        return PlayerControl.AllPlayerControls.ToArray().FirstOrOptional(c => c.GetClientId() == clientId);
    }

    public static string PadRightV2(this object text, int num)
    {
        int bc = 0;
        var t = text.ToString();
        foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
        return t?.PadRight(Mathf.Max(num - (bc - t.Length), 0));
    }


    public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");

    public static AudioClip LoadAudioClip(string path, string clipName = "UNNAMED_TOR_AUDIO_CLIP")
    {
        // must be "raw (headerless) 2-channel signed 32 bit pcm (le)" (can e.g. use Audacity� to export)
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(path);
            var byteAudio = new byte[stream.Length];
            _ = stream.Read(byteAudio, 0, (int)stream.Length);
            float[] samples = new float[byteAudio.Length / 4]; // 4 bytes per sample
            int offset;
            for (int i = 0; i < samples.Length; i++)
            {
                offset = i * 4;
                samples[i] = (float)BitConverter.ToInt32(byteAudio, offset) / Int32.MaxValue;
            }

            int channels = 2;
            int sampleRate = 48000;
            AudioClip audioClip = AudioClip.Create(clipName, samples.Length, channels, sampleRate, false);
            audioClip.SetData(samples, 0);
            return audioClip;
        }
        catch
        {
            System.Console.WriteLine("Error loading AudioClip from resources: " + path);
        }

        return null;

        /* Usage example:
        AudioClip exampleClip = Helpers.loadAudioClipFromResources("Lotus.assets.exampleClip.raw");
        if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(exampleClip, false, 0.8f);
        */
    }

    public static Vent GetFurthestVentFromPlayers()
    {
        List<Vector2> playerPositions = new();
        Players.GetAlivePlayers().ForEach(p => playerPositions.Add(p.GetTruePosition()));
        Vent furthestVent = null!;
        float maxDistance = float.MinValue;

        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            Vector2 ventPosition = vent.transform.position;
            float minDistanceToPlayers = float.MaxValue;

            playerPositions.ForEach(pos =>
            {
                float distance = Vector2.Distance(ventPosition, pos);
                if (distance < minDistanceToPlayers)
                {
                    minDistanceToPlayers = distance;
                }
            });

            if (minDistanceToPlayers > maxDistance)
            {
                maxDistance = minDistanceToPlayers;
                furthestVent = vent;
            }
        }

        return furthestVent;
    }

    public static string ColorString(Color32 color, string str) =>
        $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";

    public static string GetOnOffColored(bool value) =>
        value ? Color.cyan.Colorize("ON") : Color.red.Colorize("OFF");
}
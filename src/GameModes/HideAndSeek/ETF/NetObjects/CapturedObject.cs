using System.Collections.Generic;
using System.Linq;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.GUI.Name;
using Lotus.Options;
using Lotus.Roles.RoleGroups.Extra.HNS.ETF;
using Lotus.RPC.CustomObjects.Builtin;
using Rewired;
using UnityEngine;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.HideAndSeek.ETF.NetObjects;

public sealed class CapturedObject: FloatingText
{
    private const float CloseDistance = 7f;

    public Fugitive Captured { get; }
    public Warden Capturer { get; }
    public Vent Holder { get; }

    public float TimeRemaining => timeLeft;

    private float timeLeft;
    private bool isAlive;

    private int ceiledTime;

    private Warden[] allWardens;
    private bool oneIsTooClose;

    public CapturedObject(Fugitive captured, Warden capturer, Vent holder)
        : base(FormatNumber(Mathf.CeilToInt(captured.GetCapturedTimeLeft())), holder.transform.position)
    {
        Captured = captured;
        Capturer = capturer;
        Holder = holder;

        isAlive = true;
        timeLeft = captured.GetCapturedTimeLeft();
        ceiledTime = Mathf.CeilToInt(timeLeft);

        // int inGameFugitiveCount = 0;
        List<Warden> wardens = [];
        Players.GetAllRoles().ForEach(r =>
        {
            if (r is Fugitive fugitive) fugitive.AlertCapturedPlayer(Holder, true);
            else if (r is Warden warden)
            {
                warden.AlertCapturedPlayer(Holder, true);
                wardens.Add(warden);
            }
        });
        allWardens = wardens.ToArray();
    }

    protected override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        if (!isAlive) return;
        CheckTooClose();
        if (oneIsTooClose) return;
        timeLeft -= Time.fixedDeltaTime;
        if (timeLeft <= 0) KillHost();
        else
        {
            int curCeil = Mathf.CeilToInt(timeLeft);
            if (curCeil == ceiledTime) return;
            ceiledTime = curCeil;
            RpcChangeSprite(FormatNumber(curCeil));
        }
    }

    public void Rescue()
    {
        isAlive = false;
        Despawn();
        Captured.Rescue(Holder, timeLeft);
        ETFGameMode.Instance.CapturedObjects.Remove(Holder.Id);
        RemoveArrows();
    }

    public void OnDisconnect()
    {
        isAlive = false;
        Despawn();
        Capturer.IncrementKilledPlayers();
        RemoveArrows();
    }

    private void KillHost()
    {
        isAlive = false;
        Captured.FreezeToDeath(Capturer);
        Capturer.IncrementKilledPlayers();
        RemoveArrows();
        Despawn();
    }

    private void RemoveArrows()
    {
        Players.GetAllRoles().ForEach(r =>
        {
            if (r is Fugitive fugitive) fugitive.AlertCapturedPlayer(Holder, false);
            else if (r is Warden warden) warden.AlertCapturedPlayer(Holder, false);
        });
    }

    private void CheckIfShouldKillAll(int inGameFugitiveCount)
    {
        List<Fugitive> allInGameFugitives = Players.GetAliveRoles()
            .OfType<Fugitive>()
            .Where(r => r.Captured() && !r.Escaped() && !r.Died())
            .ToList();
        if (inGameFugitiveCount != allInGameFugitives.Count) return;
        timeLeft = 0;
        ETFGameMode.Instance.CapturedObjects.Values.ForEach(o => o.timeLeft = 0);
    }

    private void CheckTooClose()
    {
        bool current = false;
        allWardens.ForEach(w =>
        {
            if (!w.MyPlayer || !w.MyPlayer.Data || w.MyPlayer.Data.Disconnected) return;
            if (Vector2.Distance(w.MyPlayer.NetTransform.body.position, Position) > CloseDistance)
                w.SetTooClose(false);
            else
            {
                w.SetTooClose(true);
                current = true;
            }
        });
        oneIsTooClose = current;
    }

    private static string FormatNumber(int time) => TextUtils.ApplySize(10f, $"{time}{GeneralOptionTranslations.SecondsSuffix}");
}
using System.Collections.Generic;
using System.Linq;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.GUI.Name;
using Lotus.Options;
using Lotus.Roles.RoleGroups.Extra.HNS.ETF;
using Lotus.RPC.CustomObjects.Builtin;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.HideAndSeek.ETF.NetObjects;

public sealed class TerminalObject: FloatingText
{
    private const string UnhackedComputer = "<size=100%><line-height=97%><cspace=0.16em><#0000>WWWWWWWW\nW</color><mark=#000000>WWWWWW</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#00ffff>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#00ffff>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#00ffff>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>WWWWWW</mark><#0000>W\nWWW</color><mark=#000000>WW</mark><#0000>WWW\nWW</color><mark=#000000>WWWW</mark><#0000>WW</color>";
    private const string FailedComputer = "<size=100%><line-height=97%><cspace=0.16em><#0000>WWWWWWWW\nW</color><mark=#000000>WWWWWW</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#ff0400>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#ff0400>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#ff0400>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>WWWWWW</mark><#0000>W\nWWW</color><mark=#000000>WW</mark><#0000>WWW\nWW</color><mark=#000000>WWWW</mark><#0000>WW</color>";
    private const string HackedComputer = "<size=100%><line-height=97%><cspace=0.16em><#0000>WWWWWWWW\nW</color><mark=#000000>WWWWWW</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#00ff0d>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#00ff0d>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#00ff0d>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>WWWWWW</mark><#0000>W\nWWW</color><mark=#000000>WW</mark><#0000>WWW\nWW</color><mark=#000000>WWWW</mark><#0000>WW</color>";

    private const string SkillCheckComing = "<size=100%><line-height=97%><cspace=0.16em><#0000>WWWWWWWW\nW</color><mark=#000000>WWWWWW</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#fffb00>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#fffb00>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#fffb00>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>WWWWWW</mark><#0000>W\nWWW</color><mark=#000000>WW</mark><#0000>WWW\nWW</color><mark=#000000>WWWW</mark><#0000>WW</color>";
    private const string SkillCheckNow = "<size=100%><line-height=97%><cspace=0.16em><#0000>WWWWWWWW\nW</color><mark=#000000>WWWWWW</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#c74fff>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#c74fff>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>W</mark><mark=#c74fff>WWWW</mark><mark=#000000>W</mark><#0000>W\nW</color><mark=#000000>WWWWWW</mark><#0000>W\nWWW</color><mark=#000000>WW</mark><#0000>WWW\nWW</color><mark=#000000>WWWW</mark><#0000>WW</color>";

    public float ComputerProgress { get; private set; }

    private readonly List<byte> hackingPlayers;
    private byte[] playersInCheck;

    private bool isDone;
    private bool isInCheck;
    private float timeTillNextCheck;

    private float checkTime;
    private float checkStart;
    private float checkDuration;
    private bool isAbleToCheck;
    private int checkRespondedCount;

    public TerminalObject(Vector2 position) : base(UnhackedComputer, position)
    {
        hackingPlayers = [];
        playersInCheck = [];
        timeTillNextCheck = Random.RandomRangeInt(12, 20);
    }

    protected override void OnFixedUpdate()
    {
        if (isInCheck)
        {
            checkTime += Time.fixedDeltaTime;
            float lateCheck = checkStart + checkDuration;
            if (!isAbleToCheck && checkTime >= checkStart && checkTime <= lateCheck)
            {
                isAbleToCheck = true;
                RpcChangeSprite(SkillCheckNow);
            }
            else if (checkTime >= 7f) FinishSkillCheck();
            else if (isAbleToCheck && checkTime >= lateCheck)
            {
                isAbleToCheck = false;
                RpcChangeSprite(SkillCheckComing);
            }
        }
        else if (hackingPlayers.Count > 0)
        {
            ComputerProgress += Time.fixedDeltaTime * hackingPlayers.Count;
            timeTillNextCheck -= Time.fixedDeltaTime;
            if (ComputerProgress >= ExtraGamemodeOptions.EscapeOptions.TerminalHackTime) FinishHacking();
            else if (timeTillNextCheck <= 0f) StartSkillCheck();
        }
        base.OnFixedUpdate();
    }

    public bool TryStartHacking(byte playerId)
    {
        if (isDone) return false;
        if (hackingPlayers.Count >= ExtraGamemodeOptions.EscapeOptions.MaxPlayersPerTerminal) return false;

        hackingPlayers.Add(playerId);
        return true;
    }

    public bool TryStopHacking(byte playerId)
    {
        // if (isInCheck && playersInCheck.Contains(playerId)) return false;
        if (!hackingPlayers.Contains(playerId)) return false;
        hackingPlayers.Remove(playerId);
        return true;
    }

    public void HandlePlayerDisconnected(byte playerId)
    {
        if (isInCheck && playersInCheck.Contains(playerId))
            playersInCheck = playersInCheck.Without(playerId).ToArray();
        hackingPlayers.Remove(playerId);
    }

    public void HitSkillcheck(byte playerId)
    {
        if (!isInCheck) return;
        if (!isAbleToCheck) return;
        if (!playersInCheck.Contains(playerId)) return;
        checkRespondedCount += 1;
    }

    private void StartSkillCheck()
    {
        checkTime = 0;
        checkRespondedCount = 0;
        checkStart = Random.RandomRange(1f, 5.5f);
        checkDuration = Random.Range(0.8f, 1.2f);

        playersInCheck = hackingPlayers.ToArray();
        playersInCheck.Filter(Utils.PlayerById).ForEach(p => p.PrimaryRole<Fugitive>()!.PropagateSkillCheck());
        isInCheck = true;

        RpcChangeSprite(SkillCheckComing);
    }

    private void FinishSkillCheck()
    {
        isInCheck = false;

        bool isSuccess = checkRespondedCount >= playersInCheck.Length;

        timeTillNextCheck = Random.RandomRangeInt(12, 20);
        if (isSuccess)
        {
            // Succeeded in skill check
            RpcChangeSprite(UnhackedComputer);
        }
        else
        {
            RpcChangeSprite(FailedComputer);
            Players.GetAllRoles().ForEach(r =>
            {
                if (r is Fugitive fugitive) fugitive.AlertFailedTerminal(this);
                else if (r is Warden warden) warden.AlertFailedTerminal(this);
            });
        }
        playersInCheck.Filter(Utils.PlayerById).ForEach(p => p.PrimaryRole<Fugitive>()!.EndSkillCheck(isSuccess));

        isInCheck = false;
        playersInCheck = [];
    }

    private void FinishHacking()
    {
        if (isDone) return;
        isDone = true;
        void ActuallyFinish()
        {
            int totalTerms = ETFGameMode.Instance.TerminalObjects.Count;
            int termsCompleted = ETFGameMode.Instance.TerminalObjects.Values.Count(o => o.ComputerProgress >= ExtraGamemodeOptions.EscapeOptions.TerminalHackTime);

            int termsRemaining = totalTerms - termsCompleted;
            Players.GetAllRoles().ForEach(r =>
            {
                if (r is Fugitive fugitive) fugitive.SetTermsRemaining(termsRemaining);
                else if (r is Warden warden) warden.SetTermsRemaining(termsRemaining);
            });
            RpcChangeSprite(HackedComputer);
            if (termsRemaining > 1) return;
            ETFGameMode.Instance.ExitObjects.Values.ForEach(o => o.MakeExitOpenable());
        }

        if (ETFGameMode.Instance.ObjectsSpawned)
        {
            ActuallyFinish();
            return;
        }

        Async.WaitUntil(() => {}, () => ETFGameMode.Instance.ObjectsSpawned, ActuallyFinish, .2f);
    }
}
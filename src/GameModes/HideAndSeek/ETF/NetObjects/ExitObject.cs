using Lotus.RPC.CustomObjects.Builtin;
using UnityEngine;
using VentLib.Utilities;

namespace Lotus.GameModes.HideAndSeek.ETF.NetObjects;

public sealed class ExitObject: FloatingText
{
    private const string LockedExit = "<size=100%><line-height=97%><cspace=0.16em><#0000>W</color><mark=#000000>WWWW</mark><#0000>W</color>\n<mark=#000000>WW</mark><mark=#121211>WW</mark><mark=#000000>WW\nW</mark><mark=#1c1c1b>WWWW</mark><mark=#000000>W\nW</mark><mark=#1c1c1b>WWWW</mark><mark=#000000>W\nW</mark><mark=#1c1c1b>WWWW</mark><mark=#000000>W\nW</mark><mark=#1c1c1b>WWWW</mark><mark=#000000>W</mark>";
    private const string UnlockedExit = "<size=100%><line-height=97%><cspace=0.16em><#0000>W</color><mark=#000000>WWWW</mark><#0000>W</color>\n<mark=#000000>WW</mark><#0000>WW</color><mark=#000000>WW\nW</mark><#0000>WWWW</color><mark=#000000>W\nW</mark><#0000>WWWW</color><mark=#000000>W\nW</mark><#0000>WWWW</color><mark=#000000>W\nW</mark><#0000>WWWW</color><mark=#000000>W</mark>";
    private const string AvailableExit = "<size=100%><line-height=97%><cspace=0.16em><#0000>W</color><mark=#000000>WWWW</mark><#0000>W</color>\n<mark=#000000>WW</mark><mark=#767875>WW</mark><mark=#000000>WW\nW</mark><mark=#767875>WWWW</mark><mark=#000000>W\nW</mark><mark=#767875>WWWW</mark><mark=#000000>W\nW</mark><mark=#767875>WWWW</mark><mark=#000000>W\nW</mark><mark=#767875>WWWW</mark><mark=#000000>W</mark>";
    private const string OpeningExit = "<size=100%><line-height=97%><cspace=0.16em><#0000>W</color><mark=#000000>WWWW</mark><#0000>W</color>\n<mark=#000000>WW</mark><mark=#d0d4cf>WW</mark><mark=#000000>WW\nW</mark><mark=#d0d4cf>WWWW</mark><mark=#000000>W\nW</mark><mark=#d0d4cf>WWWW</mark><mark=#000000>W\nW</mark><mark=#d0d4cf>WWWW</mark><mark=#000000>W\nW</mark><mark=#d0d4cf>WWWW</mark><mark=#000000>W</mark>";

    public bool CanEscape => isOpen;

    private bool openable;
    private bool isOpen;
    private byte openingPlayerId;

    public ExitObject(Vector2 position) : base(LockedExit, position)
    {
        openingPlayerId = byte.MaxValue;
    }

    public void MakeExitOpenable()
    {
        if (openable) return;
        openable = true;
        RpcChangeSprite(AvailableExit);
    }

    public bool TryOpenExit(byte playerId)
    {
        if (isOpen) return false;
        if (!openable) return false;
        if (openingPlayerId != byte.MaxValue) return false;
        openingPlayerId = playerId;
        RpcChangeSprite(OpeningExit);
        return true;
    }

    public bool CancelOpen(byte playerId)
    {
        if (isOpen) return true;
        if (openingPlayerId != playerId) return true;
        openingPlayerId = byte.MaxValue;
        RpcChangeSprite(AvailableExit);
        return true;
    }

    public void OpenExit()
    {
        if (isOpen) return;
        isOpen = true;
        RpcChangeSprite(UnlockedExit);
    }

    private static string GetIcon(Color color) => $"<size=400%><font=\"VCR SDF\"><line-height=85%>{color.Colorize("█")}</line-height></font></size>";
}
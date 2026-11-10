using Hazel;
using UnityEngine;
using VentLib.Networking.RPC;
using VentLib.Utilities;

namespace Lotus.RPC.CustomObjects.Builtin;
public sealed class RedFlag : Flag
{
    public RedFlag(Vector2 position) : base(position)
    {
        CreateNetObject("<size=100%><line-height=97%><cspace=0.16em><mark=#000000>W</mark><mark=#ff0000>WWW</mark><#0000>WW</color>\n<mark=#000000>W</mark><mark=#ff0000>WWWWW</mark>\n<mark=#000000>W</mark><#0000>WW</color><mark=#ff0000>WWW</mark>\n<mark=#000000>W</mark><#0000>WWWWW</color>\n<mark=#000000>W</mark><#0000>WWWWW</color>\n<mark=#000000>W</mark><#0000>WWWWW</color>", position);
    }
}

public sealed class BlueFlag : Flag
{
    public BlueFlag(Vector2 position): base(position)
    {
        CreateNetObject("<size=100%><line-height=97%><cspace=0.16em><mark=#000000>W</mark><mark=#0000ff>WWW</mark><#0000>WW</color>\n<mark=#000000>W</mark><mark=#0000ff>WWWWW</mark>\n<mark=#000000>W</mark><#0000>WW</color><mark=#0000ff>WWW</mark>\n<mark=#000000>W</mark><#0000>WWWWW</color>\n<mark=#000000>W</mark><#0000>WWWWW</color>\n<mark=#000000>W</mark><#0000>WWWWW</color>", position);
    }
}

public class Flag : CustomNetObject
{
    private Vector2 startPosition;

    public Flag(Vector2 position)
    {
        startPosition = position;
    }

    public void Return() => SnapTo(startPosition, SendOption.Reliable);
}
using System;
using Lotus.API.Odyssey;
using UnityEngine;
using VentLib.Utilities;

namespace Lotus.GUI.Name;

public class LiveString
{
    public static LiveString Empty = new("");

    private readonly Color? mainColor;
    private readonly Func<GameState, string> valueSupplier;

    public LiveString(Func<GameState, string> supplier, Color? color = null)
    {
        mainColor = color;
        valueSupplier = supplier;
    }

    public LiveString(string value, Color? color = null) : this(_ => value, color)
    {
    }

    // hopefully this won't decrease performance too bad
    public LiveString(Func<String> supplier, Color? color = null) : this(_ => supplier(), color)
    { }

    public static explicit operator LiveString(Func<GameState, string> supplier) => new(supplier);

    public static implicit operator Func<GameState, string>(LiveString liveString) => liveString.valueSupplier;

    public override string ToString() => GetValue(Game.State);
    public string GetValue(GameState curState) => mainColor == null ? valueSupplier(curState) : mainColor.Value.Colorize(valueSupplier(curState));
}
using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.API;
using Lotus.Logging;
using Lotus.Managers;
using UnityEngine;
using VentLib.Utilities.Extensions;

namespace Lotus.GameModes.HideAndSeek.ETF;

// ReSharper disable once InconsistentNaming
public class ETFPositions
{
    private static readonly Dictionary<MapNames, Vector2[]> TerminalPositions = new()
    {
        [MapNames.Skeld] = [
            new Vector2(-21.7f, -7.9f), new Vector2(-21.2f, -3.6f), new Vector2(-12.7f, -3.8f), new Vector2(-17.7f, 2.6f), new Vector2(-18.1f, -13.2f),
            new Vector2(-6.7f, -8.3f), new Vector2(-0.3f, -16.7f), new Vector2(-7.2f, -4.9f), new Vector2(1.6f, 5.4f), new Vector2(9.8f, 2.3f), new Vector2(6.5f, -3.5f),
            new Vector2(16.8f, -4.7f), new Vector2(9.2f, -12.2f), new Vector2(2.7f, -16.2f), new Vector2(5.7f, -9.6f)
        ],
        [MapNames.MiraHQ] = [
            new Vector2(-3.5f, 3.1f), new Vector2(16.2f, 0.3f), new Vector2(16.0f, 4.2f), new Vector2(9.7f, 3.9f), new Vector2(6.1f, 1.2f), new Vector2(2.4f, 13.2f),
            new Vector2(11.4f, 10.6f), new Vector2(14.8f, 20.3f), new Vector2(22.0f, 20.6f), new Vector2(17.8f, 23.7f), new Vector2(25.5f, 4.9f), new Vector2(25.5f, 0.1f),
            new Vector2(20.5f, -1.9f), new Vector2(19.5f, 4.6f)
        ],
        [MapNames.Polus] = [
            new Vector2(16.6f, -1.6f), new Vector2(9.7f, -12.1f), new Vector2(2.6f, -10.8f), new Vector2(2.1f, -16.9f), new Vector2(1.4f, -20.1f),
            new Vector2(12.3f, -16.1f), new Vector2(12.0f, -23.3f), new Vector2(22.3f, -17.7f), new Vector2(21.3f, -22.5f), new Vector2(7.0f, -7.3f), new Vector2(30.2f, -7.6f),
            new Vector2(40.3f, -7.5f), new Vector2(39.6f, -9.9f), new Vector2(36.5f, -19.5f), new Vector2(24.1f, -24.7f), new Vector2(20.4f, -11.8f)
        ],
        [MapNames.Airship] = [
            new Vector2(-11.9f, 11.0f), new Vector2(5.3f, 15.3f), new Vector2(4.7f, 8.9f), new Vector2(-7.2f, 0.9f), new Vector2(-13.3f, 1.6f), new Vector2(-20.9f, -1.1f),
            new Vector2(-14.0f, -4.9f), new Vector2(-14.5f, -8.4f), new Vector2(-6.5f, -11.2f), new Vector2(-10.5f, -12.3f), new Vector2(7.1f, -10.8f),
            new Vector2(25.1f, -9.8f), new Vector2(29.1f, -6.1f), new Vector2(37.6f, -3.2f), new Vector2(19.9f, 11.2f), new Vector2(10.8f, 9.0f), new Vector2(21.3f, -0.6f),
            new Vector2(15.4f, 1.9f)
        ],
        [MapNames.Fungle] = [
            Vector2.zero
        ]
    };

    private static readonly Dictionary<MapNames, Vector2[]> ExitPositions = new()
    {
        [MapNames.Skeld] = [
            new Vector2(-16.9f, -7.9f), new Vector2(-16.9f, -3.2f), new Vector2(-9.1f, 0.5f), new Vector2(3.8f, -1.9f), new Vector2(-4.0f, 5.2f), new Vector2(11.9f, -6.4f),
            new Vector2(5.1f, -12.6f), new Vector2(-3.2f, -10.5f), new Vector2(-8.1f, -11.4f), new Vector2(-12.2f, -14.3f)
        ],
        [MapNames.MiraHQ] = [
            new Vector2(-4.2f, -1.3f), new Vector2(12.5f, -1.3f), new Vector2(12.5f, 4.8f), new Vector2(6.1f, 6.4f), new Vector2(6.2f, 12.2f), new Vector2(17.8f, 11.7f),
            new Vector2(17.8f, 20.3f), new Vector2(23.2f, 6.6f), new Vector2(19.5f, 0.3f)
        ],
        [MapNames.Polus] = [
            new Vector2(27.8f, -22.7f), new Vector2(39.1f, -14.5f), new Vector2(27.4f, -12.4f), new Vector2(22.1f, -7.5f), new Vector2(20.6f, -14.5f), new Vector2(14.5f, -16.7f),
            new Vector2(16.4f, -25.4f), new Vector2(8.0f, -25.1f), new Vector2(7.3f, -15.4f), new Vector2(12.2f, -9.6f), new Vector2(25.7f, -7.0f)
        ],
        [MapNames.Airship] = [
            new Vector2(26.0f, 0.6f), new Vector2(12.7f, -2.5f), new Vector2(6.3f, -2.5f), new Vector2(0.1f, -0.9f), new Vector2(-0.3f, 8.7f), new Vector2(16.6f, 15.2f),
            new Vector2(-9.9f, -6.6f), new Vector2(-13.6f, -14.2f), new Vector2(1.5f, -12.3f), new Vector2(7.1f, -14.2f), new Vector2(28.9f, -1.5f), new Vector2(38.1f, 1.0f),
            new Vector2(-8.8f, 5.5f), new Vector2(10.3f, -6.2f)
        ],
        [MapNames.Fungle] = [
            Vector2.one
        ]
    };

    private readonly List<Vector2> myTerminalPositions = [];
    private readonly List<Vector2> myExitPositions = [];

    private readonly List<Vector2> chosenExitPositions = [];

    public ETFPositions()
    {
        MapNames realName = (MapNames)AUSettings.MapId();
        MapNames givenName = realName is MapNames.Dleks ? MapNames.Skeld : realName;

        try
        {
            DevLogger.Log($"checking. {givenName} ({ExitPositions.ContainsKey(givenName)} - {TerminalPositions.ContainsKey(givenName)})");
            myExitPositions = ExitPositions[givenName].ToList();
            myTerminalPositions = TerminalPositions[givenName].ToList();
        }
        catch (Exception exception)
        {
            FatalErrorHandler.ForceEnd(exception, "Setup Phase");
        }
        if (realName is not MapNames.Dleks) return;
        myExitPositions = myExitPositions.Select(v => new Vector2(-v.x, v.y)).ToList();
        myTerminalPositions = myTerminalPositions.Select(v => new Vector2(-v.x, v.y)).ToList();
    }

    public Vector2? NewTerminalPosition()
    {
        if (!myTerminalPositions.Any()) return null;
        return myTerminalPositions.PopRandom();
    }

    public Vector2? NewExitPosition()
    {
        if (!myExitPositions.Any()) return null;
        if (!chosenExitPositions.Any())
        {
            Vector2 position = myExitPositions.PopRandom();
            chosenExitPositions.Add(position);
            return position;
        }

        Vector2 furthestPosition = Vector2.zero;
        float maxDistance = float.MinValue;

        foreach (var position in myExitPositions)
        {
            float minDistanceToExit = float.MaxValue;

            chosenExitPositions.ForEach(pos =>
            {
                float distance = Vector2.Distance(position, pos);
                if (distance < minDistanceToExit)
                {
                    minDistanceToExit = distance;
                }
            });

            if (minDistanceToExit > maxDistance)
            {
                maxDistance = minDistanceToExit;
                furthestPosition = position;
            }
        }

        myExitPositions.Remove(furthestPosition);
        chosenExitPositions.Add(furthestPosition);
        return furthestPosition;
    }
}
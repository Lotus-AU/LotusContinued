using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppSystem;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Chat.Commands;
using Lotus.Extensions;
using Lotus.Managers.History;
using Lotus.Managers.History.Events;
using Lotus.Options;
using Lotus.Roles;
using Lotus.Roles.Builtins;
using Lotus.Roles.Interfaces;
using Lotus.Victory;
using Lotus.Victory.Conditions;
using TMPro;
using UnityEngine;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Object = UnityEngine.Object;

namespace Lotus.Victory.Patches;

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
public class EndGameResultPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(EndGameResultPatch));

    public static void Postfix(EndGameManager __instance)
    {
        var winnerText = Object.Instantiate(__instance.WinText, __instance.transform);
        WinDelegate winDelegate = Game.GetWinDelegate();
        string winResult = new Optional<IWinCondition>(winDelegate.WinCondition()).Map(wc =>
        {
            string t;
            if (wc is IFactionWinCondition factionWin)
            {
                t = factionWin.Factions().Select(f => f.Color.Colorize(f.Name())).Distinct().Fuse();
                List<FrozenPlayer> additionalWinners = Game.MatchData.GameHistory.AdditionalWinners;
                if (additionalWinners.Count > 0)
                {
                    string awText = additionalWinners.Select(fp => new List<CustomRole> { fp.MainRole }.Concat(fp.Subroles).MaxBy(r => r.DisplayOrder)).Fuse();
                    t += $" + {awText}";
                }
            }
            else t = Game.MatchData.GameHistory.LastWinners.Select(lw => lw.MainRole.ColoredRoleName()).Fuse();

            if (Game.MatchData.GameHistory.LastWinners.Any()) __instance.BackgroundBar.material.color = Game.MatchData.GameHistory.LastWinners.First().MainRole.RoleColor;

            string? wcText = wc.GetWinReason().ReasonText;
            string reasonText = wcText == null ? "" : $"\n<size=1.5>{LastResultCommand.LRTranslations.WinReasonText.Formatted(wcText)}</size>";
            return $"<size=3>{LastResultCommand.LRTranslations.WinResultText.Formatted(t)}</size>{reasonText}";
        }).OrElse("").TrimStart('\n', '\r');
        {

            var pos = __instance.WinText.transform.localPosition;
            pos.y = 1.5f;
            winnerText.transform.position = pos;
            winnerText.text = Color.white.Colorize(winResult);
            winnerText.name = "WinnerText";
        }

        var position = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, Camera.main.nearClipPlane));

        GameObject gameInfo = UnityEngine.Object.Instantiate(__instance.WinText.gameObject, __instance.transform);
        gameInfo.transform.localScale = new Vector3(.8f, .8f, 1f);

        TMPro.TMP_Text gameInfoTextMesh = gameInfo.GetComponent<TMPro.TMP_Text>();
        gameInfoTextMesh.alignment = TMPro.TextAlignmentOptions.TopLeft;
        gameInfoTextMesh.color = Color.white;
        gameInfoTextMesh.fontSizeMin = 1.5f;
        gameInfoTextMesh.fontSizeMax = 1.5f;
        gameInfoTextMesh.fontSize = 1.5f;

        var gameInfoTextMeshRectTransform = gameInfoTextMesh.GetComponent<RectTransform>();
        gameInfoTextMeshRectTransform.anchoredPosition = new Vector2(position.x + 3.5f, position.y - 0.1f);

        gameInfo.transform.localPosition = new Vector3(-10, 2.9f, -14);

        List<PlayerHistory>? playerHistory = Game.MatchData.GameHistory.PlayerHistory;
        if (playerHistory == null)
        {
            gameInfoTextMesh.text = "Could not generate game history.";
            return;
        }
        gameInfoTextMesh.text = "End Game Result:\n";


        const string indent = "  {0}";

        HashSet<byte> winners = Game.MatchData.GameHistory.LastWinners.Select(p => p.MyPlayer.PlayerId).ToHashSet();
        playerHistory
            .Where(ph => ph.MainRole is not GameMaster)
            .OrderBy(StatusOrder)
            .ForEach(history =>
            {
                bool isWinner = winners.Contains(history.PlayerId);

                string winnerPrefix = isWinner ? ModConstants.Palette.WinnerColor.Colorize("★ ") + "{0}" : indent;

                string statusText = history.Status is PlayerStatus.Dead ? history.CauseOfDeath?.SimpleName() ?? history.Status.ToString() : history.Status.ToString();
                string playerStatus = LastResultCommand.StatusColor(history.Status).Colorize(statusText);

                string statText = history.MainRole.Statistics().FirstOrOptional().Map(t => $" | {t.Name()}: {t.GetGenericValue(history.UniquePlayerId)}").OrElse("");

                int colorId = history.Outfit.ColorId;
                string coloredName = ((Color)Palette.PlayerColors[colorId]).Colorize(ModConstants.ColorNames[colorId]);
                string modifiers = history.Subroles.Count == 0 ? "" : $" {history.Subroles
                    .Where(sr => sr is ISubrole)
                    .Select(sr => sr.RoleColor.Colorize(((ISubrole)sr).Identifier() ?? sr.RoleName))
                    .Fuse("")}";

                gameInfoTextMesh.text += winnerPrefix.Formatted($"{history.Name} : {coloredName} - {playerStatus} ({history.MainRole.ColoredRoleName()}{modifiers}){statText}\n");
            });

        if (GeneralOptions.MiscellaneousOptions.EventLogType != 2)
        {
            gameInfoTextMesh.text += "\n\nEvent Log:\n";

            List<IHistoryEvent> historyEvents = Game.MatchData.GameHistory.Events;
            if (GeneralOptions.MiscellaneousOptions.EventLogType == 1) historyEvents = historyEvents.Where(h => h is IKillEvent).ToList();
            if (historyEvents.Any()) historyEvents.ForEach(history => gameInfoTextMesh.text += history.GenerateMessage() + "\n");
            else gameInfoTextMesh.text += "No events for this session.";
        }

        // The code onwards is so inefficient...
        // but do I care? That's right, no. I do not.

        Object.FindObjectsOfType<PoolablePlayer>()
            .Where(p => p.transform.parent == __instance.transform)
            .Do(p => p.gameObject.Destroy());

        bool flag = GameManager.Instance.DidHumansWin(EndGameResult.CachedGameOverReason);
        var cachedPlayerData = EndGameResult.CachedWinners;

        int row = 0;
        int playerIndex = 0;
        int playersInRow = 1;
        int bottomRowCount = 9;

        float zBase = -8f;
        float xSpacing = .75f;
        float baseScale = 0.9f;
        float rowHeight = 1f;

        while (playersInRow > 0)
        {
            playersInRow = bottomRowCount - row * 2;
            if (playersInRow <= 0) break;

            float startX = -(playersInRow - 1) * xSpacing / 2;
            float rowYBase = row * rowHeight;
            int midpoint = playersInRow / 2;

            int[] positions = new int[playersInRow];

            for (int i = 0; i < playersInRow; i++)
            {
                positions[i] = i;
            }

            int[] reorderedPositions = new int[playersInRow];

            reorderedPositions[0] = midpoint;

            int rightPos = midpoint + 1;
            int leftPos = midpoint - 1;
            int index = 1; // Start at 1 because we already placed the middle position

            while (index < playersInRow)
            {
                // Place right player if within bounds
                if (rightPos < playersInRow && index < playersInRow)
                {
                    reorderedPositions[index++] = rightPos++;
                }

                // Place left player if within bounds
                if (leftPos >= 0 && index < playersInRow)
                {
                    reorderedPositions[index++] = leftPos--;
                }
            }

            // create players
            for (int i = 0; i < playersInRow && playerIndex < cachedPlayerData.Count; i++)
            {
                int posIndex = reorderedPositions[i];
                float xPosition = startX + posIndex * xSpacing;
                int distanceFromMid = Mathf.Abs(posIndex - midpoint);

                float zPosition = zBase + distanceFromMid * 2f;
                float yPosition = rowYBase - (midpoint - distanceFromMid) * .1f;

                CachedPlayerData cachedPlayerData2 = cachedPlayerData[playerIndex];

                PoolablePlayer poolablePlayer = Object.Instantiate<PoolablePlayer>(__instance.PlayerPrefab, __instance.transform);
                poolablePlayer.transform.localPosition = new Vector3(xPosition, yPosition -1.2f, zPosition) * 0.9f;

                float scale = Mathf.Lerp(baseScale, 0.65f, (float)distanceFromMid / (playersInRow / 2)) * 0.9f;
                Vector3 scaleVector = new(scale, scale, 1f);
                poolablePlayer.transform.localScale = scaleVector;

                bool shouldFlipX = (posIndex - midpoint) < 0;
                if (cachedPlayerData2.IsDead)
                {
                    poolablePlayer.SetBodyAsGhost();
                    poolablePlayer.SetDeadFlipX(shouldFlipX);
                }
                else poolablePlayer.SetFlipX(shouldFlipX);

                poolablePlayer.UpdateFromPlayerOutfit(cachedPlayerData2.Outfit, PlayerMaterial.MaskType.None, cachedPlayerData2.IsDead, true, null, false);
                if (flag) poolablePlayer.ToggleName(false);
                else
                {
                    Color color = cachedPlayerData2.IsImpostor ? Palette.ImpostorRed : Palette.White;
                    poolablePlayer.SetName(cachedPlayerData2.PlayerName, scaleVector.Inv(), color, -15f);
                    Vector3 namePosition = new Vector3(0f, -1.31f, -0.5f);
                    poolablePlayer.SetNamePosition(namePosition);
                    if (AprilFoolsMode.ShouldHorseAround() && GameOptionsManager.Instance.CurrentGameOptions.GameMode == AmongUs.GameOptions.GameModes.HideNSeek)
                    {
                        poolablePlayer.SetBodyType(PlayerBodyTypes.Normal);
                        poolablePlayer.SetFlipX(false);
                    }
                }

                playerIndex++;
            }

            zBase += playersInRow + 1;
            baseScale -= .1f;

            row++;
        }

        Async.Schedule(() => AmongUsClient.Instance.StartCoroutine(LerpEndGame(__instance, winnerText, gameInfoTextMesh).WrapToIl2Cpp()), 3.2f);

        float DeathTimeOrder(PlayerHistory ph) => ph.CauseOfDeath == null ? 0f : (7200f - (float)ph.CauseOfDeath.Timestamp().TimeSpan().TotalSeconds) / 7200f;
        float StatusOrder(PlayerHistory ph) => winners.Contains(ph.PlayerId) ? (float)ph.Status + DeathTimeOrder(ph) : (float)ph.Status + 99 + DeathTimeOrder(ph);
    }

    private static IEnumerator LerpEndGame(EndGameManager gameManager, TextMeshPro winnerText, TMP_Text gameInfoText)
    {
        if (gameManager == null) yield break;
        float LERP_DURATION = 2f;

        //-- Objects

        //-- Targets and positions
        Vector3 egmCurPos = gameManager.transform.localPosition;
        Vector3 egmEndPos = new(2, 0, 0);
        Vector3 winTextCurPos = gameManager.WinText.transform.localPosition;
        Vector3 winTextEndPos = new(0, 2.8f, -14f);
        Vector3 winTextCurScale = gameManager.WinText.transform.localScale;
        Vector3 winTextEndScale = new(1.2f, 1.2f, 1f);
        Vector3 winnerCurPos = winnerText.transform.localPosition;
        Vector3 winnerEndPos = new(0, 2.1f, -14);
        Vector3 gameCurPos = gameInfoText.transform.localPosition;
        Vector3 gameEndPos = new(-2.5333f - 2, 2.9f, -14);
        Vector3 bgBarCurScale = gameManager.BackgroundBar.transform.localScale;
        Vector3 bgBarEndScale = new(10.667f, 2.5f, 100f);

        Vector3 exitCurPos = gameManager.Navigation.ExitButton.transform.localPosition;
        Vector3 exitEndPos = new(-1.4167f, -2.3f, -14f);
        Vector3 playAgainCurPos = gameManager.Navigation.PlayAgainButton.transform.localPosition;
        Vector3 playAgainEndPos = new(4.7833f, -2.3f, -14f);

        Vector3 defScale = new(0.8f, 0.8f, 1f);

        var poolablePlayer = gameManager.Navigation.ProgressionScreen.FindChild<Transform>("PoolablePlayer");
        var levelAndXp = gameManager.Navigation.ProgressionScreen.FindChild<Transform>("LevelAndXp");
        var titleText = gameManager.Navigation.ProgressionScreen.FindChild<TextMeshPro>("TitleText");

        titleText.transform.localPosition = new(0, 1.9f, 9f);
        levelAndXp.transform.localPosition = new(0, -.3f, 0f);
        poolablePlayer.transform.localPosition = new(-.08f, .67f, 0f);
        poolablePlayer.transform.localScale = new(0.6f, 0.6f, 1f);

        float elasped = 0f;
        while (elasped < LERP_DURATION)
        {
            if (gameManager == null || gameManager.gameObject == null || !Object.IsNativeObjectAlive(gameManager.gameObject) || !gameManager.gameObject.activeSelf) break;
            elasped += Time.deltaTime;
            float t = Mathf.Clamp01(EaseSineInOut(elasped / LERP_DURATION));

            gameManager.transform.localPosition = Vector3.Lerp(egmCurPos, egmEndPos, t);
            gameInfoText.transform.localPosition = Vector3.Lerp(gameCurPos, gameEndPos, t);
            winnerText.transform.localPosition = Vector3.Lerp(winnerCurPos, winnerEndPos, t);
            gameManager.WinText.transform.localPosition = Vector3.Lerp(winTextCurPos, winTextEndPos, t);
            gameManager.WinText.transform.localScale = Vector3.Lerp(winTextCurScale, winTextEndScale, t);
            gameManager.BackgroundBar.transform.localScale = Vector3.Lerp(bgBarCurScale, bgBarEndScale, t);
            gameManager.Navigation.ExitButton.transform.localScale = Vector3.Lerp(Vector3.one, defScale, t);
            gameManager.Navigation.ExitButton.transform.localPosition = Vector3.Lerp(exitCurPos, exitEndPos, t);
            gameManager.Navigation.PlayAgainButton.transform.localScale = Vector3.Lerp(Vector3.one, defScale, t);
            gameManager.Navigation.PlayAgainButton.transform.localPosition = Vector3.Lerp(playAgainCurPos, playAgainEndPos, t);
            yield return null;
        }

        float EaseSineInOut(float t) =>  0.5f * (1 - Mathf.Cos(t * Mathf.PI));
    }
}
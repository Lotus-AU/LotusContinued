using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using Lotus.Utilities;
using Lotus.Extensions;
using Lotus.GUI.Menus;
using Lotus.Logging;
using TMPro;
using UnityEngine;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;
using VentLib.Utilities.Optionals;
using Object = UnityEngine.Object;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace Lotus.GUI.Patches;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
class SplashPatch
{
    public static GameObject AmongUsLogo = null!;
    private static UnityOptional<GameObject> _customSplash = UnityOptional<GameObject>.Null();
    internal static ModUpdateMenu ModUpdateMenu = null!;
    internal static UnityOptional<GameObject> UpdateButton = UnityOptional<GameObject>.Null();
    public static bool UpdateReady;
    public static PassiveButton playLocalButton = null!;

    private static GameObject howToPlayButton = null!;

    private static bool onlineBgActive;

    private static (string name, float pixelsPerUnit)[] buttonsToFind = [
        ("PlayButton", 105), ("AcountButton", 100), ("SettingsButton", 100), ("BottomButtonBounds/CreditsButton", 100), ("BottomButtonBounds/ExitGameButton", 100)
    ]; // yes "Account" is mispelled lmao


    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    static void Prefix(MainMenuManager __instance)
    {
        if ((AmongUsLogo = GameObject.Find("LOGO-AU")) != null)
        {
            AmongUsLogo.transform.localPosition = new Vector3(0.008f, -0.3125f, 0f);
            AmongUsLogo.GetComponent<SpriteRenderer>().sprite = LotusAssets.LoadSprite("main_menu/newautitle.png", 110);
            AmongUsLogo.transform.parent.GetComponent<AspectSize>().Destroy();
            Async.Schedule(() => AmongUsLogo.transform.localScale = new Vector3(0.35f, 0.35f, 1f), 0.01f);
        }

        /*GameObject playOnlineButton = GameObject.Find("PlayOnlineButton");
        playOnlineButton.transform.localPosition = new Vector3(-4.15f, -0.65f); // 1.25 is good

        GameObject playLocalButton = GameObject.Find("PlayLocalButton");
        playLocalButton.transform.localPosition = new Vector3(-4.15f, -1.65f); // 2.25 is good

        howToPlayButton = GameObject.Find("HowToPlayButton");
        howToPlayButton.transform.localPosition = new Vector3(-4.15f, -2.5f);
        Async.Schedule(() => howToPlayButton.GetComponentInChildren<TextMeshPro>().text = "Settings", 0.1f);

        GameObject freePlayButton = GameObject.Find("FreePlayButton");
        freePlayButton.gameObject.SetActive(false); // TODO: make discord button

        GameObject quitButton = GameObject.Find("/MainUI/ExitGameButton");
        quitButton.transform.localPosition = new Vector3(4.5f, -2.5f);

        AccountTab accountTab = Object.FindObjectOfType<AccountTab>();
        AccountTabPatch.ModifyAccountTabLocation(accountTab);*/


        /*AdjustBottomButtons(__instance, GameObject.Find("BottomButtons"));*/

        DevLogger.Log("??");
        AccountManager.Instance.accountTab.FindChild<Transform>("AccountWindow/Card", true).localPosition = new Vector3(0f, 1.3f, 0f);
        AccountManager.Instance.accountTab.FindChildOrEmpty<Transform>("GameHeader").IfPresent(g => g.gameObject.SetActive(false));
        DevLogger.Log("?????");


        __instance.screenTint.gameObject.transform.localPosition += new Vector3(1000f, 0f);
        __instance.screenTint.enabled = false;
        __instance.rightPanelMask.SetActive(true);
        // The background texture (large sprite asset)
        __instance.mainMenuUI.FindChild<SpriteRenderer>("BackgroundTexture").transform.gameObject.SetActive(false);
        // The glint on the Among Us Menu
        __instance.mainMenuUI.FindChild<SpriteRenderer>("WindowShine").transform.gameObject.SetActive(false);
        __instance.mainMenuUI.FindChild<Transform>("ScreenCover").gameObject.SetActive(false);

        GameObject leftPanel = __instance.mainMenuUI.FindChild<Transform>("LeftPanel").gameObject;
        GameObject rightPanel = __instance.mainMenuUI.FindChild<Transform>("RightPanel").gameObject;
        rightPanel.GetComponent<SpriteRenderer>().enabled = false;
        GameObject maskedBlackScreen = rightPanel.FindChild<Transform>("MaskedBlackScreen").gameObject;
        maskedBlackScreen.GetComponent<SpriteRenderer>().enabled = false;
        Transform accountButtons = maskedBlackScreen.FindChild<Transform>("AccountButtons", true);
        accountButtons.gameObject.FindChild<Transform>("Divider", true).localPosition += new Vector3(1000f, 0f);
        accountButtons.gameObject.FindChild<Transform>("Header", true).localPosition += new Vector3(1000f, 0f);
        accountButtons.FindChild("Account_CTA").localPosition += new Vector3(0, 1, 0);
        accountButtons.FindChild("Stats_CTA").localPosition += new Vector3(0, 1, 0);

        maskedBlackScreen.transform.localPosition = new Vector3(-3.345f, -2.05f);
        maskedBlackScreen.transform.localScale = new Vector3(7.35f, 4.5f, 4f);

        leftPanel.GetComponent<SpriteRenderer>().enabled = false;
        leftPanel.FindChild<SpriteRenderer>("Divider").enabled = false;
        leftPanel.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name == "Shine").ForEach(r => r.enabled = false);

        float iconWidth = OperatingSystem.IsWindows() ? 1.425f : 1.3f;
        float colliderWidth = 1.2f;
        PassiveButton inventoryButton = MakeIconButton(__instance.inventoryButton, new Vector3(0.558f, 0.558f, 1), LotusAssets.LoadSprite("main_menu/InventoryIconInactive.png"),
            activeSprite: LotusAssets.LoadSprite("main_menu/InventoryIconHighlighted.png"));
        inventoryButton.transform.localPosition = new Vector3(1.06f, -2.403f, 0f);
        foreach (var scaledSprite in inventoryButton.GetComponent<AspectScaledAsset>().allSprites)
        {
            scaledSprite.Sprite.size = new Vector2(iconWidth, 1.415f);
            scaledSprite.OriginalWidth = iconWidth;
        }
        foreach (var scaledCollider in inventoryButton.GetComponent<AspectScaledAsset>().allColliders)
        {
            scaledCollider.Collider.size = new Vector2(colliderWidth, 0.6565f);
            scaledCollider.OriginalWidth = iconWidth;
        }

        PassiveButton discordButton = Object.Instantiate(inventoryButton, __instance.transform);
        discordButton.transform.localPosition = new Vector3(0.08f, -2.403f, 0f);
        discordButton.transform.localScale = new Vector3(0.329f, 0.558f, 1f);
        discordButton.Modify(() => Application.OpenURL(ModConstants.DiscordInvite));
        {
            var ogRender = discordButton.inactiveSprites.GetComponent<SpriteRenderer>();
            ogRender.enabled = false;
            SpriteRenderer discordRenderer;
            {
                var discordIcon = new GameObject("DiscordIcon");
                discordIcon.transform.SetParent(discordButton.gameObject.transform);
                discordIcon.transform.localPosition = Vector3.zero;
                discordRenderer = discordIcon.AddComponent<SpriteRenderer>();
                discordIcon.transform.localScale = new Vector3(1.7f, 1f, 1f);
            }
            Sprite activeSprite = LotusAssets.LoadSprite("main_menu/DiscordHighlighted.png", 200);
            Sprite inactiveSprite = LotusAssets.LoadSprite("main_menu/DiscordInactive.png", 200);
            ogRender.sprite = activeSprite;
            discordButton.activeSprites = null;
            discordButton.OnMouseOver = new UnityEngine.Events.UnityEvent();
            discordButton.OnMouseOut = new UnityEngine.Events.UnityEvent();
            discordButton.OnMouseOver.AddListener((Action)(() => discordRenderer.sprite = ogRender.sprite));
            discordButton.OnMouseOut.AddListener((Action)(() => discordRenderer.sprite = inactiveSprite));
            discordRenderer.sprite = inactiveSprite;
            discordRenderer.enabled = true;
            Async.Schedule(() => discordRenderer.sprite = inactiveSprite, 0.001f);
        }
        discordButton.name = "DiscordButton";

        PassiveButton websiteButton = Object.Instantiate(inventoryButton, __instance.transform);
        websiteButton.transform.localPosition = new Vector3(-0.9f, -2.4f, 0f);
        websiteButton.transform.localScale = new Vector3(0.28f, 0.278f, 1f);
        websiteButton.Modify(() => Application.OpenURL(ModConstants.WebsiteLink));
        {
            var ogRender = websiteButton.inactiveSprites.GetComponent<SpriteRenderer>();
            ogRender.enabled = false;
            SpriteRenderer websiteRenderer;
            {
                var websiteIcon = new GameObject("WebsiteIcon");
                websiteIcon.transform.SetParent(websiteButton.gameObject.transform);
                websiteIcon.transform.localPosition = Vector3.zero;
                websiteRenderer = websiteIcon.AddComponent<SpriteRenderer>();
                websiteIcon.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            Sprite activeSprite = LotusAssets.LoadSprite("main_menu/WebsiteHighlighted.png");
            Sprite inactiveSprite = LotusAssets.LoadSprite("main_menu/WebsiteInactive.png");
            ogRender.sprite = activeSprite;
            websiteButton.activeSprites = null;
            websiteButton.OnMouseOver = new UnityEngine.Events.UnityEvent();
            websiteButton.OnMouseOut = new UnityEngine.Events.UnityEvent();
            websiteButton.OnMouseOver.AddListener((Action)(() => websiteRenderer.sprite = ogRender.sprite));
            websiteButton.OnMouseOut.AddListener((Action)(() => websiteRenderer.sprite = inactiveSprite));
            websiteRenderer.sprite = inactiveSprite;
            websiteRenderer.enabled = true;
            Async.Schedule(() => websiteRenderer.sprite = inactiveSprite, 0.001f);
        }
        websiteButton.name = "WebsiteButton";

        PassiveButton shopButton = MakeIconButton(__instance.shopButton, new Vector3(0.558f, 0.558f, 1), LotusAssets.LoadSprite("main_menu/ShopIconInactive.png"),
            activeSprite: LotusAssets.LoadSprite("main_menu/ShopIconHighlighted.png"));
        shopButton.transform.localPosition = new Vector3(2.04f, -2.403f, 0f);
        foreach (var scaledSprite in shopButton.GetComponent<AspectScaledAsset>().allSprites)
        {
            scaledSprite.Sprite.size = new Vector2(iconWidth, 1.415f);
            scaledSprite.OriginalWidth = iconWidth;
        }
        foreach (var scaledCollider in shopButton.GetComponent<AspectScaledAsset>().allColliders)
        {
            scaledCollider.Collider.size = new Vector2(colliderWidth, 0.6565f);
            scaledCollider.OriginalWidth = iconWidth;
        }

        PassiveButton newsButton = MakeIconButton(__instance.newsButton, new Vector3(0.558f, 0.558f, 1), LotusAssets.LoadSprite("main_menu/AnnouncementIconInactive.png"),
            activeSprite: LotusAssets.LoadSprite("main_menu/AnnouncementIconHighlighted.png"));
        newsButton.transform.localPosition = new Vector3(3.02f, -2.36f, 0f);
        foreach (var scaledSprite in newsButton.GetComponent<AspectScaledAsset>().allSprites)
        {
            scaledSprite.Sprite.size = new Vector2(iconWidth, 1.415f);
            scaledSprite.OriginalWidth = iconWidth;
        }
        foreach (var scaledCollider in newsButton.GetComponent<AspectScaledAsset>().allColliders)
        {
            scaledCollider.Collider.size = new Vector2(colliderWidth, 0.6565f);
            scaledCollider.OriginalWidth = iconWidth;
        }

        // next would be new Vector3(4f, -2.403f, 0f);

        __instance.playButton.transform.localPosition -= new Vector3(0f, 1.4f);
        // __instance.playButton.transform.localPosition += new Vector3(.02f, 0f, 0f);

        // SpriteRenderer activeSpriteRender = __instance.playButton.activeSprites.GetComponent<SpriteRenderer>();
        // activeSpriteRender.color = new Color(1f, 0f, 0.62f);

        // SpriteRenderer inactiveSpriteRender = __instance.playButton.inactiveSprites.GetComponent<SpriteRenderer>();
        // inactiveSpriteRender.color = new Color(1f, 0f, 0.35f);
        // inactiveSpriteRender.sprite = activeSpriteRender.sprite;

        var playBG = new GameObject("PlayOnlineAnchor");
        playBG.transform.localPosition = new Vector3(1.5f, .4f, 0f);
        playBG.transform.localScale = Vector3.one;
        playBG.gameObject.SetActive(false);

        var bgRenderer = playBG.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = LotusAssets.LoadSprite("Credits/Images/background.png", 180);

        __instance.playButton.activeTextColor = Color.white;
        __instance.playButton.inactiveTextColor = Color.white;

        var enterCodeButtons = __instance.FindChild<Transform>("EnterCodeButtons", true).gameObject;
        enterCodeButtons.GetComponent<AspectPosition>().Destroy();
        enterCodeButtons.transform.SetParent(playBG.transform);
        var onlineButtons = __instance.onlineButtons;
        onlineButtons.GetComponent<AspectPosition>().Destroy();
        onlineButtons.FindChild<AspectSize>("AspectSize").Destroy();

        onlineButtons.transform.SetParent(playBG.transform);
        Async.Schedule(() =>
        {
            if (playBG.gameObject.active) onlineButtons.gameObject.SetActive(true);
        }, .1f);

        if (OperatingSystem.IsAndroid())
        {
            Vector3 nudgeLeft = new Vector3(-0.5f, 0, 0);
            Vector3 nudgeRight = new Vector3(0.5f, 0, 0);
            Async.Schedule(() => onlineButtons.FindChild<Transform>("Divider").localScale
                = enterCodeButtons.FindChild<Transform>("Divider").localScale, .1f);
            enterCodeButtons.FindChild<AspectPosition>("TitleEnterCode").Destroy();
            enterCodeButtons.FindChild<AspectPosition>("BackButton").Destroy();
            onlineButtons.FindChild<AspectPosition>("Text_TMP").Destroy();
            enterCodeButtons.FindChild<Transform>("TitleEnterCode").localPosition += nudgeLeft;
            enterCodeButtons.FindChild<Transform>("BackButton").localPosition += nudgeRight;
            onlineButtons.FindChild<Transform>("Text_TMP").localPosition += nudgeLeft;
        }

        __instance.onlineButtons = playBG;

        SpriteRenderer onlineDivider = onlineButtons.FindChild<SpriteRenderer>("Divider");
        onlineDivider.transform.localPosition = new(0, 1.84f, 0f);
        onlineDivider.transform.localScale = new(0.8f, 1f, 1f);
        onlineButtons.transform.localPosition = new(0, -.35f, -9);
        {
            onlineButtons.FindChild<AspectPosition>("Text_TMP").DistanceFromEdge = new(-.3f, 0f, 0f);
            onlineButtons.FindChild<PassiveButton>("BackButton").gameObject.SetActive(false);

            PassiveButton enterCodeButton = onlineButtons.FindChild<PassiveButton>("Enter Code Button");
            enterCodeButton.Modify(() =>
            {
                onlineButtons.gameObject.SetActive(false);
                enterCodeButtons.gameObject.SetActive(true);
            });
        }

        SpriteRenderer codeDivider = enterCodeButtons.FindChild<SpriteRenderer>("Divider");
        codeDivider.transform.localPosition = new(0, 1.84f, 0f);
        codeDivider.transform.localScale = new(0.61f, 1f, 1f);
        enterCodeButtons.transform.localPosition = new(0, -.3f, -9);
        {
            PassiveButton backButton = enterCodeButtons.FindChild<PassiveButton>("BackButton");
            backButton.Modify(() =>
            {
                onlineButtons.gameObject.SetActive(true);
                enterCodeButtons.gameObject.SetActive(false);
            });
        }

        __instance.playButton.Modify(() =>
        {
            __instance.ResetScreen();
            Async.Schedule(() =>
            {
                onlineBgActive = !onlineBgActive;
                playBG.gameObject.SetActive(onlineBgActive);
                onlineButtons.gameObject.SetActive(true);
                enterCodeButtons.gameObject.SetActive(false);
            }, .01f);
        });
        Async.Schedule(() => __instance.playButton.buttonText.text = "Play Online", 0.001f);

        // you don't even want to know the pain we went through to do this...
        buttonsToFind.ForEach(buttonInfo =>
        {
            GameObject buttonObject = GameObject.Find("Main Buttons/" + buttonInfo.name);
            if (buttonObject == null)
            {
                StaticLogger.Debug($"Could not find main menu button named: {buttonInfo.name}");
                return;
            }

            string directoryName = "main_menu/" + buttonInfo.name switch
            {
                "BottomButtonBounds/CreditsButton" => "Credits",
                "BottomButtonBounds/ExitGameButton" => "Quit",
                "SettingsButton" => "Bottom",
                "AcountButton" => "Bottom",
                "PlayButton" => "Top",
                _ => buttonInfo.name.Replace("Button", "")
            };
            buttonObject.FindChild<SpriteRenderer>("Highlight", true).sprite = LotusAssets.LoadSprite(directoryName + "Highlighted.png", buttonInfo.pixelsPerUnit);
            buttonObject.FindChild<SpriteRenderer>("Inactive", true).sprite = LotusAssets.LoadSprite(directoryName + "Inactive.png", buttonInfo.pixelsPerUnit);
            buttonObject.FindChild<SpriteRenderer>("Highlight", true).color = Color.white;
            buttonObject.FindChild<SpriteRenderer>("Inactive", true).color = Color.white;
            switch (buttonInfo.name)
            {
                case "BottomButtonBounds/ExitGameButton":
                    buttonObject.transform.localPosition = new Vector3(0.8854f, 0, buttonObject.transform.localPosition.z);
                    buttonObject.transform.localScale = new Vector3(0.86f, 1.1f, buttonObject.transform.localScale.z);
                    Transform fontPlacerExitGame = buttonObject.FindChild<Transform>("FontPlacer", true);
                    fontPlacerExitGame.localPosition = new Vector3(-0.1753f, 0.0217f, fontPlacerExitGame.localPosition.z);
                    fontPlacerExitGame.localScale = new Vector3(1f, 0.8f, fontPlacerExitGame.localScale.z);
                    break;
                case "BottomButtonBounds/CreditsButton":
                    buttonObject.transform.localPosition = new Vector3(-0.9067f, 0f, buttonObject.transform.localPosition.z);
                    buttonObject.transform.localScale = new Vector3(0.86f, 1.1f, buttonObject.transform.localScale.z);
                    Transform fontPlacerCredits = buttonObject.FindChild<Transform>("FontPlacer", true);
                    fontPlacerCredits.localPosition = new Vector3(-0.1753f, 0.0217f, fontPlacerCredits.localPosition.z);
                    fontPlacerCredits.localScale = new Vector3(1f, 0.8f, fontPlacerCredits.localScale.z);
                    Async.Schedule(() => fontPlacerCredits.gameObject.FindChild<TextMeshPro>("Text_TMP", true).text = "Pl Credits", 0.001f);
                    // GameObject.Find("CreditsButton/FontPlacer/Text_TMP").GetComponent<TextMeshPro>().text = "PL Credits";
                    break;
                case "SettingsButton":
                    buttonObject.transform.localPosition = new Vector3(0.0004f, -1.2905f, buttonObject.transform.localPosition.z);
                    buttonObject.transform.localScale = new Vector3(0.842f, 0.962f, buttonObject.transform.localScale.z);
                    Transform fontPlacerSettings = buttonObject.FindChild<Transform>("FontPlacer", true);
                    fontPlacerSettings.localPosition = new Vector3(0f, -0.099f, fontPlacerSettings.localPosition.z);
                    fontPlacerSettings.localScale = new Vector3(1, 1, fontPlacerSettings.localScale.z);
                    break;
                case "AcountButton":
                    buttonObject.transform.localPosition = new Vector3(0.0004f, -0.6476f, buttonObject.transform.localPosition.z);
                    buttonObject.transform.localScale = new Vector3(0.842f, 0.962f, buttonObject.transform.localScale.z);
                    Transform fontPlacerAccount = buttonObject.FindChild<Transform>("FontPlacer", true);
                    fontPlacerAccount.localPosition = new Vector3(0.0283f, -0.099f, fontPlacerAccount.localPosition.z);
                    fontPlacerAccount.localScale = new Vector3(1, 1, fontPlacerAccount.localScale.z);
                    break;
                case "PlayButton":
                    buttonObject.transform.localPosition = new Vector3(-0f, 0.9167f, buttonObject.transform.localPosition.z);
                    buttonObject.transform.localScale = new Vector3(1f, 1.1f, buttonObject.transform.localScale.z);
                    break;
            }
        });

        GameObject bottomButtonBounds = GameObject.Find("Main Buttons/BottomButtonBounds");
        if (bottomButtonBounds != null)
        {
            bottomButtonBounds.transform.localPosition = new Vector3(0.0195f, -1.981f);
            bottomButtonBounds.transform.localScale = new Vector3(0.995f, 1, 1f);
        }

        playLocalButton = Object.Instantiate(__instance.playButton, __instance.playButton.transform.parent);
        playLocalButton.name = "PlayLocalButton";
        playLocalButton.transform.localPosition = new Vector3(-0f, 0.0429f, playLocalButton.transform.localPosition.z);
        playLocalButton.transform.localScale = new Vector3(1, 1.1f, 1f);
        // playLocalButton.activeSprites.GetComponent<SpriteRenderer>().color = activeSpriteRender.color;
        playLocalButton.inactiveSprites.FindChild<SpriteRenderer>("Icon", true).sprite = LotusAssets.LoadSprite("main_menu/LittleDudeIconNew.png", 450);
        playLocalButton.activeSprites.FindChild<SpriteRenderer>("Icon", true).sprite = LotusAssets.LoadSprite("main_menu/LittleDudeIconNew.png", 450);
        playLocalButton.inactiveSprites.FindChild<SpriteRenderer>("Icon", true).transform.localScale = new Vector3(.8f, .7f, 1f);
        playLocalButton.activeSprites.FindChild<SpriteRenderer>("Icon", true).transform.localScale = new Vector3(.8f, .7f, 1f);
        playLocalButton.OnClick = __instance.playLocalButton.OnClick;
        Async.Schedule(() => playLocalButton.buttonText.text = "Play Local", 0.001f);
        playLocalButton.gameObject.GetComponent<AspectScaledAsset>().Destroy();
        {
            AspectScaledAsset aspectScaledAsset = __instance.playButton.GetComponent<AspectScaledAsset>();
            foreach (var sr in playLocalButton.GetComponentsInChildren<SpriteRenderer>(true))
                aspectScaledAsset.allSprites.Add(new AspectScaledAsset.ScaledSprite(sr.size.x, sr));
            foreach (var tmp in playLocalButton.GetComponentsInChildren<TextMeshPro>(true))
                aspectScaledAsset.allText.Add(new AspectScaledAsset.ScaledText(tmp.rectTransform.sizeDelta.x, tmp));
            foreach (var bc2D in playLocalButton.GetComponentsInChildren<BoxCollider2D>(true))
                aspectScaledAsset.allColliders.Add(new AspectScaledAsset.ScaledCollider(bc2D.size.x, bc2D));
        }

        // __instance.myAccountButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.95f, 0f, 1f);
        // __instance.myAccountButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.85f);
        __instance.myAccountButton.activeTextColor = Color.white;
        __instance.myAccountButton.inactiveTextColor = Color.white;
        __instance.myAccountButton.OnClick.AddListener((Action)(() =>
        {
            playBG.SetActive(false);
        }));

        // __instance.settingsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.85f);
        // __instance.settingsButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.85f);
        __instance.settingsButton.activeTextColor = Color.white;
        __instance.settingsButton.inactiveTextColor = Color.white;

        var tohLogo = new GameObject("titleLogo_PL");
        tohLogo.transform.position = OperatingSystem.IsWindows() ? new Vector3(4.5f, -2.1f): new Vector3(5.5f, -2.1f);
        tohLogo.transform.localScale = new Vector3(1f, 1f, 1f);
        var renderer = tohLogo.AddComponent<SpriteRenderer>();
        renderer.sprite = LotusAssets.LoadSprite("Lotus_Icon.png", 1100);

        _customSplash.OrElseSet(InitializeSplash);
        PlayerParticles particles = Object.FindObjectOfType<PlayerParticles>();
        particles.gameObject.SetActive(false);

        DevLogger.Log("Skipping mod update menu as it breaks the game.");

        // ModUpdateMenu = __instance.gameObject.AddComponent<ModUpdateMenu>();
        // ModUpdateMenu.AnchorObject.transform.localPosition += new Vector3(0f, 0f, -9f);

        DevLogger.Log("?????c");


        /*GameObject updateButton = Object.Instantiate(playLocalButton, __instance.transform);*/
        /*Async.Schedule(() =>
        {
            TextMeshPro tmp = updateButton.GetComponentInChildren<TextMeshPro>();
            tmp.text = "Update Found!";
            tmp.enableWordWrapping = true;
        }, 0.1f);
        updateButton.transform.localPosition += new Vector3(0f, 1.85f);
        updateButton.transform.localScale -= new Vector3(0f, 0.25f);
        updateButton.GetComponentInChildren<ButtonRolloverHandler>().OutColor = ModConstants.Palette.GeneralColor5;
        updateButton.GetComponentInChildren<SpriteRenderer>().color = ModConstants.Palette.GeneralColor5;
        Button.ButtonClickedEvent buttonClickedEvent = new();
        updateButton.GetComponentInChildren<PassiveButton>().OnClick = buttonClickedEvent;
        buttonClickedEvent.AddListener((UnityAction)(Action)(() => ModUpdateMenu.Open()));*/

        /*UpdateButton = UnityOptional<GameObject>.Of(updateButton);

        if (!ProjectLotus.ModUpdater.HasUpdate) updateButton.gameObject.SetActive(false);*/
        FriendsListManager.Instance.StopPolling();
        FriendsListManager.Instance.OnSignOut();
    }

    [QuickPostfix(typeof(MainMenuManager), nameof(MainMenuManager.ResetScreen))]
    public static void ResetScreenPostfix(MainMenuManager __instance)
    {
        var playOnlineAnchor = GameObject.Find("PlayOnlineAnchor");
        // if (playOnlineAnchor != null) playOnlineAnchor.SetActive(false);
    }

    private static GameObject InitializeSplash()
    {
        GameObject splashArt = new("SplashArt");
        // splashArt.transform.localPosition = new Vector3(0.3545f, -0.0455f, 600f);
        splashArt.transform.localPosition = new Vector3(0f, 0f, 600f);
        var spriteRenderer = splashArt.AddComponent<SpriteRenderer>();
        // spriteRenderer.sprite = LotusAssets.LoadSprite("PLBackground-Upscale.png", 250);
        spriteRenderer.sprite = LotusAssets.LoadSprite("PLBackground-Upscale.png", OperatingSystem.IsWindows() ? 190 : 155);
        return splashArt;
    }

    private static PassiveButton MakeIconButton(PassiveButton passiveButton, Vector3 localScale, Sprite inactiveSprite, Sprite? activeSprite)
    {
        passiveButton.transform.SetParent(GameObject.Find("MainMenuManager").transform, true);
        // passiveButton.GetComponent<AspectScaledAsset>().allColliders.Clear();
        SpriteRenderer icon = passiveButton.FindChild<SpriteRenderer>("Icon");
        SpriteRenderer buttonRender = passiveButton.inactiveSprites.GetComponent<SpriteRenderer>();
        icon.sprite = inactiveSprite;
        buttonRender.sprite = icon.sprite;
        if (activeSprite != null) icon.sprite = activeSprite;
        else icon.sprite = null;
        passiveButton.activeSprites = null;
        passiveButton.GetComponentInChildren<TextMeshPro>().enabled = false;
        passiveButton.transform.localScale = localScale;
        passiveButton.OnMouseOver.AddListener((Action)(() =>
        {
            if (icon.sprite == null) buttonRender.color = Color.green;
            else buttonRender.sprite = icon.sprite;
        }));
        passiveButton.OnMouseOut.AddListener((Action)(() =>
        {
            if (icon.sprite == null) buttonRender.color = Color.white;
            else buttonRender.sprite = inactiveSprite;
        }));
        icon.enabled = false;

        // Button Specific Things
        // News Button Icon
        NewsCountButton? newsCountButton = passiveButton.GetComponentInChildren<NewsCountButton>();
        if (newsCountButton != null)
        {
            Transform newItem = newsCountButton.FindChild<Transform>("NewItem", true);
            newItem.gameObject.GetComponent<AspectPosition>().Destroy();
            newItem.localPosition = new Vector3(0.35f, 0.35f, -3f);
            newItem.localScale = 1.2f * Vector3.one;
        }

        // Shop Button Icon
        if (passiveButton.name == "ShopButton")
        {
            Transform newItem = passiveButton.FindChild<Transform>("NewItem", true);
            newItem.gameObject.GetComponent<AspectPosition>().Destroy();
            newItem.localPosition = new Vector3(0.8f, 0.8f, -3f);
            newItem.localScale = 1.2f * Vector3.one;
        }

        passiveButton.Debug();
        return passiveButton;
    }

    [QuickPostfix(typeof(MainMenuManager), nameof(MainMenuManager.OpenGameModeMenu))]
    private static void InterceptPlayClick(MainMenuManager __instance)
    {

    }

    [QuickPostfix(typeof(PassiveButton), nameof(PassiveButton.ReceiveMouseOver))]
    private static void InterceptMouseOver(PassiveButton __instance)
    {
        if (__instance.activeSprites != null) return;
        if (__instance.inactiveSprites != null) __instance.inactiveSprites.SetActive(true);
    }
}
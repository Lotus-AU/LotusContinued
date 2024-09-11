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
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime;

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

    private static (string name, float pixelsPerUnit)[] buttonsToFind = [
        ("PlayButton", 105), ("AcountButton", 100), ("SettingsButton", 100), ("BottomButtonBounds/CreditsButton", 100), ("BottomButtonBounds/ExitGameButton", 100)
    ]; // yes "Account" is mispelled lmao


    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    static void Prefix(MainMenuManager __instance)
    {
        if ((AmongUsLogo = GameObject.Find("LOGO-AU")) != null)
        {
            AmongUsLogo.transform.localPosition = new Vector3(0.008f, -0.3125f, 0f);
            AmongUsLogo.GetComponent<SpriteRenderer>().sprite = AssetLoader.LoadLotusSprite("main_menu.newautitle.png", 110);
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

        Async.Schedule(() =>
        {
            GameObject accountManager = FindAccountManager();
            accountManager.transform.localPosition = new Vector3(0f, 1.6782f, 0f);
        }, 0.1f);

        maskedBlackScreen.transform.localPosition = new Vector3(-3.345f, -2.05f);
        maskedBlackScreen.transform.localScale = new Vector3(7.35f, 4.5f, 4f);

        leftPanel.GetComponent<SpriteRenderer>().enabled = false;
        leftPanel.FindChild<SpriteRenderer>("Divider").enabled = false;
        leftPanel.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name == "Shine").ForEach(r => r.enabled = false);

        PassiveButton inventoryButton = MakeIconButton(__instance.inventoryButton, new Vector3(0.26f, 1.2f, 1f), AssetLoader.LoadLotusSprite("main_menu.InventoryIconInactive.png", 100),
            activeSprite: AssetLoader.LoadLotusSprite("main_menu.InventoryIconHighlighted.png", 100));
        inventoryButton.transform.localPosition = new Vector3(5.55f, -1.96f, 0f);

        PassiveButton discordButton = Object.Instantiate(inventoryButton, __instance.transform);
        discordButton.transform.localPosition = new Vector3(0.34f, -2.4f, 0f);
        discordButton.transform.localScale = new Vector3(0.329f, 0.56f, 1f);
        discordButton.Modify(() => Application.OpenURL(ModConstants.LinkTree));
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
            Sprite activeSprite = AssetLoader.LoadLotusSprite("main_menu.DiscordHighlighted.png", 200);
            Sprite inactiveSprite = AssetLoader.LoadLotusSprite("main_menu.DiscordInactive.png", 200);
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

        PassiveButton shopButton = MakeIconButton(__instance.shopButton, new Vector3(0.265f, 1.223f, 1f), AssetLoader.LoadLotusSprite("main_menu.ShopIconInactive.png", 100),
            activeSprite: AssetLoader.LoadLotusSprite("main_menu.ShopIconHighlighted.png", 100));
        shopButton.transform.localPosition = new Vector3(6.6909f, -1.9571f, 0f);

        PassiveButton newsButton = MakeIconButton(__instance.newsButton, new Vector3(0.223f, 1.527f, 0f), AssetLoader.LoadLotusSprite("main_menu.AnnouncementIconInactive.png", 100),
            activeSprite: AssetLoader.LoadLotusSprite("main_menu.AnnouncementIconHighlighted.png", 100));
        newsButton.transform.localPosition = new Vector3(7.7629f, -1.8329f, 0f);

        __instance.playButton.transform.localPosition -= new Vector3(0f, 1.4f);
        // __instance.playButton.transform.localPosition += new Vector3(.02f, 0f, 0f);

        // SpriteRenderer activeSpriteRender = __instance.playButton.activeSprites.GetComponent<SpriteRenderer>();
        // activeSpriteRender.color = new Color(1f, 0f, 0.62f);

        // SpriteRenderer inactiveSpriteRender = __instance.playButton.inactiveSprites.GetComponent<SpriteRenderer>();
        // inactiveSpriteRender.color = new Color(1f, 0f, 0.35f);
        // inactiveSpriteRender.sprite = activeSpriteRender.sprite;

        __instance.playButton.activeTextColor = Color.white;
        __instance.playButton.inactiveTextColor = Color.white;
        __instance.playButton.OnClick = __instance.PlayOnlineButton.OnClick;
        Async.Schedule(() => __instance.playButton.buttonText.text = "Play Online", 0.001f);

        // you dont even want to know the pain we went through to do this...
        buttonsToFind.ForEach(buttonInfo =>
        {
            GameObject buttonObject = GameObject.Find("Main Buttons/" + buttonInfo.name);
            if (buttonObject == null)
            {
                StaticLogger.Debug($"Could not find main menu button named: {buttonInfo.name}");
                return;
            }

            string directoryName = buttonInfo.name switch
            {
                "BottomButtonBounds/CreditsButton" => "Credits",
                "BottomButtonBounds/ExitGameButton" => "Quit",
                "SettingsButton" => "Bottom",
                "AcountButton" => "Bottom",
                "PlayButton" => "Top",
                _ => buttonInfo.name.Replace("Button", "")
            };
            buttonObject.FindChild<SpriteRenderer>("Highlight", true).sprite = AssetLoader.LoadLotusSprite("main_menu." + directoryName + "Highlighted.png", buttonInfo.pixelsPerUnit);
            buttonObject.FindChild<SpriteRenderer>("Inactive", true).sprite = AssetLoader.LoadLotusSprite("main_menu." + directoryName + "Inactive.png", buttonInfo.pixelsPerUnit);
            buttonObject.FindChild<SpriteRenderer>("Highlight", true).color = Color.white;
            buttonObject.FindChild<SpriteRenderer>("Inactive", true).color = Color.white;
            switch (buttonInfo.name)
            {
                case "BottomButtonBounds/ExitGameButton":
                    buttonObject.transform.localPosition = new Vector3(0.8854f, 0, buttonObject.transform.localPosition.z);
                    buttonObject.transform.localScale = new Vector3(0.86f, 1.1f, buttonObject.transform.localScale.z);
                    Transform fontPlacerExitGame = buttonObject.transform.FindChild("FontPlacer");
                    fontPlacerExitGame.localPosition = new Vector3(-0.1753f, 0.0217f, fontPlacerExitGame.localPosition.z);
                    fontPlacerExitGame.localScale = new Vector3(1f, 0.8f, fontPlacerExitGame.localScale.z);
                    break;
                case "BottomButtonBounds/CreditsButton":
                    buttonObject.transform.localPosition = new Vector3(-0.9067f, 0f, buttonObject.transform.localPosition.z);
                    buttonObject.transform.localScale = new Vector3(0.86f, 1.1f, buttonObject.transform.localScale.z);
                    Transform fontPlacerCredits = buttonObject.transform.FindChild("FontPlacer");
                    fontPlacerCredits.localPosition = new Vector3(-0.1753f, 0.0217f, fontPlacerCredits.localPosition.z);
                    fontPlacerCredits.localScale = new Vector3(1f, 0.8f, fontPlacerCredits.localScale.z);
                    // GameObject.Find("CreditsButton/FontPlacer/Text_TMP").GetComponent<TextMeshPro>().text = "PL Credits";
                    break;
                case "SettingsButton":
                    buttonObject.transform.localPosition = new Vector3(0.0004f, -1.2905f, buttonObject.transform.localPosition.z);
                    buttonObject.transform.localScale = new Vector3(0.842f, 0.962f, buttonObject.transform.localScale.z);
                    Transform fontPlacerSettings = buttonObject.transform.FindChild("FontPlacer");
                    fontPlacerSettings.localPosition = new Vector3(0f, -0.099f, fontPlacerSettings.localPosition.z);
                    fontPlacerSettings.localScale = new Vector3(1, 1, fontPlacerSettings.localScale.z);
                    break;
                case "AcountButton":
                    buttonObject.transform.localPosition = new Vector3(0.0004f, -0.6476f, buttonObject.transform.localPosition.z);
                    buttonObject.transform.localScale = new Vector3(0.842f, 0.962f, buttonObject.transform.localScale.z);
                    Transform fontPlacerAccount = buttonObject.transform.FindChild("FontPlacer");
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
        playLocalButton.inactiveSprites.FindChild<SpriteRenderer>("Icon", true).sprite = AssetLoader.LoadLotusSprite("main_menu.LittleDudeIconNew.png", 450);
        playLocalButton.activeSprites.FindChild<SpriteRenderer>("Icon", true).sprite = AssetLoader.LoadLotusSprite("main_menu.LittleDudeIconNew.png", 450);
        playLocalButton.inactiveSprites.FindChild<SpriteRenderer>("Icon", true).transform.localScale = new Vector3(.8f, .7f, 1f);
        playLocalButton.activeSprites.FindChild<SpriteRenderer>("Icon", true).transform.localScale = new Vector3(.8f, .7f, 1f);
        playLocalButton.OnClick = __instance.playLocalButton.OnClick;
        Async.Schedule(() => playLocalButton.buttonText.text = "Play Local", 0.001f);

        // __instance.myAccountButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.95f, 0f, 1f);
        // __instance.myAccountButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.85f);
        __instance.myAccountButton.activeTextColor = Color.white;
        __instance.myAccountButton.inactiveTextColor = Color.white;

        // __instance.settingsButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.85f);
        // __instance.settingsButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0.85f);
        __instance.settingsButton.activeTextColor = Color.white;
        __instance.settingsButton.inactiveTextColor = Color.white;

        var tohLogo = new GameObject("titleLogo_TOH");
        tohLogo.transform.position = new Vector3(4.5f, -2.1f);
        tohLogo.transform.localScale = new Vector3(1f, 1f, 1f);
        var renderer = tohLogo.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetLoader.LoadSprite("Lotus.assets.Lotus_Icon.png", 1100f);

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

    private static GameObject InitializeSplash()
    {
        GameObject splashArt = new("SplashArt");
        splashArt.transform.localPosition = new Vector3(0.3545f, -0.0455f, 600f);
        var spriteRenderer = splashArt.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = AssetLoader.LoadSprite("Lotus.assets.PLBackground-Upscale.png", 250f);
        return splashArt;
    }

    private static PassiveButton MakeIconButton(PassiveButton passiveButton, Vector3 localScale, Sprite inactiveSprite, Sprite? activeSprite)
    {
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
            newsCountButton.FindChild<Transform>("NewItem", true).localScale = new Vector3(4f, 0.6f, 1f);
            // passiveButton.transform.localScale += new Vector3(0f, 0.2f);
        }

        // Shop Button Icon
        if (passiveButton.name == "ShopButton") passiveButton.FindChild<SpriteRenderer>("Sprite", true).transform.localScale = new Vector3(3.55f, 0.95f, 1f);

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

    // save me from my pain and suffering
    static GameObject FindAccountManager()
    {
        Il2CppSystem.Type gameObjectType = Il2CppType.Of<GameObject>();

        Il2CppReferenceArray<Object> allObjects = Resources.FindObjectsOfTypeAll(gameObjectType);
        foreach (var obj in allObjects)
        {
            GameObject? gameObject = obj.TryCast<GameObject>();
            if (gameObject != null)
            {
                if (gameObject.name == "AccountManager" && gameObject.hideFlags == HideFlags.None)
                {
                    return gameObject;
                }

                var accountManagerComponent = gameObject.GetComponent<AccountManager>();
                if (accountManagerComponent != null && gameObject.hideFlags == HideFlags.None)
                {
                    return accountManagerComponent.gameObject;
                }
            }
        }

        return null!;
    }
}
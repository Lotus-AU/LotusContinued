using System;
using System.IO;
using Lotus.Managers.Announcements;
using Lotus.Managers.Combo;
using Lotus.Managers.Friends;
using Lotus.Managers.Templates;
using Lotus.Managers.Titles;
using UnityEngine;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.Managers;

// TODO: Create copy of local storage in cache folder and have file checking if cached, if file DNE then copy files from cache into main game
[LoadStatic]
public static class PluginDataManager
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(PluginDataManager));

    #if ANDROID
    private static readonly string ModifiableDataDirectoryPath = Path.Combine(Application.persistentDataPath, "LOTUS_DATA");
    private static readonly string ModifiableDataDirectoryPathOld = Path.Combine(Application.persistentDataPath, "TOHTOR_DATA");
    private static readonly string ModifiableHiddenDataDirectoryPath = Path.Combine(Application.persistentDataPath, "ProjectLotus");
    private static readonly string LegacyHiddenDataDirectoryPath = Path.Combine(Application.persistentDataPath, "TownOfHostTheOtherRoles");
    #else
    private const string ModifiableDataDirectoryPath = "./LOTUS_DATA";
    private const string ModifiableDataDirectoryPathOld = "./TOHTOR_DATA";
    private static readonly string ModifiableHiddenDataDirectoryPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/ProjectLotus");
    private static readonly string LegacyHiddenDataDirectoryPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/TownOfHostTheOtherRoles");
    #endif

    private const string TitleDirectory = "Titles";

    private const string ReadAnnouncementsFile = "ReadAnnouncements.yaml";
    private const string ModdedPlayerFile = "ModdedPlayers.yaml";
    private const string BannedPlayerFile = "BannedPlayers.yaml";
    private const string ComboListFile = "RoleComboInfo.yaml";
    private const string WordListFile = "BannedWords.yaml";
    private const string TemplateFile = "Templates.yaml";

    private const string WhitelistFile = "Whitelist.txt";
    private const string FriendListFile = "Friends.txt";

    public static readonly DirectoryInfo ModifiableDataDirectory;
    public static readonly DirectoryInfo HiddenDataDirectory;

    public static CustomAnnouncementManager AnnouncementManager;
    public static WhitelistManager WhitelistManager;
    public static ComboListManager ComboListManager;
    public static TemplateManager TemplateManager;
    public static FriendManager FriendManager;
    public static TitleManager TitleManager;
    public static ChatManager ChatManager;
    public static ModManager ModManager;
    public static BanManager BanManager;

    static PluginDataManager()
    {
        MigrateOldDirectory();
        MigrateOldHiddenDirectory();

        ModifiableDataDirectory = new DirectoryInfo(ModifiableDataDirectoryPath);
        HiddenDataDirectory = new DirectoryInfo(ModifiableHiddenDataDirectoryPath);
        if (!ModifiableDataDirectory.Exists) ModifiableDataDirectory.Create();
        if (!HiddenDataDirectory.Exists) HiddenDataDirectory.Create();

        AnnouncementManager = TryLoad(() => new CustomAnnouncementManager(ModifiableDataDirectory.GetFile(ReadAnnouncementsFile)), "Announcement Manager")!;
        WhitelistManager = TryLoad(() => new WhitelistManager(ModifiableDataDirectory.GetFile(WhitelistFile)), "Whitelist Manager")!;
        ComboListManager = TryLoad(() => new ComboListManager(ModifiableDataDirectory.GetFile(ComboListFile)), "Combolist Manager")!;
        TemplateManager = TryLoad(() => new TemplateManager(ModifiableDataDirectory.GetFile(TemplateFile)), "Template Manager")!;
        FriendManager = TryLoad(() => new FriendManager(ModifiableDataDirectory.GetFile(FriendListFile)), "Friend Manager")!;
        TitleManager = TryLoad(() => new TitleManager(ModifiableDataDirectory.GetDirectory(TitleDirectory)), "Title Manager")!;
        ModManager = TryLoad(() => new ModManager(ModifiableDataDirectory.GetFile(ModdedPlayerFile)), "Mod Manager")!;
        BanManager = TryLoad(() => new BanManager(ModifiableDataDirectory.GetFile(BannedPlayerFile)), "Ban Manager")!;
        ChatManager = TryLoad(() => new ChatManager(ModifiableDataDirectory.GetFile(WordListFile)), "Chat Manager")!;
    }

    public static void ReloadAll(Action<(Exception ex, string erorrStuff)> onError)
    {
        try
        {
            MigrateOldDirectory();
            MigrateOldHiddenDirectory();

            if (!ModifiableDataDirectory.Exists) ModifiableDataDirectory.Create();
            if (!HiddenDataDirectory.Exists) HiddenDataDirectory.Create();
        }
        catch (Exception ex)
        {
            onError((ex, "Moving the Old Directory"));
        }

        WhitelistManager = TryLoad(() => new WhitelistManager(ModifiableDataDirectory.GetFile(WhitelistFile)), "Whitelist Manager", onError)!;
        ComboListManager = TryLoad(() => new ComboListManager(ModifiableDataDirectory.GetFile(ComboListFile)), "Combolist Manager", onError)!;
        TemplateManager = TryLoad(() => new TemplateManager(ModifiableDataDirectory.GetFile(TemplateFile)), "Template Manager", onError)!;
        FriendManager = TryLoad(() => new FriendManager(ModifiableDataDirectory.GetFile(FriendListFile)), "Friend Manager", onError)!;
        TitleManager = TryLoad(() => new TitleManager(ModifiableDataDirectory.GetDirectory(TitleDirectory)), "Title Manager", onError)!;
        BanManager = TryLoad(() => new BanManager(ModifiableDataDirectory.GetFile(BannedPlayerFile)), "Ban Manager", onError)!;
        ChatManager = TryLoad(() => new ChatManager(ModifiableDataDirectory.GetFile(WordListFile)), "Chat Manager", onError)!;
        ModManager = TryLoad(() => new ModManager(ModifiableDataDirectory.GetFile(ModdedPlayerFile)), "Mod Manager", onError)!;
    }

    private static T? TryLoad<T>(Func<T> loadFunction, string name, Action<(Exception ex, string erorrStuff)>? onError = null)
    {
        try
        {
            log.Trace($"Loading {name}", "PluginDataManager");
            return loadFunction();
        }
        catch (Exception exception)
        {
            if (onError != null) onError((exception, name));
            else log.Exception($"Failed to load {name}", exception);
            return default;
        }
    }

    internal static void MigrateOldDirectory()
    {
        DirectoryInfo oldDirectory = new(ModifiableDataDirectoryPathOld);
        if (!oldDirectory.Exists) return;
        try
        {
            oldDirectory.MoveTo(ModifiableDataDirectoryPath);
        }
        catch
        {
            // ignored
        }
    }

    internal static void MigrateOldHiddenDirectory()
    {
        DirectoryInfo oldDirectory = new(LegacyHiddenDataDirectoryPath);
        if (!oldDirectory.Exists) return;
        try
        {
            oldDirectory.MoveTo(ModifiableHiddenDataDirectoryPath);
        }
        catch
        {
            // ignored
        }
    }
}
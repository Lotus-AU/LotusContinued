using System;
using System.IO;
using Lotus.Managers.Friends;
using Lotus.Managers.Templates;
using Lotus.Managers.Titles;
using VentLib.Utilities.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.Managers;

// TODO: Create copy of local storage in cache folder and have file checking if cached, if file DNE then copy files from cache into main game
[LoadStatic]
public static class PluginDataManager
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(PluginDataManager));

    private const string ModifiableDataDirectoryPath = "./LOTUS_DATA";
    private const string ModifiableDataDirectoryPathOld = "./TOHTOR_DATA";
    private static readonly string ModifiableHiddenDataDirectoryPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/ProjectLotus");
    private static readonly string LegacyHiddenDataDirectoryPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/TownOfHostTheOtherRoles");
    private const string TitleDirectory = "Titles";

    private const string ModdedPlayerFile = "ModdedPlayers.yaml";
    private const string BannedPlayerFile = "BannedPlayers.yaml";
    private const string WordListFile = "BannedWords.yaml";
    private const string TemplateFile = "Templates.yaml";

    private const string FriendListFile = "Friends.txt";

    public static readonly DirectoryInfo ModifiableDataDirectory;
    public static readonly DirectoryInfo HiddenDataDirectory;

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

        TemplateManager = TryLoad(() => new TemplateManager(ModifiableDataDirectory.GetFile(TemplateFile)), "Template Manager")!;
        TitleManager = TryLoad(() => new TitleManager(ModifiableDataDirectory.GetDirectory(TitleDirectory)), "Title Manager")!;
        FriendManager = TryLoad(() => new FriendManager(ModifiableDataDirectory.GetFile(FriendListFile)), "Friend Manager")!;
        BanManager = TryLoad(() => new BanManager(ModifiableDataDirectory.GetFile(BannedPlayerFile)), "Ban Manager")!;
        ModManager = TryLoad(() => new ModManager(ModifiableDataDirectory.GetFile(ModdedPlayerFile)), "Mod Manager")!;
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

        TemplateManager = TryLoad(() => new TemplateManager(ModifiableDataDirectory.GetFile(TemplateFile)), "Template Manager", onError)!;
        TitleManager = TryLoad(() => new TitleManager(ModifiableDataDirectory.GetDirectory(TitleDirectory)), "Title Manager", onError)!;
        FriendManager = TryLoad(() => new FriendManager(ModifiableDataDirectory.GetFile(FriendListFile)), "Friend Manager", onError)!;
        BanManager = TryLoad(() => new BanManager(ModifiableDataDirectory.GetFile(BannedPlayerFile)), "Ban Manager", onError)!;
        ModManager = TryLoad(() => new ModManager(ModifiableDataDirectory.GetFile(ModdedPlayerFile)), "Mod Manager", onError)!;
        ChatManager = TryLoad(() => new ChatManager(ModifiableDataDirectory.GetFile(WordListFile)), "Chat Manager", onError)!;
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
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using ChatThree.Ipc;
using ChatThree.Resources;
using ChatThree.Util;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XivCommon;

namespace ChatThree;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    internal const string PluginName = "Chat 3";

    internal static string Name => PluginName;

    [PluginService]
    internal static IPluginLog Log { get; private set; }

    [PluginService]
    internal DalamudPluginInterface Interface { get; init; }

    [PluginService]
    internal IChatGui ChatGui { get; init; }

    [PluginService]
    internal IClientState ClientState { get; init; }

    [PluginService]
    internal ICommandManager CommandManager { get; init; }

    [PluginService]
    internal ICondition Condition { get; init; }

    [PluginService]
    internal IDataManager DataManager { get; init; }

    [PluginService]
    internal IFramework Framework { get; init; }

    [PluginService]
    internal IGameGui GameGui { get; init; }

    [PluginService]
    internal IKeyState KeyState { get; init; }

    [PluginService]
    internal IObjectTable ObjectTable { get; init; }

    [PluginService]
    internal IPartyList PartyList { get; init; }

    [PluginService]
    internal ITargetManager TargetManager { get; init; }

    [PluginService]
    internal ITextureProvider TextureProvider { get; init; }

    [PluginService]
    internal IGameInteropProvider GameInteropProvider { get; init; }

    [PluginService]
    internal IGameConfig GameConfig { get; init; }

    internal Configuration Config { get; }
    internal Commands Commands { get; }
    internal XivCommonBase Common { get; }
    internal TextureCache TextureCache { get; }
    internal GameFunctions.GameFunctions Functions { get; }
    internal Store Store { get; }
    internal IpcManager Ipc { get; }
    internal ExtraChat ExtraChat { get; }
    internal PluginUi Ui { get; }

    internal int DeferredSaveFrames = -1;

    internal DateTime GameStarted { get; }

#pragma warning disable CS8618
    public Plugin()
    {
        // If Chat2 is enabled, refuse to load.
        foreach (var plugin in this.Interface!.InstalledPlugins)
        {
            if (plugin.InternalName == "ChatTwo")
            {
                if (plugin.IsLoaded)
                {
                    this.Interface!.UiBuilder.AddNotification("Chat 3 cannot be loaded while Chat 2 is loaded", null, NotificationType.Error);
                    throw new Exception("cowardly refusing to load Chat 3 while Chat 2 is loaded");
                }
                break;
            }
        }

        // If Chat3 hasn't started yet, try to copy over the config from Chat2.
        // This is super janky but makes migrating a breeze.
        this.Config = this.Interface!.GetPluginConfig() as Configuration ?? new Configuration();
        if (!this.Config.DidFirstStart)
        {
            Log.Debug("Attempting to copy over Chat 2 data");
            var chat3Config = this.Interface!.ConfigFile.FullName;
            var chat2Config = Path.Combine(this.Interface!.ConfigFile.DirectoryName!, "ChatTwo.json");
            Log.Debug($"Chat 3 config: {chat3Config}");
            Log.Debug($"Chat 2 config: {chat2Config}");

            try
            {
                if (!File.Exists(chat3Config) && File.Exists(chat2Config))
                {
                    // Read the config into memory as a string, then replace
                    // Chat2 with Chat3. This updates the assembly references.
                    var configData = File.ReadAllText(chat2Config);
                    configData = configData.Replace("ChatTwo", "ChatThree");
                    configData = configData.Replace("Chat 2", "Chat 3");
                    Log.Debug($"cp '{chat2Config}' '{chat3Config}'");
                    File.WriteAllText(chat3Config, configData);
                    this.Config = this.Interface!.GetPluginConfig() as Configuration ?? new Configuration();
                    this.Interface!.UiBuilder.AddNotification("Chat 3 copied over Chat 2 config", null, NotificationType.Info);
                }
            }
            catch (Exception e)
            {
                this.Interface!.UiBuilder.AddNotification("Chat 3 failed to copy over Chat 2 config", e.ToString(), NotificationType.Error);
                Log.Error($"Failed to copy over Chat 2 config: {e}");
                this.Config = new Configuration();
                this.Interface!.SavePluginConfig(this.Config);
            }
        }
        this.Config.Migrate();
        this.Config.DidFirstStart = true;
        this.Interface!.SavePluginConfig(this.Config);

        this.GameStarted = Process.GetCurrentProcess().StartTime.ToUniversalTime();

        if (this.Config.Tabs.Count == 0)
        {
            this.Config.Tabs.Add(TabsUtil.VanillaGeneral);
        }

        this.LanguageChanged(this.Interface.UiLanguage);

        this.Commands = new Commands(this);
        this.Common = new XivCommonBase(this.Interface);
        this.TextureCache = new TextureCache(this.TextureProvider!);
        this.Functions = new GameFunctions.GameFunctions(this);
        this.Store = new Store(this);
        this.Ipc = new IpcManager(this.Interface);
        this.ExtraChat = new ExtraChat(this);
        this.Ui = new PluginUi(this);

        // let all the other components register, then initialise commands
        this.Commands.Initialise();

        if (this.Interface.Reason is not PluginLoadReason.Boot)
        {
            this.Store.FilterAllTabs(false);
        }

        this.Framework!.Update += this.FrameworkUpdate;
        this.Interface.LanguageChanged += this.LanguageChanged;
    }
#pragma warning restore CS8618

    public void Dispose()
    {
        this.Interface.LanguageChanged -= this.LanguageChanged;
        this.Framework.Update -= this.FrameworkUpdate;
        GameFunctions.GameFunctions.SetChatInteractable(true);

        this.Ui.Dispose();
        this.ExtraChat.Dispose();
        this.Ipc.Dispose();
        this.Store.Dispose();
        this.Functions.Dispose();
        this.TextureCache.Dispose();
        this.Common.Dispose();
        this.Commands.Dispose();
    }

    internal void SaveConfig()
    {
        this.Interface.SavePluginConfig(this.Config);
    }

    internal void LanguageChanged(string langCode)
    {
        var info = this.Config.LanguageOverride is LanguageOverride.None
            ? new CultureInfo(langCode)
            : new CultureInfo(this.Config.LanguageOverride.Code());

        Language.Culture = info;
    }

    private static readonly string[] ChatAddonNames = {
        "ChatLog",
        "ChatLogPanel_0",
        "ChatLogPanel_1",
        "ChatLogPanel_2",
        "ChatLogPanel_3",
    };

    private void FrameworkUpdate(IFramework framework)
    {
        if (this.DeferredSaveFrames >= 0 && this.DeferredSaveFrames-- == 0)
        {
            this.SaveConfig();
        }

        if (!this.Config.HideChat)
        {
            return;
        }

        foreach (var name in ChatAddonNames)
        {
            if (GameFunctions.GameFunctions.IsAddonInteractable(name))
            {
                GameFunctions.GameFunctions.SetAddonInteractable(name, false);
            }
        }
    }
}

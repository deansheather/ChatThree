using ChatThree.Resources;
using ChatThree.Util;
using ImGuiNET;

namespace ChatThree.Ui.SettingsTabs;

internal sealed class Database : ISettingsTab
{
    private Configuration Mutable { get; }
    private Plugin Plugin { get; }

    public string Name => Language.Options_Database_Tab + "###tabs-database";

    internal Database(Configuration mutable, Plugin plugin)
    {
        this.Plugin = plugin;
        this.Mutable = mutable;
    }

    private bool _showAdvanced;

    public void Draw(bool changed)
    {
        var io = ImGui.GetIO();
        if (changed)
        {
            this._showAdvanced = io.KeyShift;
        }

        ImGuiUtil.OptionCheckbox(ref this.Mutable.DatabaseBattleMessages, Language.Options_DatabaseBattleMessages_Name, Language.Options_DatabaseBattleMessages_Description);
        ImGui.Spacing();

        if (ImGuiUtil.OptionCheckbox(ref this.Mutable.LoadPreviousSession, Language.Options_LoadPreviousSession_Name, Language.Options_LoadPreviousSession_Description))
        {
            if (this.Mutable.LoadPreviousSession)
            {
                this.Mutable.FilterIncludePreviousSessions = true;
            }
        }

        ImGui.Spacing();

        if (ImGuiUtil.OptionCheckbox(ref this.Mutable.FilterIncludePreviousSessions, Language.Options_FilterIncludePreviousSessions_Name, Language.Options_FilterIncludePreviousSessions_Description))
        {
            if (!this.Mutable.FilterIncludePreviousSessions)
            {
                this.Mutable.LoadPreviousSession = false;
            }
        }

        ImGuiUtil.OptionCheckbox(
            ref this.Mutable.SharedMode,
            Language.Options_SharedMode_Name,
            string.Format(Language.Options_SharedMode_Description, Plugin.PluginName)
        );
        ImGuiUtil.WarningText(string.Format(Language.Options_SharedMode_Warning, Plugin.PluginName));

        ImGui.Spacing();

        if (ImGui.Button(Language.Options_ClearDatabase_Button) && io.KeyCtrl && io.KeyShift)
        {
            Plugin.Log.Warning("Clearing database");
            this.Plugin.Store.ClearDatabase();
            foreach (var tab in this.Plugin.Config.Tabs)
            {
                tab.Clear();
            }
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(Language.Options_ClearDatabase_Tooltip);
            ImGui.EndTooltip();
        }

        ImGui.Spacing();

        if (this._showAdvanced && ImGui.TreeNodeEx(Language.Options_Database_Advanced))
        {
            ImGui.PushTextWrapPos();
            ImGuiUtil.WarningText(Language.Options_Database_Advanced_Warning);

            if (ImGui.Button("Checkpoint"))
            {
                this.Plugin.Store.Database.Checkpoint();
            }

            if (ImGui.Button("Rebuild"))
            {
                this.Plugin.Store.Database.Rebuild();
            }

            ImGui.PopTextWrapPos();
            ImGui.TreePop();
        }
    }
}

﻿using ChatTwo.Code;
using ChatTwo.Util;
using Dalamud.Interface;
using ImGuiNET;

namespace ChatTwo.Ui.SettingsTabs;

internal sealed class Tabs : ISettingsTab {
    private Configuration Mutable { get; }

    public string Name => "Tabs";

    internal Tabs(Configuration mutable) {
        this.Mutable = mutable;
    }

    public void Draw() {
        if (ImGuiUtil.IconButton(FontAwesomeIcon.Plus, tooltip: "Add")) {
            this.Mutable.Tabs.Add(new Tab());
        }

        var toRemove = -1;
        for (var i = 0; i < this.Mutable.Tabs.Count; i++) {
            var tab = this.Mutable.Tabs[i];

            if (ImGui.TreeNodeEx($"{tab.Name}###tab-{i}")) {
                ImGui.PushID($"tab-{i}");

                if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, tooltip: "Delete")) {
                    toRemove = i;
                }

                ImGui.SameLine();

                if (ImGuiUtil.IconButton(FontAwesomeIcon.ArrowUp, tooltip: "Move up") && i > 0) {
                    (this.Mutable.Tabs[i - 1], this.Mutable.Tabs[i]) = (this.Mutable.Tabs[i], this.Mutable.Tabs[i - 1]);
                }

                ImGui.SameLine();

                if (ImGuiUtil.IconButton(FontAwesomeIcon.ArrowDown, tooltip: "Move down") && i < this.Mutable.Tabs.Count - 1) {
                    (this.Mutable.Tabs[i + 1], this.Mutable.Tabs[i]) = (this.Mutable.Tabs[i], this.Mutable.Tabs[i + 1]);
                }

                ImGui.InputText("Name", ref tab.Name, 512, ImGuiInputTextFlags.EnterReturnsTrue);
                ImGui.Checkbox("Show unread count", ref tab.DisplayUnread);
                ImGui.Checkbox("Show timestamps", ref tab.DisplayTimestamp);

                var input = tab.Channel?.ToChatType().Name() ?? "<None>";
                if (ImGui.BeginCombo("Input channel", input)) {
                    if (ImGui.Selectable("<None>", tab.Channel == null)) {
                        tab.Channel = null;
                    }

                    foreach (var channel in Enum.GetValues<InputChannel>()) {
                        if (ImGui.Selectable(channel.ToChatType().Name(), tab.Channel == channel)) {
                            tab.Channel = channel;
                        }
                    }

                    ImGui.EndCombo();
                }

                if (ImGui.TreeNodeEx("Channels")) {
                    foreach (var type in Enum.GetValues<ChatType>()) {
                        var enabled = tab.ChatCodes.ContainsKey(type);
                        if (ImGui.Checkbox($"##{type.Name()}-{i}", ref enabled)) {
                            if (enabled) {
                                tab.ChatCodes[type] = ChatSourceExt.All;
                            } else {
                                tab.ChatCodes.Remove(type);
                            }
                        }

                        ImGui.SameLine();

                        if (ImGui.TreeNodeEx($"{type.Name()}##{i}")) {
                            tab.ChatCodes.TryGetValue(type, out var sourcesEnum);
                            var sources = (uint) sourcesEnum;

                            foreach (var source in Enum.GetValues<ChatSource>()) {
                                if (ImGui.CheckboxFlags(source.ToString(), ref sources, (uint) source)) {
                                    tab.ChatCodes[type] = (ChatSource) sources;
                                }
                            }

                            ImGui.TreePop();
                        }
                    }


                    ImGui.TreePop();
                }

                ImGui.TreePop();

                ImGui.PopID();
            }
        }

        if (toRemove > -1) {
            this.Mutable.Tabs.RemoveAt(toRemove);
        }
    }
}
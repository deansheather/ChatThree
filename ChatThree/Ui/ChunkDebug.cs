using ChatThree.Util;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility;
using ImGuiNET;
using System.Numerics;
using DalamudPartyFinderPayload = Dalamud.Game.Text.SeStringHandling.Payloads.PartyFinderPayload;
using ChatThreePartyFinderPayload = ChatThree.Util.PartyFinderPayload;
using Lumina.Text.Payloads;

namespace ChatThree.Ui;

internal class ChunkDebug : IUiComponent
{
    private ChatLog ChatLog { get; }

    private bool IsOpen = false;
    private Chunk? chunk;
    private bool ScrollToTop = false;

    public ChunkDebug(ChatLog chatLog)
    {
        this.ChatLog = chatLog;
    }

    public void Dispose() {}

    public void Open(Chunk chunk)
    {
        this.chunk = chunk;
        this.IsOpen = true;
        this.ScrollToTop = true;
    }

    public void Draw()
    {
        if (!this.IsOpen || this.chunk == null)
        {
            return;
        }

        ImGui.SetNextWindowSize(new Vector2(500, 750) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);

        if (!ImGui.Begin($"{Plugin.Name} Chunk Debugger###Chat3ChunkDebug", ref this.IsOpen))
        {
            ImGui.End();
            return;
        }

        if (this.ScrollToTop)
        {
            ImGui.SetScrollHereY();
            this.ScrollToTop = false;
        }

        var style = ImGui.GetStyle();

        ImGui.Text("Chunk:");
        ImGui.Indent(style.IndentSpacing);
        ChatLog.DrawChunks([this.chunk], true, null, 0);
        ImGui.Unindent(style.IndentSpacing);
        ImGui.NewLine();

        RenderMetadataDictionary("Chunk Metadata", new Dictionary<string, string?> {
            { "Type", this.chunk.GetType().ToString() },
            { "Source", EnumName(this.chunk.Source) },
            { "LinkType", this.chunk.Link?.GetType().ToString() ?? "None" },
            { "GetSeString()", this.chunk.GetSeString()?.ToString() ?? "None" },
            { "StringValue()", this.chunk.StringValue() },
        });

        if (this.chunk is TextChunk textChunk)
        {
            RenderMetadataDictionary("TextChunk Metadata", new Dictionary<string, string?> {
                { "FallbackColour", textChunk.FallbackColour?.ToString() ?? "None" },
                { "Foreground", textChunk.Foreground?.ToString("X") ?? "None" },
                { "Glow", textChunk.Glow?.ToString("X") ?? "None" },
                { "Italic", textChunk.Italic.ToString() },
                { "Content", textChunk.Content },
            });
        }

        if (this.chunk is IconChunk iconChunk)
        {
            RenderMetadataDictionary("IconChunk Metadata", new Dictionary<string, string?> {
                { "Icon", iconChunk.Icon.ToString() },
            });
        }

        switch (this.chunk.Link)
        {
            case MapLinkPayload map:
                {
                    RenderMetadataDictionary("Link MapLinkPayload", new Dictionary<string, string?> {
                        { "Map.RowId", map.Map?.RowId.ToString() },
                        { "Map.PlaceName", map.Map?.PlaceName.Value?.Name.ToString() },
                        { "Map.PlaceNameRegion", map.Map?.PlaceNameRegion.Value?.Name.ToString() },
                        { "Map.PlaceNameSub", map.Map?.PlaceNameSub.Value?.Name.ToString() },
                        { "TerritoryType.RowId", map.TerritoryType?.RowId.ToString() },
                        { "RawX", map.RawX.ToString() },
                        { "RawY", map.RawY.ToString() },
                        { "XCoord", map.XCoord.ToString() },
                        { "YCoord", map.YCoord.ToString() },
                        { "CoordinateString", map.CoordinateString },
                        { "DataString", map.DataString },
                    });
                    break;
                }
            case QuestPayload quest:
                {
                    RenderMetadataDictionary("Link QuestPayload", new Dictionary<string, string?> {
                        { "Quest.RowId", quest.Quest?.RowId.ToString() },
                        { "Quest.Name", quest.Quest?.Name.ToString() },
                    });
                    break;
                }
            case DalamudLinkPayload link:
                {
                    RenderMetadataDictionary("Link DalamudLinkPayload", new Dictionary<string, string?> {
                        { "CommandId", link.CommandId.ToString() },
                        { "Plugin", link.Plugin },
                    });
                    break;
                }
            case DalamudPartyFinderPayload pf:
                {
                    RenderMetadataDictionary("Link PartyFinderPayload", new Dictionary<string, string?> {
                        { "ListingId", pf.ListingId.ToString() },
                        { "LinkType", EnumName(pf.LinkType) },
                    });
                    break;
                }
            case PlayerPayload player:
                {
                    RenderMetadataDictionary("Link PlayerPayload", new Dictionary<string, string?> {
                        { "PlayerName", player.PlayerName },
                        { "World.Name", player.World.Name },
                    });
                    break;
                }
            case ItemPayload item:
                {
                    RenderMetadataDictionary("Link ItemPayload", new Dictionary<string, string?> {
                        { "ItemId", item.ItemId.ToString() },
                        { "RawItemId", item.RawItemId.ToString() },
                        { "Kind", EnumName(item.Kind) },
                        { "IsHQ", item.IsHQ.ToString() },
                        { "Item.Name", item.Item?.Name.ToString() },
                    });
                    break;
                }
            case AutoTranslatePayload at:
                {
                    RenderMetadataDictionary("Link AutoTranslatePayload", new Dictionary<string, string?> {
                        { "Text", at.Text },
                    });
                    break;
                }
            case RawPayload raw:
                {
                    RenderMetadataDictionary("Link RawPayload", new Dictionary<string, string?> {
                        { "Data", string.Join(" ", raw.Data.Select(b => b.ToString("X2"))) },
                    });
                    break;
                }
            case StatusPayload status:
                {
                    RenderMetadataDictionary("Link StatusPayload", new Dictionary<string, string?> {
                        { "Status.RowId", status.Status.RowId.ToString() },
                        { "Status.Name", status.Status.Name },
                        { "Status.Icon", status.Status.Icon.ToString() }
                    });
                    break;
                }

            case ChatThreePartyFinderPayload pf:
                {
                    RenderMetadataDictionary("Link PartyFinderPayload", new Dictionary<string, string?> {
                        { "Id", pf.Id.ToString() }
                    });
                    break;
                }
            case AchievementPayload achievement:
                {
                    RenderMetadataDictionary("Link AchievementPayload", new Dictionary<string, string?> {
                        { "Id", achievement.Id.ToString() }
                    });
                    break;
                }
            case URIPayload uri:
                {
                    RenderMetadataDictionary("Link URIPayload", new Dictionary<string, string?> {
                        { "Uri", uri.Uri.ToString() }
                    });
                    break;
                }
        }

        ImGui.Separator();
        ImGui.NewLine();
        if (this.chunk.Message != null)
        {
            ImGui.Text("Message:");
            ImGui.Indent(style.IndentSpacing);
            var lineWidth = ImGui.GetContentRegionAvail().X;
            if (chunk.Message.Sender.Count > 0)
            {
                this.ChatLog.DrawChunks(chunk.Message.Sender, true, null, lineWidth);
                ImGui.SameLine();
            }

            if (chunk.Message.Content.Count == 0)
            {
                this.ChatLog.DrawChunks(new[] { new TextChunk(ChunkSource.Content, null, " ") }, true, null, lineWidth);
            }
            else
            {
                this.ChatLog.DrawChunks(chunk.Message.Content, true, null, lineWidth);
            }
            ImGui.Unindent(style.IndentSpacing);
            ImGui.NewLine();

            RenderMetadataDictionary("Message Metadata", new Dictionary<string, string?> {
                { "Id", this.chunk.Message.Id.ToString() },
                { "Receiver", this.chunk.Message.Receiver.ToString() },
                { "ContentId", this.chunk.Message.ContentId.ToString() },
                { "Date", this.chunk.Message.Date.ToLocalTime().ToString() },
                { "Code.Type", EnumName(this.chunk.Message.Code.Type) },
                { "Code.Source", EnumName(this.chunk.Message.Code.Source) },
                { "Code.Target", EnumName(this.chunk.Message.Code.Target) },
                { "SenderSource", this.chunk.Message.SenderSource.ToString() },
                { "ContentSource", this.chunk.Message.ContentSource.ToString() },
                { "SortCode", this.chunk.Message.SortCode?.ToString() ?? "None" },
                { "ExtraChatChannel", this.chunk.Message.ExtraChatChannel.ToString() },
                { "Hash", this.chunk.Message.Hash.ToString() },
            });

            if (ImGui.CollapsingHeader($"Sender chunks ({this.chunk.Message.Sender.Count})"))
            {
                ImGui.PushID("Sender");
                for (var i = 0; i < this.chunk.Message.Sender.Count; i++)
                {
                    this.RenderMessageChunk(i, this.chunk.Message.Sender[i]);
                }
                ImGui.PopID();
            }

            ImGui.NewLine();
            if (ImGui.CollapsingHeader($"Content chunks ({this.chunk.Message.Content.Count})"))
            {
                ImGui.PushID("Content");
                for (var i = 0; i < this.chunk.Message.Content.Count; i++)
                {
                    this.RenderMessageChunk(i, this.chunk.Message.Content[i]);
                }
                ImGui.PopID();
            }
        }
        else
        {
            ImGui.Text("Could not find associated message.");
        }

        ImGui.End();
    }

    private void RenderMessageChunk(int i, Chunk chunk)
    {
        ImGui.PushID(i);
        ImGui.Text($"Chunk {i}:");
        ImGui.SameLine();
        if (ImGui.Button("View"))
        {
            this.Open(chunk);
        }

        var style = ImGui.GetStyle();
        ImGui.Indent(style.IndentSpacing);
        ChatLog.DrawChunks([chunk], true, null, 0);
        ImGui.Unindent(style.IndentSpacing);
        ImGui.PopID();
    }

    private static string? EnumName<T>(T? value) where T : Enum
    {
        if (value == null)
        {
            return null;
        }
        var rawValue = Convert.ChangeType(value, value.GetTypeCode());
        return (Enum.GetName(value.GetType(), value) ?? "Unknown") + $" ({rawValue})";
    }

    private static void RenderMetadataDictionary(string name, Dictionary<string, string?> metadata)
    {
        var style = ImGui.GetStyle();

        ImGui.Text($"{name}:");
        ImGui.Indent(style.IndentSpacing);
        if (!ImGui.BeginTable($"##chat3-{name}", 2, 0))
        {
            ImGui.EndTable();
            ImGui.Unindent(style.IndentSpacing);
            return;
        }
        ImGui.TableSetupColumn($"##chat3-{name}-key", 0, 0.4f);
        ImGui.TableSetupColumn($"##chat3-{name}-value");
        for (var i = 0; i < metadata.Count; i++)
        {
            var (key, value) = metadata.ElementAt(i);
            ImGui.PushID(i);
            ImGui.TableNextColumn();
            ImGui.Text(key);
            ImGui.TableNextColumn();
            ImGuiTextVisibleWhitespace(value);
            ImGui.PopID();
        }
        ImGui.EndTable();
        ImGui.Unindent(style.IndentSpacing);
        ImGui.NewLine();
    }

    // ImGuiTextVisibleWhitespace replaces leading and trailing whitespace with
    // visible characters. The extra characters are rendered with a muted font.
    private static void ImGuiTextVisibleWhitespace(string? original, bool wrap = true)
    {
        if (string.IsNullOrEmpty(original))
        {
            var str = original == null ? "(null)" : "(empty)";
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 0.5f));
            ImGui.TextUnformatted(str);
            ImGui.PopStyleColor();
            return;
        }

        var text = original;
        var start = 0;
        var end = text.Length;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

        void WriteText(string text)
        {
            if (wrap)
            {
                ImGui.TextWrapped(text);
            }
            else
            {
                ImGui.TextUnformatted(text);
            }
        }

        while (start < end && char.IsWhiteSpace(text[start]))
        {
            start++;
        }
        if (start > 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 0.5f));
            WriteText(new string('_', start));
            ImGui.PopStyleColor();
            ImGui.SameLine();
        }

        while (end > start && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        WriteText(text[start..end]);
        if (end < text.Length)
        {
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 0.5f));
            WriteText(new string('_', text.Length - end));
            ImGui.PopStyleColor();
        }

        ImGui.PopStyleVar();
    }
}

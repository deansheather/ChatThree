using ChatThree.Code;
using ChatThree.Util;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using LiteDB;
using System.Text.RegularExpressions;

namespace ChatThree;

internal class SortCode
{
    internal ChatType Type { get; set; }
    internal ChatSource Source { get; set; }

    internal SortCode(ChatType type, ChatSource source)
    {
        this.Type = type;
        this.Source = source;
    }

    public SortCode()
    {
    }

    private bool Equals(SortCode other)
    {
        return this.Type == other.Type && this.Source == other.Source;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj.GetType() == this.GetType() && this.Equals((SortCode)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)this.Type * 397) ^ (int)this.Source;
        }
    }
}

internal class Message
{
    // ReSharper disable once UnusedMember.Global
    internal ObjectId Id { get; } = ObjectId.NewObjectId();
    internal ulong Receiver { get; }
    internal ulong ContentId { get; set; }

    [BsonIgnore]
    internal float? Height;

    [BsonIgnore]
    internal bool IsVisible;

    internal DateTime Date { get; }
    internal ChatCode Code { get; }
    internal List<Chunk> Sender { get; }
    internal List<Chunk> Content { get; }

    internal SeString SenderSource { get; }
    internal SeString ContentSource { get; }

    internal SortCode SortCode { get; }
    internal Guid ExtraChatChannel { get; }

    internal int Hash { get; }

    internal Message(ulong receiver, ChatCode code, List<Chunk> sender, List<Chunk> content, SeString senderSource, SeString contentSource)
    {
        this.Receiver = receiver;
        this.Date = DateTime.UtcNow;
        this.Code = code;
        this.Sender = sender;
        this.Content = content;
        this.Content = this.ReplaceContentURLs();
        this.SenderSource = senderSource;
        this.ContentSource = contentSource;
        this.SortCode = new SortCode(this.Code.Type, this.Code.Source);
        this.ExtraChatChannel = this.ExtractExtraChatChannel();
        this.Hash = this.GenerateHash();

        foreach (var chunk in sender.Concat(content))
        {
            chunk.Message = this;
        }
    }

    internal Message(ObjectId id, ulong receiver, ulong contentId, DateTime date, BsonDocument code, BsonArray sender, BsonArray content, BsonValue senderSource, BsonValue contentSource, BsonDocument sortCode)
    {
        this.Id = id;
        this.Receiver = receiver;
        this.ContentId = contentId;
        this.Date = date;
        this.Code = BsonMapper.Global.ToObject<ChatCode>(code);
        this.Sender = BsonMapper.Global.Deserialize<List<Chunk>>(sender);
        this.Content = BsonMapper.Global.Deserialize<List<Chunk>>(content);
        this.Content = this.ReplaceContentURLs();
        this.SenderSource = BsonMapper.Global.Deserialize<SeString>(senderSource);
        this.ContentSource = BsonMapper.Global.Deserialize<SeString>(contentSource);
        this.SortCode = BsonMapper.Global.ToObject<SortCode>(sortCode);
        this.ExtraChatChannel = this.ExtractExtraChatChannel();
        this.Hash = this.GenerateHash();

        foreach (var chunk in this.Sender.Concat(this.Content))
        {
            chunk.Message = this;
        }
    }

    internal Message(ObjectId id, ulong receiver, ulong contentId, DateTime date, BsonDocument code, BsonArray sender, BsonArray content, BsonValue senderSource, BsonValue contentSource, BsonDocument sortCode, BsonValue extraChatChannel)
    {
        this.Id = id;
        this.Receiver = receiver;
        this.ContentId = contentId;
        this.Date = date;
        this.Code = BsonMapper.Global.ToObject<ChatCode>(code);
        this.Sender = BsonMapper.Global.Deserialize<List<Chunk>>(sender);
        this.Content = BsonMapper.Global.Deserialize<List<Chunk>>(content);
        this.Content = this.ReplaceContentURLs();
        this.SenderSource = BsonMapper.Global.Deserialize<SeString>(senderSource);
        this.ContentSource = BsonMapper.Global.Deserialize<SeString>(contentSource);
        this.SortCode = BsonMapper.Global.ToObject<SortCode>(sortCode);
        this.ExtraChatChannel = BsonMapper.Global.Deserialize<Guid>(extraChatChannel);
        this.Hash = this.GenerateHash();

        foreach (var chunk in this.Sender.Concat(this.Content))
        {
            chunk.Message = this;
        }
    }

    private int GenerateHash()
    {
        return this.SortCode.GetHashCode()
               ^ this.ExtraChatChannel.GetHashCode()
               ^ string.Join("", this.Sender.Select(c => c.StringValue())).GetHashCode()
               ^ string.Join("", this.Content.Select(c => c.StringValue())).GetHashCode();
    }

    private Guid ExtractExtraChatChannel()
    {
        if (this.ContentSource.Payloads.Count > 0 && this.ContentSource.Payloads[0] is RawPayload raw)
        {
            // this does an encode and clone every time it's accessed, so cache
            var data = raw.Data;
            if (data[1] == 0x27 && data[2] == 18 && data[3] == 0x20)
            {
                return new Guid(data[4..^1]);
            }
        }

        return Guid.Empty;
    }

    // URLRegex returns a regex object that matches URLs like:
    // https://example.com
    // http://example.com
    // www.example.com
    // https://sub.example.com
    // example.com
    // sub.example.com
    //
    // It matches URLs with www. or https:// prefix, and also matches URLs
    // without a prefix on specific TLDs.
    private static Regex URLRegex = new Regex(
        @"((https?:\/\/|www\.)[a-z0-9-]+(\.[a-z0-9-]+)*|([a-z0-9-]+(\.[a-z0-9-]+)*\.(com|net|org|co|io|app)))(:[\d]{1,5})?(\/[^\s]+)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // ReplaceContentURLs replaces all URLs in this.Content with URLChunks.
    private List<Chunk> ReplaceContentURLs()
    {
        var newChunks = new List<Chunk>();
        void AddChunk(Chunk chunk) {
            chunk.Message = this;
            newChunks.Add(chunk);
        }

        foreach (var chunk in this.Content)
        {
            // If it's not a text chunk or it already links to somewhere,
            // ignore it.
            if (chunk is not TextChunk text || chunk.Link != null)
            {
                newChunks.Add(chunk);
                continue;
            }

            // Find all URLs with the regex and insert URLChunks.
            var matches = URLRegex.Matches(text.Content);
            var remainderIndex = 0;
            foreach (Match match in matches.Cast<Match>())
            {
                // Add the text before the URL.
                if (match.Index > remainderIndex)
                {
                    AddChunk(text.CopyStyle(chunk.Source, chunk.Link, text.Content[remainderIndex..match.Index]));
                }

                // Update the remainder index.
                remainderIndex = match.Index + match.Length;

                // Add the URL.
                try
                {
                    var link = URIPayload.ResolveURI(match.Value);
                    AddChunk(text.CopyStyle(chunk.Source, link, match.Value));
                }
                catch (UriFormatException)
                {
                    Plugin.Log.Debug($"Invalid URL accepted by Regex but failed URI parsing: '{match.Value}'");
                    // If the URL is invalid, set the remainder index to the
                    // beginning of the match so it'll get included in the next
                    // text chunk.
                    remainderIndex = match.Index;
                }
            }

            // Add the text after the last URL.
            if (remainderIndex < text.Content.Length)
            {
                AddChunk(text.CopyStyle(chunk.Source, chunk.Link, text.Content[remainderIndex..]));
            }
        }

        return newChunks;
    }
}

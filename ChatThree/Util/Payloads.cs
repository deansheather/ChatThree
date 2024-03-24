using Dalamud.Game.Text.SeStringHandling;

namespace ChatThree.Util;

internal class PartyFinderPayload : Payload
{
    public override PayloadType Type => (PayloadType)0x50;

    internal uint Id { get; }

    internal PartyFinderPayload(uint id)
    {
        this.Id = id;
    }

    protected override byte[] EncodeImpl()
    {
        throw new NotImplementedException();
    }

    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        throw new NotImplementedException();
    }
}

internal class AchievementPayload : Payload
{
    public override PayloadType Type => (PayloadType)0x51;

    internal uint Id { get; }

    internal AchievementPayload(uint id)
    {
        this.Id = id;
    }

    protected override byte[] EncodeImpl()
    {
        throw new NotImplementedException();
    }

    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        throw new NotImplementedException();
    }
}

internal class URIPayload(Uri uri) : Payload
{
    public override PayloadType Type => PayloadType.Unknown;

    public Uri Uri { get; init; } = uri;

    private static readonly string[] ExpectedSchemes = ["http", "https"];
    private static readonly string DefaultScheme = "https";

    // ResolveURI takes the Text of the base TextChunk and turns it into a full
    // URL that can be opened in a browser.
    //
    // This mostly just adds `https://` if there's no protocol.
    public static URIPayload ResolveURI(string rawURI)
    {
        ArgumentNullException.ThrowIfNull(rawURI);

        // Check for expected scheme ://, if not add https://
        foreach (var scheme in ExpectedSchemes)
        {
            if (rawURI.StartsWith($"{scheme}://"))
            {
                return new URIPayload(new Uri(rawURI));
            }
        }
        if (rawURI.Contains("://"))
        {
            throw new UriFormatException($"Unsupported scheme in URL: {rawURI}");
        }

        return new URIPayload(new Uri($"{DefaultScheme}://{rawURI}"));
    }

    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        throw new NotImplementedException();
    }

    protected override byte[] EncodeImpl()
    {
        // IDK why EncodeImpl is being called on this class, but we don't
        // really care about the game handling these payloads so just pretend
        // we don't exist.
        return [];
    }
}

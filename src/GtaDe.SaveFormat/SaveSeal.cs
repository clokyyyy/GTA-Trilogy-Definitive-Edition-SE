using System.Security.Cryptography;

namespace GtaDe.SaveFormat;

/// <summary>How the 16-byte integrity seal at offset 8 is stored.</summary>
public enum SealVariant
{
    /// <summary>The MD5 digest is stored as-is.</summary>
    Plain,

    /// <summary>Every byte of the MD5 digest is inverted (XOR 0xFF) before storing.</summary>
    Inverted,
}

/// <summary>
/// The integrity seal the Definitive Edition stores at offset 8 of every save slot.
/// </summary>
/// <remarks>
/// <para>
/// The digest is computed over a buffer that is the same length as the file, with the first
/// 24 bytes (magic, version and the seal field itself) replaced by zeros:
/// </para>
/// <code>
/// digest = MD5( zeros(24) || file[24..] )
/// </code>
/// <para>
/// GTA III: The Definitive Edition (save version 16) stores the <em>inverted</em> digest.
/// Verified against five real save files produced by the retail game. Older documented builds
/// stored the plain digest for GTA III, so both variants are supported and the variant actually
/// used by a file is detected on load and preserved on save.
/// </para>
/// <para>
/// The game validates this value. A save with an incorrect seal is silently treated as an empty
/// slot, with no error message, so it must be recomputed after every edit.
/// </para>
/// </remarks>
public static class SaveSeal
{
    /// <summary>Offset of the 16-byte seal within the file.</summary>
    public const int Offset = 8;

    /// <summary>Length of the seal in bytes.</summary>
    public const int Length = 16;

    /// <summary>Number of leading bytes excluded from (zeroed in) the digest input.</summary>
    public const int ZeroedPrefixLength = 24;

    /// <summary>The variant used by GTA III: The Definitive Edition.</summary>
    public const SealVariant DefaultVariant = SealVariant.Inverted;

    /// <summary>Computes the raw MD5 digest for <paramref name="data"/>, ignoring the stored seal.</summary>
    public static byte[] ComputeDigest(ReadOnlySpan<byte> data)
    {
        if (data.Length < ZeroedPrefixLength)
        {
            throw new SaveFormatException(
                $"A save file must be at least {ZeroedPrefixLength} bytes long to carry a seal.");
        }

        using var md5 = MD5.Create();
        Span<byte> prefix = stackalloc byte[ZeroedPrefixLength];
        prefix.Clear();

        var digest = new byte[MD5.HashSizeInBytes];
        md5.TransformBlock(prefix.ToArray(), 0, ZeroedPrefixLength, null, 0);

        var body = data[ZeroedPrefixLength..].ToArray();
        md5.TransformFinalBlock(body, 0, body.Length);
        md5.Hash!.CopyTo(digest, 0);
        return digest;
    }

    /// <summary>Computes the seal bytes exactly as they should appear in the file.</summary>
    public static byte[] Compute(ReadOnlySpan<byte> data, SealVariant variant)
    {
        var digest = ComputeDigest(data);
        if (variant == SealVariant.Inverted)
        {
            for (var i = 0; i < digest.Length; i++)
            {
                digest[i] ^= 0xFF;
            }
        }

        return digest;
    }

    /// <summary>Reads the seal currently stored in the file.</summary>
    public static byte[] Read(byte[] data)
    {
        if (data.Length < Offset + Length)
        {
            throw new SaveFormatException("The file is too short to contain a seal.");
        }

        return data.AsSpan(Offset, Length).ToArray();
    }

    /// <summary>
    /// Determines which variant the stored seal matches, or <c>null</c> when the seal is invalid.
    /// </summary>
    public static SealVariant? Detect(byte[] data)
    {
        var stored = Read(data);
        var digest = ComputeDigest(data);

        if (stored.AsSpan().SequenceEqual(digest))
        {
            return SealVariant.Plain;
        }

        for (var i = 0; i < digest.Length; i++)
        {
            digest[i] ^= 0xFF;
        }

        return stored.AsSpan().SequenceEqual(digest) ? SealVariant.Inverted : null;
    }

    /// <summary>Rewrites the seal in place so the game will accept the file.</summary>
    public static void Apply(byte[] data, SealVariant variant)
    {
        var seal = Compute(data, variant);
        seal.CopyTo(data.AsSpan(Offset, Length));
    }
}

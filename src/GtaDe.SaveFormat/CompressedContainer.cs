using System.IO.Compression;
using System.Text;

namespace GtaDe.SaveFormat;

/// <summary>
/// One record written by Unreal's <c>FArchive::SerializeCompressed</c>.
/// </summary>
/// <remarks>
/// All fields are little-endian int64 except the tag, which is a uint32 followed by four
/// padding bytes:
/// <code>
/// +0   u32 tag = 0x9E2A83C1 (PACKAGE_FILE_TAG)
/// +4   u32 padding
/// +8   i64 blockSize            observed 131072
/// +16  i64 totalCompressedSize
/// +24  i64 totalDecompressedSize
/// +32  N x { i64 compressed; i64 decompressed }   where N = ceil(total / blockSize)
/// </code>
/// followed by N consecutive raw zlib streams.
/// </remarks>
public sealed class CompressedRecord
{
    public const int SummarySize = 32;
    public const int BlockEntrySize = 16;

    /// <summary>Unreal's <c>PACKAGE_FILE_TAG</c>, stored little-endian as <c>C1 83 2A 9E</c>.</summary>
    public static ReadOnlySpan<byte> Magic => [0xC1, 0x83, 0x2A, 0x9E];

    public required int FileOffset { get; init; }
    public required long BlockSize { get; init; }
    public required long CompressedSize { get; init; }
    public required long DecompressedSize { get; init; }

    /// <summary>Per-block (compressed, decompressed) size pairs.</summary>
    public required IReadOnlyList<(long Compressed, long Decompressed)> Blocks { get; init; }

    public int HeaderSize => SummarySize + (Blocks.Count * BlockEntrySize);

    /// <summary>Total bytes this record occupies in the file, including its header.</summary>
    public int TotalSize => HeaderSize + (int)CompressedSize;
}

/// <summary>
/// Reads the Unreal compressed container that carries the script symbol table (the METADATA block).
/// </summary>
/// <remarks>
/// The container is never re-compressed. Its bytes are preserved verbatim inside the save
/// buffer, which is what makes a byte-exact round-trip possible: .NET's deflate implementation
/// would not reproduce the game's exact compressed stream.
/// </remarks>
public sealed class CompressedContainer
{
    /// <summary>Upper bound for one record's metadata; real saves hold a few kilobytes.</summary>
    private const long MaxDecompressedBytes = 64L * 1024 * 1024;

    private CompressedContainer(int offset, int length, IReadOnlyList<CompressedRecord> records, byte[] decompressed)
    {
        FileOffset = offset;
        Length = length;
        Records = records;
        Decompressed = decompressed;
    }

    /// <summary>Offset of the first record header in the save file.</summary>
    public int FileOffset { get; }

    /// <summary>Total byte length of the container in the save file.</summary>
    public int Length { get; }

    /// <summary>Offset of the first byte after the container.</summary>
    public int EndOffset => FileOffset + Length;

    public IReadOnlyList<CompressedRecord> Records { get; }

    /// <summary>Concatenated decompressed payload of every record.</summary>
    public byte[] Decompressed { get; }

    public static bool IsRecordAt(byte[] data, int offset) =>
        offset >= 0 &&
        offset + 4 <= data.Length &&
        data.AsSpan(offset, 4).SequenceEqual(CompressedRecord.Magic);

    public static CompressedContainer Read(byte[] data, int offset)
    {
        if (!IsRecordAt(data, offset))
        {
            throw new SaveFormatException(
                $"Expected an Unreal compression tag at offset 0x{offset:X} but found " +
                $"{Convert.ToHexString(data.AsSpan(offset, Math.Min(4, data.Length - offset)))}.");
        }

        var records = new List<CompressedRecord>();
        var payload = new MemoryStream();
        var cursor = offset;

        while (IsRecordAt(data, cursor))
        {
            var record = ReadRecord(data, cursor);
            var blockStart = cursor + record.HeaderSize;

            foreach (var (compressed, decompressed) in record.Blocks)
            {
                var expanded = Inflate(data, blockStart, (int)compressed, (int)decompressed);
                if (expanded.Length != (int)decompressed)
                {
                    throw new SaveFormatException(
                        $"Compressed block at 0x{blockStart:X} expanded to {expanded.Length} bytes, " +
                        $"expected {decompressed}.");
                }

                payload.Write(expanded, 0, expanded.Length);
                blockStart += (int)compressed;
            }

            records.Add(record);
            cursor += record.TotalSize;
        }

        return new CompressedContainer(offset, cursor - offset, records, payload.ToArray());
    }

    private static CompressedRecord ReadRecord(byte[] data, int offset)
    {
        if (offset + CompressedRecord.SummarySize > data.Length)
        {
            throw new SaveFormatException($"Truncated compression record header at 0x{offset:X}.");
        }

        var blockSize = ByteOps.ReadInt64(data, offset + 8);
        var compressedSize = ByteOps.ReadInt64(data, offset + 16);
        var decompressedSize = ByteOps.ReadInt64(data, offset + 24);

        if (blockSize <= 0 || compressedSize < 0 || decompressedSize < 0
            || decompressedSize > MaxDecompressedBytes || compressedSize > data.Length)
        {
            throw new SaveFormatException($"Compression record at 0x{offset:X} has implausible sizes.");
        }

        var blockCount = (int)((decompressedSize + blockSize - 1) / blockSize);
        var entriesStart = offset + CompressedRecord.SummarySize;
        if ((long)entriesStart + ((long)blockCount * CompressedRecord.BlockEntrySize) > data.Length)
        {
            throw new SaveFormatException($"Truncated compression block table at 0x{entriesStart:X}.");
        }

        var blocks = new (long, long)[blockCount];
        long compressedTotal = 0;
        long decompressedTotal = 0;
        for (var i = 0; i < blockCount; i++)
        {
            var entry = entriesStart + (i * CompressedRecord.BlockEntrySize);
            var compressed = ByteOps.ReadInt64(data, entry);
            var decompressed = ByteOps.ReadInt64(data, entry + 8);
            if (compressed < 0 || decompressed < 0 || compressed > data.Length || decompressed > MaxDecompressedBytes)
            {
                throw new SaveFormatException($"Compression record at 0x{offset:X} has an implausible block entry.");
            }

            blocks[i] = (compressed, decompressed);
            compressedTotal += compressed;
            decompressedTotal += decompressed;
        }

        if (compressedTotal != compressedSize || decompressedTotal != decompressedSize)
        {
            throw new SaveFormatException(
                $"Compression record at 0x{offset:X} block table sums to " +
                $"({compressedTotal}, {decompressedTotal}) but the summary says " +
                $"({compressedSize}, {decompressedSize}).");
        }

        var record = new CompressedRecord
        {
            FileOffset = offset,
            BlockSize = blockSize,
            CompressedSize = compressedSize,
            DecompressedSize = decompressedSize,
            Blocks = blocks,
        };

        if (offset + record.TotalSize > data.Length)
        {
            throw new SaveFormatException(
                $"Compression record at 0x{offset:X} claims {record.TotalSize} bytes but the file is too short.");
        }

        return record;
    }

    private static byte[] Inflate(byte[] data, int offset, int count, int expected)
    {
        using var source = new MemoryStream(data, offset, count, writable: false);
        using var zlib = new ZLibStream(source, CompressionMode.Decompress);

        // Read one byte past the declared size so an oversized stream is detected without inflating all of it.
        var buffer = new byte[expected + 1];
        var total = 0;
        int read;
        while (total < buffer.Length && (read = zlib.Read(buffer, total, buffer.Length - total)) > 0)
        {
            total += read;
        }

        return total == buffer.Length ? buffer : buffer.AsSpan(0, total).ToArray();
    }

    /// <summary>
    /// The payload is prefixed with a little-endian uint32 equal to <c>payloadLength - 4</c>.
    /// This returns the text after that prefix.
    /// </summary>
    public string GetPayloadText()
    {
        if (Decompressed.Length < 4)
        {
            return string.Empty;
        }

        var declared = ByteOps.ReadInt32(Decompressed, 0);
        var available = Decompressed.Length - 4;
        var length = declared >= 0 && declared <= available ? declared : available;
        return Encoding.Latin1.GetString(Decompressed, 4, length);
    }
}

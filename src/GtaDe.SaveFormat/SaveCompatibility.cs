using System.IO;

namespace GtaDe.SaveFormat;

/// <summary>Accepts recognized layout families independently of their revision numbers.</summary>
internal static class SaveCompatibility
{
    private const int MaxFileBytes = 32 * 1024 * 1024;

    public static SaveParts Read(byte[] data)
    {
        if (data.Length > MaxFileBytes)
        {
            throw new SaveFormatException("The save exceeds the supported 32 MiB file-size limit.");
        }

        var buffer = (byte[])data.Clone();
        var header = SaveHeader.Read(buffer);
        var seal = SaveSeal.Detect(buffer);
        var matches = new List<SaveParts>();
        var failures = new List<string>();

        foreach (var game in Enum.GetValues<GameKind>())
        {
            try
            {
                var layout = SaveLayout.Read(buffer, game);
                ValidateFields(buffer, layout);
                var symbols = ScriptSymbolTable.Parse(layout.Metadata.GetPayloadText());
                var globals = GlobalVariableSpace.Create(buffer, layout[SaveBlockKind.Scripts], symbols);
                matches.Add(new SaveParts(buffer, layout, symbols, globals,
                    seal ?? (game == GameKind.Gta3 ? SealVariant.Inverted : SealVariant.Plain),
                    seal is not null));
            }
            catch (Exception ex) when (ex is SaveFormatException or InvalidDataException
                or ArgumentException or OverflowException or IndexOutOfRangeException)
            {
                failures.Add($"{game.DisplayName()}: {ex.Message}");
            }
        }

        if (matches.Count != 1)
        {
            var reason = matches.Count > 1
                ? "the game layout is ambiguous"
                : "the editable layout could not be validated. " + string.Join(" ", failures);
            throw new SaveFormatException(
                $"Recognized Definitive Edition header, revision {header.Version}, but {reason}. Refusing to edit.");
        }

        var match = matches[0];
        if (seal is null && GameDetection.FromVersion(header.Version) != match.Layout.Game)
        {
            throw new SaveFormatException(
                $"Recognized {match.Layout.Game.DisplayName()} Definitive Edition layout, revision {header.Version}, " +
                "but its checksum could not be verified. Refusing to edit an untested revision with an invalid or unsupported checksum.");
        }

        return match;
    }

    private static void ValidateFields(byte[] data, SaveLayout layout)
    {
        void Require(bool condition, string field)
        {
            if (!condition)
            {
                throw new SaveFormatException($"The {field} does not match a supported editable layout.");
            }
        }

        void Size(SaveBlockKind kind, int expected) =>
            Require(layout[kind].DataLength == expected, $"{kind} size");

        if (layout.Game == GameKind.SanAndreas)
        {
            var block = layout[SaveBlockKind.SimpleVariables];
            Require(block.Offset == layout.Header.Length + 8 &&
                ByteOps.ReadUInt32(data, layout.Header.Length) == layout.Header.Version &&
                ByteOps.ReadUInt32(data, layout.Header.Length + 4) == layout.Header.Version,
                "San Andreas header extension");
            var simple = new SanAndreasSimpleVars(data, block);
            var keyLength = ByteOps.ReadInt32(data, block.DataOffset);
            Require(block.DataLength == 4 + keyLength + 187, "SIMPLE_VARIABLES size");
            Require(keyLength == layout.Header.LastMissionKeyLength &&
                data.AsSpan(block.DataOffset + 4, keyLength).SequenceEqual(
                    data.AsSpan(SaveHeader.LastMissionOffset, keyLength)), "SIMPLE_VARIABLES mission key");
            Require(simple.Hour < 24 && simple.Minute < 60 &&
                Enum.IsDefined(simple.CurrentWeather) && Enum.IsDefined(simple.PreviousWeather) &&
                float.IsFinite(simple.WeatherInterpolation), "clock/weather fields");
            Size(SaveBlockKind.PlayerInfo, 44);
            Size(SaveBlockKind.Stats, 1948);
            Size(SaveBlockKind.Tags, 104);
            var player = new SanAndreasPlayerInfoBlock(data, layout[SaveBlockKind.PlayerInfo]);
            Require(ByteOps.ReadInt32(data, layout[SaveBlockKind.PlayerInfo].DataOffset) == 40 &&
                player.MaxHealth > 0 && player.MaxArmour > 0, "PLAYERINFO fields");
            Require(new SanAndreasPlayerPed(data, layout[SaveBlockKind.PedPool]).IsAvailable, "CJ ped/weapon fields");
            Require(new SanAndreasTagsBlock(data, layout[SaveBlockKind.Tags]).Count == 100, "TAGS count");
            var stats = new SanAndreasStatsBlock(data, layout[SaveBlockKind.Stats]);
            for (var id = 0; id < SanAndreasStatsBlock.FloatStatCount; id++)
            {
                Require(float.IsFinite(stats.GetFloatStat(id)), $"float stat {id}");
            }
        }
        else
        {
            var offset = layout.SimpleVarsOffset;
            Require(ByteOps.ReadUInt32(data, offset) == SimpleVars.Signature, "SimpleVars signature");
            Require(data[offset + 0x1C] < 24 && data[offset + 0x1D] < 60, "clock fields");
            var maxWeather = layout.Game == GameKind.Gta3 ? 3 : 5;
            Require(ByteOps.ReadUInt16(data, offset + 0x40) <= maxWeather &&
                ByteOps.ReadUInt16(data, offset + 0x42) <= maxWeather &&
                float.IsFinite(ByteOps.ReadSingle(data, offset + 0x46)), "weather fields");
            Size(SaveBlockKind.PlayerInfo, layout.Game == GameKind.Gta3 ? 376 : 392);
            Size(SaveBlockKind.Stats, layout.Game == GameKind.Gta3 ? 468 : 635);
            var player = layout[SaveBlockKind.PlayerInfo].DataOffset;
            Require(ByteOps.ReadInt32(data, player + 0x17) == 100 &&
                float.IsFinite(ByteOps.ReadSingle(data, player + 0x0B)), "player fields");
            if (layout.Game == GameKind.Gta3)
            {
                Require(new GarageBlock(data, layout[SaveBlockKind.Garages]).IsRecognised, "garage fields");
            }
            else
            {
                Require(new ViceCityPlayerPed(data, layout[SaveBlockKind.PedPool]).IsAvailable, "player ped/weapon fields");
            }
        }
    }
}

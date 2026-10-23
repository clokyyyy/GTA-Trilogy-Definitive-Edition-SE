using GtaDe.Definitions;
using GtaDe.Editing;

namespace GtaDe.SaveFormat.Tests;

public class CompatibilityTests
{
    public const string Revision41Sample = "GTASA-revision41.sav";

    public static byte[] WithRevision(string sample, uint revision)
    {
        var bytes = SampleSaves.Bytes(sample);
        var original = DeSaveFile.Parse(bytes);
        ByteOps.WriteUInt32(bytes, SaveHeader.VersionOffset, revision);
        ByteOps.WriteUInt32(bytes, SaveHeader.VersionRepeatOffset, revision);
        if (original.Game == GameKind.SanAndreas)
        {
            ByteOps.WriteUInt32(bytes, original.Layout.Header.Length, revision);
            ByteOps.WriteUInt32(bytes, original.Layout.Header.Length + 4, revision);
        }

        SaveSeal.Apply(bytes, original.SealVariant);
        return bytes;
    }

    [Fact]
    public void ActualRevision41HasTheVerifiedLayoutAndShiftedGlobals()
    {
        var bytes = SampleSaves.Bytes(Revision41Sample);
        var save = Assert.IsType<SanAndreasSaveFile>(DeSaveFile.Parse(bytes));
        Assert.Equal(41u, save.Layout.Header.Version);
        Assert.Equal("ZERO_4", save.LastMissionKey);
        Assert.True(save.SealWasValid);
        Assert.Equal(SealVariant.Inverted, save.SealVariant);
        Assert.False(save.IsUntestedRevision);
        Assert.Equal(bytes, save.ToBytes());
        Assert.Equal(3652, save.Symbols.Globals.Count);
        Assert.Equal(1_147_035, save.PlayerInfo.Money);
        Assert.Equal(save.PlayerInfo.Money, save.PlayerInfo.VisibleMoney);
        Assert.Equal(220, save.PlayerInfo.MaxHealth);
        Assert.Equal(150, save.PlayerInfo.MaxArmour);
        Assert.Equal(220f, save.PlayerPed.Health);
        Assert.Equal(150f, save.PlayerPed.Armour);
        Assert.Equal(SanAndreasWeapon.Ak47, save.PlayerPed.Weapons[5].Type);
        Assert.Equal(60, save.PlayerPed.Weapons[5].AmmoTotal);
        Assert.Equal(48, save.Stats.GetIntStat(147));
        Assert.Equal(100, save.Tags.SprayedCount);
        Assert.Equal(9, save.SimpleVars.Hour);
        Assert.Equal(20, save.SimpleVars.Minute);
        Assert.Equal(SanAndreasWeather.SunnySanFierro, save.SimpleVars.CurrentWeather);
        Assert.Equal(1812, save.Symbols.RequireGlobal("FLAG_SWEET_MISSION_COUNTER").Index);
        Assert.Equal(9, save.Globals.GetInt("FLAG_SWEET_MISSION_COUNTER"));
        var searchlights = save.Layout[SaveBlockKind.Searchlights];
        Assert.Equal(0x8FBCC, searchlights.Offset);
        Assert.Equal(new byte[] { 0 }, searchlights.Data(bytes).ToArray());
        Assert.Equal(searchlights.Offset, save.Layout[SaveBlockKind.Briefs].EndOffset);
        Assert.Equal(bytes.Length, searchlights.EndOffset);
        var editor = new SanAndreasProgressionEditor(save);
        var flags = SanAndreasCatalogue.ActivityTabs.SelectMany(t => t.Entries)
            .Concat(SanAndreasCatalogue.Safehouses).Concat(SanAndreasCatalogue.CatalinaRobberies)
            .Append(SanAndreasCatalogue.KingInExile);
        Assert.All(flags, flag => Assert.True(editor.Has(flag), flag.Variable));
        Assert.All(SanAndreasCatalogue.Strands, strand => Assert.True(editor.Has(strand), strand.Counter));
    }

    [Theory]
    [InlineData("GTA3sf1.sav", GameKind.Gta3)]
    [InlineData("GTAVCsf8.sav", GameKind.ViceCity)]
    [InlineData("GTASAsf1.sav", GameKind.SanAndreas)]
    [InlineData(Revision41Sample, GameKind.SanAndreas)]
    public void UnknownRevisionsUseStructureAndRetainRevisionAndSeal(string sample, GameKind game)
    {
        var bytes = WithRevision(sample, 1001);
        var save = DeSaveFile.Parse(bytes);
        Assert.Equal(game, save.Game);
        Assert.Equal(game, GameDetection.Detect(bytes));
        Assert.Equal(game, SaveLayout.Read(bytes).Game);
        Assert.True(save.IsUntestedRevision);
        Assert.Equal(bytes, save.ToBytes());
        switch (save)
        {
            case Gta3SaveFile gta3:
                gta3.PlayerInfo.SetMoney(12345);
                break;
            case ViceCitySaveFile vc:
                vc.PlayerInfo.SetMoney(12345);
                break;
            case SanAndreasSaveFile sa:
                sa.PlayerInfo.SetMoney(12345);
                break;
        }

        var reloaded = DeSaveFile.Parse(save.ToBytes());
        Assert.True(reloaded.SealWasValid);
        Assert.Equal(save.SealVariant, reloaded.SealVariant);
        Assert.Equal(1001u, reloaded.Layout.Header.Version);
        Assert.Equal(1001u, reloaded.Layout.Header.VersionRepeat);
        var money = reloaded switch
        {
            Gta3SaveFile gta3 => gta3.PlayerInfo.Money,
            ViceCitySaveFile vc => vc.PlayerInfo.Money,
            SanAndreasSaveFile sa => sa.PlayerInfo.Money,
            _ => throw new InvalidOperationException(),
        };
        Assert.Equal(12345, money);
    }

    [Theory]
    [InlineData("GTA3sf1.sav")]
    [InlineData("GTAVCsf8.sav")]
    [InlineData("GTASAsf1.sav")]
    [InlineData(Revision41Sample)]
    public void UnknownRevisionsRequireAValidChecksum(string sample)
    {
        var bytes = WithRevision(sample, 1001);
        bytes[SaveSeal.Offset] ^= 0x80;
        var error = Assert.Throws<SaveFormatException>(() => DeSaveFile.Parse(bytes));
        Assert.Contains("checksum", error.Message);
    }

    [Theory]
    [InlineData("GTA3sf1.sav")]
    [InlineData("GTAVCsf8.sav")]
    [InlineData("GTASAsf1.sav")]
    [InlineData(Revision41Sample)]
    public void TestedRevisionsKeepTheExistingInvalidSealWarningWorkflow(string sample)
    {
        var bytes = SampleSaves.Bytes(sample);
        bytes[SaveSeal.Offset] ^= 0x80;
        Assert.False(DeSaveFile.Parse(bytes).SealWasValid);
    }

    [Fact]
    public void ARevisionNumberCannotOverrideGameIdentity()
    {
        var bytes = WithRevision(Revision41Sample, GameDetection.ViceCityVersion);
        var save = DeSaveFile.Parse(bytes);
        Assert.IsType<SanAndreasSaveFile>(save);
        Assert.True(save.IsUntestedRevision);
        Assert.Throws<SaveFormatException>(() => ViceCitySaveFile.Parse(bytes));
        Assert.Throws<SaveFormatException>(() => Gta3SaveFile.Parse(bytes));
    }

    [Theory]
    [InlineData("GTASAsf1.sav")]
    [InlineData(Revision41Sample)]
    public void AllSanAndreasEditingSurfacesOnlyChangeTheirIntendedBytes(string sample)
    {
        var bytes = SampleSaves.Bytes(sample);
        var save = SanAndreasSaveFile.Parse(bytes);
        var allowed = new HashSet<int>(Enumerable.Range(SaveSeal.Offset, SaveSeal.Length));
        void Allow(int offset, int length) => allowed.UnionWith(Enumerable.Range(offset, length));
        void AllowGlobal(string name, int element = 0)
        {
            var g = save.Symbols.RequireGlobal(name);
            Allow(save.Globals.BaseOffset + g.Index + (element * 4), 4);
        }

        var player = save.Layout[SaveBlockKind.PlayerInfo].DataOffset;
        Allow(player + 4, 4);
        Allow(player + 16, 4);
        Allow(player + 32, 8);
        save.PlayerInfo.SetMoney(1234);
        save.PlayerInfo.InfiniteSprint = false;
        save.PlayerInfo.FastReload = true;
        save.PlayerInfo.Fireproof = false;
        save.PlayerInfo.MaxHealth = 200;
        save.PlayerInfo.MaxArmour = 200;
        save.PlayerInfo.GetOutOfJailFree = true;
        save.PlayerInfo.FreeHealthCare = true;
        save.PlayerInfo.DriveByAllowed = false;

        var ped = save.Layout[SaveBlockKind.PedPool].DataOffset;
        Allow(ped + 0x24, 8);
        Allow(ped + 0x2C + (5 * 28), 28);
        save.PlayerPed.Health = 180;
        save.PlayerPed.Armour = 99;
        save.PlayerPed.Weapons[5].Set(SanAndreasWeapon.M4, 600);
        var stats = save.Layout[SaveBlockKind.Stats].DataOffset;
        Allow(stats + (23 * 4), 4);
        Allow(stats + 328 + ((160 - 120) * 4), 4);
        save.Stats.SetFloatStat(23, 999f);
        save.Stats.SetIntStat(160, 777);
        Allow(save.Layout[SaveBlockKind.Tags].DataOffset + 4, 100);
        save.Tags.SetSprayedCount(7);
        Allow(save.SimpleVars.Offset + 0x1F, 2);
        Allow(save.SimpleVars.Offset + 0x3F, 10);
        save.SimpleVars.Hour = 3;
        save.SimpleVars.Minute = 4;
        save.SimpleVars.ForcedWeather = SanAndreasSimpleVars.WeatherNotForced;
        save.SimpleVars.SetWeather(SanAndreasWeather.FoggySanFierro);

        var editor = new SanAndreasProgressionEditor(save);
        var sweet = SanAndreasCatalogue.Strands.Single(s => s.Id == "sweet");
        AllowGlobal(sweet.Counter);
        editor.SetComplete(sweet, sweet.Missions[2], false);
        var house = SanAndreasCatalogue.Safehouses[0];
        Assert.True(editor.Has(house));
        AllowGlobal(house.Variable, house.Element);
        editor.Set(house, false);
        AllowGlobal("FLAG_GRAV_PASSED_1STIME");
        editor.SetVariable("FLAG_GRAV_PASSED_1STIME", 1);

        var output = save.ToBytes();
        Assert.Equal(bytes.Length, output.Length);
        Assert.NotEqual(bytes, output);
        for (var i = 0; i < bytes.Length; i++)
        {
            Assert.True(bytes[i] == output[i] || allowed.Contains(i), $"Unexpected edit at 0x{i:X}");
        }

        var reload = SanAndreasSaveFile.Parse(output);
        Assert.True(reload.SealWasValid);
        Assert.Equal(save.SealVariant, reload.SealVariant);
        Assert.Equal(1234, reload.PlayerInfo.Money);
        Assert.Equal(1234, reload.PlayerInfo.VisibleMoney);
        Assert.Equal(200, reload.PlayerInfo.MaxHealth);
        Assert.Equal(200, reload.PlayerInfo.MaxArmour);
        Assert.Equal(180f, reload.PlayerPed.Health);
        Assert.Equal(99f, reload.PlayerPed.Armour);
        Assert.Equal(SanAndreasWeapon.M4, reload.PlayerPed.Weapons[5].Type);
        Assert.Equal(600, reload.PlayerPed.Weapons[5].AmmoTotal);
        Assert.Equal(999f, reload.Stats.GetFloatStat(23));
        Assert.Equal(777, reload.Stats.GetIntStat(160));
        Assert.Equal(7, reload.Tags.SprayedCount);
        Assert.Equal(3, reload.SimpleVars.Hour);
        Assert.Equal(4, reload.SimpleVars.Minute);
        Assert.Equal(SanAndreasWeather.FoggySanFierro, reload.SimpleVars.CurrentWeather);
        Assert.Equal(2, reload.Globals.GetInt(sweet.Counter));
        var progress = new SanAndreasProgressionEditor(reload);
        Assert.False(progress.IsSet(house));
        Assert.Equal(1, progress.GetVariable("FLAG_GRAV_PASSED_1STIME"));
    }

    [Theory]
    [InlineData("player")]
    [InlineData("clock")]
    [InlineData("tags")]
    [InlineData("weapons")]
    [InlineData("globals")]
    [InlineData("anchor")]
    [InlineData("section")]
    [InlineData("metadata")]
    [InlineData("nan-anchor")]
    [InlineData("extension")]
    public void AValidChecksumDoesNotMakeAnUnverifiedLayoutEditable(string corruption)
    {
        var bytes = WithRevision(Revision41Sample, 1001);
        var save = SanAndreasSaveFile.Parse(bytes);
        switch (corruption)
        {
            case "player":
                ByteOps.WriteInt32(bytes, save.Layout[SaveBlockKind.PlayerInfo].DataOffset, 36);
                break;
            case "clock":
                bytes[save.SimpleVars.Offset + 0x1F] = 255;
                break;
            case "tags":
                ByteOps.WriteInt32(bytes, save.Layout[SaveBlockKind.Tags].DataOffset, 101);
                break;
            case "weapons":
                ByteOps.WriteInt32(bytes, save.Layout[SaveBlockKind.PedPool].DataOffset + 0x2C + 28, 999);
                break;
            case "globals":
                ByteOps.WriteInt32(bytes, save.Layout[SaveBlockKind.Scripts].DataOffset, int.MaxValue);
                break;
            case "anchor":
                var g = save.Symbols.RequireGlobal("ONE_SIXTEENTH");
                ByteOps.WriteSingle(bytes, save.Globals.BaseOffset + g.Index, 100);
                break;
            case "section":
                bytes[save.Layout[SaveBlockKind.Stats].Offset + 4] = (byte)'X';
                break;
            case "metadata":
                ByteOps.WriteInt64(bytes, save.Layout.Metadata.FileOffset + 8, long.MaxValue);
                break;
            case "nan-anchor":
                ByteOps.WriteSingle(bytes, save.Globals.BaseOffset +
                    save.Symbols.RequireGlobal("ONE_SIXTEENTH").Index, float.NaN);
                break;
            case "extension":
                ByteOps.WriteUInt32(bytes, save.Layout.Header.Length + 4, 1);
                break;
        }

        SaveSeal.Apply(bytes, save.SealVariant);
        var error = Assert.Throws<SaveFormatException>(() => DeSaveFile.Parse(bytes));
        Assert.Contains("layout", error.Message);
        Assert.Contains("1001", error.Message);
    }

    [Fact]
    public void MismatchedHeaderRevisionsAreRejected()
    {
        var bytes = SampleSaves.Bytes(Revision41Sample);
        ByteOps.WriteUInt32(bytes, SaveHeader.VersionRepeatOffset, 33);
        Assert.Contains("mismatched", Assert.Throws<SaveFormatException>(() => DeSaveFile.Parse(bytes)).Message);
    }

    [Fact]
    public void AnInsertedByteCannotShiftEditableStatsSilently()
    {
        var original = SampleSaves.Bytes(Revision41Sample);
        var save = SanAndreasSaveFile.Parse(original);
        var offset = save.Layout[SaveBlockKind.Stats].DataOffset;
        var bytes = original[..offset].Concat(new byte[] { 0 }).Concat(original[offset..]).ToArray();
        SaveSeal.Apply(bytes, save.SealVariant);
        Assert.Contains("Stats size", Assert.Throws<SaveFormatException>(() => DeSaveFile.Parse(bytes)).Message);
    }

    [Fact]
    public void UnrelatedFilesAndTruncatedSavesAreRejected()
    {
        Assert.Throws<SaveFormatException>(() => DeSaveFile.Parse(new byte[128]));
        var bytes = SampleSaves.Bytes(Revision41Sample)[..512];
        SaveSeal.Apply(bytes, SealVariant.Inverted);
        Assert.Throws<SaveFormatException>(() => DeSaveFile.Parse(bytes));
    }

    [Fact]
    public void PreservedOpaqueTailDoesNotRequireANewRevisionWhitelistEntry()
    {
        var original = WithRevision(Revision41Sample, 9000);
        var bytes = original.Concat(new byte[] { 0x55, 0xAA, 0x00, 0xFE }).ToArray();
        SaveSeal.Apply(bytes, SealVariant.Inverted);
        var save = SanAndreasSaveFile.Parse(bytes);
        save.PlayerInfo.SetMoney(42);
        Assert.Equal(bytes[^4..], save.ToBytes()[^4..]);
    }
}

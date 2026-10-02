using Xunit.Abstractions;

namespace GtaDe.SaveFormat.Tests;

public class SaveSealTests
{
    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void EverySampleUsesTheInvertedSeal(string name)
    {
        var data = SampleSaves.Bytes(name);
        Assert.Equal(SealVariant.Inverted, SaveSeal.Detect(data));
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void RecomputingTheSealReproducesTheStoredBytes(string name)
    {
        var data = SampleSaves.Bytes(name);
        var stored = SaveSeal.Read(data);

        SaveSeal.Apply(data, SealVariant.Inverted);

        Assert.Equal(stored, SaveSeal.Read(data));
    }

    [Fact]
    public void TheSealCoversEditsMadeAfterTheHeader()
    {
        var data = SampleSaves.Bytes("GTA3sf1.sav");
        var before = SaveSeal.Compute(data, SealVariant.Inverted);

        data[^1] ^= 0xFF;
        var after = SaveSeal.Compute(data, SealVariant.Inverted);

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void TheSealIgnoresTheFirstTwentyFourBytes()
    {
        var data = SampleSaves.Bytes("GTA3sf1.sav");
        var before = SaveSeal.ComputeDigest(data);

        for (var i = 0; i < SaveSeal.ZeroedPrefixLength; i++)
        {
            data[i] ^= 0x5A;
        }

        Assert.Equal(before, SaveSeal.ComputeDigest(data));
    }
}

public class SaveLayoutTests(ITestOutputHelper output)
{
    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void EverySampleParses(string name)
    {
        var data = SampleSaves.Bytes(name);
        var layout = SaveLayout.Read(data);

        Assert.Equal(16u, layout.Header.Version);
        Assert.Equal(layout.Header.Version, layout.Header.VersionRepeat);
        Assert.InRange(layout.Header.Timestamp, new DateTime(2021, 1, 1), DateTime.UtcNow.AddDays(1));
        Assert.Equal(20, layout.Blocks.Count);

        output.WriteLine($"{name}: {data.Length} bytes, last mission {layout.Header.LastMissionKey}, " +
                         $"saved {layout.Header.Timestamp:u}");
        foreach (var block in layout.Blocks)
        {
            output.WriteLine($"  0x{block.Offset:X6} {block.Kind,-20} {block.Tag ?? "-",-4} " +
                             $"data 0x{block.DataOffset:X6} len {block.DataLength}");
        }

        output.WriteLine($"  0x{layout.TrailerOffset:X6} trailer              len {layout.TrailerLength}");
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void BlocksTileTheFileWithoutGaps(string name)
    {
        var data = SampleSaves.Bytes(name);
        var layout = SaveLayout.Read(data);

        var cursor = layout.Blocks[0].Offset;
        foreach (var block in layout.Blocks)
        {
            Assert.Equal(cursor, block.Offset);
            cursor = block.EndOffset;
        }

        Assert.Equal(layout.TrailerOffset, cursor);
        Assert.Equal(data.Length, layout.TrailerOffset + layout.TrailerLength);
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void MetadataDecompressesToTheSymbolTable(string name)
    {
        var data = SampleSaves.Bytes(name);
        var layout = SaveLayout.Read(data);

        var text = layout.Metadata.GetPayloadText();

        Assert.StartsWith("//Globalvariables", text, StringComparison.Ordinal);
        Assert.Contains("//EndSyncPoints", text, StringComparison.Ordinal);
        output.WriteLine($"{name}: metadata {layout.Metadata.Length} bytes compressed in " +
                         $"{layout.Metadata.Records.Count} record(s) -> {layout.Metadata.Decompressed.Length} bytes");
    }
}

public class RoundTripTests
{
    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void LoadingAndWritingBackIsByteIdentical(string name)
    {
        var original = SampleSaves.Bytes(name);
        var save = Gta3SaveFile.Parse(original);

        Assert.True(save.SealWasValid);
        Assert.False(save.IsModified);
        Assert.Equal(original, save.ToBytes());
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void RevertUndoesEdits(string name)
    {
        var original = SampleSaves.Bytes(name);
        var save = Gta3SaveFile.Parse(original);

        save.Globals.SetInt("MAIN.ONE_SIXTEENTH", 1234);
        Assert.True(save.IsModified);

        save.Revert();
        Assert.False(save.IsModified);
        Assert.Equal(original, save.ToBytes());
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void EditsSurviveAWriteAndReload(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), $"gta3-edit-{Guid.NewGuid():N}.sav");

        try
        {
            File.WriteAllBytes(path, SampleSaves.Bytes(name));

            var save = Gta3SaveFile.Load(path);
            save.PlayerInfo.SetMoney(1_234_567);
            save.Stats.CriminalsCaught = 321;
            save.SimpleVars.Hour = 7;
            save.Save(createBackup: false);

            var reloaded = Gta3SaveFile.Load(path);

            // A rewritten file must still pass the game's own integrity check, otherwise the slot
            // shows up as empty in the load menu rather than failing loudly.
            Assert.True(reloaded.SealWasValid);
            Assert.Equal(1_234_567, reloaded.PlayerInfo.Money);
            Assert.Equal(321, reloaded.Stats.CriminalsCaught);
            Assert.Equal(7, reloaded.SimpleVars.Hour);
        }
        finally
        {
            File.Delete(path);
        }
    }
    [Fact]
    public void OverwritingKeepsTheOriginalAsBackupAndLeavesNoTempFile()
    {
        var name = SampleSaves.All[0];
        var path = Path.Combine(Path.GetTempPath(), $"gta3-replace-{Guid.NewGuid():N}.sav");
        var original = SampleSaves.Bytes(name);

        try
        {
            File.WriteAllBytes(path, original);

            var save = Gta3SaveFile.Load(path);
            save.PlayerInfo.SetMoney(42);
            save.Save(createBackup: true);
            save.PlayerInfo.SetMoney(43);
            save.Save(createBackup: true);

            Assert.False(File.Exists(path + ".tmp"));
            Assert.Equal(original, File.ReadAllBytes(path + ".bak"));
            Assert.Equal(43, Gta3SaveFile.Load(path).PlayerInfo.Money);
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".bak");
        }
    }
}

public class ScriptSymbolTableTests(ITestOutputHelper output)
{
    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void TheSymbolTableParses(string name)
    {
        var save = SampleSaves.Load(name);
        var symbols = save.Symbols;

        Assert.NotEmpty(symbols.Globals);
        Assert.NotEmpty(symbols.ScriptNames);
        Assert.NotEmpty(symbols.Missions);

        output.WriteLine($"{name}: {symbols.Globals.Count} globals in {symbols.Scopes.Count()} scopes, " +
                         $"{symbols.ScriptNames.Count} script names, {symbols.Missions.Count} missions, " +
                         $"{symbols.Subscripts.Count} subscripts, max index {symbols.MaxIndex}");
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void TheGlobalBaseIsAnchoredByTheCompilerConstants(string name)
    {
        var save = SampleSaves.Load(name);

        Assert.Equal(0.0625f, save.Globals.GetFloat("MAIN.ONE_SIXTEENTH"));
        Assert.Equal(0.03125f, save.Globals.GetFloat("MAIN.ONE_THIRTYSECOND"));
        Assert.Equal(0.015625f, save.Globals.GetFloat("MAIN.ONE_SIXTYFOURTH"));

        var scripts = save.Layout[SaveBlockKind.Scripts];
        Assert.Equal(scripts.Offset + 20, save.Globals.BaseOffset);
    }

    [Theory]
    [MemberData(nameof(SampleSaves.AllNames), MemberType = typeof(SampleSaves))]
    public void GlobalIndicesFitInsideTheDeclaredVariableSpace(string name)
    {
        var save = SampleSaves.Load(name);
        var expected = save.Symbols.MaxIndex + 4 + GlobalVariableSpace.OpaqueHeaderLength;

        Assert.Equal(save.Globals.Size, expected);
    }

    [Fact]
    public void TheBackupAlwaysHoldsThePristineOriginalNoMatterHowOftenTheSaveIsEdited()
    {
        var folder = Directory.CreateTempSubdirectory("gta3-backup-test");
        try
        {
            var path = Path.Combine(folder.FullName, "GTA3sf1.sav");
            var pristine = SampleSaves.Bytes("GTA3sf1.sav");
            File.WriteAllBytes(path, pristine);

            var first = Gta3SaveFile.Load(path);
            first.PlayerInfo.SetMoney(111_111);
            first.Save();

            var backup = path + ".bak";
            Assert.True(File.Exists(backup));
            Assert.Equal(pristine, File.ReadAllBytes(backup));

            // Editing and testing repeatedly must not let a later write become "the backup".
            var second = Gta3SaveFile.Load(path);
            second.PlayerInfo.SetMoney(222_222);
            second.Save();

            Assert.Equal(pristine, File.ReadAllBytes(backup));
            Assert.Equal(222_222, Gta3SaveFile.Load(path).PlayerInfo.Money);

            // Restoring the backup has to give back a save the game would accept.
            File.Copy(backup, path, overwrite: true);
            var restored = Gta3SaveFile.Load(path);
            Assert.Equal(pristine, restored.Data);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public void SavingLeavesNoTemporaryFilesBehind()
    {
        var folder = Directory.CreateTempSubdirectory("gta3-temp-test");
        try
        {
            var path = Path.Combine(folder.FullName, "GTA3sf1.sav");
            File.WriteAllBytes(path, SampleSaves.Bytes("GTA3sf1.sav"));

            var save = Gta3SaveFile.Load(path);
            save.PlayerInfo.SetMoney(999);
            save.Save();

            Assert.Empty(Directory.GetFiles(folder.FullName, "*.tmp"));
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }
}

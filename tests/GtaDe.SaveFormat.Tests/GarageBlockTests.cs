using Xunit;

namespace GtaDe.SaveFormat.Tests;

/// <summary>
/// The Garages block layout was recovered by inspection, so these tests pin it to facts that
/// could only hold if the interpretation is right: the recovered garage types and world
/// positions have to land on GTA III's real garages.
/// </summary>
public class GarageBlockTests
{
    [Theory]
    [InlineData("GTA3sf1.sav")]
    [InlineData("GTA3sf2.sav")]
    [InlineData("GTA3sf9.sav")]
    public void TheBlockIsTheSizeTheReaderExpects(string sample)
    {
        var save = SampleSaves.Load(sample);

        Assert.True(save.Garages.IsRecognised);
        Assert.Equal(27, save.Garages.ActiveGarageCount);
    }

    [Theory]
    [InlineData("GTA3sf1.sav")]
    [InlineData("GTA3sf2.sav")]
    [InlineData("GTA3sf9.sav")]
    public void UsedGarageSlotsStopExactlyAtTheActiveCount(string sample)
    {
        var save = SampleSaves.Load(sample);
        var garages = save.Garages.Garages;

        Assert.Equal(GarageBlock.GarageCount, garages.Count);
        Assert.All(garages.Take(save.Garages.ActiveGarageCount), g => Assert.False(g.IsEmpty));
        Assert.All(garages.Skip(save.Garages.ActiveGarageCount), g => Assert.True(g.IsEmpty));
    }

    [Fact]
    public void TheRecoveredGaragesAreTheOnesGtaIiiActuallyHas()
    {
        var garages = SampleSaves.Load("GTA3sf1.sav").Garages.Garages
            .Where(g => !g.IsEmpty)
            .ToList();

        // GTA III has exactly three Pay 'n' Sprays, three bomb shops and three safehouses.
        Assert.Equal(3, garages.Count(g => g.Type == GarageType.Respray));
        Assert.Equal(1, garages.Count(g => g.Type == GarageType.BombShop1));
        Assert.Equal(1, garages.Count(g => g.Type == GarageType.BombShop2));
        Assert.Equal(1, garages.Count(g => g.Type == GarageType.BombShop3));
        Assert.Single(garages, g => g.Type == GarageType.HideoutOne);
        Assert.Single(garages, g => g.Type == GarageType.HideoutTwo);
        Assert.Single(garages, g => g.Type == GarageType.HideoutThree);
        Assert.Single(garages, g => g.Type == GarageType.Crusher);

        // One safehouse per island, and the crusher is in Portland.
        Assert.Equal("Portland", garages.Single(g => g.Type == GarageType.HideoutOne).Island);
        Assert.Equal("Staunton Island", garages.Single(g => g.Type == GarageType.HideoutTwo).Island);
        Assert.Equal("Shoreside Vale", garages.Single(g => g.Type == GarageType.HideoutThree).Island);
        Assert.Equal("Portland", garages.Single(g => g.Type == GarageType.Crusher).Island);

        // Import/export: Portland's list garage and Shoreside's crane garage.
        Assert.Equal("Portland", garages.Single(g => g.Type == GarageType.CollectCars1).Island);
        Assert.Equal("Shoreside Vale", garages.Single(g => g.Type == GarageType.CollectCars2).Island);
    }

    [Theory]
    [InlineData("GTA3sf1.sav")]
    [InlineData("GTA3sf2.sav")]
    [InlineData("GTA3sf9.sav")]
    public void EveryGarageHasSaneWorldBounds(string sample)
    {
        foreach (var garage in SampleSaves.Load(sample).Garages.Garages.Where(g => !g.IsEmpty))
        {
            Assert.InRange(garage.X1, -2100f, 2100f);
            Assert.InRange(garage.X2, -2100f, 2100f);
            Assert.InRange(garage.Y1, -2100f, 2100f);
            Assert.InRange(garage.Y2, -2100f, 2100f);
            Assert.True(garage.X1 <= garage.X2, $"garage {garage.Index} has inverted X bounds");
            Assert.True(garage.Y1 <= garage.Y2, $"garage {garage.Index} has inverted Y bounds");
        }
    }

    [Fact]
    public void StoredCarsSitInsideTheirSafehouse()
    {
        var save = SampleSaves.Load("GTA3sf1.sav");
        var parked = save.Garages.StoredCars.Where(c => !c.IsEmpty).ToList();
        var portland = save.Garages.Garages.Single(g => g.Type == GarageType.HideoutOne);

        Assert.NotEmpty(parked);
        Assert.All(parked, car =>
        {
            Assert.InRange(car.X, -2100f, 2100f);
            Assert.InRange(car.Y, -2100f, 2100f);
        });

        // The only car in this save is in the Portland safehouse, so it has to be inside it.
        var first = parked[0];
        Assert.InRange(first.X, portland.X1 - 10f, portland.X2 + 10f);
        Assert.InRange(first.Y, portland.Y1 - 10f, portland.Y2 + 10f);
    }

    [Fact]
    public void FreeBombsAndRespraysToggleWithoutDisturbingAnythingElse()
    {
        var save = SampleSaves.Load("GTA3sf1.sav");
        var before = (byte[])save.Data.Clone();

        Assert.False(save.Garages.BombsAreFree);
        Assert.False(save.Garages.RespraysAreFree);

        save.Garages.BombsAreFree = true;
        save.Garages.RespraysAreFree = true;

        Assert.True(save.Garages.BombsAreFree);
        Assert.True(save.Garages.RespraysAreFree);

        var changed = before.Select((b, i) => (b, i))
            .Where(p => p.b != save.Data[p.i])
            .Select(p => p.i)
            .ToList();

        var block = save.Layout[SaveBlockKind.Garages];
        Assert.Equal([block.DataOffset + 4, block.DataOffset + 8], changed);

        save.Garages.BombsAreFree = false;
        save.Garages.RespraysAreFree = false;

        Assert.Equal(before, save.Data);
    }

    [Fact]
    public void ClearingAStoredCarOnlyTouchesThatSlot()
    {
        var save = SampleSaves.Load("GTA3sf1.sav");
        var before = (byte[])save.Data.Clone();
        var occupied = save.Garages.StoredCars.First(c => !c.IsEmpty);

        save.Garages.ClearStoredCar(occupied.Slot);

        Assert.True(save.Garages.StoredCars[occupied.Slot].IsEmpty);

        var changed = before.Select((b, i) => (b, i))
            .Where(p => p.b != save.Data[p.i])
            .Select(p => p.i)
            .ToList();

        Assert.All(changed, i => Assert.InRange(i, occupied.Offset, occupied.Offset + 43));
    }

    [Fact]
    public void ReadingTheBlockNeverModifiesTheSave()
    {
        foreach (var sample in SampleSaves.All)
        {
            var save = SampleSaves.Load(sample);
            var before = (byte[])save.Data.Clone();

            _ = save.Garages.Garages;
            _ = save.Garages.StoredCars;
            _ = save.Garages.CarsCollected;
            _ = save.Garages.BankVansCollected;
            _ = save.Garages.PoliceCarsCollected;

            Assert.Equal(before, save.Data);
        }
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GtaDe.Definitions;
using GtaDe.SaveFormat;

namespace GtaDe.App.ViewModels;

/// <summary>A car parked in a safehouse garage.</summary>
public sealed class ParkedCarViewModel(StoredCar car, GarageSlot? garage)
{
    public int Slot { get; } = car.Slot;

    public string Vehicle { get; } = Gta3Vehicles.Name(car.ModelIndex);

    public string Location { get; } = garage is { } g ? $"{g.Type.ToDisplayName()} — {g.Island}" : "Unknown garage";

    public string Position { get; } = $"{car.X:F1}, {car.Y:F1}, {car.Z:F1}";
}

/// <summary>One import/export garage and the delivery state of each vehicle it wants.</summary>
public sealed partial class GarageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private double _progress;

    public GarageViewModel(ImportExportGarage garage, IEnumerable<ToggleItem> slots)
    {
        Garage = garage;
        Slots = new ObservableCollection<ToggleItem>(slots);
        Update();
    }

    public ImportExportGarage Garage { get; }

    public string Name => Garage.Name;

    public string Location => Garage.Island.ToDisplayName();

    public ObservableCollection<ToggleItem> Slots { get; }

    public void Update()
    {
        var done = Slots.Count(s => s.IsComplete);
        Summary = $"{done} / {Slots.Count} delivered";
        Progress = Slots.Count == 0 ? 0 : done * 100.0 / Slots.Count;
    }
}

public sealed partial class GaragesPageViewModel(MainWindowViewModel shell) : PageViewModel(shell)
{
    public override string Title => "Garages";

    public override string Description =>
        "Safehouse parking, the free bomb and respray flags, and the vehicles each crane garage still wants.";

    public override string Icon => "M5 11 6.5 6.5h11L19 11m-1.5 5a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3m-11 0a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3M18.9 6A1.5 1.5 0 0 0 17.5 5h-11A1.5 1.5 0 0 0 5.1 6L3 12v8a1 1 0 0 0 1 1h1a1 1 0 0 0 1-1v-1h12v1a1 1 0 0 0 1 1h1a1 1 0 0 0 1-1v-8l-2.1-6Z";

    public ObservableCollection<GarageViewModel> Garages { get; } = [];

    /// <summary>Cars currently parked in the safehouse garages.</summary>
    public ObservableCollection<ParkedCarViewModel> ParkedCars { get; } = [];

    [ObservableProperty]
    private bool _bombsAreFree;

    [ObservableProperty]
    private bool _respraysAreFree;

    [ObservableProperty]
    private string _parkedCarsSummary = string.Empty;

    [ObservableProperty]
    private ParkedCarViewModel? _selectedParkedCar;

    partial void OnBombsAreFreeChanged(bool value)
    {
        if (Shell.Save is { } save && save.Garages.BombsAreFree != value)
        {
            Edit(() => save.Garages.BombsAreFree = value);
        }
    }

    partial void OnRespraysAreFreeChanged(bool value)
    {
        if (Shell.Save is { } save && save.Garages.RespraysAreFree != value)
        {
            Edit(() => save.Garages.RespraysAreFree = value);
        }
    }

    [RelayCommand]
    private void ClearSelectedCar()
    {
        if (SelectedParkedCar is not { } car || Shell.Save is not { } save)
        {
            return;
        }

        Edit(() => save.Garages.ClearStoredCar(car.Slot));
        RefreshParkedCars(save);
    }

    private void RefreshParkedCars(SaveFormat.Gta3SaveFile save)
    {
        ParkedCars.Clear();

        if (!save.Garages.IsRecognised)
        {
            ParkedCarsSummary = "This save's garage block is an unexpected size, so it is left untouched.";
            return;
        }

        foreach (var car in save.Garages.StoredCars.Where(c => !c.IsEmpty))
        {
            ParkedCars.Add(new ParkedCarViewModel(car, save.Garages.GarageFor(car)));
        }

        ParkedCarsSummary = ParkedCars.Count == 0
            ? "No cars are parked in any safehouse."
            : $"{ParkedCars.Count} of {SaveFormat.GarageBlock.StoredCarCount} parking spaces in use.";
    }

    public override void Refresh()
    {
        if (Shell.Save is not { } save)
        {
            Garages.Clear();
            ParkedCars.Clear();
            ParkedCarsSummary = string.Empty;
            return;
        }

        BombsAreFree = save.Garages.BombsAreFree;
        RespraysAreFree = save.Garages.RespraysAreFree;
        RefreshParkedCars(save);

        if (Garages.Count == 0)
        {
            foreach (var garage in Gta3Catalogue.ImportExportGarages)
            {
                var slots = garage.Vehicles.Select(vehicle =>
                {
                    var name = $"{garage.SlotPrefix}{vehicle.Slot}";
                    return new ToggleItem(
                        vehicle.Name,
                        $"Slot {vehicle.Slot}",
                        name,
                        save.Globals.GetFlag(name),
                        value => Edit(() =>
                        {
                            Shell.Save!.Globals.SetFlag(name, value);
                            SynchroniseCount(garage);
                        }));
                });

                Garages.Add(new GarageViewModel(garage, slots));
            }
        }
        else
        {
            foreach (var (view, garage) in Garages.Zip(Gta3Catalogue.ImportExportGarages))
            {
                foreach (var (slot, vehicle) in view.Slots.Zip(garage.Vehicles))
                {
                    slot.SetWithoutApplying(save.Globals.GetFlag($"{garage.SlotPrefix}{vehicle.Slot}"));
                }

                view.Update();
            }
        }
    }

    /// <summary>
    /// Keeps the garage's own "slots filled" counter in step with the individual slots, the same
    /// way the script does when a car is delivered.
    /// </summary>
    private void SynchroniseCount(ImportExportGarage garage)
    {
        var globals = Shell.Save!.Globals;
        if (!globals.Has(garage.FilledCountVariable))
        {
            return;
        }

        var filled = garage.Vehicles.Count(v => globals.GetFlag($"{garage.SlotPrefix}{v.Slot}"));
        globals.SetInt(garage.FilledCountVariable, filled);
    }

    [RelayCommand]
    private void DeliverAll() => Edit(() =>
    {
        foreach (var garage in Gta3Catalogue.ImportExportGarages)
        {
            foreach (var vehicle in garage.Vehicles)
            {
                Shell.Save!.Globals.SetFlag($"{garage.SlotPrefix}{vehicle.Slot}", true);
            }

            SynchroniseCount(garage);
        }
    });
}

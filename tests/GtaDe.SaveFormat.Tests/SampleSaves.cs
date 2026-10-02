using System.Reflection;

namespace GtaDe.SaveFormat.Tests;

/// <summary>Real save files produced by the retail game, used as parser fixtures.</summary>
public static class SampleSaves
{
    public static string Directory { get; } = System.IO.Path.Combine(
        System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "Samples");

    public static readonly string[] All =
    [
        "GTA3sf1.sav",
        "GTA3sf2.sav",
        "GTA3sf9.sav",
        "GTA3sf1-original.sav",
        "GTA3sf1-patched.sav",
    ];

    public static string Path(string name) => System.IO.Path.Combine(Directory, name);

    public static byte[] Bytes(string name) => File.ReadAllBytes(Path(name));

    public static Gta3SaveFile Load(string name) => Gta3SaveFile.Load(Path(name));

    public static TheoryData<string> AllNames
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var name in All)
            {
                data.Add(name);
            }

            return data;
        }
    }
}

# GTA Trilogy: Definitive Edition Save Editor

A native Windows save editor for *GTA III*, *Vice City* and *San Andreas – The Definitive Edition*
(the Unreal Engine remasters), built on .NET 9 and Avalonia. No Electron, no browser engine: it
starts instantly and draws its own light or dark themed UI.

## Requirements

| To… | You need |
|---|---|
| **Run the editor** | Windows 10 or 11, 64-bit, and the [.NET 9 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/9.0). Nothing else: every library is bundled inside the exe. |
| **Build from source** | The [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (9.0.100 or newer). NuGet restores every package on first build. Visual Studio 2022 17.12+, Rider or VS Code with C# Dev Kit are optional. |

## Running it

```
dist\
├─ GtaDeSaveEditor.exe     single file: every managed and native library is bundled inside
└─ config\
   └─ settings.json        theme, recent files, selected game, last save per game (created on first run)
```

It needs the [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0).
Settings are portable: they live in `config\` next to the exe. If that folder isn't writable
(for example under `Program Files`), the editor falls back to `%AppData%\GtaDeSaveEditor`.
Native graphics libraries (Skia, HarfBuzz, ANGLE) are unpacked once to `%TEMP%\.net\` on first launch.

Open a save from:

```
Documents\Rockstar Games\GTA III Definitive Edition\Profiles\<profile id>\GTA3sf1.sav
Documents\Rockstar Games\GTA Vice City Definitive Edition\Profiles\<profile id>\GTAVCsf1.sav
Documents\Rockstar Games\GTA San Andreas Definitive Edition\Profiles\<profile id>\GTASAsf1.sav
```

The editor finds the folder for the selected game on its own when you click **Open save…**, and
recognises which game a file belongs to from its contents.

Only one copy of the editor runs at a time: launching the exe again while it is open does nothing.

The editor remembers the last save you opened for each game. It reopens that save when it starts
and when you switch games with the profile switcher, and asks first if switching would throw away
unsaved edits. Turn this off with **Settings → Reopen the last save for each game**.

The pages adapt to the window. Cards sit in aligned rows of up to two columns (three for story
strands and side-activity toggles), and cards in the same row share one height. Long lists, such as
"Every other stat", span the full width at the bottom of the page with their fields split into columns.

> **Close the game first.** GTA III DE rewrites its save files while running, and it keeps the
> whole save in memory, so it would overwrite your edits when it next autosaves. The editor shows
> a warning banner if a file changes on disk underneath it.

## What you can edit — GTA III

| Page | What it covers |
|---|---|
| **Dashboard** | Progress at a glance, and an honest list of what this save format does *not* store |
| **Player** | Money, hidden-package bonuses, the permanent perks (infinite sprint, fast reload, free health care, get-out-of-jail-free), wanted-level settings |
| **Story missions** | Every story mission, grouped by strand, with the completion counter kept in step |
| **Side missions** | Vigilante, Paramedic, Firefighter, Taxi, Rampages and Unique Stunt Jumps, each with its own subpage and reward bookkeeping |
| **Collectibles** | Hidden packages and the bonuses they unlock |
| **Stats** | Everything the in-game stats screen shows |
| **World** | Clock, weather, island unlocks and other world state |
| **Garages** | Safehouse parking, free bombs/resprays, and import/export delivery slots |
| **Script globals** | Every one of the save's ~4,400 named script variables, searchable (advanced) |
| **Research tools** | Diff two saves with each change named, inspect any offset, export the symbol table (advanced) |

### Game profiles

The switcher at the top of the navigation rail picks between **GTA III**, **Vice City** and
**San Andreas**. Each profile brings its own banner, logo, round icon, window icon and accent
colour (amber, pink, green) in both the light and dark themes. Opening a save switches to the
profile of the game that wrote it.
Artwork lives in `src/GtaDe.App/Assets/Games/<Game>/`
(Vice City and San Andreas art from [SteamGridDB](https://www.steamgriddb.com/)).

## What you can edit — Vice City

| Page | What it covers |
|---|---|
| **Dashboard** | Completion, money, missions, packages, rampages, jumps, assets and safehouses at a glance |
| **Player** | Money, current health and armour, maximum health and armour, all ten weapon slots with ammo, hidden packages, perks (infinite sprint, fast reload, fireproof, free health care, get-out-of-jail-free) |
| **Story missions** | Every contact and asset strand (Colonel, Ken, Avery, Kent Paul, Lance, Umberto, Cortez, Phil, Love Fist, Auntie Poulet, Mitch Baker, Print Works, Malibu, Film Studio and so on) |
| **Side activities** | Subpages for Rampages, Unique jumps, Vehicle jobs, Races & stadium, Challenges (chopper checkpoints, RC, trials, range) and Store robberies, with totals and rewards kept in step |
| **Empire** | Business-asset ownership and safehouse purchases |
| **Stats** | Every stats-menu figure, kills by type, and the vehicle-job script counters |
| **World** | Clock, weather and weather lock |

Vice City DE *does* store the player ped, so health, armour and weapons there are real edits.
Ownership flags change what the mission script believes; for-sale pickups may stay visible until
the script next refreshes them.

## What you can edit — San Andreas

| Page | What it covers |
|---|---|
| **Dashboard** | Completion, money, story missions, gang tags, snapshots, horseshoes, oysters and safehouses at a glance |
| **Player** | Money, current and maximum health and armour, all thirteen weapon slots with ammo, perks (infinite sprint, fireproof, free health care, get-out-of-jail-free, drive-by) |
| **Story missions** | Every story strand (Sweet, Ryder, Big Smoke, OG Loc, C.R.A.S.H., The Truth, Woozie, Zero, Wang Cars, Toreno, the casino and heist strands, Madd Dogg, Grove Street, Riot and the rest) driven by their mission counters, plus the Catalina robberies |
| **Side activities** | Subpages for Vehicle jobs, Schools (gold medals), Races and challenges, Gyms and misc, and Wardrobe and shops, with job levels and counters |
| **Collectibles** | Gang tags, snapshots, horseshoes and oysters, with the totals and reward flags kept in step |
| **Safehouses** | Every buyable house and hotel suite, grouped by region |
| **Stats** | Body (fat, muscle, stamina, lung capacity), vehicle and weapon skills, respect, girlfriend progress, and every other stats-menu figure |
| **World** | Clock, weather and weather lock, and the in-game date |

San Andreas DE stores the player ped too, so health, armour and weapons are real edits.

### Achievements

None of the three games stores achievements in its save. Every block in a DE save is one of the
classic game blocks, and none of them holds achievement or unlock data. Achievements belong to
Steam or the Rockstar Games Launcher, which award them when the game reports the event (for
example, collecting the last package). A save edit cannot unlock an achievement directly. What it
*can* do is set one up: put a counter one short of its target (99 of 100 packages, or a job one
level below its cap), save, then finish the last step in-game so the game awards it itself.

### What a GTA III DE save does *not* contain

Health, armour and the weapon loadout are **not** stored. The mission script rebuilds them when the
save loads. The editor says so on the Player page rather than offering fields the game ignores.

`Stats.ProgressMade` is deliberately left alone: it is not a simple sum of the things the editor
changes, so writing a guess would produce a save the game disagrees with.

## How it avoids corrupting saves

- The save is held as **one mutable byte array and edited in place**. Regions the editor does not
  understand are copied through untouched, so an unmodified save round-trips byte-for-byte.
- The file's integrity seal (`NOT(MD5(zeros(24) || file[24:]))`) is recomputed on write. A save with
  a bad seal shows up as an **empty slot** in the load menu, so this has to be exact.
- The script-global base offset is verified against three compiler constants
  (`ONE_SIXTEENTH`, `ONE_THIRTYSECOND`, `ONE_SIXTYFOURTH`) on every load, and loading **fails** if
  they don't line up. A widely circulated offset table for this format is wrong by 8 bytes; this
  check is what catches that.
- A **`.bak` copy of the original** is written before the first change, and is never overwritten,
  so editing and re-testing repeatedly cannot destroy the pristine file.
- Writes go to a `.tmp` file first and are then moved into place.

## Validating an edit in-game

1. Quit GTA III DE completely.
2. Open the save, change something obvious — money is easiest — and click **Save**.
3. Start the game and load that slot.
4. Check the value, then quit again.

If anything looks wrong, restore the backup: delete `GTA3sf1.sav` and rename `GTA3sf1.sav.bak`
back to `GTA3sf1.sav`.

Two things are worth confirming this way, because they are the only parts that could not be proven
from the save files alone:

- The **hidden-package bonus thresholds** (10 / 30 / 50 / 70 packages) used by the Collectibles page.
- That a save still loads cleanly after an edit.

## Building from source

```
dotnet build
dotnet test
dotnet publish src\GtaDe.App -p:PublishProfile=win-x64
```

Run these from the folder that holds `GtaDeSaveEditor.sln`. Every path in the build is relative to
the solution, so the source tree can live anywhere. The publish profile
(`src\GtaDe.App\Properties\PublishProfiles\win-x64.pubxml`) produces a single
`dist\GtaDeSaveEditor.exe` with embedded debug symbols, framework-dependent on the .NET 9 Desktop Runtime.

`package.ps1` publishes and writes both release archives to `release\`: the editor zip and a clean
source zip with no `bin\`, `obj\`, `dist\` or `config\` folders.

### Toolchain and libraries

| Component | Version | Used by | License |
|---|---|---|---|
| .NET SDK / C# compiler (Roslyn) | 9.0, C# `latest` | everything | MIT |
| [Avalonia](https://avaloniaui.net/) (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`) | 11.3.22 | UI | MIT |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | 8.4.0 | view models (source generators) | MIT |
| SkiaSharp, HarfBuzzSharp, ANGLE (via Avalonia) | bundled | rendering | MIT / BSD |
| `Avalonia.Diagnostics` | 11.3.22 | Debug builds only | MIT |
| xUnit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk | 2.9.2, 2.8.2, 17.x | tests | Apache-2.0 / MIT |
| `Avalonia.Headless.XUnit`, `Avalonia.Skia` | 11.3.22 | headless UI tests | MIT |

`GtaDe.SaveFormat` and `GtaDe.Definitions` have no package dependencies.

### Projects

| Project | Role |
|---|---|
| `src/GtaDe.SaveFormat` | Container parsing, the integrity seal, typed blocks, the script-global space, and the save differ |
| `src/GtaDe.Definitions` | Mission/collectible/vehicle catalogue and the consistency rules between them |
| `src/GtaDe.App` | Avalonia desktop UI |
| `tests/GtaDe.SaveFormat.Tests` | Format tests, run against real retail saves |
| `tests/GtaDe.App.Tests` | Headless UI tests that render every page in both themes and **fail on any binding or missing-resource warning** |

The strongest test in the suite is `SynchronisingAGameWrittenSaveChangesNothing`: applying every
consistency rule to an untouched, game-written save must produce zero byte changes. It caught every
wrong assumption made while working the format out.

## Credits

The editor's code is original. Knowing where the classic game data sits inside the Definitive Edition
saves (player info, stats, simple variables, garages, script globals) was possible thanks to years of
public community research, especially the [GTAMods wiki](https://gtamods.com/) and the stat and ID
names collected by the GTA fan reverse-engineering community. The Definitive Edition container,
compression and integrity seal were worked out from retail saves for this project.

## License and disclaimer

The editor's source code is released under the [MIT License](LICENSE).

This is an unofficial fan tool, not affiliated with or endorsed by Rockstar Games or Take-Two
Interactive. *Grand Theft Auto* and the game names, logos and artwork are trademarks of their
owners. The game artwork in `src/GtaDe.App/Assets/Games` is included only to identify each game
profile. Always keep a backup of your saves; the editor writes a `.bak` copy by default.

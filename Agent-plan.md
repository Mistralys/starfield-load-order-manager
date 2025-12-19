# The problem to solve

The game Starfield uses a line-based text file called `Plugins.txt` in which each line contains the file name of a mod to load when starting the game. This is typically referred to as the "Load Order". Once a save game has been created, it is crucial that the order of existing lines in the file is not modified: New lines can be added, but the existing ones must stay in the same order.

Changing the load order in the middle of a save game can cause all manner of issues. Internal object references depend on the load order. For example, this means that if you are wearing a spacesuit that is added by a mod and that mod's position in the lkoad order changes, you will lose that spacesuit.

The problem is that the game itself, as well as mod manager tools, tend to change the order around - hence the need for a small application to observe and fix these changes.

## The application's working principle

- When the user is satisfied with the `Plugins.txt` file, make a copy of it.
- The copied file will be used as reference for the correct order of mods.
- When you run the tool, it will load the reference file and sort all entries according to the reference file's order.
- Any new mod files are appended at the end.

## The technology

The application will be a WPF .NET 9 application.

## File name case handling

My mod manager tool lowercases all mod file names. While Starfield does not mind this, it will restore the correct file name case when it loads the `Plugins.txt` file.

To guarantee stable `Plugins.txt` contents, the mod file names should always use the original file name case of the mods. This can be done by cross-referening the mod names with all `.esp` and `.esm` files as can be found in the game's installation folder under `Data` (where all mods are stored). This way, even if the mod manager lowercases mod names, they will always be restored to the original file name case.

## Configuration settings

The application needs two configuration settings:

- Local appdata Starfield folder location (where `Plugins.txt` is located)
- Starfield game installation folder (where the `Data` folder with mods is located)

These settings should be configured first thing when running the application if they are not set, or if any of the folders cannot be found on disk. The folders cannot always be easily auto-discovered, as installations can vary a lot between gaming platforms (Steam, GoG, etc.). However, to help with configuration, typical locations can be checked and pre-filled if found.

## Example `Plugins.txt`

This is from my current Starfield game:

```
*sfta01.esm
*sfbgs00c.esm
*sfbgs00b.esm
*kgcdoom.esm
*sfbgs019.esm
*sfbgs021.esm
*StarfieldCommunityPatch.esm
*AmazonCrew.esm
*ShipBuilderCategories.esm
*BetterShipPartFlips.esm
*BetterShipPartSnaps.esm
*Better_Living.esm
*Richer Merchants.esm
*xatmosPerkUpVendors.esp
*BuySwimsuits.esm
*SP2_CapitalPlanetDesktopGlobes.esm
*fixgraydockingcolors.esm
*DayLengthMessage.esm
*Eit_Clothiers_Z.esm
*Easy Digipick.esm
*Eli_RenamedSnowglobes.esm
*Faster Waiting and Sleeping.esm
*Nanosuit_f_new.esm
*OutpostFishTank.esm
*Fragile.esm
*GagarinNewDawn.esm
*galacticmeshfixes.esm
*Pegasus_BotanicsUtilities.esm
*Grogu.esm
*PXC_HelloKittySanitationRobot.esm
*IncreasedBounties.esm
*HealthIncreaseAfterLevel100.esm
*KZ_AOS.esm
*KZ_Mantis.esm
*KZ_TCA.esm
*Minimum Enemy Level.esm
*KZ_TCR.esm
*MQ204 - Choose your companionV2.esm
*miss_o_intwalls_03a.esm
*MarkedLandmarkBooks.esm
*More Buildable Mission Boards.esm
*MoreOwnableShips.esm
*Inquisitor_MoreRewarding_MainQuest.esm
*OmbreHairColours.esm
*OutpostShipbuilderUnlocked.esm
*OS_OutpostShower.esm
*Outpost Vending Machines.esm
*OutpostBuildArea.esm
*polyamory.esm
*Starfield Extended - Armor & Clothing Crafting.esm
*SeizureOfShips.esm
*Ship Power Fix.esm
*Sit To Add Ship.esm
*SmarterSpacesuitAutohide.esm
*TrackersFix.esm
*TIG_MagazinesVol2.esm
*UnlimitedMannequins.esm
*VariousCrew.esm
*WSEP_LaserCutter_Fallout.esm
*WSEP_LaserCutter_Constellation.esm
*WSEP_WASP.esm
*RyujinXCombatech.esm
*MoreImmersiveLandingsTakeOffs.esm
*vivs_furnishyourfleet.esm
*arc_lights3.esm
*arc_signs.esm
*QOG-JunkRecycler.esm
*x2357factionweaponskinsryujin.esm
*x2357primeweaponskinsvaruunweapons.esm
*x2357primeweaponskinsstarlash.esm
*forgottenfrontierspoi.esm
*ClothingPack_Standalone_Z.esm
*KZ_BountyH_Replacer.esm
*KZ_Ecliptic_Replacer.esm
*KZ_Merc_Replacer.esm
*HeartOfCydonia.esm
*falklandsystems.esm
*kinggathcreations_legendary.esm
*GB_SBCX_Falkland.esm
*GB_SBCX_ShatteredSpace.esm
*GB_ShipBuilderConfigurator.esm
*Patch-SPE-TNsAIO.esm
*SP2_ImmersiveLandingRamps.esm
*SP2_LandingBayCargoAccess.esm
*stroudpremiumedition.esm
*TNShipModsAllInOne.esm
*TN_AIO_FalklandSystems.esm
*SP2_CraftablePrimersAndDiodes.esm
*DWN_SimpleDigipickCrafting.esm
*More Brewable Teas and Coffees.esm
*VisibleCompanionAffinity.esm
*Better Crowd Citizens.esm
*BetterNPCs.esm
*ChameleonImproved.esm
*NASALandmark.esm
```

Note that the lines are prepended with `*`: This means that the plugin is enabled. This must always be present for all lines. There is no use case where a line does not have a `*`, as this is handled by the mod manager. Only enabled mods get written into the file.


# 🌟 Complete Agent Guide: Starfield Load Order Keeper app

This project uses **C\#**, **WPF**, and **.NET 9** with the **CommunityToolkit.Mvvm** for the MVVM implementation.

### Phase 1: Configuration and File Utilities (Foundation)

First, we set up the configuration system that manages the critical file paths.

#### Task 1.1: Data Models

Create the following two classes in a `Models` folder.

1.  **`AppConfigModel.cs`**

    ```csharp
    namespace LoadOrderKeeper.Models
    {
        public class AppConfigModel
        {
            // Location of Plugins.txt
            public string StarfieldAppDataPath { get; set; } = string.Empty;
            // Location of the game root, containing the Data folder
            public string StarfieldGamePath { get; set; } = string.Empty;

            public bool IsValid()
            {
                return !string.IsNullOrWhiteSpace(StarfieldAppDataPath) &&
                       !string.IsNullOrWhiteSpace(StarfieldGamePath) &&
                       Directory.Exists(StarfieldAppDataPath) &&
                       Directory.Exists(StarfieldGamePath) &&
                       Directory.Exists(Path.Combine(StarfieldGamePath, "Data"));
            }

            public string GetPluginsFilePath() => Path.Combine(StarfieldAppDataPath, "Plugins.txt");
            public string GetReferenceFilePath() => Path.Combine(StarfieldAppDataPath, "Plugins.reference.txt");
        }
    }
    ```

2.  **`ModEntryModel.cs`** (From previous step, ensure it uses `LoadOrderKeeper.Models`)

#### Task 1.2: Settings Service (`SettingsService.cs`)

Create a static service for saving and loading application settings using `System.Text.Json`.

```csharp
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Services
{
    public static class SettingsService
    {
        private static readonly string ConfigPath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                         "LoadOrderKeeper", 
                         "config.json");

        public static async Task<AppConfigModel> LoadSettingsAsync()
        {
            if (!File.Exists(ConfigPath))
            {
                // Return default model if file doesn't exist
                return new AppConfigModel();
            }

            try
            {
                var json = await File.ReadAllTextAsync(ConfigPath);
                return JsonSerializer.Deserialize<AppConfigModel>(json) ?? new AppConfigModel();
            }
            catch
            {
                // Handle corrupted file by returning default
                return new AppConfigModel();
            }
        }

        public static async Task SaveSettingsAsync(AppConfigModel config)
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(ConfigPath, json);
        }

        // Optional helper to check common Steam path (Can be expanded if needed)
        public static string TryGetDefaultSteamPath()
        {
            // Example: C:\Program Files (x86)\Steam\steamapps\common\Starfield
            var steamPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam", "steamapps", "common", "Starfield");
            
            return Directory.Exists(Path.Combine(steamPath, "Data")) ? steamPath : string.Empty;
        }

        // Optional helper for AppData path (Starfield's default location)
        public static string TryGetDefaultAppDataPath()
        {
            // Example: %LOCALAPPDATA%\Starfield
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Starfield");
            
            return Directory.Exists(appDataPath) ? appDataPath : string.Empty;
        }
    }
}
```

### Phase 2: Revised File Service (Core Logic)

The `FileService` must now be updated to accept the `AppConfigModel` to perform case restoration and determine file paths dynamically.

#### Task 2.1: Revised `FileService.cs`

Replace the existing `FileService.cs` content with the following, focusing on the new logic in `GetCaseLookup` and how it's used in `ApplyLoadOrderAsync`.

```csharp
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Services
{
    public static class FileService
    {
        // -------------------------------------------------------------------
        // NEW: Case Restoration Logic
        // -------------------------------------------------------------------
        private static Dictionary<string, string> GetCaseLookup(string gamePath)
        {
            string dataPath = Path.Combine(gamePath, "Data");
            if (!Directory.Exists(dataPath))
            {
                // Cannot perform lookup if the Data folder is missing
                return new Dictionary<string, string>(); 
            }

            // Search for all .esp and .esm files
            var files = Directory.EnumerateFiles(dataPath, "*.esm", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(dataPath, "*.esp", SearchOption.TopDirectoryOnly));

            // Key: Lowercase filename, Value: Original case filename
            return files.ToDictionary(
                p => Path.GetFileName(p).ToLowerInvariant(), 
                p => Path.GetFileName(p)                      
            );
        }

        // -------------------------------------------------------------------
        // File Reading Helpers (Updated to take Model)
        // -------------------------------------------------------------------
        private static async Task<List<ModEntryModel>> ReadFileAsync(string filePath)
        {
            if (!File.Exists(filePath)) return new List<ModEntryModel>();

            var lines = await File.ReadAllLinesAsync(filePath, System.Text.Encoding.UTF8);
            
            return lines
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                .Select(line => new ModEntryModel(line))
                .ToList();
        }

        // -------------------------------------------------------------------
        // Public Actions
        // -------------------------------------------------------------------
        public static bool DoesReferenceFileExist(AppConfigModel config)
        {
            return File.Exists(config.GetReferenceFilePath());
        }

        public static async Task CreateReferenceFileAsync(AppConfigModel config)
        {
            string targetPath = config.GetPluginsFilePath();
            string referencePath = config.GetReferenceFilePath();

            // Check if target exists before copying
            if (!File.Exists(targetPath))
            {
                throw new FileNotFoundException($"Target file not found: {targetPath}");
            }
            
            // Read the raw content (including comments/empty lines) to preserve file structure exactly.
            string content = await File.ReadAllTextAsync(targetPath, System.Text.Encoding.UTF8);
            await File.WriteAllTextAsync(referencePath, content, System.Text.Encoding.UTF8);
        }

        public static async Task ApplyLoadOrderAsync(AppConfigModel config)
        {
            if (!config.IsValid()) 
                throw new InvalidOperationException("Configuration paths are invalid.");

            string targetPath = config.GetPluginsFilePath();
            string referencePath = config.GetReferenceFilePath();
            
            // 1. Generate Lookup Table
            var caseLookup = GetCaseLookup(config.StarfieldGamePath);

            // 2. Read and Filter
            var referenceMods = await ReadFileAsync(referencePath);
            var currentMods = await ReadFileAsync(targetPath);

            // Use a hash set for efficient O(1) checking if a mod exists in the current list.
            var currentModSet = new HashSet<ModEntryModel>(currentMods);

            var finalOrder = new List<string>();
            var newMods = new List<ModEntryModel>();
            
            // Identify new mods (present in currentMods but not in referenceMods)
            foreach (var mod in currentMods)
            {
                if (!referenceMods.Contains(mod))
                {
                    newMods.Add(mod);
                }
            }

            // 3. Reorder (Prioritize Reference Order)
            foreach (var referenceMod in referenceMods)
            {
                if (currentModSet.Contains(referenceMod))
                {
                    // Case Restore: Check the filename against the lookup dictionary
                    string cleanFileName = referenceMod.FileName.ToLowerInvariant();
                    if (caseLookup.TryGetValue(cleanFileName, out string? correctCase))
                    {
                        finalOrder.Add(correctCase);
                    }
                    else
                    {
                        // Use the filename from the reference file if lookup fails (e.g., deleted mod)
                        finalOrder.Add(referenceMod.FileName); 
                    }
                }
            }

            // 4. Append and Case Restore New Mods
            foreach (var newMod in newMods)
            {
                // Case Restore for new mods
                string cleanFileName = newMod.FileName.ToLowerInvariant();
                if (caseLookup.TryGetValue(cleanFileName, out string? correctCase))
                {
                    finalOrder.Add(correctCase);
                }
                else
                {
                    finalOrder.Add(newMod.FileName);
                }
            }

            // 5. Write the final ordered list back to the target file.
            await File.WriteAllLinesAsync(targetPath, finalOrder, System.Text.Encoding.UTF8);
        }
    }
}
```

### Phase 3: View Models (Logic Connectors)

Now, we build the application state and command logic.

#### Task 3.1: Settings View Model (`SettingsViewModel.cs`)

This handles the configuration UI.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using System.Threading.Tasks;

namespace LoadOrderKeeper.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _starfieldAppDataPath = SettingsService.TryGetDefaultAppDataPath();

        [ObservableProperty]
        private string _starfieldGamePath = SettingsService.TryGetDefaultSteamPath();

        public SettingsViewModel(AppConfigModel initialConfig)
        {
            // Initialize with current settings if they exist
            if (!string.IsNullOrWhiteSpace(initialConfig.StarfieldAppDataPath))
                StarfieldAppDataPath = initialConfig.StarfieldAppDataPath;
            
            if (!string.IsNullOrWhiteSpace(initialConfig.StarfieldGamePath))
                StarfieldGamePath = initialConfig.StarfieldGamePath;
        }

        [RelayCommand]
        private void BrowsePath(string pathType)
        {
            // IMPORTANT: The UI Agent must use the WPF OpenFolderDialog 
            // implementation here to let the user select a folder.

            // Placeholder for the agent to implement dialog opening logic:
            // 1. Create and open OpenFolderDialog (or similar)
            // 2. If dialog result is OK, set the corresponding property.
            
            // Example stub:
            // if (pathType == "AppData") StarfieldAppDataPath = dialogResult;
            // else if (pathType == "GamePath") StarfieldGamePath = dialogResult;
        }

        // Method to package the data back into the Model
        public AppConfigModel GetConfig()
        {
            return new AppConfigModel
            {
                StarfieldAppDataPath = StarfieldAppDataPath,
                StarfieldGamePath = StarfieldGamePath
            };
        }
    }
}
```

#### Task 3.2: Main View Model (`MainViewModel.cs`)

This manages the main window state and commands.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using System.Threading.Tasks;
using System.Windows; // Used for MessageBox/Application exit in error cases

namespace LoadOrderKeeper.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // Observable properties for UI feedback
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(FixLoadOrderCommand))]
        private AppConfigModel _config = new AppConfigModel();
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReferenceCommand))]
        [NotifyCanExecuteChangedFor(nameof(FixLoadOrderCommand))]
        private bool _refExists;

        [ObservableProperty]
        private string _statusMessage = "Loading settings...";

        [ObservableProperty]
        private bool _isBusy;

        // Constructor
        public MainViewModel()
        {
            Task.Run(LoadInitialStateAsync);
        }

        private async Task LoadInitialStateAsync()
        {
            Config = await SettingsService.LoadSettingsAsync();
            RefExists = FileService.DoesReferenceFileExist(Config);
            
            if (Config.IsValid())
            {
                StatusMessage = "Ready. Configuration is valid.";
            }
            else
            {
                StatusMessage = "Configuration is required. Please set paths in the Settings window.";
            }
        }

        // --- Commands ---

        [RelayCommand(CanExecute = nameof(CanFixLoadOrder))]
        private async Task FixLoadOrderAsync()
        {
            IsBusy = true;
            StatusMessage = "Applying load order fix...";
            
            try
            {
                await FileService.ApplyLoadOrderAsync(Config);
                StatusMessage = "Load order successfully applied and fixed!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR: {ex.Message}";
                MessageBox.Show($"Failed to fix load order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        private bool CanFixLoadOrder() => Config.IsValid() && RefExists && !IsBusy;


        [RelayCommand(CanExecute = nameof(CanCreateReference))]
        private async Task CreateReferenceAsync()
        {
            IsBusy = true;
            StatusMessage = "Creating reference file...";
            
            try
            {
                await FileService.CreateReferenceFileAsync(Config);
                RefExists = true;
                StatusMessage = "Reference created successfully! You can now fix the load order.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"ERROR: {ex.Message}";
                MessageBox.Show($"Failed to create reference: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        private bool CanCreateReference() => Config.IsValid() && !RefExists && !IsBusy;


        [RelayCommand]
        private void OpenSettings()
        {
            // IMPORTANT: The UI Agent must implement the logic to show the Settings Window here.
            
            // 1. Create a SettingsViewModel using the current Config:
            var settingsVm = new SettingsViewModel(Config);
            
            // 2. Create the SettingsWindow (xaml) and set its DataContext to settingsVm.
            // 3. Show the SettingsWindow (Dialog).
            // 4. If the user saves/closes:
            
            // Placeholder for save/update logic:
            // Config = settingsVm.GetConfig();
            // SettingsService.SaveSettingsAsync(Config);
            // RefExists = FileService.DoesReferenceFileExist(Config);
            // StatusMessage = Config.IsValid() ? "Configuration updated." : "Configuration is invalid.";
        }

        [RelayCommand]
        private void ExitApplication()
        {
            Application.Current.Shutdown();
        }
    }
}
```

### Phase 4: User Interface (The Views)

Finally, create the WPF XAML structure.

#### Task 4.1: Settings Window (`SettingsWindow.xaml` and Code-Behind)

This window allows the user to configure the paths.

  * **Window Title:** "Load Order Keeper Settings"

  * **Layout:** Use a `Grid` or `StackPanel` for two pairs of `TextBlock`/`TextBox` controls.

  * **Data Bindings:**

      * `TextBox` for AppData Path: `Text="{Binding StarfieldAppDataPath, UpdateSourceTrigger=PropertyChanged}"`
      * `Button` for AppData Browse: `Command="{Binding BrowsePathCommand}" CommandParameter="AppData"`
      * `TextBox` for Game Path: `Text="{Binding StarfieldGamePath, UpdateSourceTrigger=PropertyChanged}"`
      * `Button` for Game Browse: `Command="{Binding BrowsePathCommand}" CommandParameter="GamePath"`
      * **Save Button:** `Command="{Binding SaveSettingsCommand}"`

  * **Code-Behind Requirement:** The `SettingsWindow.xaml.cs` must implement the `BrowsePathCommand` logic (using a `System.Windows.Forms.FolderBrowserDialog` or similar WPF equivalent) and the logic for the `SaveSettingsCommand` to close the window.

#### Task 4.2: Main Window (`MainWindow.xaml`)

  * **Data Context:** Set to `MainViewModel`.
  * **Layout:** Simple vertical `StackPanel` layout.
  * **Key Controls:**

| Control | Type | Binding / Visibility | Notes |
| :--- | :--- | :--- | :--- |
| Menu | `Menu` | Contains `File` -\> `Settings` (`Command="{Binding OpenSettingsCommand}"`) and `Exit` (`Command="{Binding ExitApplicationCommand}"`). | Essential for accessing configuration. |
| Path Display | `TextBlock` | `Text="Target: {Binding Config.StarfieldAppDataPath}"` | Displays the path. |
| **"Create Ref"** | `Button` | `Command="{Binding CreateReferenceCommand}"` | **Enabled:** `CanCreateReference` |
| **"Fix Load Order"** | `Button` | `Command="{Binding FixLoadOrderCommand}"` | **Enabled:** `CanFixLoadOrder` |
| Progress Bar | `ProgressBar` | `IsIndeterminate="{Binding IsBusy}"`, `Visibility="{Binding IsBusy, Converter={StaticResource BooleanToVisibilityConverter}}"` | Shows activity during file operations. |
| Status | `TextBlock` | `Text="{Binding StatusMessage}"` | Displays current status and errors. |



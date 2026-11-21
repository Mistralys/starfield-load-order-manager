# File Tree (Logical Overview)

> Complete directory and file structure of the project.

---

```text
.
├─ Starfield Load Order Keeper.csproj
├─ App.xaml
├─ App.xaml.cs
├─ AssemblyInfo.cs
├─ MainWindow.xaml
├─ MainWindow.xaml.cs
├─ Constants/
│  └─ UserMessages.cs
├─ Coordinators/
│  ├─ ICoordinator.cs
│  ├─ CoordinatorBase.cs
│  ├─ FileMonitoringCoordinator.cs
│  ├─ StatusCoordinator.cs
│  ├─ UpdateCheckCoordinator.cs
│  ├─ ProfileCoordinator.cs
│  ├─ ConfigurationCoordinator.cs
│  ├─ GameLauncherCoordinator.cs
│  ├─ WindowManager.cs
│  └─ Events/
│     ├─ CoordinatorEventArgs.cs       (ChangeDetectedEventArgs, SteamWarningChangedEventArgs, SortingRecommendationChangedEventArgs)
│     ├─ ProfileChangedEventArgs.cs
│     └─ ConfigValidationChangedEventArgs.cs
├─ Models/
│  ├─ AppConfigModel.cs
│  ├─ DebugStateModel.cs
│  ├─ DiffLineModel.cs
│  ├─ ModDiffModel.cs
│  ├─ ModEntryModel.cs
│  ├─ PendingChangesModel.cs
│  ├─ PluginsComparisonResult.cs
│  ├─ ProfileModel.cs
│  ├─ ReferenceVersionMetadataModel.cs
│  ├─ StatusMessageModel.cs
│  └─ UpdateCheckResult.cs
├─ Services/
│  ├─ Abstractions/                    (reserved for future service interfaces)
│  ├─ DateTimeFormattingService.cs
│  ├─ DebugStateService.cs
│  ├─ DiffService.cs
│  ├─ ErrorLoggingService.cs
│  ├─ FileOperationsService.cs
│  ├─ FileService.cs
│  ├─ LocalizationService.cs          (legacy; primary LocalizationService lives in ViewTexts/)
│  ├─ ProfileService.cs
│  ├─ ReferenceHistoryService.cs
│  ├─ ReferenceManagementService.cs
│  ├─ SettingsService.cs
│  ├─ UpdateCheckService.cs
│  ├─ VersionService.cs
│  ├─ ViewModelInitializer.cs
│  └─ WindowLifecycleService.cs
├─ Helpers/
│  ├─ CoordinatorEventBinder.cs
│  └─ SteamLibraryVdfParser.cs         (bounded parser for Steam libraryfolders.vdf)
├─ ViewModels/
│  ├─ CommentInputViewModel.cs
│  ├─ ConfirmationDialogViewModel.cs
│  ├─ DiffDialogViewModel.cs
│  ├─ ErrorDialogViewModel.cs
│  ├─ LanguageOption.cs
│  ├─ MainViewModel.cs
│  ├─ ManageProfilesViewModel.cs
│  ├─ ProfilePropertiesViewModel.cs
│  ├─ ReferenceHistoryViewModel.cs
│  ├─ SettingsViewModel.cs
│  ├─ SwitchProfileViewModel.cs
│  ├─ UpdateOptionsViewModel.cs
│  └─ ViewPendingChangesViewModel.cs
├─ Views/
│  ├─ AboutWindow.xaml
│  ├─ AboutWindow.xaml.cs
│  ├─ CommentInputDialog.xaml
│  ├─ CommentInputDialog.xaml.cs
│  ├─ ConfirmationDialog.xaml
│  ├─ ConfirmationDialog.xaml.cs
│  ├─ DiffWindow.xaml
│  ├─ DiffWindow.xaml.cs
│  ├─ ErrorDialog.xaml
│  ├─ ErrorDialog.xaml.cs
│  ├─ ManageProfilesWindow.xaml
│  ├─ ManageProfilesWindow.xaml.cs
│  ├─ ProfilePropertiesWindow.xaml
│  ├─ ProfilePropertiesWindow.xaml.cs
│  ├─ ReferenceHistoryWindow.xaml
│  ├─ ReferenceHistoryWindow.xaml.cs
│  ├─ SettingsWindow.xaml
│  ├─ SettingsWindow.xaml.cs
│  ├─ SwitchProfileWindow.xaml
│  ├─ SwitchProfileWindow.xaml.cs
│  ├─ UpdateOptionsDialog.xaml
│  ├─ UpdateOptionsDialog.xaml.cs
│  ├─ ViewPendingChangesWindow.xaml
│  └─ ViewPendingChangesWindow.xaml.cs
├─ Controls/
│  ├─ ConfigInvalidOverlay.xaml
│  └─ ConfigInvalidOverlay.xaml.cs
├─ Converters/
│  ├─ ActiveProfileVisibilityConverter.cs
│  ├─ BooleanAndConverter.cs
│  ├─ BoolToChevronKindConverter.cs
│  ├─ ChangeSummaryConverter.cs
│  ├─ CountToVisibilityConverter.cs
│  ├─ InverseBooleanToVisibilityConverter.cs
│  ├─ InverseCountToVisibilityConverter.cs
│  └─ ReplacementCommandParameterConverter.cs
├─ Styles/
│  ├─ ButtonStyles.xaml
│  ├─ DataGridStyles.xaml
│  ├─ DiffBrushes.xaml
│  ├─ TextStyles.xaml
│  └─ WindowStyles.xaml
├─ ViewTexts/
│  ├─ AboutViewModel.cs
│  ├─ CommentInputTexts.cs
│  ├─ CommonTexts.cs
│  ├─ ConfigInvalidOverlayTexts.cs
│  ├─ ConfirmationDialogTexts.cs
│  ├─ DiffDialogTexts.cs
│  ├─ ErrorDialogTexts.cs
│  ├─ LocalizationService.cs
│  ├─ MainWindowStatusTexts.cs
│  ├─ MainWindowTexts.cs
│  ├─ ManageProfilesTexts.cs
│  ├─ MenuViewModel.cs
│  ├─ ProfilePropertiesTexts.cs
│  ├─ ReferenceHistoryTexts.cs
│  ├─ ReferenceManagementStatusTexts.cs
│  ├─ SettingsWindowTexts.cs
│  ├─ SwitchProfileTexts.cs
│  ├─ UpdateOptionsTexts.cs
│  ├─ ViewModelInitializerStatusTexts.cs
│  ├─ ViewPendingChangesTexts.cs
│  └─ Locales/
│     ├─ de-DE.json
│     ├─ en-US.json
│     ├─ es-ES.json
│     ├─ fr-FR.json
│     ├─ it-IT.json
│     ├─ ja-JP.json
│     ├─ pt-BR.json
│     └─ zh-CN.json
├─ Docs/
│  └─ Agents/
│     ├─ project-manifest/              (this manifest)
│     │  ├─ README.md
│     │  ├─ tech-stack.md
│     │  ├─ file-tree.md
│     │  ├─ api-surface.md
│     │  ├─ data-flows.md
│     │  ├─ constraints.md
│     │  ├─ localization.md
│     │  ├─ file-formats.md
│     │  └─ ui-design.md
│     ├─ Sorting Scenarios/             (real-world diff/sort test cases)
│     │  └─ *.md
│     ├─ example-plugins.txt            (sample Plugins.txt for testing)
│     └─ example-steam-library.vdf      (sample Steam library config for testing)
├─ .github/
│  ├─ dependabot.yml
│  ├─ workflows/
│  │  └─ release.yml                    (tag-triggered release: tests, MSI, ZIP, GitHub release)
│  └─ skills/
│     └─ release-check/
│        └─ SKILL.md                    (agent skill: the judgment half of the release gate)
├─ Tools/
│  ├─ Menu.ps1                          (developer menu: build, run, merge docs, release check)
│  ├─ MergeDocs.ps1
│  ├─ README.md
│  ├─ ReleaseCheck.ps1                  (entry point of the mechanical release gate)
│  └─ ReleaseCheck/
│     ├─ Common.ps1                     (result model, git/process helpers, version and changelog parsing)
│     ├─ Self-Test.ps1                  (fixture-based tests of the checks themselves)
│     ├─ ReleaseCheck.allow.json        (accepted findings, each with a reason; empty by default)
│     └─ Checks/
│        ├─ Version.ps1                 (changelog and version progression)
│        ├─ Git.ps1                     (tree, branch, upstream, tags)
│        ├─ Files.ps1                   (unicode integrity and repository hygiene)
│        ├─ Localization.ps1            (locale files versus code, csproj, plural and access-key rules)
│        ├─ Ui.ps1                      (menu wiring and hard-coded UI text)
│        ├─ Docs.ps1                    (manifest sync and link integrity)
│        ├─ Packaging.ps1               (csproj, installer, workflow consistency)
│        └─ Dotnet.ps1                  (Release build, tests, advisories, optional publish)
└─ Tests/
   └─ LoadOrderKeeper.Tests/
      ├─ LoadOrderKeeper.Tests.csproj
      ├─ README.md                        (test project overview, trait filter guide, scenario infrastructure notes)
      ├─ TestConfigContext.cs
      ├─ TestEnvironment.cs               ([ModuleInitializer]: redirects SettingsService to a temp config folder for the whole test run)
      ├─ AssemblyInfo.cs                  (disables test parallelization; all tests share one temp config.json)
      ├─ ScenarioTestBase.cs              (abstract base: StandardModList, SetupStandardReferenceAsync, assertion helpers)
      ├─ ScenarioTests.cs                 (Scenarios 01–16: diff detection and sorting behavior)
      ├─ ReplacementDetectionDiagnostics.cs  ([Trait("Category","Diagnostic")] companion to Scenario_16; logs LCS diff for regression triage)
      ├─ ClassifyChangesTests.cs          (isolation tests for DiffService.ClassifyChanges — constructs ModEntryModel lists directly, asserts structural properties per classification step)
      ├─ DiffServiceTests.cs
      ├─ FileServiceTests.cs
      ├─ ProfileServiceTests.cs
      ├─ ReferenceHistoryServiceTests.cs
      ├─ SettingsServiceTests.cs
      ├─ Fixtures/
      │  ├─ EnglishLocaleFixture.cs      (IDisposable; forces en-US on LocalizationService singleton for test class lifetime)
      │  └─ LocaleSequentialCollection.cs (xUnit CollectionDefinition with DisableParallelization=true; groups locale-sensitive test classes)
      ├─ Helpers/
      │  └─ SteamLibraryVdfParserTests.cs (fixture and grammar tests for Steam libraryfolders.vdf)
      ├─ Coordinators/
      │  ├─ ConfigurationCoordinatorTests.cs
      │  ├─ FileMonitoringCoordinatorTests.cs
      │  ├─ GameLauncherCoordinatorTests.cs
      │  ├─ ProfileCoordinatorTests.cs
      │  ├─ StatusCoordinatorTests.cs
      │  └─ UpdateCheckCoordinatorTests.cs
      ├─ Models/
      │  ├─ AppConfigModelTests.cs
      │  ├─ ModEntryModelTests.cs
      │  └─ ProfileModelTests.cs
      ├─ Services/
      │  ├─ DateTimeFormattingServiceTests.cs
      │  ├─ DebugStateServiceTests.cs    (change filtering, enum names, file facts, sanitizing)
      │  ├─ ErrorLoggingServiceTests.cs
      │  ├─ VersionServiceTests.cs
      │  └─ ViewModelInitializerTests.cs
      ├─ ViewModels/
      │  ├─ DiffDialogViewModelTests.cs  (counts, visibility booleans, count text, guidance cards, sidebar state, confirmation message, reactive refresh, Dispose safety)
      │  └─ MainViewModelTests.cs
      └─ ViewTexts/
         ├─ LocalizationCompletenessTests.cs
         └─ LocalizationServiceTests.cs
```

---

[<< Back to Index](README.md)

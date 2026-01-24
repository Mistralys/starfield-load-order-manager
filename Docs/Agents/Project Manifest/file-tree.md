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
│     ├─ CoordinatorEventArgs.cs
│     ├─ ChangeDetectedEventArgs.cs
│     ├─ SortingRecommendationChangedEventArgs.cs
│     ├─ SteamWarningChangedEventArgs.cs
│     ├─ ProfileChangedEventArgs.cs
│     └─ ConfigValidationChangedEventArgs.cs
├─ Models/
│  ├─ AppConfigModel.cs
│  ├─ DebugStateModel.cs
│  ├─ DiffLineModel.cs
│  ├─ LanguageOption.cs
│  ├─ ModDiffModel.cs
│  ├─ ModEntryModel.cs
│  ├─ PendingChangesModel.cs
│  ├─ PluginsComparisonResult.cs
│  ├─ ProfileModel.cs
│  ├─ ReferenceVersionMetadataModel.cs
│  ├─ StatusMessageModel.cs
│  └─ UpdateCheckResult.cs
├─ Services/
│  ├─ DateTimeFormattingService.cs
│  ├─ DebugStateService.cs
│  ├─ DiffService.cs
│  ├─ ErrorLoggingService.cs
│  ├─ FileOperationsService.cs
│  ├─ FileService.cs
│  ├─ LocalizationService.cs
│  ├─ ProfileService.cs
│  ├─ ReferenceHistoryService.cs
│  ├─ ReferenceManagementService.cs
│  ├─ SettingsService.cs
│  ├─ UpdateCheckService.cs
│  ├─ VersionService.cs
│  ├─ ViewModelInitializer.cs
│  └─ WindowLifecycleService.cs
├─ Helpers/
│  └─ CoordinatorEventBinder.cs
├─ ViewModels/
│  ├─ AboutViewModel.cs
│  ├─ CommentInputViewModel.cs
│  ├─ ConfirmationDialogViewModel.cs
│  ├─ DiffDialogViewModel.cs
│  ├─ ErrorDialogViewModel.cs
│  ├─ MainViewModel.cs
│  ├─ ManageProfilesViewModel.cs
│  ├─ MenuViewModel.cs
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
│  ├─ ChangeSummaryConverter.cs
│  ├─ CountToVisibilityConverter.cs
│  ├─ InverseBooleanToVisibilityConverter.cs
│  ├─ InverseCountToVisibilityConverter.cs
│  └─ ReplacementCommandParameterConverter.cs
├─ Styles/
│  ├─ ButtonStyles.xaml
│  ├─ DataGridStyles.xaml
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
│  ├─ MainWindowTexts.cs
│  ├─ ManageProfilesTexts.cs
│  ├─ MenuViewModel.cs
│  ├─ ProfilePropertiesTexts.cs
│  ├─ ReferenceHistoryTexts.cs
│  ├─ SettingsWindowTexts.cs
│  ├─ SwitchProfileTexts.cs
│  ├─ UpdateOptionsTexts.cs
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
│     ├─ Application Description/
│     │  ├─ README.md
│     │  ├─ Architecture/
│     │  │  ├─ coordinator-pattern.md
│     │  │  └─ mvvm-structure.md
│     │  └─ Features/
│     │     ├─ change-detection.md
│     │     ├─ configuration-validation.md
│     │     ├─ exception-handling.md
│     │     ├─ game-integration.md
│     │     ├─ load-order-management.md
│     │     ├─ multilingual-support.md
│     │     ├─ profile-system.md
│     │     ├─ reference-history.md
│     │     ├─ steam-detection.md
│     │     └─ version-check.md
│     ├─ Project Manifest/
│     │  ├─ README.md
│     │  ├─ tech-stack.md
│     │  ├─ file-tree.md
│     │  ├─ api-coordinators.md
│     │  ├─ api-models.md
│     │  ├─ api-services.md
│     │  ├─ api-viewmodels.md
│     │  ├─ api-views.md
│     │  ├─ data-flows.md
│     │  └─ constraints-invariants.md
│     ├─ Development History/
│     │  ├─ 01-initial-agent-plan.md
│     │  ├─ 02-add-content-diff.md
│     │  ├─ 03-numbered-mod-order.md
│     │  ├─ 04-enabled-disabled-status-awareness.md
│     │  ├─ 05-problem-resolution-controls.md
│     │  ├─ 06-profiles-feature.md
│     │  ├─ 07-group-dependent-mod-changes.md
│     │  ├─ 13-steam-guard.md
│     │  ├─ 14-refactor-file-monitoring-coordinator.md
│     │  ├─ 15-window-manager-coordinator.md
│     │  ├─ 16-status-coordinator.md
│     │  ├─ 17-update-check-coordinator.md
│     │  ├─ 18-profile-coordinator.md
│     │  ├─ 19-configuration-coordinator.md
│     │  ├─ 20-game-launcher-coordinator.md
│     │  ├─ 20-view-pending-changes.md
│     │  ├─ 21-invalid-config-handling.md
│     │  └─ coordinator-refactoring-complete-summary.md
│     ├─ implementation-guidelines.md
│     └─ example-plugins.txt
└─ Tests/
   └─ LoadOrderKeeper.Tests/
      ├─ LoadOrderKeeper.Tests.csproj
      ├─ TestConfigContext.cs
      ├─ Coordinators/
      │  ├─ ConfigurationCoordinatorTests.cs
      │  ├─ FileMonitoringCoordinatorTests.cs
      │  ├─ GameLauncherCoordinatorTests.cs
      │  ├─ ProfileCoordinatorTests.cs
      │  ├─ StatusCoordinatorTests.cs
      │  ├─ UpdateCheckCoordinatorTests.cs
      │  └─ WindowManagerTests.cs
      ├─ Models/
      │  ├─ AppConfigModelTests.cs
      │  ├─ DiffLineModelTests.cs
      │  ├─ ModDiffModelTests.cs
      │  ├─ ModEntryModelTests.cs
      │  └─ ProfileModelTests.cs
      ├─ Services/
      │  ├─ DateTimeFormattingServiceTests.cs
      │  ├─ DebugStateServiceTests.cs
      │  ├─ DiffServiceTests.cs
      │  ├─ ErrorLoggingServiceTests.cs
      │  ├─ FileOperationsServiceTests.cs
      │  ├─ FileServiceTests.cs
      │  ├─ ProfileServiceTests.cs
      │  ├─ ReferenceHistoryServiceTests.cs
      │  ├─ ReferenceManagementServiceTests.cs
      │  ├─ SettingsServiceTests.cs
      │  ├─ UpdateCheckServiceTests.cs
      │  └─ VersionServiceTests.cs
      └─ ViewTexts/
         ├─ LocalizationCompletenessTests.cs
         └─ LocalizationServiceTests.cs
```

---

[<< Back to Index](README.md)

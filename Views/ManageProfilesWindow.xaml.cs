using System.Windows;
using System.Windows.Input;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views;

public partial class ManageProfilesWindow : Window
{
    private readonly AppConfigModel _config;

    public ManageProfilesWindow(AppConfigModel config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        InitializeComponent();
        
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ManageProfilesViewModel vm)
        {
            await vm.LoadProfilesAsync();
        }
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ManageProfilesViewModel oldVm)
        {
            oldVm.CloseRequested -= OnCloseRequested;
            oldVm.AddProfileRequested -= OnAddProfileRequested;
            oldVm.EditProfileRequested -= OnEditProfileRequested;
            oldVm.CopyProfileRequested -= OnCopyProfileRequested;
        }

        if (e.NewValue is ManageProfilesViewModel newVm)
        {
            newVm.CloseRequested += OnCloseRequested;
            newVm.AddProfileRequested += OnAddProfileRequested;
            newVm.EditProfileRequested += OnEditProfileRequested;
            newVm.CopyProfileRequested += OnCopyProfileRequested;
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private async void OnAddProfileRequested(object? sender, ProfileModel e)
    {
        if (DataContext is not ManageProfilesViewModel vm)
        {
            return;
        }

        var profiles = await ProfileService.LoadProfilesAsync(_config);
        var propertiesVm = new ProfilePropertiesViewModel(profiles);
        var propertiesWindow = new ProfilePropertiesWindow
        {
            Owner = this,
            DataContext = propertiesVm
        };

        if (propertiesWindow.ShowDialog() == true)
        {
            var (label, description) = propertiesVm.GetProfileData();
            try
            {
                await ProfileService.CreateProfileAsync(_config, label, description);
                await vm.LoadProfilesAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to create profile: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private async void OnEditProfileRequested(object? sender, ProfileModel profile)
    {
        if (DataContext is not ManageProfilesViewModel vm)
        {
            return;
        }

        var profiles = await ProfileService.LoadProfilesAsync(_config);
        var propertiesVm = new ProfilePropertiesViewModel(profile, profiles);
        var propertiesWindow = new ProfilePropertiesWindow
        {
            Owner = this,
            DataContext = propertiesVm
        };

        if (propertiesWindow.ShowDialog() == true)
        {
            var (label, description) = propertiesVm.GetProfileData();
            try
            {
                await ProfileService.UpdateProfileAsync(_config, profile, label, description);
                await vm.LoadProfilesAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to update profile: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private async void OnCopyProfileRequested(object? sender, ProfileModel sourceProfile)
    {
        if (DataContext is not ManageProfilesViewModel vm)
        {
            return;
        }

        var profiles = await ProfileService.LoadProfilesAsync(_config);
        
        // Use a simple input dialog to get the new label
        var newLabel = PromptForNewLabel(sourceProfile.Label);
        if (string.IsNullOrWhiteSpace(newLabel))
        {
            return;
        }

        // Validate the new label
        if (profiles.Any(p => string.Equals(p.Label, newLabel.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            System.Windows.MessageBox.Show(
                "A profile with this label already exists.",
                "Validation Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            await ProfileService.CopyProfileAsync(_config, sourceProfile.Id, newLabel.Trim());
            await vm.LoadProfilesAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to copy profile: {ex.Message}",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnProfileDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ManageProfilesViewModel vm && vm.SelectedProfile != null)
        {
            if (!vm.SelectedProfile.IsDefault)
            {
                OnEditProfileRequested(this, vm.SelectedProfile);
            }
        }
    }

    private string? PromptForNewLabel(string suggestedLabel)
    {
        var dialog = new Window
        {
            Owner = this,
            Title = "Copy Profile",
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Background = System.Windows.Media.Brushes.White
        };

        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        var textBlock = new System.Windows.Controls.TextBlock
        {
            Text = "Enter a label for the new profile:",
            Margin = new Thickness(0, 0, 0, 8)
        };
        System.Windows.Controls.Grid.SetRow(textBlock, 0);

        var textBox = new System.Windows.Controls.TextBox
        {
            Text = $"{suggestedLabel} Copy",
            Margin = new Thickness(0, 0, 0, 16),
            MaxLength = 30
        };
        System.Windows.Controls.Grid.SetRow(textBox, 1);

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        };
        System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

        var okButton = new System.Windows.Controls.Button
        {
            Content = "OK",
            Width = 80,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };
        okButton.Click += (s, e) => { dialog.DialogResult = true; dialog.Close(); };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Cancel",
            Width = 80,
            IsCancel = true
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        grid.Children.Add(textBlock);
        grid.Children.Add(textBox);
        grid.Children.Add(buttonPanel);

        dialog.Content = grid;

        textBox.Focus();
        textBox.SelectAll();

        return dialog.ShowDialog() == true ? textBox.Text : null;
    }
}

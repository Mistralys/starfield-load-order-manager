using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views;

public partial class SwitchProfileWindow : Window
{
    private readonly AppConfigModel _config;

    public SwitchProfileWindow(AppConfigModel config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SwitchProfileViewModel vm)
        {
            await vm.LoadProfilesAsync();
            UpdateActiveProfileIndicators();
        }
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SwitchProfileViewModel oldVm)
        {
            oldVm.ProfileSelected -= OnProfileSelected;
        }

        if (e.NewValue is SwitchProfileViewModel newVm)
        {
            newVm.ProfileSelected += OnProfileSelected;
        }
    }

    private async void OnProfileSelected(object? sender, ProfileModel profile)
    {
        if (profile.Id == _config.ActiveProfileId)
        {
            // Already active, just close
            Close();
            return;
        }

        try
        {
            await ProfileService.SwitchProfileAsync(_config, profile.Id);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to switch profile: {ex.Message}",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnProfileCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { Tag: ProfileModel profile } && DataContext is SwitchProfileViewModel vm)
        {
            vm.SelectProfile(profile);
        }
    }

    private void UpdateActiveProfileIndicators()
    {
        if (DataContext is not SwitchProfileViewModel vm)
        {
            return;
        }

        var activeProfileId = _config.ActiveProfileId ?? "default";

        // Find all profile cards and update their active indicators
        var itemsControl = FindVisualChild<ItemsControl>(this);
        if (itemsControl == null)
        {
            return;
        }

        for (int i = 0; i < itemsControl.Items.Count; i++)
        {
            if (itemsControl.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter container)
            {
                var profile = itemsControl.Items[i] as ProfileModel;
                var icon = FindVisualChild<MaterialDesignThemes.Wpf.PackIcon>(container, "ActiveIcon");
                if (icon != null && profile != null)
                {
                    icon.Visibility = profile.Id == activeProfileId ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent, string? name = null) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            
            if (child is T typedChild && (name == null || (child is FrameworkElement fe && fe.Name == name)))
            {
                return typedChild;
            }

            var result = FindVisualChild<T>(child, name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}

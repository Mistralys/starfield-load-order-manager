using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.ViewModels;

public partial class UpdateOptionsViewModel : ObservableObject
{
    public string WindowTitle { get; }
    public string MessageText { get; }
    public string NexusmodsButtonText { get; } = "Open on Nexusmods";
    public string GitHubButtonText { get; } = "Open on GitHub";
    public string CancelButtonText { get; } = "Cancel";
    
    public string NexusmodsUrl { get; }
    public string GitHubUrl { get; }

    public event EventHandler? CloseRequested;

    public UpdateOptionsViewModel(string currentVersion, string? latestVersion)
    {
        NexusmodsUrl = UpdateCheckService.GetNexusModsUrl();
        GitHubUrl = UpdateCheckService.GetGitHubReleasesUrl();

        if (string.IsNullOrEmpty(latestVersion) || latestVersion == "Unknown")
        {
            WindowTitle = "Download Options";
            MessageText = $"Unable to check for updates automatically.\n\nCurrent version: {currentVersion}\n\nYou can check for updates manually at these locations:";
        }
        else
        {
            WindowTitle = "Download Update";
            MessageText = $"A new version is available!\n\nCurrent version: {currentVersion}\nLatest version: {latestVersion}\n\nChoose your preferred download source:";
        }
    }

    [RelayCommand]
    private void OpenNexusmods()
    {
        OpenUrl(NexusmodsUrl);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        OpenUrl(GitHubUrl);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch
        {
            // Silent failure
        }
    }
}

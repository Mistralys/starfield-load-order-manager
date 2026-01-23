using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.ViewTexts;

namespace LoadOrderKeeper.ViewModels;

public partial class UpdateOptionsViewModel : ObservableObject
{
    private readonly UpdateOptionsTexts _texts = new();

    public string WindowTitle { get; }
    public string MessageText { get; }
    public string NexusmodsButtonText => _texts.NexusmodsButtonText;
    public string GitHubButtonText => _texts.GitHubButtonText;
    public string CancelButtonText => _texts.CancelButtonText;
    
    public string NexusmodsUrl { get; }
    public string GitHubUrl { get; }

    public event EventHandler? CloseRequested;

    public UpdateOptionsViewModel(string currentVersion, string? latestVersion)
    {
        NexusmodsUrl = UpdateCheckService.GetNexusModsUrl();
        GitHubUrl = UpdateCheckService.GetGitHubReleasesUrl();

        if (string.IsNullOrEmpty(latestVersion) || latestVersion == "Unknown")
        {
            WindowTitle = _texts.WindowTitleOptions;
            MessageText = string.Format(_texts.MessageCheckFailed, currentVersion);
        }
        else
        {
            WindowTitle = _texts.WindowTitleDownload;
            MessageText = string.Format(_texts.MessageUpdateAvailable, currentVersion, latestVersion);
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

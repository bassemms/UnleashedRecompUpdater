using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Newtonsoft.Json.Linq;

namespace UpdateCheckerApp
{
    public partial class MainWindow : Window
    {
        public string localVersion = "";

        public enum UpdateStatus
        {
            Checking,
            UpToDate,
            UpdateAvailable,
            Error,
        };

        public UpdateStatus Status { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            string filePath = @".\UnleashedRecomp.exe";

            if (!System.IO.File.Exists(filePath))
            {
                UpdateStatusText.Text =
                    "Error: UnleashedRecomp.exe not found.\nMake sure to put this file in the same directory as the game.";

                CheckUpdateButton.IsEnabled = false;
            }
            else
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                localVersion = versionInfo.FileVersion ?? "0.0.0.0";
                Console.WriteLine($"Local version: {localVersion}");
                CurrentVersion.Text = $"Current version: {localVersion}";
            }
        }

        private async void OnCheckUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            UpdateStatusText.Text = "Checking for updates...";

            bool updateAvailable = await CheckForUpdatesAsync();

            switch (Status)
            {
                case UpdateStatus.Error:
                    UpdateStatusText.Text = "Error checking for updates.";
                    break;
                case UpdateStatus.UpToDate:
                    UpdateStatusText.Text = "Your software is up to date!";
                    break;
                case UpdateStatus.UpdateAvailable:
                    UpdateStatusText.Text = "Update available!";
                    break;
            }
        }

        private async Task<bool> CheckForUpdatesAsync()
        {
            string apiUrl =
                "https://api.github.com/repos/hedge-dev/UnleashedRecomp/releases/latest";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "UnleashedRecomp");

                    string response = await client.GetStringAsync(apiUrl);
                    JObject json = JObject.Parse(response);
                    string latestVersion = json["tag_name"]?.ToString() ?? "0.0.0.0";

                    Console.WriteLine($"Latest online version: {latestVersion}");
                    LoggingText.Text = $"Latest online version: {latestVersion}";

                    return string.Compare(
                            localVersion,
                            latestVersion,
                            StringComparison.OrdinalIgnoreCase
                        ) < 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking updates: {ex.Message}");
                    LoggingText.Text = $"Error checking updates: {ex.Message}";
                    Status = UpdateStatus.Error;
                    return false;
                }
            }
        }
    }
}

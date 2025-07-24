using MAUI_Class_Tracker.Models;
using MAUI_Class_Tracker.Services;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Debug = System.Diagnostics.Debug;


#if ANDROID
using Android;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Microsoft.Maui.ApplicationModel;
#endif

namespace MAUI_Class_Tracker.Views;

public partial class HomePage : ContentPage
{
    private readonly DataService _dataService;
    public ObservableCollection<Term> Terms { get; set; } = new();

    public Command OpenFlyoutCommand => new Command(() =>
    {
        Shell.Current.FlyoutIsPresented = true;
    });

    public HomePage(DataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        BindingContext = this;

        //Debug
        //CheckIfDatabaseExistsAsync();
    }

    // Debug
    //private async Task CheckIfDatabaseExistsAsync()
    //{
    //    string dbFileName = "wgu_tracker.db";
    //    string dbPath = Path.Combine(FileSystem.AppDataDirectory, dbFileName);
    //if (File.Exists(dbPath))
    //{
    //    Debug.WriteLine($"DB FOUND at: {dbPath}");
    //    await DisplayAlert("Database Check", $"DB EXISTS:\n{dbPath}", "OK");
    //}
    //else
    //{
    //    Debug.WriteLine($"DB NOT FOUND at: {dbPath}");
    //    await DisplayAlert("Database Check", $"DB NOT FOUND:\n{dbPath}", "OK");
    //}
    //}


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await LoadTermsAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Failed to load terms: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to load terms.", "OK");
        }
    }

    private async Task RequestNotificationPermissionAsync()
    {
#if ANDROID
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            var permission = Manifest.Permission.PostNotifications;

            if (ContextCompat.CheckSelfPermission(Platform.CurrentActivity, permission) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(Platform.CurrentActivity, new[] { permission }, 0);
            }
        }
#endif
    }

    private async Task LoadTermsAsync()
    {
        try
        {
            var allTerms = await _dataService.GetTermsAsync();

            Terms.Clear();
            foreach (var term in allTerms.OrderBy(t => t.StartDate))
            {
                Terms.Add(term);
                Debug.WriteLine($"[DEBUG] Loaded {Terms.Count} terms.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] LoadTermsAsync failed: {ex.Message}");
            await Shell.Current.DisplayAlert("Load Error", "Could not load terms.", "OK");
        }
    }

    private async void OnAddTermClicked(object sender, EventArgs e)
    {
        string title = await DisplayPromptAsync("New Term", "Enter a title for the term:");

        if (string.IsNullOrWhiteSpace(title))
        {
            await DisplayAlert("Error", "A title is required to create a term.", "OK");
            return;
        }

        int userId = Preferences.Get("UserId", 0);
        var newTerm = new Term
        {
            Title = title.Trim(),
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            UserId = userId
        };

        var savedTerm = await _dataService.SaveTermAsync(newTerm);
        if (savedTerm == null || savedTerm.TermId == 0)
        {
            Debug.WriteLine("[ERROR] Could not save or reload new term.");
            await DisplayAlert("Error", "Failed to load saved term. Please try again.", "OK");
            return;
        }
        await LoadTermsAsync();
        await Shell.Current.GoToAsync($"{nameof(TermDetailPage)}?termId={savedTerm.TermId}");
    }


        private async void OnManageTermsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TermListPage));
    }

    private async void OnTermTapped(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Term selectedTerm)
        {
            await Shell.Current.GoToAsync($"TermDetailPage?termId={selectedTerm.TermId}");
        }
    }

    private async void OnResetDatabaseClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Reset Database", "This will delete ALL terms, courses, and assessments. Continue?", "Yes", "No");
        if (!confirm) return;

        await _dataService.DeleteAllTermsAsync();
        await DisplayAlert("Database Reset", "All data has been deleted.", "OK");
    }


}
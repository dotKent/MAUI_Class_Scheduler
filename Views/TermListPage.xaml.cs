using System.Collections.ObjectModel;
using System.Diagnostics;
using MAUI_Class_Tracker.Models;
using MAUI_Class_Tracker.Services;
using Microsoft.Maui.Controls;
using Plugin.LocalNotification;

namespace MAUI_Class_Tracker.Views;

public partial class TermListPage : ContentPage
{
    private readonly DataService _dataService;

    public ObservableCollection<Term> FilteredTerms { get; set; } = new();
    private List<Term> _allTerms = new();

    public Command OpenFlyoutCommand => new Command(() =>
    {
        Shell.Current.FlyoutIsPresented = true;
    });

    public TermListPage(DataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTerms();
    }

    private async Task LoadTerms()
    {
        _allTerms = await _dataService.GetTermsAsync(); 
        FilteredTerms.Clear();

        foreach (var term in _allTerms)
        {
            FilteredTerms.Add(term);
            Debug.WriteLine($"[DEBUG] Loaded Term Id={term.TermId}, Title={term.Title}");
        }

        TermsCollectionView.ItemsSource = FilteredTerms;
    }
    public Command<Term> OnSelectTermCommand => new Command<Term>(async (term) =>
    {
        if (term != null)
        {
            await Shell.Current.GoToAsync($"termDetailPage?termId={term.TermId}");
        }
    });

    private void OnTermSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim() ?? string.Empty;

        var filtered = _allTerms.Where(t =>
            (!string.IsNullOrEmpty(t.Title) && t.Title.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            t.StartDate.ToString("d").Contains(query)
        );

        FilteredTerms.Clear();
        foreach (var item in filtered)
            FilteredTerms.Add(item);
    }

    private async void OnAddTermClicked(object sender, EventArgs e)
    {
        string newTitle = await DisplayPromptAsync("New Term", "Enter a title for the new term:");

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            Debug.WriteLine("[DEBUG] User cancelled new term entry.");
            return;
        }

        var existingTerms = await _dataService.GetTermsAsync();
        if (existingTerms.Any(t => t.Title.Trim().Equals(newTitle.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            await DisplayAlert("Duplicate Term", "A term with that title already exists. Please choose a different name.", "OK");
            return;
        }

        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddMonths(3);


        if (endDate <= startDate)
        {
            await DisplayAlert("Invalid Dates", "End date must be after start date.", "OK");
            return;
        }

        var newTerm = new Term
        {
            Title = newTitle.Trim(),
            StartDate = startDate,
            EndDate = endDate
        };

        var savedTerm = await _dataService.SaveTermAsync(newTerm);
        var reloadedTerm = await _dataService.GetTermByIdAsync(savedTerm.TermId);

        if (reloadedTerm == null || reloadedTerm.TermId == 0)
        {
            Debug.WriteLine("[ERROR] Could not reload saved term — aborting.");
            await DisplayAlert("Error", "Failed to reload the new term. Please try again.", "OK");
            return;
        }

        await LoadTerms();

        await Shell.Current.GoToAsync($"{nameof(TermDetailPage)}?termId={reloadedTerm.TermId}");
    }

    private async void OnNotifyTermClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Term term)
        {
            var notification = new NotificationRequest
            {
                Title = "Term Reminder",
                Description = $"Reminder: {term.Title} is active from {term.StartDate:MMM dd} to {term.EndDate:MMM dd}.",
                NotificationId = term.TermId + 1000,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now.AddSeconds(1) // for demo/test purposes
                }
            };

            await LocalNotificationCenter.Current.Show(notification);
            if (DateTime.Now > term.StartDate)
            {
                await LocalNotificationCenter.Current.Show(notification);
                await DisplayAlert("Notification Set",
                $"You will be reminded when {term.Title} ends on {term.EndDate:MMM dd, yyyy}.",
                "OK");
            }
            else
            {
                await LocalNotificationCenter.Current.Show(notification);
                await DisplayAlert("Notification Set",
                $"You will be reminded when {term.Title} starts on {term.StartDate:MMM dd, yyyy} and ends on {term.EndDate:MMM dd, yyyy}.",
                "OK");
            }

        }
    }

    private async void OnShareTermClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Term term)
        {
            var message = $"Term: {term.Title}\nStart: {term.StartDate:MMM dd, yyyy}\nEnd: {term.EndDate:MMM dd, yyyy}";

            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Text = message,
                Title = $"Share Term - {term.Title}"
            });
        }
    }

    private async void OnAddNoteToTermClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Term term)
        {
            string note = await DisplayPromptAsync("Add Note", $"Enter a note for term '{term.Title}':");

            if (!string.IsNullOrWhiteSpace(note))
            {
                term.Notes = note.Trim(); // assumes Term model has a Notes property
                await _dataService.SaveTermAsync(term);
                Debug.WriteLine($"[DEBUG] Note added to Term Id={term.TermId}");
            }
        }
    }

    private async void OnEditTermClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Term term)
        {
            if (term.TermId == 0)
            {
                Debug.WriteLine("[ERROR] CRASH - No TermId found for selected term!");
                await DisplayAlert("Error", "This term does not have a valid ID. Please refresh and try again.", "OK");
                return;
            }

            var freshTerm = await _dataService.GetTermByIdAsync(term.TermId);

            if (freshTerm == null)
            {
                Debug.WriteLine("[ERROR] Could not load term from DB.");
                await DisplayAlert("Error", "Failed to load the selected term. Please try again.", "OK");
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(TermDetailPage)}?termId={freshTerm.TermId}");
        }
        else
        {
            Debug.WriteLine("[ERROR] Invalid sender or CommandParameter in OnEditTermClicked.");
        }
    }

    private async void OnDeleteTermClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Term termToDelete)
        {
            bool confirm = await DisplayAlert("Delete Term", $"Are you sure you want to delete '{termToDelete.Title}'?", "Yes", "No");
            if (!confirm) return;

            await _dataService.DeleteTermAsync(termToDelete);
            await LoadTerms();
        }
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Reset All Terms", "Are you sure? This will delete ALL terms and courses!", "Yes", "No");
        if (!confirm) return;

        await _dataService.DeleteAllTermsAsync();
        await LoadTerms();
    }
}

using System.Diagnostics;
using MAUI_Class_Tracker.Models;
using MAUI_Class_Tracker.Services;
using System.Collections.ObjectModel;

namespace MAUI_Class_Tracker.Views;

public partial class Search : ContentPage
{
    private readonly DataService _dataService;
    public ObservableCollection<object> SearchResults { get; set; } = new();

    public Command OpenFlyoutCommand => new Command(() =>
    {
        Shell.Current.FlyoutIsPresented = true;
    });

    public Search(DataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        BindingContext = this;
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        SearchResults.Clear();

        string query = e.NewTextValue ?? "";

        if (string.IsNullOrWhiteSpace(query))
            return;

        var terms = await _dataService.GetTermsAsync();
        var courses = await _dataService.GetCoursesAsync();
        var assessments = await _dataService.GetAssessmentsAsync();

        foreach (var term in terms.Where(t => t.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
        {
            SearchResults.Add(new SearchResult
            {
                Display = $"Term: {term.Title}",
                Detail = $"Start: {term.StartDate:MM/dd/yyyy} - End: {term.EndDate:MM/dd/yyyy}",
                Item = term
            });
        }

        foreach (var course in courses.Where(c => c.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
        {
            SearchResults.Add(new SearchResult
            {
                Display = $"Course: {course.Title}",
                Detail = $"Start: {course.StartDate:MM/dd/yyyy} - End: {course.EndDate:MM/dd/yyyy}",
                Item = course
            });
        }

        foreach (var assessment in assessments.Where(a => a.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
        {
            SearchResults.Add(new SearchResult
            {
                Display = $"Assessment: {assessment.Name}",
                Detail = $"Due: {assessment.DueDate:MM/dd/yyyy}",
                Item = assessment
            });
        }
    }

    private async void OnResultTapped(object sender, EventArgs e)
    {
        if (sender is not Element element || element.BindingContext is not SearchResult result || result.Item == null)
        {
            Debug.WriteLine("[DEBUG] Invalid tap or missing data.");
            return;
        }

        object item = result.Item;

        if (item is Assessment assessment)
        {
            await Shell.Current.GoToAsync($"AssessmentDetailPage?assessmentId={assessment.AssessmentId}&courseId={assessment.CourseId}");
        }
        else if (item is Course course)
        {
            await Shell.Current.GoToAsync($"CourseDetailPage?courseId={course.CourseId}&termId={course.TermId}");
        }
        else if (item is Term term)
        {
            await Shell.Current.GoToAsync($"TermDetailPage?termId={term.TermId}");
        }
        else
        {
            Debug.WriteLine("[DEBUG] Unknown item type tapped in search.");
        }
    }

}

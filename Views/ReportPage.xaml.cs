using System.Diagnostics;
using MAUI_Class_Tracker.Models;
using MAUI_Class_Tracker.Services;
using System.Collections.ObjectModel;

namespace MAUI_Class_Tracker.Views;

public partial class ReportPage : ContentPage
{
    private readonly DataService _dataService;
    private ObservableCollection<ReportItem> _reportItems = new();

    private List<ReportItem> AllItems = new();

    public Command OpenFlyoutCommand => new Command(() =>
    {
        Shell.Current.FlyoutIsPresented = true;
    });

    public ReportPage(DataService dataService)
    {
        InitializeComponent(); // Must be FIRST
        _dataService = dataService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadReportAsync();
        ReportCollectionView.ItemsSource = _reportItems.OrderBy(r => r.Date).ToList();

        if (ReportCollectionView == null)
        {
            Debug.WriteLine("[ERROR] ReportCollectionView is null. Check XAML or InitializeComponent.");
            return;
        }
    }

    private async Task LoadReportAsync()
    {
        _reportItems.Clear();
        AllItems.Clear();

        int userId = Preferences.Get("UserId", 0);
        var terms = await _dataService.GetTermsAsync();
        var allCourses = new List<Course>();
        var allAssessments = new List<Assessment>();

        foreach (var term in terms)
        {
            var courses = await _dataService.GetCoursesByTermIdAsync(term.TermId);
            allCourses.AddRange(courses);

            foreach (var course in courses)
            {
                var assessments = await _dataService.GetAssessmentsForCourseAsync(course.CourseId);
                allAssessments.AddRange(assessments);
            }
        }

        foreach (var term in terms)
        {
            var item = new ReportItem
            {
                Title = $"Term: {term.Title}",
                Subtitle = $"{term.StartDate:MMM dd} - {term.EndDate:MMM dd}",
                Detail = $"Courses: {allCourses.Count(c => c.TermId == term.TermId)}",
                Date = term.StartDate,
                TermId = term.TermId
            };
            _reportItems.Add(item);
            AllItems.Add(item);
        }

        foreach (var course in allCourses)
        {
            var item = new ReportItem
            {
                Title = $"Course: {course.Title}",
                Subtitle = course.Status,
                Detail = $"Instructor: {course.InstructorName ?? "Missing"}",
                Date = course.EndDate,
                CourseId = course.CourseId
            };
            _reportItems.Add(item);
            AllItems.Add(item);
        }

        foreach (var assessment in allAssessments)
        {
            var item = new ReportItem
            {
                Title = $"Assessment: {assessment.Name}",
                Subtitle = assessment.Type,
                Detail = $"Due: {assessment.DueDate:MM/dd/yyyy}",
                Date = assessment.DueDate,
                AssessmentId = assessment.AssessmentId,
                CourseId = assessment.CourseId
            };
            _reportItems.Add(item);
            AllItems.Add(item);
        }

    }

    private void OnUpcomingClicked(object sender, EventArgs e)
    {
        var twoWeeksFromNow = DateTime.Today.AddDays(14);
        var upcoming = AllItems
            .Where(i => i.Date.HasValue && i.Date.Value <= twoWeeksFromNow)
            .OrderBy(i => i.Date)
            .ToList();

        ReportCollectionView.ItemsSource = upcoming;
    }

    private async void OnTentativeClicked(object sender, EventArgs e)
    {
        var courses = await _dataService.GetCoursesAsync();

        var tentative = courses
            .Where(c =>
                string.IsNullOrWhiteSpace(c.InstructorName) ||
                string.IsNullOrWhiteSpace(c.InstructorPhone) ||
                string.IsNullOrWhiteSpace(c.InstructorEmail))
            .Select(c => new ReportItem
            {
                Title = $"Course: {c.Title}",
                Subtitle = "Missing instructor info",
                Detail = $"Instructor: {c.InstructorName ?? "?"} | Phone: {c.InstructorPhone ?? "?"} | Email: {c.InstructorEmail ?? "?"}",
                CourseId = c.CourseId
            })
            .ToList();

        ReportCollectionView.ItemsSource = tentative;
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadReportAsync();
        ReportCollectionView.ItemsSource = _reportItems.OrderBy(r => r.Date).ToList();
    }

    private async void OnReportItemTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is ReportItem item)
        {
            Debug.WriteLine($"[DEBUG] Tapped: {item.Title}");

            if (item.AssessmentId > 0)
            {
                await Shell.Current.GoToAsync($"AssessmentDetailPage?assessmentId={item.AssessmentId}&courseId={item.CourseId}");
            }
            else if (item.CourseId > 0)
            {
                await Shell.Current.GoToAsync($"CourseDetailPage?courseId={item.CourseId}");
            }
            else if (item.TermId > 0)
            {
                await Shell.Current.GoToAsync($"TermDetailPage?termId={item.TermId}");
            }
            else
            {
                Debug.WriteLine("[DEBUG] No navigation route matched.");
            }
        }
        else
        {
            Debug.WriteLine("[DEBUG] Tap handler could not resolve ReportItem.");
        }
    }

}
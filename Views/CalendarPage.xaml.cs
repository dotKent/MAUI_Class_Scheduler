using MAUI_Class_Tracker.Models;
using MAUI_Class_Tracker.Services;
using MAUI_Class_Tracker.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MAUI_Class_Tracker.Views;

public partial class CalendarPage : ContentPage
{
    private readonly DataService _dataService;

    public ObservableCollection<CalendarTermViewModel> CalendarItems { get; set; } = new();

    public Command OpenFlyoutCommand => new(() => Shell.Current.FlyoutIsPresented = true);

    public CalendarPage(DataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCalendarItemsAsync();
    }

    private async Task LoadCalendarItemsAsync()
    {
        CalendarItems.Clear();

        var terms = await _dataService.GetTermsAsync();
        var orderedTerms = terms.OrderByDescending(t => t.StartDate);

        foreach (var term in terms)
        {
            var calendarTerm = new CalendarTermViewModel
            {
                Term = term
            };

            var courses = await _dataService.GetCoursesByTermIdAsync(term.TermId);
            var orderedCourses = courses.OrderBy(c => c.StartDate);

            foreach (var course in courses)
            {
                var assessments = await _dataService.GetAssessmentsForCourseAsync(course.CourseId);
                var orderedAssessments = assessments.OrderBy(a => a.DueDate).ToList();
                calendarTerm.Courses.Add(new CalendarCourseViewModel
                {
                    Course = course,
                    Assessments = assessments
                });
            }

            CalendarItems.Add(calendarTerm);
        }
    }
}


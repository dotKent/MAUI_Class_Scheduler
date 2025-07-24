using MAUI_Class_Tracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Class_Tracker.ViewModels
{
    public class CalendarCourseViewModel
    {
        public string Title => Course.Title;
        public DateTime StartDate => Course.StartDate;
        public DateTime EndDate => Course.EndDate;
        public string InstructorName => Course.InstructorName;

        public Course Course { get; set; }
        public List<Assessment> Assessments { get; set; } = new();
    }
}

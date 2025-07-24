using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Class_Tracker.Models
{
    public class Course
    {
        [PrimaryKey, AutoIncrement]
        public int CourseId { get; set; }

        public string? Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DateRange => $"{StartDate:MMM dd} - {EndDate:MMM dd}";
        public string? Status { get; set; }

        public string? InstructorName { get; set; }
        public string? InstructorPhone { get; set; }
        public string? InstructorEmail { get; set; }
        public string? Notes { get; set; } = string.Empty;

        public int UserId { get; set; }
        [Indexed]
        public int TermId { get; set; }

        }
}

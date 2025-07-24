using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Class_Tracker.Models
{
    public class Term
    {
        [PrimaryKey, AutoIncrement]
        public int TermId { get; set; }

        public string Title { get; set; }
        public string? Notes { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int UserId { get; set; }
        public int CourseId { get; set; }

    }
}


using MAUI_Class_Tracker.Models;
using System.Diagnostics;
using System.IO;
using SQLite;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Maui.Storage;

namespace MAUI_Class_Tracker.Services
{
    public class DataService
    {
        private SQLiteAsyncConnection _database;


        public async Task InitializeAsync()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "wgu_tracker.db");
            if (_database != null)
                return;

            //////////// DEV ONLY: force delete to fix schema mismatch
            //if (File.Exists(dbPath))
            //{
            //    File.Delete(dbPath);
            //    Debug.WriteLine("[DEBUG] Deleted old DB to reset schema.");
            //}

            _database = new SQLiteAsyncConnection(dbPath);

            try
            {
                await _database.CreateTableAsync<User>();
                await _database.CreateTableAsync<Term>();
                await _database.CreateTableAsync<Course>();
                await _database.CreateTableAsync<Assessment>();
                Debug.WriteLine("[DEBUG] All tables created or verified.");

                var existingTerms = await _database.Table<Term>().ToListAsync();
                int userId = Preferences.Get("UserId", 0);
                if (userId == 0)
                {
                    Debug.WriteLine("[SeedDataAsync] No valid user ID found — skipping seed.");
                    return;
                }
                if (!existingTerms.Any())
                {
                    await _database.InsertAsync(new Term
                    {
                        Title = "Fall 2025",
                        StartDate = new DateTime(2025, 8, 15),
                        EndDate = new DateTime(2025, 12, 15),
                        UserId = userId
                    });
                    Debug.WriteLine("[DEBUG] Seeded default term.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Could not read from database: {ex}");
            }
        }

        // ----------- CREDENTIALS -------------

        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        public async Task SaveUserAsync(User user)
        {
            await _database.InsertAsync(user);

            // Fetch the inserted row by username (which should be unique)
            var created = await _database.Table<User>()
                .Where(u => u.Username == user.Username)
                .OrderByDescending(u => u.Id)
                .FirstOrDefaultAsync();

            if (created != null)
                user.Id = created.Id;
        }

        public async Task<User?> AuthenticateUserAsync(string username, string password)
        {
            if (_database == null)
                await InitializeAsync();

            string hash = DataService.HashPassword(password);
            Debug.WriteLine($"[DEBUG] Login attempt: {username}, hash: {hash}");

            var matchedUser = await _database.Table<User>()
                .FirstOrDefaultAsync(u => u.Username == username && u.HashedPassword == hash);

            if (matchedUser == null)
                Debug.WriteLine($"[DEBUG] No user found matching that hash.");
            else
                Debug.WriteLine($"[DEBUG] Authenticated user: {matchedUser.Username}, Id={matchedUser.Id}");

            return matchedUser;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            await InitializeAsync();
            int userId = Preferences.Get("UserId", 0);

            return await _database.Table<User>()
                          .Where(t => t.Id == userId)
                          .ToListAsync();
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            if (_database == null)
                await InitializeAsync();
            return await _database.Table<User>()
                                  .FirstOrDefaultAsync(u => u.Username == username);
        }


        // ----------- SEARCH -------------------

        public async Task<List<string>> SearchAllAsync(string keyword)
        {
            keyword = keyword?.ToLower().Trim();

            var userId = Preferences.Get("UserId", 0);

            var termResults = await _database.Table<Term>()
                .Where(t => t.UserId == userId && t.Title.ToLower().Contains(keyword))
                .ToListAsync();

            var courseResults = await _database.Table<Course>()
                .Where(c => c.Title.ToLower().Contains(keyword))
                .ToListAsync();

            var assessmentResults = await _database.Table<Assessment>()
                .Where(a => a.Name.ToLower().Contains(keyword))
                .ToListAsync();

            // Combine them into a flat result for simplicity
            var results = new List<string>();
            results.AddRange(termResults.Select(t => $"Term: {t.Title}"));
            results.AddRange(courseResults.Select(c => $"Course: {c.Title}"));
            results.AddRange(assessmentResults.Select(a => $"Assessment: {a.Name}"));

            return results;
        }


        // ----------- TERM CRUD -------------

        public async Task<Term> GetTermByIdAsync(int id)
        {
            if (_database == null)
                await InitializeAsync();

            return await _database.Table<Term>().Where(t => t.TermId == id).FirstOrDefaultAsync();
        }


        public async Task<List<Term>> GetTermsAsync()
        {
            try
            {
                if (_database == null)
                    await InitializeAsync();

                int userId = Preferences.Get("UserId", 0);
                var terms = await _database.Table<Term>()
                                           .Where(t => t.UserId == userId)
                                           .ToListAsync();

                Debug.WriteLine($"[DEBUG] Loaded {terms.Count} terms for UserId={userId}");
                return terms ?? new List<Term>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to load terms: {ex}");
                return new List<Term>();
            }
        }
        public async Task<Term> SaveTermAsync(Term term)
        {
            if (_database == null)
                await InitializeAsync();

            if (term == null)
            {
                Debug.WriteLine("[SaveTermAsync] Term is null.");
                return null!;
            }

            if (string.IsNullOrWhiteSpace(term.Title))
            {
                Debug.WriteLine("[SaveTermAsync] Title is empty — not saving.");
                return term;
            }

            term.UserId = Preferences.Get("UserId", 0);

            var existingTerms = await _database.Table<Term>()
                                               .Where(t => t.UserId == term.UserId)
                                               .ToListAsync();

            if (existingTerms.Any(t => t.Title.Trim().Equals(term.Title.Trim(), StringComparison.OrdinalIgnoreCase) && t.TermId != term.TermId))
            {
                Debug.WriteLine($"[SaveTermAsync] Duplicate title detected for '{term.Title}'. Not saving.");
                return term;
            }

            if (term.TermId != 0)
            {
                await _database.UpdateAsync(term);
                Debug.WriteLine($"[SaveTermAsync] Updated term: {term.Title}, Id={term.TermId}, UserId={term.UserId}");
            }
            else
            {
                await _database.InsertAsync(term);
                Debug.WriteLine($"[SaveTermAsync] Inserted new term: {term.Title}, New Id={term.TermId}, UserId={term.UserId}");
            }

            // Optional: verify terms in DB
            var verifyTerms = await _database.Table<Term>().ToListAsync();
            foreach (var t in verifyTerms)
            {
                Debug.WriteLine($"[VERIFY] Term in DB: Id={t.TermId}, Title={t.Title}, UserId={term.UserId}");
            }

            return term;
        }

        public async Task DeleteTermAsync(Term term)
        {
            if (_database == null)
                await InitializeAsync();

            var courses = await GetCoursesByTermIdAsync(term.TermId);
            foreach (var course in courses)
            {
                await DeleteCourseAsync(course);
            }


            await _database.DeleteAsync(term);
            Debug.WriteLine($"[DEBUG] Deleted Term: {term.Title}, Id={term.TermId}, UserId={term.UserId}");
        }

        public async Task DeleteAllTermsAsync() // Full reset
        {
            if (_database == null)
                await InitializeAsync();

            Debug.WriteLine("[DEBUG] Starting FULL DELETE of Terms, Courses, Assessments");

            await _database.DeleteAllAsync<Assessment>();
            await _database.DeleteAllAsync<Course>();
            await _database.DeleteAllAsync<Term>();

            Debug.WriteLine("[DEBUG] All Terms, Courses, Assessments deleted.");

            var remainingTerms = await _database.Table<Term>().ToListAsync();
            Debug.WriteLine($"[DEBUG] Remaining Terms after delete: {remainingTerms.Count}");
        }

        // ----------- COURSE CRUD -------------

        public async Task<Course> GetCourseByIdAsync(int id)
        {
            if (_database == null)
                await InitializeAsync();

            return await _database.FindAsync<Course>(id);
        }

        public async Task<List<Course>> GetCoursesByTermIdAsync(int termId)
        {
            if (_database == null)
                await InitializeAsync();

            int userId = Preferences.Get("UserId", 0);
            return await _database.Table<Course>()
                                  .Where(c => c.TermId == termId && c.UserId == userId)
                                  .ToListAsync();
        }

        public async Task<List<Course>> GetCoursesAsync()
        {
            try
            {
                if (_database == null)
                    await InitializeAsync();

                int userId = Preferences.Get("UserId", 0);
                var courses = await _database.Table<Course>()
                                             .Where(c => c.UserId == userId)
                                             .ToListAsync();

                Debug.WriteLine($"[DEBUG] Loaded {courses.Count} courses for UserId={userId}");
                return courses ?? new List<Course>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to load courses: {ex}");
                return new List<Course>();
            }
        }

        public async Task<bool> SaveCourseAsync(Course course)
        {

            if (_database == null)
                await InitializeAsync();

            if (course == null)
            {
                Debug.WriteLine("[SaveCourseAsync] Course is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(course.Title))
            {
                Debug.WriteLine("[SaveCourseAsync] Title is missing — not saving.");
                return false;
            }

            course.UserId = Preferences.Get("UserId", 0);

            var existingCourses = await _database.Table<Course>()
                                                 .Where(c => c.TermId == course.TermId && c.UserId == course.UserId)
                                                 .ToListAsync();

            // Check 6-course limit
            if (course.CourseId == 0 && existingCourses.Count >= 6)
            {
                Debug.WriteLine($"[SaveCourseAsync] Cannot add more than 6 courses to TermId={course.TermId}.");
                return false;
            }

            // Check for duplicate title
            if (existingCourses.Any(c => c.Title.Trim().Equals(course.Title.Trim(), StringComparison.OrdinalIgnoreCase)
                                      && c.CourseId != course.CourseId))
            {
                Debug.WriteLine($"[SaveCourseAsync] Duplicate course title in TermId={course.TermId}: '{course.Title}'. Not saving.");
                return false;
            }

            if (course.CourseId != 0)
            {
                await _database.UpdateAsync(course);
                Debug.WriteLine($"[SaveCourseAsync] Updated course: {course.Title}, Id={course.CourseId}, UserId={course.UserId}");
            }
            else
            {
                course.CourseId = await _database.InsertAsync(course);
                Debug.WriteLine($"[SaveCourseAsync] Inserted new course: {course.Title}, Id={course.CourseId}, UserId={course.UserId}");
            }
            return true;
        }

        public async Task DeleteCourseAsync(Course course)
        {
            if (_database == null)
                await InitializeAsync();

            // First delete related assessments
            var assessments = await GetAssessmentsForCourseAsync(course.CourseId);
            foreach (var assessment in assessments)
            {
                await DeleteAssessmentAsync(assessment);
            }

            // Now delete course
            await _database.DeleteAsync(course);
            Debug.WriteLine($"[DEBUG] Deleted Course: {course.Title}, Id={course.CourseId}, UserId={course.UserId}");
        }

        // ----------- ASSESSMENT CRUD -------------

        public async Task<Assessment> GetAssessmentByIdAsync(int id)
        {
            if (_database == null)
                await InitializeAsync();

            return await _database.FindAsync<Assessment>(id);
        }

        public async Task<List<Assessment>> GetAssessmentsForCourseAsync(int courseId)
        {
            int userId = Preferences.Get("UserId", 0);
            if (_database == null)
                await InitializeAsync();

            return await _database.Table<Assessment>()
                                  .Where(a => a.CourseId == courseId && a.UserId == userId)
                                  .ToListAsync();
        }

        public async Task<Assessment?> GetAssessmentByTypeAsync(int courseId, string type)
        {
            int userId = Preferences.Get("UserId", 0);
            return await _database.Table<Assessment>()
                .Where(a => a.CourseId == courseId && a.Type == type && a.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Assessment>> GetAssessmentsAsync()
        {
            try
            {
                if (_database == null)
                    await InitializeAsync();

                int userId = Preferences.Get("UserId", 0);
                var assessments = await _database.Table<Assessment>()
                                                 .Where(a => a.UserId == userId)
                                                 .ToListAsync();

                Debug.WriteLine($"[DEBUG] Loaded {assessments.Count} assessments for UserId={userId}");
                return assessments ?? new List<Assessment>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to load assessments: {ex}");
                return new List<Assessment>();
            }
        }

        public async Task<Assessment> SaveAssessmentAsync(Assessment assessment)
        {
            if (_database == null)
                await InitializeAsync();

            if (string.IsNullOrWhiteSpace(assessment.Name))
            {
                Debug.WriteLine("[SaveAssessmentAsync] Assessment name is empty — not saving.");
                return assessment;
            }

            assessment.UserId = Preferences.Get("UserId", 0);

            if (assessment.AssessmentId != 0)
            {
                await _database.UpdateAsync(assessment);
                Debug.WriteLine($"[SaveAssessmentAsync] Updated assessment: {assessment.Name}, Id={assessment.AssessmentId}, UserId={assessment.UserId}");
            }
            else
            {
                assessment.AssessmentId = await _database.InsertAsync(assessment);
                Debug.WriteLine($"[SaveAssessmentAsync] Inserted new assessment: {assessment.Name}, Id={assessment.AssessmentId}, UserId={assessment.UserId}");
            }

            return assessment;
        }

        public async Task DeleteAssessmentAsync(Assessment assessment)
        {
            if (_database == null)
                await InitializeAsync();

            await _database.DeleteAsync(assessment);
            Debug.WriteLine($"[DEBUG] Deleted Assessment: {assessment.Name}, Id={assessment.AssessmentId}, UserId={assessment.UserId}");
        }

        // ----------- SEED DATA -------------

        public async Task SeedDataAsync()
        {
            if (_database == null)
                await InitializeAsync();

            var terms = await _database.Table<Term>().ToListAsync();
            if (terms.Any())
                return;

            // Seed Term
            var term = new Term
            {
                Title = "Spring Term",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(4),
                UserId = Preferences.Get("UserId", 0)
            };
            await SaveTermAsync(term);

            // Seed Course
            var course = new Course
            {
                TermId = term.TermId,
                Title = "Mobile App Dev",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(2),
                Status = "In Progress",
                InstructorName = "Anika Patel",
                InstructorPhone = "555-123-4567",
                InstructorEmail = "anika.patel@strimeuniversity.edu"
            };
            await SaveCourseAsync(course);

            // Seed Assessments
            var objective = new Assessment
            {
                CourseId = course.CourseId,
                Name = "Objective Assessment",
                Type = "Objective",
                DueDate = DateTime.Now.AddMonths(1)
            };
            await SaveAssessmentAsync(objective);

            var performance = new Assessment
            {
                CourseId = course.CourseId,
                Name = "Performance Assessment",
                Type = "Performance",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(14)
            };
            await SaveAssessmentAsync(performance);

            Debug.WriteLine("[DEBUG] Seed data created.");
        }
    }
}

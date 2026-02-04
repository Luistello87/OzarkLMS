using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OzarkLMS.Data;
using OzarkLMS.Models;
using Microsoft.AspNetCore.Authorization;

namespace OzarkLMS.Controllers
{
    [Authorize]
    public class AssignmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AssignmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Assignments/Create?courseId=5
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> Create(int? courseId)
        {
            if (courseId == null) return NotFound();

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            if (User.IsInRole("instructor"))
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                if (course.InstructorId != userId) return Forbid();
            }

            ViewBag.Course = course;
            return View(new Assignment { CourseId = course.Id });
        }

        // GET: Assignments/Edit/5 (To add questions)
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return NotFound();

            if (User.IsInRole("instructor"))
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                if (assignment.Course.InstructorId != userId) return Forbid();
            }

            if (assignment == null) return NotFound();

            return View(assignment);
        }

        // POST: Assignments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> Create([Bind("CourseId,Title,DueDate,Type,MaxAttempts,Description,SubmissionType,Points")] Assignment assignment, IFormFile? attachment)
        {
            if (ModelState.IsValid)
            {
                // Ensure Date is UTC for Postgres
                assignment.DueDate = DateTime.SpecifyKind(assignment.DueDate, DateTimeKind.Utc);
                
                // Security Check
                if (User.IsInRole("instructor"))
                {
                     var courseCheck = await _context.Courses.FindAsync(assignment.CourseId);
                     if(courseCheck == null) return NotFound();
                     
                     var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                     if (courseCheck.InstructorId != userId) return Forbid();
                }
                
                // Handle Attachment
                if (attachment != null && attachment.Length > 0)
                {
                     var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                     if(!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                     var fileName = Guid.NewGuid() + Path.GetExtension(attachment.FileName);
                     var filePath = Path.Combine(uploads, fileName);
                     using (var stream = new FileStream(filePath, FileMode.Create))
                     {
                         await attachment.CopyToAsync(stream);
                     }
                     assignment.AttachmentUrl = "/uploads/" + fileName;
                }

                _context.Add(assignment);
                await _context.SaveChangesAsync();
                
                // If it is a quiz, redirect to Edit page to add questions
                if(assignment.Type == "quiz")
                {
                     return RedirectToAction(nameof(Edit), new { id = assignment.Id });
                }

                return RedirectToAction("Details", "Courses", new { id = assignment.CourseId, tab = "assignments" });
            }
            
            var course = await _context.Courses.FindAsync(assignment.CourseId);
            ViewBag.Course = course;
            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> AddQuestion([FromForm] int assignmentId, [FromForm] string text, [FromForm] int points)
        {
            var question = new Question { AssignmentId = assignmentId, Text = text, Points = points };
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = assignmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> AddOption([FromForm] int questionId, [FromForm] int assignmentId, [FromForm] string text, [FromForm] bool isCorrect)
        {
            // Enforce Single Correct Option Logic
            if (isCorrect)
            {
                var existingOptions = await _context.QuestionOptions
                    .Where(o => o.QuestionId == questionId && o.IsCorrect)
                    .ToListAsync();
                
                foreach (var opt in existingOptions)
                {
                    opt.IsCorrect = false;
                }
            }

            var option = new QuestionOption { QuestionId = questionId, Text = text, IsCorrect = isCorrect };
            _context.QuestionOptions.Add(option);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = assignmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id, int assignmentId)
        {
            var q = await _context.Questions.FindAsync(id);
            if (q != null) 
            {
                _context.Questions.Remove(q);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Edit), new { id = assignmentId });
        }
        // GET: Assignments/Take/5
        public async Task<IActionResult> Take(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return NotFound();

            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Ensure user is logged in
        public async Task<IActionResult> SubmitQuiz(int assignmentId, Dictionary<int, int> answers)
        {
             var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
             if (userIdClaim == null) return Unauthorized();
             var userId = int.Parse(userIdClaim.Value);
             
             // Null check answers just in case
             if (answers == null) answers = new Dictionary<int, int>();

             // Simple Auto-Grading Logic
             int score = 0;
             var questions = await _context.Questions.Include(q => q.Options).Where(q => q.AssignmentId == assignmentId).ToListAsync();
             
             foreach(var q in questions)
             {
                 if (answers.TryGetValue(q.Id, out int selectedOptionId))
                 {
                     var correctOption = q.Options.FirstOrDefault(o => o.IsCorrect);
                     if (correctOption != null && correctOption.Id == selectedOptionId)
                     {
                         score += q.Points;
                     }
                 }
             }
             
             var submission = new Submission
             {
                 AssignmentId = assignmentId,
                 StudentId = userId,
                 Score = score,
                 Content = "Quiz Submission (Auto-Graded)",
                 SubmittedAt = DateTime.UtcNow
             };
             
             _context.Submissions.Add(submission);
             await _context.SaveChangesAsync();
             
             var assignment = await _context.Assignments.FindAsync(assignmentId);
             if (assignment != null)
             {
                 return RedirectToAction("Details", "Courses", new { id = assignment.CourseId, tab = "grades" });
             }
             return RedirectToAction("Index", "Courses");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, string? content, IFormFile? file)
        {
             var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
             if (userIdClaim == null) return Unauthorized();
             var userId = int.Parse(userIdClaim.Value);
             
             string? fileUrl = null;
             
            // Handle File Upload
             if (file != null && file.Length > 0)
             {
                 // ensure uploads folder
                 var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                 if(!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                 var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                 var filePath = Path.Combine(uploads, fileName);
                 using (var stream = new FileStream(filePath, FileMode.Create))
                 {
                     await file.CopyToAsync(stream);
                 }
                 fileUrl = "/uploads/" + fileName;
             }

             var submission = new Submission
             {
                 AssignmentId = assignmentId,
                 StudentId = userId,
                 Content = content ?? "",
                 AttachmentUrl = fileUrl,
                 SubmittedAt = DateTime.UtcNow
             };

             _context.Submissions.Add(submission);
             await _context.SaveChangesAsync();

             var assignment = await _context.Assignments.FindAsync(assignmentId);
             if (assignment != null)
             {
                 return RedirectToAction("Details", "Courses", new { id = assignment.CourseId, tab = "assignments" });
             }
             return RedirectToAction("Index", "Courses");
        }

        // GET: Assignments/Submissions/5
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> Submissions(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null) return NotFound();

            var submissions = await _context.Submissions
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == id)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            ViewBag.Submissions = submissions;
            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin, instructor")]
        public async Task<IActionResult> GradeSubmission(int submissionId, int score, string? feedback)
        {
            var submission = await _context.Submissions.FindAsync(submissionId);
            if (submission != null)
            {
                submission.Score = score;
                submission.Feedback = feedback;
                await _context.SaveChangesAsync();
                
                return RedirectToAction(nameof(Submissions), new { id = submission.AssignmentId });
            }
            return NotFound();
        }
    }
}

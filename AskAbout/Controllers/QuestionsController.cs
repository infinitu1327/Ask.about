using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AskAbout.Data;
using AskAbout.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using AskAbout.ViewModels;
using AskAbout.ViewModels.Questions;

namespace AskAbout.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostingEnvironment _appEnvironment;
        private readonly UserManager<User> _userManager;

        public QuestionsController(ApplicationDbContext context,
            IHostingEnvironment appEnvironment,
            UserManager<User> userManager)
        {
            _context = context;
            _appEnvironment = appEnvironment;
            _userManager = userManager;
        }

        // GET: Questions
        public async Task<IActionResult> Index()
        {
            List<Like> likes = await _context.Likes
                .Include(l => l.User)
                .ToListAsync();
            return View(await _context.Questions
                .Include(q => q.Topic)
                .Include(q => q.User)
                .Include(q => q.Replies)
                .Include(q => q.Likes)
                    .ThenInclude(l => l.User)
                .AsNoTracking()
                .ToListAsync());
        }

        public async Task<IActionResult> Recent()
        {
            return View("Index", await _context.Questions
                .Include(q => q.Topic)
                .Include(q => q.User)
                .Include(q => q.Replies)
                .Include(q => q.Likes)
                    .ThenInclude(l => l.User)
                .OrderByDescending(q => q.Date)
                .AsNoTracking()
                .ToListAsync());
        }

        public async Task<IActionResult> Popular()
        {
            return View("Index", await _context.Questions
                .Include(q => q.Topic)
                .Include(q => q.User)
                .Include(q => q.Replies)
                .Include(q => q.Likes)
                    .ThenInclude(l => l.User)
                .OrderByDescending(q => q.Likes.Where(l => l.IsLiked == true).Count() - q.Likes.Where(l => l.IsLiked == false).Count() + q.Replies.Count * 2)
                .AsNoTracking()
                .ToListAsync());
        }

        // GET: Questions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.Topic)
                .Include(q => q.Likes)
                .Include(q => q.Replies)
                    .ThenInclude(r => r.User)
                .Include(q => q.Replies)
                    .ThenInclude(r => r.Comments)
                .SingleOrDefaultAsync(m => m.Id == id);

            if (question == null)
            {
                return NotFound();
            }

            DetailsQuestionViewModel model = new DetailsQuestionViewModel()
            {
                Question = question
            };

            return View(model);
        }

        // GET: Questions/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            CreateQuestionViewModel model = new CreateQuestionViewModel()
            {
                Topics = await _context.Topics.AsNoTracking().ToListAsync()
            };
            return View(model);
        }

        // POST: Questions/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Text,Attachment")] Question question, string topic, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                question.Date = DateTime.Now;
                question.User = await _userManager.GetUserAsync(HttpContext.User);
                question.Topic = await _context.Topics.SingleOrDefaultAsync(t => t.Name == topic);
                Rating rating = new Rating()
                {
                    User = question.User,
                    Topic = question.Topic
                };

                if (file != null)
                {
                    string fileName = DateTime.Now.Ticks.ToString() + ".jpg";
                    string dirPath = Path.Combine(_appEnvironment.WebRootPath, "Uploads");
                    Directory.CreateDirectory(dirPath);
                    string path = Path.Combine(dirPath, fileName);
                    using (var stream = System.IO.File.Create(path))
                    {
                        await file.CopyToAsync(stream);
                        question.Attachment = fileName;
                    }
                }

                _context.Add(question);
                _context.Add(rating);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(question);
        }

        // GET: Questions/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.Likes)
                .Include(q => q.Replies)
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.Id == id);
            if (question == null)
            {
                return NotFound();
            }
            User user = await _userManager.GetUserAsync(HttpContext.User);
            if (question.User.Equals(user) || question.Likes.Count != 0 || question.Replies.Count != 0)
            {
                return NotFound();
            }

            return View(question);
        }

        // POST: Questions/Edit/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Text,Attachment")] Question question, IFormFile file)
        {
            if (id != question.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    question.Date = DateTime.Now;

                    if (file != null)
                    {
                        string fileName = DateTime.Now.Ticks.ToString() + ".jpg";
                        string dirPath = Path.Combine(_appEnvironment.WebRootPath, "Uploads");
                        Directory.CreateDirectory(dirPath);
                        string path = Path.Combine(dirPath, fileName);
                        using (var stream = System.IO.File.Create(path))
                        {
                            await file.CopyToAsync(stream);
                            question.Attachment = fileName;
                        }
                    }

                    _context.Update(question);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(question.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            return View(question);
        }

        // GET: Questions/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Question question = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.Replies)
                .Include(q => q.Likes)
                .SingleOrDefaultAsync(m => m.Id == id);

            if (question == null)
            {
                return NotFound();
            }

            User user = await _userManager.GetUserAsync(HttpContext.User);

            if (!question.User.Equals(user))
            {
                return NotFound();
            }            

            if (question.Attachment != null)
            {
                System.IO.File.Delete(Path.Combine(_appEnvironment.WebRootPath, "Uploads", question.Attachment));
            }

            foreach (Reply reply in question.Replies)
            {
                if (reply.Attachment != null)
                {
                    System.IO.File.Delete(Path.Combine(_appEnvironment.WebRootPath, "Uploads", reply.Attachment));
                }

                _context.Replies.Remove(reply);
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> Search(string title)
        {
            if (title != null)
            {
                var results = await _context.Questions
                                .Where(q => q.Title.ToLower().Contains(title.ToLower()))
                                .ToListAsync();

                return Json(new { results });
            }
            else
            {
                return Json(new { });
            }            
        }

        // GET: Questions/Like/5
        [HttpGet]
        [Authorize]
        public async Task<StatusCodeResult> Like(int id)
        {
            User user = await _userManager.GetUserAsync(HttpContext.User);
            Question question = await GetQuestion(id);
            Like like = await GetLike(user, question);
            Rating rating = await GetRating(user, question);

            if (like != null)
            {
                if (like.IsLiked == null)
                {
                    like.IsLiked = true;
                    rating.Amount++;
                }
                else
                {
                    if (like.IsLiked == false)
                    {
                        like.IsLiked = true;
                        rating.Amount += 2;
                    }
                    else
                    {
                        return StatusCode(404);
                    }
                }
            }
            else
            {
                like = new Like()
                {
                    IsLiked = true,
                    User = user,
                    Question = question
                };

                _context.Likes.Add(like);
                rating.Amount++;
            }

            await _context.SaveChangesAsync();
            return StatusCode(200);
        }

        // GET: Questions/Dislike/5
        [HttpGet]
        [Authorize]
        public async Task<StatusCodeResult> Dislike(int id)
        {
            User user = await _userManager.GetUserAsync(HttpContext.User);
            Question question = await GetQuestion(id);
            Like like = await GetLike(user, question);
            Rating rating = await GetRating(user, question);

            if (like != null)
            {
                if (like.IsLiked == null)
                {
                    like.IsLiked = false;
                    rating.Amount--;
                }
                else
                {
                    if (like.IsLiked == true)
                    {
                        like.IsLiked = false;
                        rating.Amount -= 2;
                    }
                    else
                    {
                        return StatusCode(404);
                    }
                }
            }
            else
            {
                like = new Like()
                {
                    IsLiked = false,
                    User = user,
                    Question = question
                };

                _context.Likes.Add(like);
                rating.Amount--;
            }

            await _context.SaveChangesAsync();
            return StatusCode(200);
        }

        // GET: Questions/ResetLike/5
        [HttpGet]
        [Authorize]
        public async Task<StatusCodeResult> ResetLike(int id)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
            {
                return StatusCode(404);
            }

            Question question = await GetQuestion(id);
            Like like = await GetLike(user, question);
            Rating rating = await GetRating(user, question);

            if (like == null)
            {
                return StatusCode(404);
            }

            if (like.IsLiked == true)
            {
                rating.Amount--;
            }
            else
            {
                rating.Amount++;
            }

            like.IsLiked = null;

            await _context.SaveChangesAsync();
            return StatusCode(200);
        }

        private async Task<Question> GetQuestion(int id)
        {
            return await _context.Questions
                .Include(q => q.Topic)
                .Include(q => q.User)
                .SingleOrDefaultAsync(q => q.Id == id);
        }

        private async Task<Like> GetLike(User user,Question question)
        {
            return await _context.Likes
                .SingleOrDefaultAsync(l => l.User.Equals(user) && l.Question.Equals(question));
        }

        private async Task<Rating> GetRating(User user,Question question)
        {
            return await _context.Rating
                .SingleOrDefaultAsync(r => r.User.Equals(question.User) && r.Topic.Equals(question.Topic));
        }

        private bool LikeExists(int id)
        {
            return _context.Likes.Any(e => e.Id == id);
        }

        private bool QuestionExists(int id)
        {
            return _context.Questions.Any(e => e.Id == id);
        }
    }
}

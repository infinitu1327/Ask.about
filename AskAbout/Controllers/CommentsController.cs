using System.Threading.Tasks;
using AskAbout.Data;
using AskAbout.Models;
using AskAbout.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskAbout.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ICommentServices _commentServices;
        private readonly ILikeServices _likeServices;

        public CommentsController(ApplicationDbContext context,
            IHostingEnvironment appEnvironment,
            UserManager<User> userManager, ICommentServices commentServices, ILikeServices likeServices)
        {
            _context = context;
            _userManager = userManager;
            _commentServices = commentServices;
            _likeServices = likeServices;
        }

        // POST: Comments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Comment comment,
            IFormFile file)
        {
            int qid = await _commentServices.Create(comment, await _userManager.GetUserAsync(HttpContext.User), file);
            return RedirectToAction("Details", "Questions", new {id = qid});
        }

        // GET: Comments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var comment = await _commentServices.Get(id.Value);
            return View(comment);
        }

        // POST: Comments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,Comment comment, IFormFile file)
        {
            if (id != comment.Id)
                return NotFound();

            int qid = await _commentServices.Edit(comment, file);
            return RedirectToAction("Details", "Questions", new {id = qid});
        }

        // GET: Comments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(HttpContext.User);
            var reply = await _context.Replies.Include(r => r.Question).SingleOrDefaultAsync(r => r.Id == id);

            var comment = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Reply)
                .SingleOrDefaultAsync(c => c.User == user && c.Reply == reply);

            if (comment == null)
                return NotFound();

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Questions", new {id = reply.Question.Id});
        }
        
        // GET: Comments/Like/5
        [HttpGet]
        [Authorize]
        public async Task<StatusCodeResult> Like(int id)
        {
            if (await _likeServices.Like(await _commentServices.Get(id), await _userManager.GetUserAsync(HttpContext.User))) return StatusCode(200);

            return StatusCode(404);
        }

        // GET: Comments/Dislike/5
        [HttpGet]
        [Authorize]
        public async Task<StatusCodeResult> Dislike(int id)
        {
            if (await _likeServices.Dislike(await _commentServices.Get(id), await _userManager.GetUserAsync(HttpContext.User))) return StatusCode(200);
            return StatusCode(404);
        }

        // GET: Comments/ResetLike/5
        [HttpGet]
        [Authorize]
        public async Task<StatusCodeResult> ResetLike(int id)
        {
            await _likeServices.RemoveLike(await _commentServices.Get(id), await _userManager.GetUserAsync(HttpContext.User));
            return StatusCode(200);
        }
    }
}
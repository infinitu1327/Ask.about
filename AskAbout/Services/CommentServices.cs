﻿using System;
using System.IO;
using System.Threading.Tasks;
using AskAbout.Data;
using AskAbout.Models;
using AskAbout.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AskAbout.Services
{
    public class CommentServices : ICommentServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IReplyServices _replyServices;
        private readonly IHostingEnvironment _appEnvironment;
        private readonly IRatingServices _ratingServices;

        public CommentServices(ApplicationDbContext context, IReplyServices replyServices, IHostingEnvironment appEnvironment, IRatingServices ratingServices)
        {
            _context = context;
            _replyServices = replyServices;
            _appEnvironment = appEnvironment;
            _ratingServices = ratingServices;
        }

        public Task<Comment> Get(int id)
        {
            return _context.Comments
                .Include(c => c.Reply)
                    .ThenInclude(r => r.Question)
                        .ThenInclude(q=>q.Topic)
                .Include(c => c.User)
                .SingleOrDefaultAsync(c => c.Id == id);
        }

        public async Task<int> Create(Comment comment, User user, IFormFile file)
        {
            comment.Id = 0;
            comment.User = user;
            comment.Date=DateTime.Now;
            comment.Reply = await _replyServices.Get(comment.Reply.Id);

            var rating = await _context.Rating
                .SingleOrDefaultAsync(r => r.User.Equals(comment.User) && r.Topic.Equals(comment.Reply.Question.Topic));

            if (rating == null)
            {
                await _ratingServices.Create(user, comment.Reply.Question.Topic);
            }

            if (file != null)
            {
                var fileName = DateTime.Now.Ticks + ".jpg";
                var dirPath = Path.Combine(_appEnvironment.WebRootPath, "Uploads");
                Directory.CreateDirectory(dirPath);
                var path = Path.Combine(dirPath, fileName);
                using (Stream stream = File.Create(path))
                {
                    await file.CopyToAsync(stream);
                    comment.Attachment = fileName;
                }
            }

            _context.Add(comment);
            await _context.SaveChangesAsync();
            return comment.Reply.Question.Id;
        }

        public async Task<int> Edit(Comment comment, IFormFile file)
        {
            var dbComment = await Get(comment.Id);

            dbComment.Text = comment.Text;
            dbComment.Date = DateTime.Now;

            if (file != null)
            {
                var fileName = DateTime.Now.Ticks + ".jpg";
                var dirPath = Path.Combine(_appEnvironment.WebRootPath, "Uploads");
                Directory.CreateDirectory(dirPath);
                var path = Path.Combine(dirPath, fileName);
                using (Stream stream = File.Create(path))
                {
                    await file.CopyToAsync(stream);
                    dbComment.Attachment = fileName;
                }
            }

            _context.Update(dbComment);
            await _context.SaveChangesAsync();
            return dbComment.Reply.Question.Id;
        }

        public async Task<int> Delete(int id)
        {
            var comment = await _context.Comments
                .Include(c=>c.Reply).ThenInclude(r=>r.Question)
                .SingleOrDefaultAsync(c => c.Id == id);

            if (comment.Attachment != null)
                File.Delete(Path.Combine(_appEnvironment.WebRootPath, "Uploads", comment.Attachment));

            _context.Remove(comment);
            await _context.SaveChangesAsync();
            return comment.Reply.Question.Id;
        }
    }
}
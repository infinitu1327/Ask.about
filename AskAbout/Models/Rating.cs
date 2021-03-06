﻿namespace AskAbout.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int Amount { get; set; }

        public string UserId { get; set; }
        public int TopicId { get; set; }

        public virtual User User { get; set; }
        public virtual Topic Topic { get; set; }
    }
}
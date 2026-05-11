using System;
using System.Collections.Generic;

namespace WebApplication1.Models
{
    public class Survey
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string CreatedById { get; set; } = null!;
        public ApplicationUser? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public bool AllowRepeatVoting { get; set; } = false;
        public TimeSpan? TimePerQuestion { get; set; }
        public TimeSpan? TimeForWholeSurvey { get; set; }
        public bool IsPublished { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}

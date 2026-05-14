using System.Collections.Generic;

namespace WebApplication1.Models
{
    public enum QuestionType
    {
        SingleChoice = 0,
        MultipleChoice = 1,
        Open = 2
    }

    public class Question
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public Survey Survey { get; set; } = null!;
        public string Text { get; set; } = null!;
        public QuestionType QuestionType { get; set; } = QuestionType.SingleChoice;
        public int Order { get; set; } = 0;
        public bool IsRequired { get; set; } = false;

        public ICollection<Option> Options { get; set; } = new List<Option>();
    }
}

namespace WebApplication1.Models
{
    public class Option
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public Question Question { get; set; } = null!;
        public string Text { get; set; } = null!;
        public int Order { get; set; } = 0;
        public int VotesCount { get; set; } = 0;
    }
}

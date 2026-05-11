using System.Collections.Generic;

namespace WebApplication1.Models.Dtos
{
    public record CreateSurveyDto(string Title, string? Description = null, bool IsPublished = false);

    public record SurveyDto(int Id, string Title, string? Description, bool IsPublished, IEnumerable<QuestionDto> Questions);

    public record CreateQuestionDto(string Text, int QuestionType = 0, bool IsRequired = false);
    public record QuestionDto(int Id, string Text, int QuestionType, bool IsRequired, IEnumerable<OptionDto> Options);

    public record CreateOptionDto(string Text);
    public record OptionDto(int Id, string Text, int Votes);

    public record VoteDto(int QuestionId, int OptionId, string? OpenAnswer = null);
}

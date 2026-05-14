using VotingApi.Domain.Enums;
using VotingApi.Domain.Exceptions;

namespace VotingApi.Domain.Entities;

public class Poll : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public PollStatus Status { get; private set; } = PollStatus.Draft;
    public DateTime? StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }
    public bool AllowMultipleVotes { get; private set; }
    public bool IsAnonymous { get; private set; }

    public User CreatedBy { get; private set; } = null!;

    private readonly List<Question> _questions = new();
    private readonly List<Vote> _votes = new();

    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();
    public IReadOnlyCollection<Vote> Votes => _votes.AsReadOnly();

    private Poll() { }

    public static Poll Create(
        string title,
        string description,
        Guid createdByUserId,
        DateTime? startsAt = null,
        DateTime? endsAt = null,
        bool allowMultipleVotes = false,
        bool isAnonymous = false)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (endsAt.HasValue && startsAt.HasValue && endsAt <= startsAt)
            throw new DomainException("EndsAt must be after StartsAt.");

        return new Poll
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            CreatedByUserId = createdByUserId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            AllowMultipleVotes = allowMultipleVotes,
            IsAnonymous = isAnonymous,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string description, DateTime? startsAt, DateTime? endsAt,
        bool allowMultipleVotes, bool isAnonymous)
    {
        if (Status == PollStatus.Closed)
            throw new DomainException("Cannot update a closed poll.");

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        StartsAt = startsAt;
        EndsAt = endsAt;
        AllowMultipleVotes = allowMultipleVotes;
        IsAnonymous = isAnonymous;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (Status != PollStatus.Draft)
            throw new DomainException("Only draft polls can be published.");
        if (!_questions.Any())
            throw new DomainException("Poll must have at least one question before publishing.");

        Status = PollStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        if (Status == PollStatus.Closed)
            throw new DomainException("Poll is already closed.");

        Status = PollStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsVotingOpen()
    {
        if (Status != PollStatus.Active) return false;
        var now = DateTime.UtcNow;
        if (StartsAt.HasValue && now < StartsAt) return false;
        if (EndsAt.HasValue && now > EndsAt) return false;
        return true;
    }

    public bool HasUserVoted(Guid userId) =>
        _votes.Any(v => v.UserId == userId);
}
namespace JobMatch.Core.Entities;

public class PostingSkill
{
    public int Id { get; set; }
    public Guid PostingId { get; set; }
    public string SkillName { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public bool Required { get; set; }
    public string Evidence { get; set; } = "";
}

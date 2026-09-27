namespace JobMatch.Core.Entities;

public class CvProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string FullText { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<CvSection> Sections { get; set; } = new();
}

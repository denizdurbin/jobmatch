namespace JobMatch.Core.Entities;

public class CvSection
{
    public int Id { get; set; }
    public Guid ProfileId { get; set; }
    public string Heading { get; set; } = "";
    public string Text { get; set; } = "";
    public float[] Vector { get; set; } = [];
}

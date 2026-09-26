namespace JobMatch.Core.Entities;

public enum PostingSource
{
    Manual,
    FranceTravail
}

public enum PostingStatus
{
    Raw,
    Extracted,
    Embedded,
    Failed
}

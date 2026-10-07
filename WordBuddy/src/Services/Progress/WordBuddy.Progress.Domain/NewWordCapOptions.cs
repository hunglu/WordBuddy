namespace WordBuddy.Progress.Domain;

/// <summary>New-word cap by due backlog, bound from <c>Vocabulary:NewWordCap</c> (D-3, all users).</summary>
public sealed class NewWordCapOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Vocabulary:NewWordCap";

    /// <summary>Backlog limit of the first tier. Default 20.</summary>
    public int LowBacklogMax { get; set; } = 20;

    /// <summary>Cap at or below <see cref="LowBacklogMax"/>. Default 10.</summary>
    public int LowBacklogCap { get; set; } = 10;

    /// <summary>Backlog limit of the second tier. Default 40.</summary>
    public int MediumBacklogMax { get; set; } = 40;

    /// <summary>Cap at or below <see cref="MediumBacklogMax"/>. Default 8.</summary>
    public int MediumBacklogCap { get; set; } = 8;

    /// <summary>Backlog limit of the third tier. Default 60.</summary>
    public int HighBacklogMax { get; set; } = 60;

    /// <summary>Cap at or below <see cref="HighBacklogMax"/>. Default 6.</summary>
    public int HighBacklogCap { get; set; } = 6;

    /// <summary>Cap above <see cref="HighBacklogMax"/>. Default 5.</summary>
    public int OverflowCap { get; set; } = 5;
}

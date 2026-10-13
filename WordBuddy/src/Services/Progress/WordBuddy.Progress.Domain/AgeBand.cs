namespace WordBuddy.Progress.Domain;

/// <summary>
/// Age range from the <c>age_band</c> token claim (issued by Identity). Used only to scale grading
/// thresholds. The date of birth never reaches Progress.
/// </summary>
public enum AgeBand
{
    /// <summary>Younger than 7.</summary>
    Under7,

    /// <summary>7 to 9 years.</summary>
    Age7To9,

    /// <summary>10 to 12 years.</summary>
    Age10To12,

    /// <summary>13 to 17 years.</summary>
    Age13To17,

    /// <summary>18 years or older.</summary>
    Adult,
}

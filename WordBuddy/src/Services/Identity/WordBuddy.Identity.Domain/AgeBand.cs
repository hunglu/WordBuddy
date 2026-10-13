namespace WordBuddy.Identity.Domain;

/// <summary>Coarse age range derived from the date of birth. Shared in the <c>age_band</c> token claim instead of the date.</summary>
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

namespace WordBuddy.Progress.Domain;

/// <summary>FSRS rating. Values match FSRS (1–4). Always derived by the server, never sent by the client.</summary>
public enum FsrsRating
{
    /// <summary>Forgotten (wrong answer).</summary>
    Again = 1,

    /// <summary>Recalled with effort (hint or slow).</summary>
    Hard = 2,

    /// <summary>Recalled normally.</summary>
    Good = 3,

    /// <summary>Recalled quickly.</summary>
    Easy = 4
}

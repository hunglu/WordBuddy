namespace WordBuddy.Progress.Domain;

/// <summary>The skill an exercise practises. Logged per review; per-skill mastery comes later (D-6).</summary>
public enum VocabularySkill
{
    /// <summary>Knows what the word means.</summary>
    Meaning,

    /// <summary>Recognises the word when heard.</summary>
    Listening,

    /// <summary>Can spell the word.</summary>
    Spelling,

    /// <summary>Can say the word.</summary>
    Pronunciation,

    /// <summary>Can use the word in context.</summary>
    Usage
}

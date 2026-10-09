Feature: Vocabulary auto-fill
  As a WordBuddy learner
  I want the add-word form to fill in a word for me
  So that I do not have to type the definition by hand

  Scenario: Auto-fill shows sense cards and adds the chosen sense
    Given the learner is logged in
    When they auto-fill the word "serendipity"
    Then they see the auto-filled sense card
    When they add the auto-filled sense
    Then the sense card shows it was added

  Scenario: Auto-fill is unavailable and the manual form still adds the word
    Given the learner is logged in
    When they auto-fill an unknown word
    Then they are told to fill in the form instead
    When they complete the manual form
    Then the unknown word appears in their vocabulary list

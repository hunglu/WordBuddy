Feature: Sharing ownership in the Community Word Pool
  As a WordBuddy learner who shared a word
  I want to see which pool words are mine and be warned before I delete a shared word
  So that I do not lose ownership by accident

  Scenario: Owner sees a "Your word" badge on their own shared word
    Given the learner is logged in
    And the learner has a shared word approved by a moderator
    When they open the Community Word Pool
    Then their shared word shows a "Your word" badge instead of "Add to My List"

  Scenario: Deleting a shared word asks for confirmation and hands the word over
    Given the learner is logged in
    And the learner has a shared word approved by a moderator
    When they delete the shared word from My Vocabulary
    Then a confirmation dialog says the word will be handed over to WordBuddy
    When they confirm the deletion
    Then the shared word is gone from My Vocabulary
    And the shared word is in the Community Word Pool with "Add to My List"

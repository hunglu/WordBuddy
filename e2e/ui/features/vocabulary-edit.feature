Feature: Edit my own vocabulary word
  As a WordBuddy learner
  I want to fix a typo in a word I added
  So that my list and reviews show the right text

  Scenario: Learner fixes a typo in their own private word
    Given the learner is logged in
    And the learner has added a private word with a typo
    When they edit the word and save the corrected text
    Then the corrected word is in their vocabulary list
    And the typo word is gone from their vocabulary list

  Scenario: Edit is not offered on a word that is in review
    Given the learner is logged in
    And the learner has added a private word with a typo
    When they submit the word for sharing
    Then the word has no Edit button

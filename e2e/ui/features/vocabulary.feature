Feature: Personal Vocabulary Builder
  As a WordBuddy learner
  I want to add words to my own vocabulary list and check my recall of them
  So that I can build and track my personal vocabulary practice

  Scenario: Learner adds a word, sees it in their list, and completes a recall check
    Given the learner is logged in
    When they add a new vocabulary word
    Then the word appears in their vocabulary list
    When they start a recall check
    Then they can complete the check and see their result

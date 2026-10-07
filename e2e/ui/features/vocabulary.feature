Feature: Personal Vocabulary Builder
  As a WordBuddy learner
  I want to add words to my own vocabulary list and review them
  So that I can build and track my personal vocabulary practice

  Scenario: Learner adds a word and sees it in their list
    Given the learner is logged in
    When they add a new vocabulary word
    Then the word appears in their vocabulary list

  Scenario: The old recall check address opens the review page
    Given the learner is logged in
    When they open the old recall check address
    Then they land on the review page

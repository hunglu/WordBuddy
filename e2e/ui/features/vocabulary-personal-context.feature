Feature: Personal note on a vocabulary word
  As a WordBuddy learner
  I want to write a private note on where I met a word
  So that I remember the word better

  Scenario: Learner adds a word with a note, edits it and clears it
    Given the learner is logged in
    And the learner has added a word with a note
    Then the word card shows the note
    When they change the note on the word card
    Then the word card shows the changed note
    When they clear the note on the word card
    Then the word card shows no note

  Scenario: The add form tells the learner that the note is private
    Given the learner is logged in
    When they open the My Vocabulary page
    Then the add form says only the learner can see the note

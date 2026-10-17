Feature: Vocabulary challenges
  As a WordBuddy learner
  I want to join a challenge and see the board
  So that I can compare my progress, and a child plays only after a guardian allows it

  # Needs the Progress scheduler running (it makes the Word Climber within a minute of the first start).
  Scenario: Adult joins the Word Climber and sees the board
    Given a new adult learner is logged in for challenges
    When they open the Challenges page
    And they join the "Word Climber" challenge
    Then the "Word Climber" challenge shows as joined
    When they open the board of the "Word Climber" challenge
    Then the board lists them as "you"

  Scenario: Child without guardian consent is asked to involve a guardian
    Given a new child learner with a supporter is logged in for challenges
    When they open the Challenges page
    Then the message "Ask your guardian" is shown
    And no challenge is listed

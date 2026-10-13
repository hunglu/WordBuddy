Feature: Admin lesson authoring
  As a WordBuddy admin
  I want to create, fill and publish vocabulary lessons
  So that learners get new lessons, and children never see words hidden from them

  Scenario: Admin creates a draft lesson, adds a word and publishes it
    Given the admin is logged in
    When they create a lesson titled with a unique name in Lesson authoring
    And they add the word "Dog" to the lesson
    And they publish the lesson
    Then the lesson shows the "Published" status
    And the lesson list shows the lesson as published with 1 word

  Scenario: A child does not see a word that is hidden from children
    Given a published lesson with a child-safe word and a word hidden from children
    When a child opens the lesson
    Then the child sees the child-safe word
    And the child does not see the hidden word

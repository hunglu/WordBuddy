Feature: Sidebar navigation selected state
  As a WordBuddy learner
  I want only the sidebar item for the page I am on to look selected
  So that I always know where I am in the app

  Scenario: Moving from My Vocabulary to Shared Pool selects only Shared Pool
    Given the learner is logged in
    When they click the "My Vocabulary" sidebar item
    And they click the "Shared Pool" sidebar item
    Then only the "Shared Pool" sidebar item is selected

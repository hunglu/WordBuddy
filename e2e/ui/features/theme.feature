Feature: UI theme tokens
  As a WordBuddy learner (child or adult)
  I want a readable theme with dark mode, a visible focus ring and no CSS motion
  So that the app is accessible in every setting

  Scenario: Explicit data-theme="dark" switches the page to the dark palette
    Given the learner is on the login page
    When the document root has data-theme "dark"
    Then the page background is the dark surface colour

  Scenario: OS dark colour scheme switches the page to the dark palette
    Given the learner's system prefers a dark colour scheme
    And the learner is on the login page
    Then the page background is the dark surface colour

  Scenario: Keyboard focus shows a visible ring on the login button
    Given the learner is on the login page
    When they tab to the log in button
    Then the focused element shows a focus ring

  Scenario: Login page has no CSS transitions
    Given the learner is on the login page
    Then no button, link or card has a CSS transition

  Scenario: Lessons page has no CSS transitions
    Given the learner is logged in
    When they open the lessons page
    Then no button, link or card has a CSS transition

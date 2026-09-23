Feature: Login
  As a WordBuddy learner
  I want to log in with my email and password
  So that I can reach my dashboard

  Scenario: Successful login
    Given the learner is on the login page
    When they submit valid credentials
    Then they land on the dashboard

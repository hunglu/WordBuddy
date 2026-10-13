Feature: User profile settings
  As a WordBuddy user
  I want to manage my date of birth and password on my profile
  So that my account stays correct and safe

  Scenario: Adult saves a date of birth
    Given a new "Adult" user is logged in on the profile page
    When they save the date of birth "1990-05-15"
    And they reload the profile page
    Then the date of birth field shows "1990-05-15"

  Scenario: Child saves a date of birth and sees the guardian hint
    Given a new "Child" user is logged in on the profile page
    Then the child hint about asking a guardian is shown
    When they save the date of birth of a 10 year old
    And they reload the profile page
    Then the date of birth field shows that date

  Scenario: A date of birth that contradicts the child age group is rejected
    Given a new "Child" user is logged in on the profile page
    When they save the date of birth "1980-01-01"
    And they reload the profile page
    Then the date of birth field is empty

  Scenario: A future date of birth is blocked
    Given a new "Adult" user is logged in on the profile page
    When they save a date of birth in the future
    Then the profile message "The date cannot be in the future" is shown

  Scenario: Adult changes the password and logs in with the new one
    Given a new "Adult" user is logged in on the profile page
    When they change the password to "NewSecret456!" using the current password
    Then the profile message "Password changed." is shown
    When they log out and log in with the new password
    Then they are logged in

  Scenario: A wrong current password is rejected
    Given a new "Adult" user is logged in on the profile page
    When they change the password to "NewSecret456!" using the wrong current password
    Then a password error is shown and no success message appears

  Scenario: Child sees the linked guardian under My supporters
    Given a new "Child" user with a linked guardian is logged in on the profile page
    Then the My supporters card lists the guardian marked "Guardian"

  Scenario: Adult without supporters sees no My supporters card
    Given a new "Adult" user is logged in on the profile page
    Then the My supporters card is not shown

Feature: Moderator role
  As a WordBuddy admin
  I want to give an adult learner the Moderator role
  So that they help with content work without getting user and role powers

  Scenario: Admin grants and revokes Moderator on the Users and roles page
    Given the admin is logged in
    And moderator role: a new adult account exists
    When moderator role: the admin opens "Users & roles" and searches for that account
    And moderator role: the admin clicks "Grant Moderator"
    Then moderator role: the account row shows the "Moderator" badge
    When moderator role: the admin clicks "Revoke Moderator"
    Then moderator role: the account row shows no "Moderator" badge

  Scenario: Moderator sees the content menu but not Users and roles
    Given moderator role: a new adult account exists
    And moderator role: the admin has granted it Moderator
    And moderator role: that account is logged in
    Then moderator role: the sidebar has the items "Moderation, Lesson authoring, Lexemes, Support links"
    And moderator role: the sidebar has no "Users & roles" item
    When moderator role: they open "/admin/users" directly
    Then moderator role: they are sent back to the home page

  Scenario: Learner sees no admin menu
    Given moderator role: a new adult account exists
    And moderator role: that account is logged in
    Then moderator role: the sidebar has no "Moderation" item
    And moderator role: the sidebar has no "Users & roles" item

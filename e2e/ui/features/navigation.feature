Feature: Sidebar navigation selected state
  As a WordBuddy learner
  I want only the sidebar item for the page I am on to look selected
  So that I always know where I am in the app

  Scenario: Moving from My Vocabulary to Shared Pool selects only Shared Pool
    Given the learner is logged in
    When they click the "My Vocabulary" sidebar item
    And they click the "Shared Pool" sidebar item
    Then only the "Shared Pool" sidebar item is selected

  Scenario Outline: Exactly one sidebar item is selected on <path>
    Given the learner is logged in
    When they open "<path>" directly
    Then only the "<item>" sidebar item is selected

    Examples:
      | path               | item          |
      | /                  | Home          |
      | /lessons           | Lessons       |
      | /vocabulary        | My Vocabulary |
      | /vocabulary/check  | My Vocabulary |
      | /vocabulary/shared | Shared Pool   |
      | /progress          | My Progress   |

  Scenario: Admin on Moderation sees only Moderation selected
    Given the admin is logged in
    When they open "/vocabulary/moderation" directly
    Then only the "Moderation" sidebar item is selected

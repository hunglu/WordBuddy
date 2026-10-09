Feature: Learner dashboard
  As a learner or a supporter
  I want to see how learning is going
  So that retention, streak and struggles are visible without raw data

  Scenario: Learner opens their own dashboard
    Given a new "Adult" dashboard learner is logged in
    When they open their dashboard
    Then the dashboard shows the Retention and Streak cards

  Scenario: Supporter opens the learner dashboard from the Supporters page
    Given a dashboard learner has an active supporter
    And the supporter is logged in
    When the supporter opens the learner dashboard from the Supporters page
    Then the learner dashboard is shown

  Scenario: Dashboard shows an error message when Progress is down
    Given a new "Adult" dashboard learner is logged in
    And the dashboard requests fail
    When they open their dashboard
    Then an error message is shown instead of the dashboard

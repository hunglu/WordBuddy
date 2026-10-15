Feature: Paged lists
  As a WordBuddy learner
  I want long lists to show one page at a time
  So that pages load fast and are easy to scan

  Scenario: My vocabulary shows a pager and moves to page 2
    Given the learner is logged in
    And the learner has more words than fit on one page
    When they open their vocabulary list
    Then the vocabulary pager shows page 1
    When they go to the next vocabulary page
    Then the vocabulary pager shows page 2

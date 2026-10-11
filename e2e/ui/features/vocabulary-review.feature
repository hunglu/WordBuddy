Feature: Daily vocabulary review
  As a WordBuddy learner
  I want to practise my words with short exercises
  So that the app schedules each word from how I actually answer, not from a self-rating

  Scenario: Adult completes a session and the server checks every answer
    Given a new "Adult" learner with 5 words is logged in
    When they open the review page
    And they answer every exercise correctly
    Then the session summary shows their answers
    And no self-rating was shown

  Scenario: A wrong answer is judged by the server and the word comes back
    Given a new "Adult" learner with 4 words is logged in
    When they open the review page
    And they answer the first exercise wrongly
    Then the server says the answer was wrong and shows the right word
    And the same number of words is still left and none is counted correct

  Scenario: Each exercise type renders from the server exercise
    Given a new "Adult" learner with 2 words is logged in
    And the exercise and review replies come from a stubbed server
    When they open the review page
    And they answer every stubbed exercise correctly
    Then they saw picture, listening and typing exercises
    And the session summary shows their answers

  Scenario: Child session skips words the server cannot serve and stops at the time cap
    Given a word shared by an adult and approved as not visible to children
    And a new "Child" learner with 4 words is logged in
    And the review session also lists the hidden shared word
    When they open the review page with a controlled clock
    Then the hidden shared word is never shown
    When 10 minutes pass
    Then a "nearly done" notice is shown
    When 5 more minutes pass
    Then the session stops with a time's up summary

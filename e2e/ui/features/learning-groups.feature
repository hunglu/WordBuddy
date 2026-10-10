Feature: Learning groups
  As a supporter
  I want to put the learners I support into a group
  So that I can give them words together and follow them as one group

  Scenario: Supporter creates a group, adds members, assigns words and views the group dashboard
    Given a supporter who supports an adult learner and a child learner with an alias and avatar
    And a child-safe shared word exists
    And the group supporter is logged in
    When the supporter creates the group "Class Alpha"
    And the supporter adds both learners to the group
    And the supporter assigns the shared word to the group
    Then the assignment result says 2 words were added
    And the Dashboard tab lists both learners

  Scenario: A child is shown by alias and avatar only
    Given a supporter who supports an adult learner and a child learner with an alias and avatar
    And the group supporter is logged in
    When the supporter creates the group "Class Beta"
    And the supporter adds both learners to the group
    Then the child is listed by alias and avatar
    And the child's real name and email are not on the page

  Scenario: Group pages show an error message when the services are down
    Given a supporter who supports an adult learner and a child learner with an alias and avatar
    And the group supporter is logged in
    And the group requests fail
    When the supporter opens the Groups page
    Then a groups error message is shown

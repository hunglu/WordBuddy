Feature: Learner support links
  As a WordBuddy learner
  I want a grown-up or partner to support me
  So that a child learns only with a supporter, and unlinks are safe for both sides

  Scenario: Child without a supporter sees the "Add a supporter" gate
    Given a new "Child" learner is logged in
    When they open the vocabulary page
    Then the "Add a supporter" screen is shown
    When they follow the "Add a supporter" link
    Then they are on the Supporters page

  Scenario: Adult accepts a child's invitation link and the gate opens
    Given a new child has created a support invitation
    And a new adult is logged in
    When the adult opens the invitation link and accepts it
    Then the link is shown as active
    When the child logs in and opens the vocabulary page
    Then the "Add a supporter" screen is not shown

  # Needs Identity started with SupportLinks__UnlinkOverrideWaitDays=0 (see e2e/docker-compose.e2e.yml).
  Scenario: Admin completes an escalated unlink request
    Given an active support link with an escalated unlink request
    And the admin is logged in on the support links page
    When the admin completes the unlink with a reason
    Then the request is no longer listed
    And the link is revoked

Feature: Lexeme admin
  As a WordBuddy admin
  I want to find a dictionary headword by part of its spelling and correct its data
  So that learners see the right part of speech, pronunciation and level

  Scenario: Admin finds a lexeme by part of its word and corrects it
    Given the admin is logged in
    And lexeme admin: a lexeme exists whose word has a unique fragment in the middle
    When lexeme admin: they search the Lexemes page for that fragment
    And lexeme admin: they open the lexeme from the results
    And lexeme admin: they set part of speech "Noun", UK IPA "/e2e-ipa/" and CEFR level "B2"
    Then lexeme admin: the form shows the message "Saved"
    And lexeme admin: after a reload the form shows UK IPA "/e2e-ipa/" and CEFR level "B2"

  Scenario: A learner cannot reach the lexeme admin pages
    Given lexeme admin: a learner is logged in
    When lexeme admin: they open "/admin/lexemes" directly
    Then lexeme admin: they are sent back to the home page
    And lexeme admin: the sidebar has no "Lexemes" item

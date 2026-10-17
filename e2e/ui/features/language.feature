Feature: Multilingual UI (English and Vietnamese)
  As a Vietnamese learner
  I want to read WordBuddy in Vietnamese
  So that I understand menus, buttons and messages

  Scenario: A Vietnamese browser sees the Vietnamese login page
    Given a browser with the language "vi-VN"
    When the learner opens the login page
    Then the login button reads "Đăng nhập"

  Scenario: The switcher changes the text without a reload
    Given a browser with the language "en-US"
    When the learner opens the login page
    And they choose "Tiếng Việt" in the language switcher
    Then the login button reads "Đăng nhập"
    And the page was not reloaded

  Scenario Outline: The chosen language is restored after login in a fresh browser
    Given a new "<ageGroup>" user logged in with the browser language "en-US"
    When they choose "Tiếng Việt" in the language switcher
    And the language is saved to the profile
    And they log in again in a fresh browser with the language "en-US"
    Then the sidebar shows "Bài học"

    Examples:
      | ageGroup |
      | Adult    |
      | Child    |

  Scenario: Lesson content stays English in the Vietnamese UI
    Given a new "Adult" user logged in with the browser language "vi-VN"
    When they open the lessons page from the sidebar
    Then the lessons heading reads "Bài học"
    And the first lesson title is still in English

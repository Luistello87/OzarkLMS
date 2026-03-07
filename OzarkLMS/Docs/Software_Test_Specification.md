# Software Test Specification: OzarkLMS

## 1. Software Features Review

The following features have been identified based on the software requirements and current codebase implementation.

| Feature ID | Feature Name | Description |
| :--- | :--- | :--- |
| **F-01** | **User Authentication** | Allows users (Students/Instructors) to register for an account, log in securely using cookies, and log out. |
| **F-02** | **Profile Management** | Users can view their profile, update their bio, upload a profile picture, and follow other users. |
| **F-03** | **Course Management** | Instructors can create new courses and manage enrollment. Students can browse and enroll in courses. |
| **F-04** | **Assignment Creation** | Instructors can create assignments (File Upload or Quiz type) with due dates, descriptions, and point values. |
| **F-05** | **Quiz Management** | Instructors can add questions and options (multiple choice) to Quiz-type assignments. |
| **F-06** | **Student Submission** | Students can upload files (for Assignments) or answer multiple-choice questions (for Quizzes) to submit work. |
| **F-07** | **Grading & Feedback** | Instructors can view student submissions, assign numeric scores, and provide text-based feedback. Quizzes are auto-graded. |
| **F-08** | **Calendar** | A visual calendar view that displays assignment due dates and course events. |
| **F-09** | **Notifications** | Automated alerts for users about important events (e.g., "New Assignment", "Grade Posted"). |
| **F-10** | **Collaboration/Chat** | A mechanism for users to interact, including chat groups and discussions (referenced by ChatGroups in Profile). |

---

## 2. Features for SQA Verification

The following features are selected for verification by Software Quality Assurance (SQA) as they represent the core functioning of the LMS.

*   **Verified**: F-01 (Authentication), F-03 (Course Mgmt), F-04 (Assignment Creation), F-06 (Submission), F-07 (Grading).
*   **Rationale**: These features cover the critical "Happy Path" for the two main stakeholders (Students and Instructors). Failure in any of these areas renders the system unusable for its primary purpose.

---

## 3. Test Cases

### Test Case 1 – Successful User Login
*   **Test Case ID**: TC-AUTH-001
*   **Test Case Name**: Successful User Login
*   **Requirement Reference**: FR-1 – User Authentication
*   **Priority**: High
*   **Test Level**: Functional / System
*   **Test Type**: Positive
*   **Pre-Conditions**: User account exists, account is active, services operational, user not currently logged in.
*   **Test Steps**:
    1.  Launch the web application in a supported browser.
    2.  Navigate to the Login page (`/Account/Login`).
    3.  Enter valid unique username and password.
    4.  Click Login.
*   **Expected Results**:
    *   User is authenticated successfully.
    *   User is redirected to the dashboard (Home Index).
    *   Authenticated session is created.
    *   No error messages are displayed.

### Test Case 2 – Unsuccessful User Login (Invalid Credentials)
*   **Test Case ID**: TC-AUTH-002
*   **Test Case Name**: Unsuccessful Login with Invalid Credentials
*   **Requirement Reference**: FR-1 – User Authentication
*   **Priority**: High
*   **Test Level**: Functional / System
*   **Test Type**: Negative
*   **Pre-Conditions**: System is online.
*   **Test Steps**:
    1.  Navigate to the Login page.
    2.  Enter a non-existent username or incorrect password.
    3.  Click Login.
*   **Expected Results**:
    *   System validates credentials.
    *   Login fails.
    *   Error message identifying invalid login attempt is displayed.
    *   User remains on the Login page.

### Test Case 3 – Assignment Creation (Success)
*   **Test Case ID**: TC-ASSIGN-001
*   **Test Case Name**: Create Assignment Successfully
*   **Requirement Reference**: FR-4 – Assignment Creation
*   **Priority**: High
*   **Test Level**: Functional
*   **Test Type**: Positive
*   **Pre-Conditions**: User is logged in as Instructor, Course exists.
*   **Test Steps**:
    1.  Navigate to Course Details.
    2.  Click "Create Assignment".
    3.  Enter valid Title, Points, and Due Date.
    4.  Select "File Upload" type.
    5.  Click Create.
*   **Expected Results**:
    *   Assignment is saved to the database.
    *   User is redirected to Course/Details.
    *   New assignment is listed.

### Test Case 4 – Event/Assignment Creation with Missing Required Title
*   **Test Case ID**: TC-ASSIGN-002
*   **Test Case Name**: Create Assignment with Missing Title
*   **Requirement Reference**: FR-4 – Assignment Validation
*   **Priority**: Medium
*   **Test Level**: Functional
*   **Test Type**: Negative
*   **Pre-Conditions**: User is logged in as Instructor.
*   **Test Steps**:
    1.  Navigate to Create Assignment page.
    2.  Fill all fields with valid data except Title.
    3.  Leave the Title field blank.
    4.  Submit the form.
*   **Expected Results**:
    *   System displays validation error indicating Title is required.
    *   Form submission is prevented.
    *   Previously entered values remain populated.
    *   No assignment is created in the database.

### Test Case 5 – Student Submission (Success)
*   **Test Case ID**: TC-SUBMIT-001
*   **Test Case Name**: Successful Student File Submission
*   **Requirement Reference**: FR-6 – Student Submission
*   **Priority**: High
*   **Test Level**: Functional
*   **Test Type**: Positive
*   **Pre-Conditions**: User is Student, enrolled, Assignment exists.
*   **Test Steps**:
    1.  Navigate to Assignment "Take" page.
    2.  Upload a valid file.
    3.  Click Submit.
*   **Expected Results**:
    *   File is accepted.
    *   Submission record is created.
    *   User sees success message or redirection.

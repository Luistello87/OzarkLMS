# Requirements Gathering Report: OzarkLMS

## 2. Project Overview and Context

### 2.1 Project Summary
OzarkLMS is a web-based Learning Management System designed to facilitate online education and course management. It serves as a centralized platform for instructors to create courses, distribute materials, and assess student performance, while providing students with a user-friendly interface to access content, submit assignments, and track their progress. The system aims to bridge the gap between complex enterprise LMS solutions and the need for a streamlined, intuitive educational tool.

### 2.2 Project Context
The system operates within an academic context, suitable for colleges or technical institutes (specifically targeted for the COMP2154 course requirements). It functions as a core tool for remote and hybrid learning environments, supporting typical educational workflows such as content delivery, grading, and student-instructor communication.

---

## 3. Stakeholder Analysis

### 3.1 Stakeholder List

| Stakeholder / Group | Role | Interests / Goals | Influence (High / Medium / Low) |
| :--- | :--- | :--- | :--- |
| **Students** | End User | Easy access to course materials, clear assignment instructions, seamless submission process, fair and timely feedback. | High |
| **Instructors** | Content Creator / Admin | Efficient course management, easy grading tools, effective communication channels, ability to track student progress. | High |
| **Administrators** | System Owner | System reliability, user management, global configuration, data security. | Medium |
| **Developers** | Solution Provider | Delivering a functional, maintainable, and robust system that meets requirements and deadlines. | Low (Post-deploy) |

### 3.2 Stakeholder Summary
The primary focus is on **Students** and **Instructors**, as their daily interaction determines the system's success. There is a potential conflict between the ease of use desired by students and the granular control or complexity sometimes required by instructors for advanced course configurations. The system must balance these by offering advanced features without cluttering the student interface.

---

## 4. Business Need and Problem Statement

### 4.1 Business Need
Currently, managing course materials, deadlines, and assignments via disparate tools (email, shared drives, paper) leads to disorganization, lost information, and frustration. There is a critical need for a unified platform that consolidates these functions to improve educational efficiency and the overall learning experience.

### 4.2 Problem Statement
Students and instructors currently struggle with fragmented communication and disorganized content delivery, resulting in missed deadlines and administrative overhead. OzarkLMS addresses this by providing a single, cohesive portal for all course-related activities, ensuring clarity and accountability.

---

## 5. Project Goals and Scope

### 5.1 High-Level Goals
1.  **Streamline Assessment**: Simplify the assignment submission and grading workflow.
2.  **Centralize Content**: Provide a central repository for all course materials and announcements.
3.  **Enhance Communication**: Facilitate clear communication between students and instructors via notifications and dashboards.
4.  **Improve Organization**: Offer intuitive tools like calendars and dashboards to help users manage their schedules.
5.  **Ensure Accessibility**: Make educational records and feedback easily accessible to authorized users.

### 5.2 Scope – In Scope
*   **User Authentication**: Secure Login and Registration for different roles (Student, Instructor, Admin).
*   **Course Management**: Creating, editing, and listing courses and modules.
*   **Assessment System**: Assignment creation, file uploading/submission, and grading interfaces.
*   **Dashboard**: Personalized landing page with announcements (`DashboardAnnouncement`) and activity summaries.
*   **Calendar**: Visual representation of assignment due dates and course events (`CalendarController`).
*   **Collaboration**: Tools for group interaction or discussions (`CollaborationController`).
*   **Notifications**: Alert system for system events and updates (`NotificationController`).

### 5.3 Scope – Out of Scope
*   Real-time video conferencing integration (e.g., Zoom/Teams built-in).
*   Payment processing for course fees.
*   Native mobile applications (iOS/Android).
*   Advanced analytics and complex reporting dashboards.
*   LTI (Learning Tools Interoperability) integrations with external publishers.

---

## 6. Functional Requirements

1.  **FR-1 (Authentication)**: The system shall allow users to register and login with secure credentials, assigning appropriate roles (Student/Instructor).
2.  **FR-2 (Course Mgmt)**: Instructors shall be able to create new courses, update details, and manage student enrollments.
3.  **FR-3 (Content)**: Instructors shall be able to create modules and upload educational materials (or create Sticky Notes/links) for students.
4.  **FR-4 (Assignments)**: The system shall allow instructors to create assignments with titles, descriptions, due dates, and max points.
5.  **FR-5 (Submissions)**: Students shall be able to upload files (`AttachmentUrl`) to complete assignments before the deadline.
6.  **FR-6 (Grading)**: Instructors shall be able to view student submissions, assign grades, and provide textual feedback.
7.  **FR-7 (Calendar)**: Users shall see a calendar view automatically populated with assignment deadlines and manually added events.
8.  **FR-8 (Notifications)**: The system shall notify users of important events (e.g., "Assignment Graded", "New Announcement").
9.  **FR-9 (Collaboration)**: Users shall have access to a collaboration area for sharing information or discussions.

---

## 7. Non-Functional Requirements

*   **NFR-1 (Performance)**: The system should load the main dashboard within 2 seconds under normal load conditions.
*   **NFR-2 (Security)**: User passwords must be stored using strong hashing algorithms. Access to grading data must be restricted to the specific instructor and student.
*   **NFR-3 (Usability)**: The user interface should be clean, intuitive, and compliant with basic web accessibility standards.
*   **NFR-4 (Reliability)**: The system should handle file upload interruptions gracefully and prevent data corruption.
*   **NFR-5 (Compatibility)**: The web application must function correctly on all modern web browsers (Chrome, Edge, Firefox).

---

## 8. Constraints and Assumptions

### 8.1 Constraints
*   **Technology Stack**: Must be developed using **C# ASP.NET Core MVC**.
*   **Database**: Must use **PostgreSQL** (`OZARK_DB`) for data persistence.
*   **Environment**: Must be deployable on standard Windows/Linux server environments supporting .NET Core.
*   **Structure**: Must adhere to the MVC design pattern as evidenced by the project structure.

### 8.2 Assumptions
*   Users have reliable internet access to use the online platform.
*   Users are familiar with basic web navigation and file uploading.
*   The hosting environment provides sufficient storage for assignment file uploads.

---

## 9. Initial Use Cases and/or User Stories

### Use Case 1: Submit Assignment
*   **Name**: Submit Assignment
*   **Actor**: Student
*   **Preconditions**: An active assignment exists and the due date has not passed.
*   **Main Flow**:
    1.  Student logs in and navigates to the specific Course.
    2.  Student clicks on the "Assignments" tab.
    3.  System displays the list of assignments; Student selects one.
    4.  System shows assignment details and a "Submit" button.
    5.  Student clicks "Submit", selects a file from their device, and confirms uploads.
    6.  System validates the file and saves the submission.
    7.  System updates the submission status to "Submitted" and displays a success message.
*   **Alternative Flows**:
    *   If the due date has passed, the system disables the submit button.

### User Stories
*   **US-1**: "As a **Student**, I want to view my grades on the dashboard so that I can track my academic progress."
*   **US-2**: "As an **Instructor**, I want to post an announcement to my course so that all enrolled students are aware of changes."
*   **US-3**: "As a **User**, I want to see a calendar of upcoming deadlines so that I can manage my time effectively."

---

## 10. Priorities and Risk Highlights

### 10.1 Requirement Priorities
*   **Must Have**: User Authentication, Course CRUD, Enrollment, Assignment Creation, File Submission, Grading.
*   **Should Have**: Calendar View, Dashboard Announcements, Notifications, Collaboration Tools.
*   **Could Have**: Profile avatars, Advanced rich-text editing, Drag-and-drop course organization.

### 10.2 Early Risk Highlights
*   **Risk**: **Database Schema Complexity**. Start-up risk in correctly modeling the relationships between Users, Courses, Enrollments, and Submissions.
    *   *Mitigation*: Finalize the Entity Relationship Diagram (ERD) early and verify with `EF Core` migrations.
*   **Risk**: **File Upload Challenges**. Handling large files or varied file types could cause storage or security issues.
    *   *Mitigation*: Implement strict validation on file size and extension types on the server side.

---

## 11. Summary and Next Steps

### Summary
This Requirements Gathering Report establishes the foundation for **OzarkLMS**, a focused and user-centric Learning Management System. We have identified the key stakeholders (Students, Instructors) and outlined the critical functional requirements needed to support a complete course lifecycle—from content creation to grading. The project leverages **ASP.NET Core** and **PostgreSQL** to build a robust, scalable, and modern educational platform.

### Next Steps
1.  **Database Design**: Finalize the database schema and apply initial Entity Framework migrations.
2.  **Core Implementation**: Build the fundamental Authentication and Course Management modules.
3.  **UI Prototyping**: Develop the core Views for the Dashboard and Assignment details to validate the user experience.
4.  **Iterative Development**: Begin the implementation of "Must Have" features, followed by "Should Have" features.

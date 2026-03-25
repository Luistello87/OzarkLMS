**Course Name:** System Development Project  
**Course Code:** COMP2154  

---

## 1. Cover Information

1.  **Project title:** OzarkLMS
2.  **Team name, if any:** OzarkLMS
3.  **Team members:**
    *   Tello, Luis
    *   Contaldo, Gonzalo
    *   Hossain, Ifrad Bin Istiaque
    *   Laquis, John Sebastian
    *   Yoon, Sanyoung

## 2. Project Context and Client Focus

### 2.1 Project Background
OzarkLMS is a web-based Learning Management System designed to facilitate online education and course management. In the domain of educational technology, it serves as a centralized platform to bridge the gap between complex enterprise LMS solutions and the need for a streamlined, intuitive tool. It addresses the issues of fragmented communication and disorganized content delivery in academic settings.

### 2.2 Client or Stakeholder Focus
The primary clients/stakeholders are **Students** and **Instructors** within an academic context (such as colleges or technical institutes).
*   **Students** care about easy access to course materials, clear assignment instructions, and a seamless submission process.
*   **Instructors** care about efficient course management, easy grading tools, and effective communication channels to track student progress.

---

## 3. Problem Statement and Business Need

### 3.1 Business Need
Currently, managing course materials, deadlines, and assignments via disparate tools (email, shared drives, paper) leads to disorganization, lost information, and frustration. There is a critical need for a unified platform that consolidates these functions to improve educational efficiency and the overall learning experience.

### 3.2 Problem Statement
Students and instructors currently struggle with fragmented communication and disorganized content delivery, resulting in missed deadlines and administrative overhead. OzarkLMS addresses this by providing a single, cohesive portal for all course-related activities, ensuring clarity and accountability.

---

## 4. Project Objectives and Scope

### 4.1 Project Objectives
1.  **Streamline Assessment:** Simplify the assignment submission workflow for students and the grading process for instructors.
2.  **Centralize Content:** provide a single repository for all course materials and announcements to reduce information loss.
3.  **Enhance Communication:** Facilitate clear updates between students and instructors via dashboard announcements and notifications.
4.  **Improve Organization:** Offer intuitive tools like calendars to help users manage their schedules and track upcoming deadlines.
5.  **Ensure Accessibility:** Make educational records and feedback easily accessible to authorized users at any time.

### 4.2 Scope – In Scope
*   **User Authentication:** Secure Login, Registration, and Role Management (Student/Instructor).
*   **Course Management:** Creating, editing, and listing courses and modules.
*   **Assessment System:** Assignment creation by instructors, file uploading/submission by students, and grading interfaces.
*   **Dashboard:** Personalized landing page with announcements and activity summaries.
*   **Calendar:** Visual representation of assignment due dates and course events.
*   **Notifications:** Alert system for important events (e.g., "Assignment Graded").

### 4.3 Scope – Out of Scope
*   Real-time video conferencing integration (e.g., Zoom/Teams).
*   Payment processing for course fees.
*   Native mobile applications (iOS/Android).
*   Advanced analytics and complex reporting dashboards.
*   LTI (Learning Tools Interoperability) integrations with external publishers.

---

## 5. Proposed Solution and System Overview

### 5.1 System Type and Users
We plan to build a **Web-based Application**.
*   **Primary Users:** Students (end users), Instructors (content creators/graders), and Administrators (system owners).

### 5.2 System Overview
OzarkLMS acts as a central hub where Instructors create Courses and Modules. Within these courses, Assignments are created. Students browse the catalog, enroll in courses, and upload submissions for assignments.
*   **Core Components:**
    *   **Auth Module:** Handles identity and access control.
    *   **Course Module:** Manages course metadata and content structure.
    *   **Assessment Module:** Handles the submission (file upload) and grading loop.
    *   **Communication Module:** Manages notifications, announcements, and calendar events.

---

## 6. Technology Stack and Tools

1.  **Programming language:** C# (Backend), HTML/CSS/JavaScript (Frontend).
2.  **Frameworks and libraries:** ASP.NET Core MVC (Web Framework), Entity Framework Core (ORM), Bootstrap (UI).
3.  **Database:** PostgreSQL (`OZARK_DB`).
4.  **Major third-party APIs or services:** None planned for the core MVP (relying on built-in .NET libraries for file handling).
5.  **Version control and project management:** Git (Version Control), GitHub (Repository), Trello/Jira (Project Management).

**Justification:** This stack uses industry-standard technologies that are robust, scalable, and well-supported. ASP.NET Core provides a strong structure for MVC applications, and PostgreSQL is a reliable open-source relational database suitable for structured educational data.

---

## 7. Feasibility, Constraints, and Risks

### 7.1 Feasibility Summary
This project is feasible within the course timeline because it focuses on core LMS features (CRUD operations, file handling, auth) without overextending into complex real-time or AI features. The team is using a familiar stack (C#/.NET), which reduces the learning curve.

### 7.2 Constraints
*   **Technology Requirements:** Must be developed using **C# ASP.NET Core MVC**.
*   **Database:** Must use **PostgreSQL**.
*   **Deployment:** Must be deployable on standard environments supporting .NET Core.
*   **Time:** Must be completed within the semester timeframe (approx. 12-14 weeks).

### 7.3 Initial Risks and Mitigations
*   **Risk:** Database Schema Complexity (modeling relationships between Users, Courses, Enrichments).
    *   *Mitigation:* Finalize the Entity Relationship Diagram (ERD) early and verify with `EF Core` migrations before heavy coding.
*   **Risk:** File Upload Challenges (storage limits, security).
    *   *Mitigation:* Implement strict validation on file size and extensions on the server side; use local filesystem storage for MVP simplicity.
*   **Risk:** Scope Creep (adding too many "nice-to-have" features).
    *   *Mitigation:* Strictly adhere to the "In Scope" list and move extra ideas to a "Future Work" backlog.

---

## 8. Project Plan and Schedule

### 8.1 Approach: Agile Backlog
We will use an Agile-inspired approach, organizing work into 2-week sprints. We will maintain a backlog of User Stories and select a subset to implement each sprint, prioritizing "Must Have" requirements.

### 8.2 Major Phases and Milestones
*   **Phase 1: Planning & Design (Weeks 1-3)** - Requirements, ERD, Initial Prototyping.
*   **Phase 2: Core Infrastructure (Weeks 4-5)** - Project setup, Authentication, Database config.
*   **Phase 3: Core Features Implementation (Weeks 6-9)** - Course Management, Assignment Creation, Submission Logic.
*   **Phase 4: Secondary Features (Weeks 10-11)** - Calendar, Dashboard, Notifications.
*   **Phase 5: Testing & Refinement (Week 12)** - QA, Bug fixes, UI Polish.
*   **Phase 6: Final Delivery (Week 13)** - Deployment, Final Presentation.

### 8.3 Task Allocation
| Team Member | Primary Responsibility |
| :--- | :--- |
| **Member 1** | Project Lead / Backend Architecture (Controllers, Auth) |
| **Member 2** | Database Design / Data Access Layer (EF Core, Repositories) |
| **Member 3** | Frontend Development (Views, Bootstrap, JavaScript) |
| **Member 4** | QA / Testing / Documentation |

---

## 9. Team Structure and Communication Plan

### 9.1 Team Roles
*   **Backend Lead:** Focuses on C# logic and API structure.
*   **Database Lead:** Manages PostgreSQL and EF Core migrations.
*   **Frontend Lead:** Manages Razor Views and CSS/JS.
*   **Coordinator/QA:** Tracks progress, manages repo, and performs testing.

### 9.2 Communication and Collaboration
*   **Messaging:** Discord/Slack for day-to-day communication.
*   **Meetings:** Weekly stand-up meeting (e.g., Mondays) to review progress and assign tasks.
*   **Task Tracking:** GitHub Projects or Trello board to track "To Do", "In Progress", and "Done".

### 9.3 Decision-Making and Conflict Resolution
Decisions will be made by consensus. If a disagreement arises, we will vote, with the Project Lead having the deciding vote if tied. Technical disagreements will be resolved by prototyping both solutions and choosing the cleaner approach.

---

## 10. Assumptions, Dependencies, and Success Criteria

### 10.1 Assumptions
*   Users have reliable internet access.
*   The hosting environment provided (or local machine) has sufficient storage for assignment files.
*   We have access to necessary development tools (Visual Studio/VS Code).

### 10.2 Dependencies
*   Availability of the PostgreSQL database server.
*   Nuget package availability for helper libraries.

### 10.3 Success Criteria
The project will be considered successful if:
1.  A student can successfully register, log in, find a course, and upload an assignment file.
2.  An instructor can create that assignment and Grade the student's submission.
3.  The system is stable (no crashes during standard workflows) and intuitive (positive feedback from peer reviews).
4.  All "Must Have" functional requirements are met.


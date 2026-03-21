## 1. Architecture Overview

The system is designed using **Clean Architecture** principles combined with **CQRS (Command Query Responsibility Segregation)**.

### Architecture Layers:

```
Client / Swagger
        ↓ JWT Bearer
WebAPI (Controllers / API)
        ↓
Application Layer (Commands & Queries)
        ↓
Domain Layer (Entities & Business Rules)
        ↓
Infrastructure Layer (Database, Background Jobs, Auth)
```
### Dependency Flow:

```
| Layer            | Responsibility                           | Dependencies |
|------------------|------------------------------------------|--------------|
| *Domain*         | Entities, enums, no logic                | None         |
| *Application*    | Business rules, service interfaces, DTOs | Domain       |
| *Infrastructure* | EF Core, repositories, background worker | Application  |
| *API*            | HTTP controllers, middleware, DI wiring  | Infrastructure |
```

---

## 2. Data Model

The system uses a **relational database SQLITE** with the following core entities:

### Core Entities:

**User**
- Id (GUID) PK
- UserName
- Password
- Role
- CreatedAt
**Class**
- Id  (GUID) PK
- Name

**Section**
- Id (GUID) PK
- Name
- ClassId FK

**Student**
- Id (GUID) PK
- Name
- ClassId FK
- SectionId FK
- UserId FK

**Subjects**
- Code (pk)
- Name

**Exam**
- Id (GUID) PK
- Type (Quarterly, Half-Yearly, Final)
- ClassId FK
- Name

**Mark**
- Id
- StudentId FK
- SubjectId FK
- ExamId FK
- Score
- RequestId (for idempotency)
- CreatedAt
- UpdatedAt

**MarkSubmissionJobs**
- Id GUID PK
- Payload 
- Status
- RetryCount
- NextRetryAt (nullable)
- CorrelationId
- CreatedAt
- ProcessesAt (nullable)

**Ranking**
- StudentId FK
- ExamId FK
- TotalMarks FK
- SectionRank
- ClassRank
- UpdatedAt

** TeacherSection**
- TeacherId (FK userId for role teacher)
- SectionId (FK section id for Section)

### Relationships:

```
Class -One2Many→ Sections -One2Many→ Students  
Student -One2Many→ Marks → Subjects + Exams  
```

---
## 3. API Design

### Authentication
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| POST | ⁠ /api/auth/login ⁠ | Public | Login, returns JWT |

### Classes
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| GET | ⁠ /api/classes ⁠ | Admin | List all classes |
| POST | ⁠ /api/classes ⁠ | Admin | Create class |
| GET | ⁠ /api/classes/{id} ⁠ | Admin | Get class |
| PUT | ⁠ /api/classes/{id} ⁠ | Admin | Update class |
| DELETE | ⁠ /api/classes/{id} ⁠ | Admin | Delete class |

### Sections
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| GET | ⁠ /api/classes/{classId}/sections ⁠ | Admin | List sections |
| POST | ⁠ /api/classes/{classId}/sections ⁠ | Admin | Create section |
| PUT | ⁠ /api/sections/{id} ⁠ | Admin | Update section |
| DELETE | ⁠ /api/sections/{id} ⁠ | Admin | Delete section |

### Students
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| GET | ⁠ /api/students ⁠ | Admin | List all students |
| POST | ⁠ /api/students ⁠ | Admin | Create student |
| GET | ⁠ /api/students/{id} ⁠ | Admin, Teacher, Student(own) | Get student |
| PUT | ⁠ /api/students/{id} ⁠ | Admin | Update student |
| DELETE | ⁠ /api/students/{id} ⁠ | Admin | Delete student |

### Subjects
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| GET | ⁠ /api/subjects ⁠ | Admin, Teacher | List subjects |
| POST | ⁠ /api/subjects ⁠ | Admin | Create subject |

### Exams
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| GET | ⁠ /api/exams ⁠ | Admin, Teacher | List exams |
| POST | ⁠ /api/exams ⁠ | Admin | Create exam |

### Users / Teacher Assignment
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| POST | ⁠ /api/users ⁠ | Admin | Create user (Teacher/Student) |
| POST | ⁠ /api/users/{teacherId}/sections/{sectionId} ⁠ | Admin | Assign teacher to section |
| DELETE | ⁠ /api/users/{teacherId}/sections/{sectionId} ⁠ | Admin | Remove assignment |

### Marks
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| POST | ⁠ /api/marks ⁠ | Teacher | Submit marks (async) → 202 |
| GET | ⁠ /api/marks/jobs/{jobId} ⁠ | Teacher | Check job status |
| GET | ⁠ /api/marks?studentId=&examId= ⁠ | Admin, Teacher, Student(own) | Query marks |

### Rankings
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| GET | ⁠ /api/rankings/class/{classId}/exam/{examId} ⁠ | Admin, Teacher | Class-wide ranking |
| GET | ⁠ /api/rankings/section/{sectionId}/exam/{examId} ⁠ | Admin, Teacher | Section ranking |
| GET | ⁠ /api/rankings/class/{classId}/exam/{examId}/top/{n} ⁠ | Admin, Teacher | Top N students |
| GET | ⁠ /api/rankings/student/{studentId}/exam/{examId} ⁠ | Admin, Teacher, Student(own) | Student rank |


## 4. Authentication Flow

The system uses **JWT-based authentication**.

### Flow:
```
User logs in → JWT token issued → Token sent in Authorization header
```

### Roles:
- **Admin**: Manage classes, sections, students
- **Teacher**: Submit/update marks of student
- **Student**: View own marks and ranking

### Security Features:
- Token expiry handling
- Role-based authorization
- Secure endpoints

---

## 5. Async Processing Design

Marks processing follows an **asynchronous workflow** to ensure scalability and responsiveness.

### Why Async?
- Avoid blocking API calls
- Handle large data efficiently
- Improve system scalability

### Workflow:

```
POST /marks
   ↓
Store request as Job (Pending)
   ↓
Background Worker picks job
   ↓
Process marks:
   - Save marks
   - Calculate total marks
   - Generate rankings
   ↓
Update database
```

### Background Worker Responsibilities:
- Process jobs from queue/table
- Compute rankings
- Update system state

### Retry Mechanism:
- Maximum 3 retries
- Exponential backoff
- Log each retry attempt

### Idempotency Handling:
- Each request has a unique RequestId
- Duplicate requests are ignored

---

## 6. Assumptions

1.⁠ ⁠A student belongs to exactly one class and one section — no transfers modeled.
2.⁠ ⁠Marks are per student per subject per exam — one record per combination.
3.⁠ ⁠Exams belong to a class — e.g., "Class 10 Quarterly" is distinct from "Class 9 Quarterly".
4.⁠ ⁠Teacher can be assigned to multiple sections; a section can have multiple teachers.
5.⁠ ⁠Maximum score per subject is not validated (business rule deferred).
6.⁠ ⁠No token refresh in v1 — re-login required on expiry.
7.⁠ ⁠SQLite chosen for portability; swap to SQL Server via connection string + EF provider change.
# StudentAssessmentSystem

Student Assessment & Ranking System built as a .NET take-home assignment.

## Requirements

- .NET 8 SDK

## How to Run

1. Open a terminal in the project root:

```bash
cd /Users/cube/Code/StudentAssessmentSystem/StudentAssessment
```

2. Restore packages:

```bash
dotnet restore
```

3. Run the API:

```bash
dotnet run --project StudentAssessment.WebAPI
```

4. Open Swagger in the browser:

```text
https://localhost:<port>/swagger
```

## Database

- The project uses SQLite
- The database file is created automatically on first run
- Database path:

```text
StudentAssessment/StudentAssessment.WebAPI/StudentAssessment.db
```

## Seed Data

On first run, the application automatically seeds:

- admin user
- teachers
- students
- classes
- sections
- subjects
- exams
- marks
- rankings

Seeded marks are also used to generate ranking data, so ranking APIs are available immediately after startup.

## Demo Login Credentials

### Admin

- Username: `admin`
- Password: `admin123`

### Teachers

- Username: `teacher1`
- Password: `teacher1123`

- Username: `teacher2`
- Password: `teacher2123`

- Username: `teacher3`
- Password: `teacher3123`

### Students

Student users are also seeded automatically with usernames like:

- `student_10th_a_01`
- `student_10th_b_03`
- `student_9th_a_05`

Password format:

- `<username>@123`

Example:

- Username: `student_10th_a_01`
- Password: `student_10th_a_01@123`

## Run Tests

Run all tests:

```bash
cd /Users/cube/Code/StudentAssessmentSystem/StudentAssessment
dotnet test
```

Run only unit tests:

```bash
dotnet test tests/StudentAssessment.UnitTests/StudentAssessment.UnitTests.csproj
```

Run only integration tests:

```bash
dotnet test tests/StudentAssesment.IntegrationTests/StudentAssessment.IntegrationTests.csproj
```

## Notes

- Swagger is enabled in development mode
- Rankings are updated asynchronously through a background service
- During first-time seeding, rankings are also generated from seeded marks
- If you want a clean database, delete `StudentAssessment.db` and run the API again

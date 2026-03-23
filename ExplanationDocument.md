# Explanation Document

## 1. Key Design Decisions

I built this project using a layered structure with separate projects for Core, Application, Infrastructure, and WebAPI. I chose this because it keeps the code organized and easier to understand. It also makes it easier to explain how the system works.

I used JWT authentication with role-based access because it is required in the assignment. The system supports three roles:

- Admin manages classes, sections, students, and users
- Teacher submits and updates marks
- Student can only view their own marks and rankings

The main design decision was to process marks asynchronously. When marks are submitted, updated, or deleted, the API creates a job instead of directly recalculating rankings. A background service processes the job and then updates the rankings. This follows the assignment requirement and keeps the API flow clean.

I also decided to store ranking results in the `Ranking` table. Because of this, ranking APIs read the saved ranking data instead of recalculating ranks every time. This makes ranking queries simpler and faster.

To avoid duplicate processing, I used correlation IDs for mark submission jobs and idempotency handling for marks.

## 2. Trade-offs Made

I used SQLite because it is simple to set up and easy to run locally. It is a good choice for an assignment, even though it would not be the best option for a large production system.

For asynchronous processing, I used a lightweight background service and a database-backed job table. I did not use a full message queue system like RabbitMQ because that would add more complexity than needed for this assignment.

I stored rankings in the database instead of recalculating them on every request. This improves read performance, but it also means ranking updates depend on the background worker completing successfully. To handle this, I added retries with exponential backoff.

I kept the code simple in many places so that the project would stay understandable and maintainable. I focused on meeting the assignment requirements clearly rather than adding extra complexity.

## 3. Challenges Faced

The biggest challenge was keeping rankings correct while marks are processed asynchronously. Since rankings are not updated directly in the API call, I had to make sure the background worker becomes the single place where mark changes and ranking updates happen.

Another challenge was tie handling in ranking. The assignment requires competition ranking like `1, 2, 2, 4`. I implemented and tested this logic so that students with the same total marks get the same rank.

Handling retries and duplicate requests was also important. In an asynchronous system, the same request may be sent more than once, or a job may fail and need to be retried. I solved this by storing job status, retry count, next retry time, and correlation ID.

Role-based access control also needed attention. I had to make sure that students can only see their own data, while teachers and admins have wider access based on their role.

## 4. Improvements With More Time

If I had more time, I would improve testing further, especially integration tests for the full async marks workflow.

I would also improve some API and query logic to reduce repeated database calls and make some endpoints cleaner.

Another improvement would be better structured logging and tracing so it is easier to follow a request from API call to background job processing.

Finally, for a larger real-world version of this project, I would consider using a dedicated message queue, adding refresh tokens for authentication, and improving Swagger and README documentation with more examples.

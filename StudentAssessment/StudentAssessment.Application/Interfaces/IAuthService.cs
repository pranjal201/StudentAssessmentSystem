namespace StudentAssessment.Application.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Generates a JWT token for the given user ID and username.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="username">The username of the user.</param>
    /// <param name="role">The role of the user (Admin, Teacher, Student).</param>
    /// <returns>A JWT token string.</returns>
    string GenerateToken(Guid userId, string username, string role);

    /// <summary>
    /// Validates a password against a bcrypt hash.
    /// </summary>
    /// <param name="password">The plaintext password to validate.</param>
    /// <param name="hash">The bcrypt hash to verify against.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Hashes a plaintext password using bcrypt.
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>A bcrypt hash of the password.</returns>
    string HashPassword(string password);
}

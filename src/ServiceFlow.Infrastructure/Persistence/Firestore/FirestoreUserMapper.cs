using Google.Cloud.Firestore;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Infrastructure.Persistence.Firestore;

internal static class FirestoreUserMapper
{
    internal static User? ToUser(DocumentSnapshot snapshot)
    {
        if (!snapshot.Exists)
            return null;

        if (!Guid.TryParse(snapshot.Id, out var id))
            return null;

        var email = snapshot.GetValue<string>("email");
        var fullName = snapshot.GetValue<string>("fullName");
        var passwordHash = snapshot.GetValue<string>("passwordHash");
        var isActive = snapshot.GetValue<bool>("isActive");
        var roleStr = snapshot.GetValue<string>("role");

        if (!Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role))
            return null;

        var createdAt = snapshot.ContainsField("createdAt")
            ? snapshot.GetValue<Timestamp>("createdAt").ToDateTimeOffset()
            : DateTimeOffset.UtcNow;
        var updatedAt = snapshot.ContainsField("updatedAt")
            ? snapshot.GetValue<Timestamp>("updatedAt").ToDateTimeOffset()
            : createdAt;

        return User.FromPersistence(id, email, fullName, passwordHash, role, isActive, createdAt, updatedAt);
    }
}

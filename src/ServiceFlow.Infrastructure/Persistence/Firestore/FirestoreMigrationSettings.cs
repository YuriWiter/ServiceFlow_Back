using Microsoft.Extensions.Options;
using ServiceFlow.Application.Common.Abstractions;

namespace ServiceFlow.Infrastructure.Persistence.Firestore;

internal sealed class FirestoreMigrationSettings(IOptions<FirestoreOptions> options) : IFirestoreMigrationSettings
{
    public bool AuthReadFromFirestore =>
        options.Value.Enabled && options.Value.AuthReadFromFirestore;
}

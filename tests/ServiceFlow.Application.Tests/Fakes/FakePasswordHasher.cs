using ServiceFlow.Application.Common.Abstractions;

namespace ServiceFlow.Application.Tests.Fakes;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed::{password}";
    public bool Verify(string password, string hash) => hash == $"hashed::{password}";
}

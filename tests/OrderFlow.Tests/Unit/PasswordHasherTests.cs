using OrderFlow.Infrastructure.Auth;

namespace OrderFlow.Tests.Unit;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentHashes()
    {
        // BCrypt adds a random salt, so identical passwords don't produce identical hashes.
        // That stops attackers from spotting users who share a password.
        var first = _hasher.Hash("Passw0rd!");
        var second = _hasher.Hash("Passw0rd!");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Passw0rd!");
        Assert.True(_hasher.Verify("Passw0rd!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Passw0rd!");
        Assert.False(_hasher.Verify("wrong", hash));
    }

    [Fact]
    public void Hash_DoesNotContainThePlainPassword()
    {
        Assert.DoesNotContain("Passw0rd!", _hasher.Hash("Passw0rd!"));
    }
}
using AlertHawk.Application.Services;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using Moq;

namespace AlertHawk.Authentication.Tests.ServicesTests;

public class MobileAuthCodeServiceTests
{
    private readonly Mock<IMobileAuthCodeRepository> _repository = new();
    private readonly MobileAuthCodeService _service;

    public MobileAuthCodeServiceTests()
    {
        _service = new MobileAuthCodeService(_repository.Object);
    }

    [Fact]
    public async Task IssueAsync_StoresSingleUseCodeAndReturnsReadableForm()
    {
        var userId = Guid.NewGuid();
        string? stored = null;
        _repository
            .Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<DateTime>()))
            .Callback<string, Guid, DateTime>((code, _, _) => stored = code)
            .Returns(Task.CompletedTask);

        var issued = await _service.IssueAsync(userId);

        Assert.NotNull(stored);
        Assert.Equal(8, stored!.Length);
        Assert.DoesNotContain('-', stored);
        Assert.Equal($"{stored[..4]}-{stored[4..]}", issued.Code);
        Assert.Matches("^[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{4}-[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{4}$", issued.Code);
        Assert.InRange(
            issued.ExpiresAt,
            DateTime.UtcNow.AddMinutes(MobileAuthCodeService.LifetimeMinutes - 1),
            DateTime.UtcNow.AddMinutes(MobileAuthCodeService.LifetimeMinutes + 1));
        _repository.Verify(r => r.CreateAsync(stored, userId, issued.ExpiresAt), Times.Once);
    }

    [Fact]
    public async Task ConsumeAsync_NormalizesHyphensAndSpaces()
    {
        var userId = Guid.NewGuid();
        _repository.Setup(r => r.ConsumeAsync("ABCD2345")).ReturnsAsync(userId);

        var result = await _service.ConsumeAsync(" abcd-2345 ");

        Assert.Equal(userId, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABCD-234")]
    [InlineData("ABCD-IIII")]
    public async Task ConsumeAsync_RejectsInvalidCodes(string? code)
    {
        var result = await _service.ConsumeAsync(code);

        Assert.Null(result);
        _repository.Verify(r => r.ConsumeAsync(It.IsAny<string>()), Times.Never);
    }
}

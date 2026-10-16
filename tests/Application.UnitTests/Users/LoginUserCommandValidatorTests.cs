using Application.Users.Login;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Users;

public sealed class LoginUserCommandValidatorTests
{
    private const string Email = "user@example.com";
    private const string Password = "Str0ng!Passw0rd";

    private readonly LoginUserCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validator_Should_HaveError_WhenEmailIsInvalid(string email)
    {
        var command = new LoginUserCommand(email, Password);

        TestValidationResult<LoginUserCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validator_Should_HaveError_WhenPasswordIsEmpty()
    {
        var command = new LoginUserCommand(Email, string.Empty);

        TestValidationResult<LoginUserCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Validator_Should_HaveError_WhenPasswordExceedsMaxLength()
    {
        var command = new LoginUserCommand(Email, new string('a', 129));

        TestValidationResult<LoginUserCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Validator_Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new LoginUserCommand(Email, Password);

        TestValidationResult<LoginUserCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

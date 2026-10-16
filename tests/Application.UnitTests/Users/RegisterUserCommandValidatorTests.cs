using Application.Users.Register;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Users;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validator_Should_HaveError_WhenEmailIsInvalid(string email)
    {
        RegisterUserCommand command = ValidCommand() with { Email = email };

        TestValidationResult<RegisterUserCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validator_Should_HaveError_WhenPasswordIsShorterThanEightCharacters()
    {
        RegisterUserCommand command = ValidCommand() with { Password = "short" };

        TestValidationResult<RegisterUserCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Validator_Should_HaveError_WhenPasswordExceedsMaxLength()
    {
        RegisterUserCommand command = ValidCommand() with { Password = new string('a', 129) };

        TestValidationResult<RegisterUserCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Validator_Should_NotHaveErrors_WhenCommandIsValid()
    {
        RegisterUserCommand command = ValidCommand();

        TestValidationResult<RegisterUserCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static RegisterUserCommand ValidCommand() =>
        new("user@example.com", "Test", "User", "Str0ng!Passw0rd");
}

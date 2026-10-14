using Application.Abstractions.Behaviors;
using Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SharedKernel;
using Shouldly;

namespace Application.UnitTests.Behaviors;

public class LoggingDecoratorTests
{
    [Theory]
    [InlineData(ErrorType.Failure, LogLevel.Error)]
    [InlineData(ErrorType.Forbidden, LogLevel.Warning)]
    [InlineData(ErrorType.NotFound, LogLevel.Information)]
    [InlineData(ErrorType.Validation, LogLevel.Information)]
    [InlineData(ErrorType.Conflict, LogLevel.Information)]
    [InlineData(ErrorType.Problem, LogLevel.Information)]
    public async Task Command_Should_LogFailureAtItsOwnSeverity(ErrorType type, LogLevel expected)
    {
        ICommandHandler<TestCommand, Guid> inner = Substitute.For<ICommandHandler<TestCommand, Guid>>();
        inner.Handle(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Guid>(new Error("Test.Error", "Test failure", type)));

        var logger = new CapturingLogger<LoggingDecorator.CommandHandler<TestCommand, Guid>>();
        var decorator = new LoggingDecorator.CommandHandler<TestCommand, Guid>(inner, logger);

        await decorator.Handle(new TestCommand(), CancellationToken.None);

        logger.Levels.LastOrDefault().ShouldBe(expected);
    }

    [Fact]
    public async Task Query_Should_LogForbiddenAsWarning()
    {
        IQueryHandler<TestQuery, Guid> inner = Substitute.For<IQueryHandler<TestQuery, Guid>>();
        inner.Handle(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Guid>(new Error("Test.Forbidden", "Test failure", ErrorType.Forbidden)));

        var logger = new CapturingLogger<LoggingDecorator.QueryHandler<TestQuery, Guid>>();
        var decorator = new LoggingDecorator.QueryHandler<TestQuery, Guid>(inner, logger);

        await decorator.Handle(new TestQuery(), CancellationToken.None);

        logger.Levels.LastOrDefault().ShouldBe(LogLevel.Warning);
    }

    public sealed record TestCommand : ICommand<Guid>;

    public sealed record TestQuery : IQuery<Guid>;

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Levels.Add(logLevel);
    }
}

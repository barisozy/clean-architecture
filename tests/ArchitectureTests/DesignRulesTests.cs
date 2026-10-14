using Application.Abstractions.Messaging;
using NetArchTest.Rules;
using SharedKernel;
using Shouldly;

namespace ArchitectureTests;

public class DesignRulesTests : BaseTest
{
    public static TheoryData<Type> HandlerInterfaces => new()
    {
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>)
    };

    [Fact]
    public void Entities_Should_BeSealed()
    {
        PredicateList entities = Types.InAssembly(DomainAssembly).That().Inherit(typeof(Entity));

        entities.GetTypes().ShouldNotBeEmpty("the rule would pass vacuously");

        ShouldPass(entities.Should().BeSealed().GetResult());
    }

    [Fact]
    public void DomainEvents_Should_BeSealed()
    {
        PredicateList events = Types.InAssembly(DomainAssembly).That().ImplementInterface(typeof(IDomainEvent));

        events.GetTypes().ShouldNotBeEmpty("the rule would pass vacuously");

        ShouldPass(events.Should().BeSealed().GetResult());
    }

    [Fact]
    public void DomainEvents_Should_BeNamedWithDomainEventSuffix()
    {
        PredicateList events = Types.InAssembly(DomainAssembly).That().ImplementInterface(typeof(IDomainEvent));

        events.GetTypes().ShouldNotBeEmpty("the rule would pass vacuously");

        ShouldPass(events.Should().HaveNameEndingWith("DomainEvent").GetResult());
    }

    [Theory]
    [MemberData(nameof(HandlerInterfaces))]
    public void Handlers_Should_BeInternalAndSealed(Type handlerInterface)
    {
        PredicateList handlers = Types.InAssembly(ApplicationAssembly).That().ImplementInterface(handlerInterface);

        handlers.GetTypes().ShouldNotBeEmpty($"no type implements {handlerInterface.Name}: the rule would pass vacuously");

        ShouldPass(handlers.Should().BeSealed().And().NotBePublic().GetResult());
    }
}

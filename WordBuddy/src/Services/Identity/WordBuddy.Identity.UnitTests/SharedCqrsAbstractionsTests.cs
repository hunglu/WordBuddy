using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Identity.Application.Extensions;
using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Identity.UnitTests;

public class SharedCqrsAbstractionsTests
{
    private static readonly string[] ForbiddenNames = ["ICommand", "IQuery", "ICommandHandler", "IQueryHandler"];

    [Fact]
    public void ServiceCollectionExtensions_AddApplication_RegistersSharedHandlerInterfaces()
    {
        ServiceCollection services = new();

        services.AddApplication();

        services.Select(d => d.ServiceType)
            .Where(t => t.IsGenericType)
            .Select(t => t.GetGenericTypeDefinition())
            .Should().Contain(typeof(ICommandHandler<,>))
            .And.Contain(typeof(IQueryHandler<,>));
    }

    [Fact]
    public void ApplicationAssembly_Types_DoNotRedeclareSharedCqrsAbstractions()
    {
        System.Reflection.Assembly application = typeof(ServiceCollectionExtensions).Assembly;

        IEnumerable<string> offenders = application.GetTypes()
            .Where(t => ForbiddenNames.Contains(t.Name.Split('`')[0]))
            .Select(t => t.FullName!);

        offenders.Should().BeEmpty();
    }
}

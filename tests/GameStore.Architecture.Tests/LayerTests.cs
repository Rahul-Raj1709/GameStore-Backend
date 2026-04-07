using FluentAssertions;
using NetArchTest.Rules;
using Xunit; // <-- ADD THIS
using GameStore.Domain.Entities;
using GameStore.Application.Interfaces;
namespace GameStore.Architecture.Tests;

public class LayerTests
{
    private const string ApplicationNamespace = "GameStore.Application";
    private const string InfrastructureNamespace = "GameStore.Infrastructure";
    private const string WebApiNamespace = "GameStore.WebApi";

    [Fact]
    public void Domain_ShouldNot_HaveDependencyOnOtherProjects()
    {
        // 1. Point at the Domain assembly
        var assembly = typeof(Game).Assembly;

        var forbiddenProjects = new[]
        {
            ApplicationNamespace,
            InfrastructureNamespace,
            WebApiNamespace
        };

        // 2. Enforce the rule
        var testResult = Types
            .InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenProjects)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue("The Domain layer must never depend on outer layers.");
    }

    [Fact]
    public void Application_ShouldNot_HaveDependencyOnInfrastructureOrWebApi()
    {
        // 1. Point at the Application assembly
        var assembly = typeof(IApplicationDbContext).Assembly;

        var forbiddenProjects = new[]
        {
            InfrastructureNamespace,
            WebApiNamespace
        };

        // 2. Enforce the rule
        var testResult = Types
            .InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenProjects)
            .GetResult();

        testResult.IsSuccessful.Should().BeTrue("The Application layer must not depend on Infrastructure or WebApi.");
    }
}
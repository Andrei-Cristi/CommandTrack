using System.Reflection;
using CommandTrack.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace CommandTrack.Api.Tests;

public sealed class UnitsControllerRouteTests
{
    [Fact]
    public void Create_UsesCollectionPostRoute()
    {
        MethodInfo method = GetAction(nameof(UnitsController.Create));

        HttpPostAttribute route = Assert.Single(
            method.GetCustomAttributes<HttpPostAttribute>());

        Assert.Null(route.Template);
    }

    [Fact]
    public void RegisterHeartbeat_UsesHeartbeatPostRouteOnly()
    {
        MethodInfo method = GetAction(
            nameof(UnitsController.RegisterHeartbeat));

        HttpPostAttribute route = Assert.Single(
            method.GetCustomAttributes<HttpPostAttribute>());

        Assert.Equal("{id:guid}/heartbeat", route.Template);
    }

    private static MethodInfo GetAction(string name)
    {
        return typeof(UnitsController).GetMethod(name)
            ?? throw new InvalidOperationException(
                $"Action '{name}' was not found.");
    }
}

using System;
using System.Linq;
using AIQ.Urban.Core.Models;
using NUnit.Framework;

namespace AIQ.Urban.Core.Tests.Models;

public class DefaultUsesTests
{
    [Test]
    public void CreateDefaults_ReturnsExpectedCount()
    {
        var uses = DefaultUses.CreateDefaults();

        Assert.That(uses, Has.Count.EqualTo(DefaultUses.All.Count));
    }

    [Test]
    public void CreateDefaults_AllHaveUniqueIds()
    {
        var uses = DefaultUses.CreateDefaults();

        var ids = uses.Select(u => u.Id).ToList();
        Assert.That(ids, Is.Unique);
    }

    [Test]
    public void CreateDefaults_ContainsExpectedNames()
    {
        var uses = DefaultUses.CreateDefaults();
        var names = uses.Select(u => u.Name).ToList();

        Assert.That(names, Does.Contain("Residential"));
        Assert.That(names, Does.Contain("Commercial"));
        Assert.That(names, Does.Contain(DefaultUses.UndefinedName));
    }

    [Test]
    public void CreateDefaults_EachInstanceIsDistinct()
    {
        var uses1 = DefaultUses.CreateDefaults();
        var uses2 = DefaultUses.CreateDefaults();

        Assert.That(uses1[0], Is.Not.SameAs(uses2[0]));
    }
}

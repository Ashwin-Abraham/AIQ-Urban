using System;
using AIQ.Urban.Core.Models;
using NUnit.Framework;

namespace AIQ.Urban.Core.Tests.Models;

public class DefaultUsesTests
{
    [Test]
    [TestCase("Residential")]
    [TestCase("residential")]
    [TestCase("RESIDENTIAL")]
    [TestCase("Commercial")]
    [TestCase("Retail")]
    [TestCase("Industrial")]
    [TestCase("MixedUse")]
    [TestCase("Public")]
    [TestCase("Park")]
    [TestCase("Parking")]
    [TestCase("Utility")]
    public void Resolve_ValidName_ReturnsUse(string name)
    {
        var result = DefaultUses.Resolve(name);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(name).IgnoreCase);
    }

    [Test]
    [TestCase("InvalidUse")]
    [TestCase("Residential ")]
    public void Resolve_InvalidName_Throws(string name)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DefaultUses.Resolve(name));
        Assert.That(ex!.Message, Does.Contain("Unknown use name"));
        Assert.That(ex.Message, Does.Contain("Residential"));
    }
}

using System;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Extensions;
using NUnit.Framework;

namespace AIQ.Urban.Core.Tests.Extensions;

public class StringExtensionTests
{
    [Test]
    [TestCase("Road", TransitType.Road)]
    [TestCase("road", TransitType.Road)]
    [TestCase("ROAD", TransitType.Road)]
    [TestCase("Railway", TransitType.Railway)]
    [TestCase("CyclePath", TransitType.CyclePath)]
    [TestCase("Pedestrian", TransitType.Pedestrian)]
    [TestCase("Waterway", TransitType.Waterway)]
    public void ParseToTransitType_ValidName_ReturnsEnum(string name, TransitType expected)
    {
        var result = name.ParseToTransitType();

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase("InvalidType")]
    [TestCase("Roadd")]
    public void ParseToTransitType_InvalidName_Throws(string name)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            name.ParseToTransitType());
        Assert.That(ex!.Message, Does.Contain("Unknown transit type"));
        Assert.That(ex.Message, Does.Contain("Road"));
    }
}

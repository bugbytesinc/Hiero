// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8602, CS8604 // Null assignments and dereferences are intentional in these tests
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Core;

public class SemanticVersionTests
{
    [Test]
    public async Task Equivalent_SemanticVersions_Are_Considered_Equal()
    {
        var major = Generator.Integer(0, 200);
        var minor = Generator.Integer(0, 200);
        var patch = Generator.Integer(0, 200);
        var sv1 = new SemanticVersion(major, minor, patch);
        var sv2 = new SemanticVersion(major, minor, patch);
        await Assert.That(sv1).IsEqualTo(sv2);
        await Assert.That(sv1 == sv2).IsTrue();
        await Assert.That(sv1 != sv2).IsFalse();
        await Assert.That(sv1.Equals(sv2)).IsTrue();
        await Assert.That(sv2.Equals(sv1)).IsTrue();
        await Assert.That(null as SemanticVersion == null as SemanticVersion).IsTrue();
    }

    [Test]
    public async Task Disimilar_SemanticVersions_Are_Not_Considered_Equal()
    {
        var major = Generator.Integer(0, 200);
        var minor = Generator.Integer(0, 200);
        var patch = Generator.Integer(0, 200);
        var sv = new SemanticVersion(major, minor, patch);
        await Assert.That(sv).IsNotEqualTo(new SemanticVersion(major, minor + 1, patch));
        await Assert.That(sv).IsNotEqualTo(new SemanticVersion(major + 1, minor, patch));
        await Assert.That(sv).IsNotEqualTo(new SemanticVersion(major, minor, patch + 1));
        await Assert.That(sv == new SemanticVersion(major, minor, patch + 1)).IsFalse();
        await Assert.That(sv != new SemanticVersion(major, minor, patch + 1)).IsTrue();
        await Assert.That(sv.Equals(new SemanticVersion(major + 1, minor, patch))).IsFalse();
        await Assert.That(sv.Equals(new SemanticVersion(major, minor + 1, patch))).IsFalse();
        await Assert.That(sv.Equals(new SemanticVersion(major, minor, patch + 1))).IsFalse();
    }

    [Test]
    public async Task Comparing_With_Null_Is_Not_Considered_Equal()
    {
        object asNull = null;
        var sv = new SemanticVersion(Generator.Integer(0, 200), Generator.Integer(0, 200), Generator.Integer(0, 200));
        await Assert.That(sv == null).IsFalse();
        await Assert.That(null == sv).IsFalse();
        await Assert.That(sv != null).IsTrue();
        await Assert.That(sv.Equals(null as SemanticVersion)).IsFalse();
        await Assert.That(sv.Equals(asNull)).IsFalse();
    }

    [Test]
    public async Task Other_Objects_Are_Not_Considered_Equal()
    {
        var sv = new SemanticVersion(Generator.Integer(0, 200), Generator.Integer(0, 200), Generator.Integer(0, 200));
        await Assert.That(sv.Equals("Something that is not a SemanticVersion")).IsFalse();
    }

    [Test]
    public async Task Cast_As_Object_Is_Considered_Equal()
    {
        var major = Generator.Integer(0, 200);
        var minor = Generator.Integer(0, 200);
        var patch = Generator.Integer(0, 200);
        var sv = new SemanticVersion(major, minor, patch);
        object equivalent = new SemanticVersion(major, minor, patch);
        await Assert.That(sv.Equals(equivalent)).IsTrue();
        await Assert.That(equivalent.Equals(sv)).IsTrue();
    }

    [Test]
    public async Task Reference_Equal_Is_Considered_Equal()
    {
        var sv = new SemanticVersion(Generator.Integer(0, 200), Generator.Integer(0, 200), Generator.Integer(0, 200));
        object reference = sv;
        await Assert.That(sv.Equals(reference)).IsTrue();
        await Assert.That(reference.Equals(sv)).IsTrue();
    }

    [Test]
    public async Task Equal_SemanticVersions_Have_Equal_HashCodes()
    {
        var major = Generator.Integer(0, 200);
        var minor = Generator.Integer(0, 200);
        var patch = Generator.Integer(0, 200);
        var sv1 = new SemanticVersion(major, minor, patch);
        var sv2 = new SemanticVersion(major, minor, patch);
        await Assert.That(sv1.GetHashCode()).IsEqualTo(sv2.GetHashCode());
    }

    [Test]
    public async Task Properties_Are_Mapped_Correctly()
    {
        var major = Generator.Integer(0, 200);
        var minor = Generator.Integer(0, 200);
        var patch = Generator.Integer(0, 200);
        var sv = new SemanticVersion(major, minor, patch);
        await Assert.That(sv.Major).IsEqualTo(major);
        await Assert.That(sv.Minor).IsEqualTo(minor);
        await Assert.That(sv.Patch).IsEqualTo(patch);
    }

    [Test]
    public async Task None_Has_Zero_Values()
    {
        var none = SemanticVersion.None;
        await Assert.That(none.Major).IsEqualTo(0);
        await Assert.That(none.Minor).IsEqualTo(0);
        await Assert.That(none.Patch).IsEqualTo(0);
    }

    [Test]
    public async Task Negative_Major_Throws_Error()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new SemanticVersion(-1, 0, 0);
        });
        await Assert.That(exception.ParamName).IsEqualTo("major");
        await Assert.That(exception.Message).StartsWith("Major Version Number cannot be negative.");
    }

    [Test]
    public async Task Negative_Minor_Throws_Error()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new SemanticVersion(0, -1, 0);
        });
        await Assert.That(exception.ParamName).IsEqualTo("minor");
        await Assert.That(exception.Message).StartsWith("Minor Version Number cannot be negative.");
    }

    [Test]
    public async Task Negative_Patch_Throws_Error()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new SemanticVersion(0, 0, -1);
        });
        await Assert.That(exception.ParamName).IsEqualTo("patch");
        await Assert.That(exception.Message).StartsWith("Patch Version Number cannot be negative.");
    }

    [Test]
    public async Task ToString_Contains_Version_Components()
    {
        var sv = new SemanticVersion(1, 2, 3);
        var result = sv.ToString();
        await Assert.That(result).Contains("1");
        await Assert.That(result).Contains("2");
        await Assert.That(result).Contains("3");
    }

    [Test]
    public async Task Internal_Proto_Constructor_Maps_Values()
    {
        var proto = new Proto.SemanticVersion { Major = 10, Minor = 20, Patch = 30 };
        var sv = new SemanticVersion(proto);
        await Assert.That(sv.Major).IsEqualTo(10);
        await Assert.That(sv.Minor).IsEqualTo(20);
        await Assert.That(sv.Patch).IsEqualTo(30);
    }

    [Test]
    public async Task Internal_Proto_Constructor_With_Null_Defaults_To_None()
    {
        var sv = new SemanticVersion(null as Proto.SemanticVersion);
        await Assert.That(sv).IsEqualTo(SemanticVersion.None);
    }
}

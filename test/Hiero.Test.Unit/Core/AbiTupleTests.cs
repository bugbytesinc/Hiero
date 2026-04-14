// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Test.Unit.Core;

public class AbiTupleTests
{
    [Test]
    public async Task Constructor_StoresValues()
    {
        var values = new object[] { 1, "hello", true };
        var tuple = new AbiTuple(values);
        await Assert.That(tuple.Values).IsEquivalentTo(values);
    }

    [Test]
    public async Task Values_ReturnsSameArray()
    {
        var values = new object[] { 42, "test" };
        var tuple = new AbiTuple(values);
        var isSame = ReferenceEquals(tuple.Values, values);
        await Assert.That(isSame).IsTrue();
    }

    [Test]
    public async Task EmptyTuple_HasEmptyValues()
    {
        var tuple = new AbiTuple();
        var length = tuple.Values.Length;
        var expected = 0;
        await Assert.That(length).IsEqualTo(expected);
    }

    [Test]
    public async Task SingleValueTuple_ContainsOneElement()
    {
        var tuple = new AbiTuple(42);
        var length = tuple.Values.Length;
        var expected = 1;
        await Assert.That(length).IsEqualTo(expected);
        var firstValue = tuple.Values[0];
        await Assert.That(firstValue).IsEqualTo((object)42);
    }

    [Test]
    public async Task MultipleValuesTuple_ContainsAllElements()
    {
        var tuple = new AbiTuple(1, "two", 3.0, true);
        var length = tuple.Values.Length;
        var expected = 4;
        await Assert.That(length).IsEqualTo(expected);
    }
}

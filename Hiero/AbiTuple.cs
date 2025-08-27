namespace Hiero;
/// <summary>
/// Represents a tuple of values that can be used in smart contract calls.
/// </summary>
public class AbiTuple
{
    /// <summary>
    /// The values contained in the tuple.
    /// </summary>
    private readonly object[] _values;
    /// <summary>
    /// Gets the values contained in the tuple.
    /// </summary>
    public object[] Values => _values;
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTuple"/> 
    /// class with the specified values.
    /// </summary>
    /// <param name="values">
    /// Values to initialize the tuple with.
    /// </param>
    public AbiTuple(params object[] values)
    {
        _values = values;
    }
}

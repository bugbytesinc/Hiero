using System.Runtime.CompilerServices;

namespace Hiero.Implementation;

internal struct ContextValue<T>
{
    private bool _hasValue;
    private T _value;

    public bool HasValue => _hasValue;
    public T Value => _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(T value)
    {
        _value = value;
        _hasValue = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _value = default!;
        _hasValue = false;
    }
}

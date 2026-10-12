// SPDX-License-Identifier: Apache-2.0
using System.Runtime.CompilerServices;

namespace Hiero.Implementation;

/// <summary>
/// A context slot that distinguishes "explicitly set" from "inherit from
/// parent". Readers test <see cref="HasValue"/> and fall back to the parent
/// context when it is false.
/// </summary>
/// <remarks>
/// <para>
/// <b>Field write order is deliberate — do not "tidy" it.</b> This is two
/// independent fields with no barrier between them, so a reader on another
/// thread can observe one write without the other. Configuring a client while
/// operations are in flight is documented as unsupported, but these orderings
/// make that mistake degrade harmlessly instead of silently producing a wrong
/// value:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <see cref="Set"/> writes the value <i>before</i> raising the flag, so a
/// reader seeing <see cref="HasValue"/> never reads a stale or default value
/// alongside it.
/// </description></item>
/// <item><description>
/// <see cref="Reset"/> lowers the flag <i>before</i> clearing the value, so a
/// reader mid-reset falls back to the parent context — the correct answer —
/// rather than seeing <see cref="HasValue"/> paired with a cleared value.
/// </description></item>
/// </list>
/// <para>
/// This narrows the window rather than closing it: without a memory barrier a
/// weakly ordered architecture may still reorder the pair. Closing it properly
/// would mean immutable contexts or locking on every read, and neither is
/// warranted for a case the public API already tells callers not to create.
/// Barriers are left out by choice, not necessity:
/// <see cref="System.Threading.Volatile"/> has non-generic <c>bool</c>
/// overloads, so release/acquire ordering on the flag would be available for
/// any <typeparamref name="T"/>.
/// </para>
/// </remarks>
internal struct ContextValue<T>
{
    private bool _hasValue;
    private T _value;

    public bool HasValue => _hasValue;
    public T Value => _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(T value)
    {
        // Value first, then the flag: this narrows the window in which a
        // concurrent reader sees the flag without the value it belongs to,
        // but guarantees nothing without a barrier (see the remarks above).
        _value = value;
        _hasValue = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        // Flag first, then the value: a concurrent reader mid-reset falls
        // through to the parent context rather than reading a cleared value
        // as though it had been set.
        _hasValue = false;
        _value = default!;
    }
}

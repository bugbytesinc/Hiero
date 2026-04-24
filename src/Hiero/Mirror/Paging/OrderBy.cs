// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Filters;

namespace Hiero.Mirror.Paging;
/// <summary>
/// Sets the sort direction for results returned by the mirror node.
/// Paging directive — does not change which records are returned, only
/// the order they appear in the response. Use the static <see cref="Ascending"/>
/// or <see cref="Descending"/> instances; the constructor is private.
/// </summary>
public class OrderBy : IMirrorPaging
{
    /// <summary>
    /// Requests that results be returned in ascending order.
    /// </summary>
    public static OrderBy Ascending = new OrderBy(true);
    /// <summary>
    /// Requests that results be returned in descending order.
    /// </summary>
    public static OrderBy Descending = new OrderBy(false);
    /// <summary>
    /// Flag indicating the sorting order
    /// is ascending (otherwise descending).
    /// </summary>
    private readonly bool _ascending;
    /// <summary>
    /// Private constructor taking argument
    /// of ascending or not.
    /// </summary>
    /// <param name="ascending">
    /// true if the desired order is ascending.
    /// </param>
    private OrderBy(bool ascending)
    {
        _ascending = ascending;
    }
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "order";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value => _ascending ? "asc" : "desc";
}

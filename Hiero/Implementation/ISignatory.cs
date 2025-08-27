using System.Threading.Tasks;

namespace Hiero.Implementation;

/// <summary>
/// Internal interface implemented by objects that 
/// can sign transactions.  Not intended for public use.
/// </summary>
internal interface ISignatory
{
    Task SignAsync(IInvoice invoice);
    PendingParams? GetSchedule();
    (byte[] R, byte[] S, int RevoeryId) SignEvm(byte[] data);
}
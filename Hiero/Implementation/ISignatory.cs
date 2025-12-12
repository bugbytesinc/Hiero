namespace Hiero.Implementation;

/// <summary>
/// Internal interface implemented by objects that 
/// can sign transactions.  Not intended for public use.
/// </summary>
internal interface ISignatory
{
    ValueTask SignAsync(IInvoice invoice);
    PendingParams? GetSchedule();
    (byte[] R, byte[] S, int RecoveryId) SignEvm(byte[] data);
}
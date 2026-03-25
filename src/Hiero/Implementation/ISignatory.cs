// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Implementation;

/// <summary>
/// Internal interface implemented by objects that 
/// can sign transactions.  Not intended for public use.
/// </summary>
internal interface ISignatory
{
    ValueTask SignAsync(IInvoice invoice);
    (byte[] R, byte[] S, int RecoveryId) SignEvm(byte[] data);
}
// SPDX-License-Identifier: Apache-2.0
//
// Compile-backed doc snippets for the File Service domain. See
// CryptoSnippets.cs for the authoring convention. Basic create/append live
// in samples/FileService/Program.cs; this file covers update and delete.

using System.Text;
using Hiero;

namespace DocSnippets;

public static class FileSnippets
{
    public static async Task UpdateFile(
        ConsensusClient client, EntityId file)
    {
        #region UpdateFile
        // Update mutable file properties (memo, expiration, key list). Only
        // non-null properties are changed — leaving a field null preserves
        // it. To replace contents entirely, use UpdateFileParams.Contents or
        // create a new file.
        var receipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = file,
            Memo = "Updated memo",
            Expiration = new ConsensusTimeStamp(DateTime.UtcNow.AddDays(90))
        });
        Console.WriteLine($"Update status: {receipt.Status}");
        #endregion
    }

    public static async Task ReplaceFileContents(
        ConsensusClient client, EntityId file, byte[] newContents)
    {
        #region UpdateFileContents
        // Overwrite the file's contents. The update transaction has a 4KB
        // payload limit — for larger replacements, truncate and append via
        // AppendFileAsync in chunks.
        var receipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = file,
            Contents = newContents
        });
        Console.WriteLine($"Update status: {receipt.Status}");
        #endregion
    }

    public static async Task DeleteFile(
        ConsensusClient client, EntityId file)
    {
        #region DeleteFile
        // Permanently delete a file. At least one of the file's key-list
        // endorsements must sign. Deleted files cannot be recovered.
        var receipt = await client.DeleteFileAsync(new DeleteFileParams
        {
            File = file
        });
        Console.WriteLine($"Delete status: {receipt.Status}");
        #endregion
    }
}

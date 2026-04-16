// SPDX-License-Identifier: Apache-2.0
//
// Compile-backed doc snippets for the AddressBook + Root (system admin)
// domains. See CryptoSnippets.cs for the authoring convention.
//
// IMPORTANT: every snippet in this file requires the client's Payer to be a
// privileged Hedera account (council node operator or superuser like 0.0.2 /
// 0.0.50 / 0.0.55). They will NOT work when signed by a normal account.

using Hiero;

namespace DocSnippets;

public static class GovernanceSnippets
{
    // -----------------------------------------------------------------
    // AddressBook
    // -----------------------------------------------------------------

    public static async Task AddConsensusNode(
        ConsensusClient client,
        EntityId newNodeAccount,
        Endorsement nodeAdminKey,
        byte[] gossipCaCert)
    {
        #region AddConsensusNode
        // Register a new consensus node in the network address book. Only
        // a council-privileged payer can invoke this. The receipt carries
        // the network-assigned NodeId which is required by all later
        // UpdateConsensusNode / RemoveConsensusNode calls.
        var receipt = await client.AddConsensusNodeAsync(new AddConsensusNodeParams
        {
            Account = newNodeAccount,
            Description = "Regional operator node",
            GossipEndpoints = new[] { new Uri("tcp://10.0.0.1:50111") },
            ServiceEndpoints = new[] { new Uri("https://rpc.example.com:50211") },
            GossipCaCertificate = gossipCaCert,
            AdminKey = nodeAdminKey,
            DeclineReward = false
        });
        Console.WriteLine($"Assigned NodeId: {receipt.NodeId}");
        #endregion
    }

    public static async Task UpdateConsensusNode(
        ConsensusClient client, ulong nodeId)
    {
        #region UpdateConsensusNode
        // Update mutable fields on an existing consensus node. Null fields
        // are preserved. The node's AdminKey (set at add time) must sign.
        // Note: the target is identified by NodeId (ulong), not EntityId.
        var receipt = await client.UpdateConsensusNodeAsync(new UpdateConsensusNodeParams
        {
            NodeId = nodeId,
            Description = "Renamed operator",
            DeclineReward = true
        });
        Console.WriteLine($"Update status: {receipt.Status}");
        #endregion
    }

    public static async Task RemoveConsensusNode(ConsensusClient client, ulong nodeId)
    {
        #region RemoveConsensusNode
        // Remove a node from the address book. Effective at the next network
        // stake-weight rebalance. The node's AdminKey must sign.
        var receipt = await client.RemoveConsensusNodeAsync(nodeId);
        Console.WriteLine($"Remove status: {receipt.Status}");
        #endregion
    }

    // -----------------------------------------------------------------
    // Root / Network upgrade orchestration
    //
    // These follow the classic Hedera upgrade dance:
    //   1. Prepare  — upload the upgrade image and freeze it for validation.
    //   2. Schedule — commit to a consensus time when nodes will restart.
    //   3. Abort    — cancel a scheduled upgrade before it fires.
    // -----------------------------------------------------------------

    public static async Task PrepareNetworkUpgrade(
        ConsensusClient client, EntityId upgradeFile, byte[] expectedHash)
    {
        #region PrepareNetworkUpgrade
        // Stage a network upgrade: nodes will validate the file's contents
        // against `FileHash` before accepting the subsequent schedule step.
        // Requires the superuser payer (typically 0.0.50 or 0.0.55).
        var receipt = await client.PrepareNetworkUpgradeAsync(new PrepareNetworkUpgradeParams
        {
            File = upgradeFile,
            FileHash = expectedHash
        });
        Console.WriteLine($"Prepare status: {receipt.Status}");
        #endregion
    }

    public static async Task ScheduleNetworkUpgrade(ConsensusClient client)
    {
        #region ScheduleNetworkUpgrade
        // After a successful PrepareNetworkUpgrade, commit to a consensus
        // timestamp when nodes will bounce and load the new image.
        var receipt = await client.ScheduleNetworkUpgradeAsync(new ScheduleNetworkUpgradeParams
        {
            Consensus = new ConsensusTimeStamp(DateTime.UtcNow.AddHours(2))
        });
        Console.WriteLine($"Schedule status: {receipt.Status}");
        #endregion
    }

    public static async Task AbortNetworkUpgrade(ConsensusClient client)
    {
        #region AbortNetworkUpgrade
        // Cancel a currently-prepared or scheduled upgrade. No-op if there
        // is no pending upgrade. The superuser key must sign.
        var receipt = await client.AbortNetworkUpgradeAsync(new AbortNetworkUpgradeParams());
        Console.WriteLine($"Abort status: {receipt.Status}");
        #endregion
    }

    public static async Task ScheduleTelemetryUpgrade(ConsensusClient client)
    {
        #region ScheduleTelemetryUpgrade
        // Schedule a telemetry-config refresh across the node fleet. Used by
        // Hedera operators to roll out observability changes without a full
        // software upgrade.
        var receipt = await client.ScheduleTelemetryUpgradeAsync(new ScheduleTelemetryUpgradeParams());
        Console.WriteLine($"Telemetry schedule status: {receipt.Status}");
        #endregion
    }

    public static async Task SuspendNetwork(ConsensusClient client)
    {
        #region SuspendNetwork
        // Freeze the entire network at a future consensus timestamp. Every
        // node will stop accepting transactions at that moment. Requires
        // the superuser payer — reserved for emergency maintenance.
        var receipt = await client.SuspendNetworkAsync(new SuspendNetworkParams
        {
            Consensus = new ConsensusTimeStamp(DateTime.UtcNow.AddHours(1))
        });
        Console.WriteLine($"Suspend status: {receipt.Status}");
        #endregion
    }

    // -----------------------------------------------------------------
    // Root / System delete and restore.
    // These bypass the usual Administrator-key gate and act on behalf of
    // the network itself. They exist to surgically remove malicious or
    // legally-problematic state without touching consensus history.
    // -----------------------------------------------------------------

    public static async Task SystemDeleteFile(
        ConsensusClient client, EntityId file)
    {
        #region SystemDeleteFile
        // Superuser-only: delete a file that is otherwise undeletable
        // (e.g. an immutable file). The file becomes inaccessible but can
        // be restored via SystemRestoreFileAsync within a grace window.
        var receipt = await client.SystemDeleteFileAsync(new SystemDeleteFileParams
        {
            File = file
        });
        Console.WriteLine($"System delete status: {receipt.Status}");
        #endregion
    }

    public static async Task SystemRestoreFile(
        ConsensusClient client, EntityId file)
    {
        #region SystemRestoreFile
        // Undo a previous SystemDeleteFileAsync within the grace window.
        var receipt = await client.SystemRestoreFileAsync(new SystemRestoreFileParams
        {
            File = file
        });
        Console.WriteLine($"System restore status: {receipt.Status}");
        #endregion
    }

    public static async Task SystemDeleteContract(
        ConsensusClient client, EntityId contract)
    {
        #region SystemDeleteContract
        // Superuser-only contract deletion, analogous to SystemDeleteFile.
        // Bypasses the contract's Administrator key check. Restorable.
        var receipt = await client.SystemDeleteContractAsync(new SystemDeleteContractParams
        {
            Contract = contract
        });
        Console.WriteLine($"System delete status: {receipt.Status}");
        #endregion
    }

    public static async Task SystemRestoreContract(
        ConsensusClient client, EntityId contract)
    {
        #region SystemRestoreContract
        // Undo a previous SystemDeleteContractAsync within the grace window.
        var receipt = await client.SystemRestoreContractAsync(new SystemRestoreContractParams
        {
            Contract = contract
        });
        Console.WriteLine($"System restore status: {receipt.Status}");
        #endregion
    }
}

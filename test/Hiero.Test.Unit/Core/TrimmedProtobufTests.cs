// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Test.Unit.Core;

/// <summary>
/// Guards the protos and proto lines trimmed from the vendored tree (see
/// reference/hedera-protobufs/TRIMMED.md): deprecated operations, an
/// internal-only node operation, a query the networks retired, and protos
/// nothing references.
/// </summary>
/// <remarks>
/// This exists because a refresh reverted the trims silently once already:
/// the v0.74.0 vendor run restored ~7,500 lines of dead generated code and
/// nothing noticed for weeks. Git records that something changed; it cannot
/// record that the change was unwanted. This test does.
/// </remarks>
public class TrimmedProtobufTests
{
    // Types defined only by protos excluded from the vendored tree. If any of
    // these resolves, the trim has been reverted — most likely by a refresh
    // whose build failure was "fixed" by dropping CN_TRIMMED_PROTOS from
    // scripts/vendor-blocknode-protos.sh rather than re-applying the field
    // comments listed in TRIMMED.md.
    private static readonly string[] TrimmedTypeNames =
    [
        "Proto.GetByKeyQuery",
        "Proto.GetByKeyResponse",
        "Proto.GetBySolidityIDQuery",
        "Proto.GetBySolidityIDResponse",
        "Proto.CryptoGetLiveHashQuery",
        "Proto.CryptoGetLiveHashResponse",
        "Proto.CryptoGetStakersQuery",
        "Proto.CryptoGetStakersResponse",
        "Proto.TransactionGetFastRecordQuery",
        "Proto.TransactionGetFastRecordResponse",
        "Proto.NetworkGetExecutionTimeQuery",
        "Proto.NetworkGetExecutionTimeResponse",
        "Proto.LiveHash",
        "Proto.CryptoAddLiveHashTransactionBody",
        "Proto.CryptoDeleteLiveHashTransactionBody",
        "Proto.UncheckedSubmitBody",
        // Internal-only node operation.
        "Com.Hedera.Hapi.Node.Hooks.HookDispatchTransactionBody",
        // Retired by the public networks (July 2026).
        "Proto.CryptoGetAccountBalanceQuery",
        "Proto.CryptoGetAccountBalanceResponse",
        // Unreferenced protos: one type from each.
        "Com.Hedera.Hapi.Block.Stream.Output.CreateTopicOutput",
        "Com.Hedera.Hapi.Block.Stream.Output.AppendFileOutput",
        "Com.Hedera.Hapi.Block.Stream.Output.UpdateNodeStakeOutput",
        "Com.Hedera.Hapi.Block.Stream.Output.CreateTokenOutput",
        "Com.Hedera.Hapi.Platform.Event.EventConsensusData",
        "Com.Hedera.Hapi.Platform.Event.GossipEvent",
        "Com.Hedera.Hapi.Services.Auxiliary.Tss.TssEncryptionKeyTransactionBody",
        "Proto.ThrottleDefinitions",
        "Org.Hiero.Block.Api.NetworkData",
        "Org.Hiero.Block.Api.ReconnectRequest",
    ];

    [Test]
    public async Task Deprecated_Protobuf_Types_Stay_Trimmed()
    {
        var assembly = typeof(Proto.Query).Assembly;
        var restored = TrimmedTypeNames.Where(name => assembly.GetType(name) is not null).ToList();

        await Assert.That(restored).IsEmpty();
    }

    [Test]
    public async Task Trimmed_Proto_Lines_Stay_Trimmed()
    {
        // LambdaSStore is a line trim in basic_types.proto with no excluded
        // file behind it, so a refresh restores it without failing the build
        // — which is exactly how it was lost during the v0.74.0 refresh. This
        // test is its only guard. (The hook_dispatch field's import of an
        // excluded file fails the build first; this is the backstop.)
        await Assert.That(Enum.GetNames<Proto.HederaFunctionality>()).DoesNotContain("LambdaSstore");
        await Assert.That(Enum.IsDefined(typeof(Proto.HederaFunctionality), 109)).IsFalse();
        await Assert.That(Enum.GetNames<Proto.TransactionBody.DataOneofCase>()).DoesNotContain("HookDispatch");
    }

    [Test]
    public async Task Guard_Resolves_Types_That_Do_Exist()
    {
        // Without this, the deny-list test would pass just as happily if the
        // namespace were renamed or the lookup silently stopped working.
        var assembly = typeof(Proto.Query).Assembly;

        await Assert.That(assembly.GetType("Proto.CryptoGetInfoResponse")).IsNotNull();
        await Assert.That(assembly.GetType("Proto.TransactionBody")).IsNotNull();
        await Assert.That(assembly.GetType("Com.Hedera.Hapi.Block.Stream.Output.TransactionOutput")).IsNotNull();
        await Assert.That(assembly.GetType("Org.Hiero.Block.Api.ServerStatusRequest")).IsNotNull();
        // Neighbours of the trimmed lines, so the line guard is known to look
        // in the right place.
        await Assert.That(Enum.IsDefined(typeof(Proto.HederaFunctionality), 110)).IsTrue();
        await Assert.That(Enum.GetNames<Proto.TransactionBody.DataOneofCase>()).Contains("HookStore");
    }

    [Test]
    public async Task Live_Hash_Response_Codes_Are_Retained()
    {
        // The EMPTY_LIVE_HASH_* codes are wire values that still exist
        // upstream, so they stay even though the live-hash protos are gone.
        // Trimming them would break ResponseCode_Has_All_Protobuf_Values.
        await Assert.That(Enum.IsDefined(typeof(ResponseCode), ResponseCode.EmptyLiveHashBody)).IsTrue();
        await Assert.That(Enum.IsDefined(typeof(ResponseCode), ResponseCode.LiveHashNotFound)).IsTrue();
    }
}

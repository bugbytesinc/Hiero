// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Hiero.Mirror;
using Hiero.Test.Helpers;
using Proto;
using System.Net;

namespace Hiero.Test.Unit.Core;

public class PrecheckExceptionTests
{
    [Test]
    public async Task Constructor_Maps_All_Properties()
    {
        var txId = new TransactionId(new EntityId(0, 0, Generator.Integer(10, 200)), DateTime.UtcNow);
        var code = ResponseCode.InsufficientTxFee;
        var fee = (ulong)Generator.Integer(1000, 5000);
        var message = "Precheck failed";
        var ex = new PrecheckException(message, txId, code, fee);
        await Assert.That(ex.Message).IsEqualTo(message);
        await Assert.That(ex.TransactionId).IsEqualTo(txId);
        await Assert.That(ex.Status).IsEqualTo(code);
        await Assert.That(ex.RequiredFee).IsEqualTo(fee);
    }

    [Test]
    public async Task Constructor_With_InnerException_Maps_All_Properties()
    {
        var txId = new TransactionId(new EntityId(0, 0, Generator.Integer(10, 200)), DateTime.UtcNow);
        var code = ResponseCode.InvalidTransaction;
        var fee = 0UL;
        var inner = new InvalidOperationException("inner");
        var ex = new PrecheckException("Precheck failed", txId, code, fee, inner);
        await Assert.That(ex.InnerException).IsEqualTo(inner);
        await Assert.That(ex.Status).IsEqualTo(code);
        await Assert.That(ex.TransactionId).IsEqualTo(txId);
        await Assert.That(ex.RequiredFee).IsEqualTo(fee);
    }

    [Test]
    public async Task Is_Exception()
    {
        var txId = new TransactionId(new EntityId(0, 0, 5), DateTime.UtcNow);
        var ex = new PrecheckException("test", txId, ResponseCode.Ok, 0);
        await Assert.That(ex).IsAssignableTo<Exception>();
    }
}

public class TransactionExceptionTests
{
    [Test]
    public async Task Constructor_Maps_Receipt_And_Derived_Properties()
    {
        var payer = new EntityId(0, 0, Generator.Integer(10, 200));
        var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(false);
        var protoTxId = new TransactionID
        {
            AccountID = new AccountID(payer),
            TransactionValidStart = new Timestamp { Seconds = seconds, Nanos = nanos }
        };
        var protoReceipt = new Proto.TransactionReceipt
        {
            Status = ResponseCodeEnum.AccountRepeatedInAccountAmounts
        };
        var receipt = new TransactionReceipt(protoTxId, protoReceipt);
        var ex = new TransactionException("Transaction failed", receipt);
        await Assert.That(ex.Message).IsEqualTo("Transaction failed");
        await Assert.That(ex.Receipt).IsEqualTo(receipt);
        await Assert.That(ex.Status).IsEqualTo(ResponseCode.AccountRepeatedInAccountAmounts);
        await Assert.That(ex.TransactionId).IsEqualTo(protoTxId.AsTransactionId());
    }

    [Test]
    public async Task Is_Exception()
    {
        var protoTxId = new TransactionID
        {
            AccountID = new AccountID(EntityId.None),
            TransactionValidStart = new Timestamp { Seconds = 0, Nanos = 0 }
        };
        var protoReceipt = new Proto.TransactionReceipt
        {
            Status = ResponseCodeEnum.Ok
        };
        var receipt = new TransactionReceipt(protoTxId, protoReceipt);
        var ex = new TransactionException("test", receipt);
        await Assert.That(ex).IsAssignableTo<Exception>();
    }
}

public class ConsensusExceptionTests
{
    [Test]
    public async Task Constructor_Maps_All_Properties()
    {
        var txId = new TransactionId(new EntityId(0, 0, Generator.Integer(10, 200)), DateTime.UtcNow);
        var code = ResponseCode.TransactionExpired;
        var ex = new ConsensusException("Consensus not reached", txId, code);
        await Assert.That(ex.Message).IsEqualTo("Consensus not reached");
        await Assert.That(ex.TransactionId).IsEqualTo(txId);
        await Assert.That(ex.Status).IsEqualTo(code);
    }

    [Test]
    public async Task Is_Exception()
    {
        var ex = new ConsensusException("test", TransactionId.None, ResponseCode.Ok);
        await Assert.That(ex).IsAssignableTo<Exception>();
    }
}

public class MirrorGrpcExceptionTests
{
    [Test]
    public async Task Constructor_Maps_All_Properties()
    {
        var code = MirrorGrpcExceptionCode.TopicNotFound;
        var inner = new Exception("grpc error");
        var ex = new MirrorGrpcException("Mirror error", code, inner);
        await Assert.That(ex.Message).IsEqualTo("Mirror error");
        await Assert.That(ex.Code).IsEqualTo(code);
        await Assert.That(ex.InnerException).IsEqualTo(inner);
    }

    [Test]
    public async Task Constructor_With_Null_InnerException()
    {
        var ex = new MirrorGrpcException("Mirror error", MirrorGrpcExceptionCode.Unavailable, null);
        await Assert.That(ex.InnerException).IsNull();
        await Assert.That(ex.Code).IsEqualTo(MirrorGrpcExceptionCode.Unavailable);
    }

    [Test]
    public async Task Is_Exception()
    {
        var ex = new MirrorGrpcException("test", MirrorGrpcExceptionCode.CommunicationError, null);
        await Assert.That(ex).IsAssignableTo<Exception>();
    }
}

public class ContractExceptionTests
{
    [Test]
    public async Task Constructor_Maps_All_Properties()
    {
        var txId = new TransactionId(new EntityId(0, 0, Generator.Integer(10, 200)), DateTime.UtcNow);
        var code = ResponseCode.ContractRevertExecuted;
        var cost = (ulong)Generator.Integer(100, 5000);
        // ContractCallResult has internal constructors; use null-safe test
        var ex = new ContractException("Contract failed", txId, code, cost, null!);
        await Assert.That(ex.Message).IsEqualTo("Contract failed");
        await Assert.That(ex.TransactionId).IsEqualTo(txId);
        await Assert.That(ex.Status).IsEqualTo(code);
        await Assert.That(ex.RequiredFee).IsEqualTo(cost);
    }

    [Test]
    public async Task Is_Exception()
    {
        var ex = new ContractException("test", TransactionId.None, ResponseCode.Ok, 0, null!);
        await Assert.That(ex).IsAssignableTo<Exception>();
    }
}

public class MirrorExceptionTests
{
    [Test]
    public async Task Constructor_Maps_All_Properties()
    {
        var message = "Mirror request failed";
        var details = new MirrorError[]
        {
            new MirrorError { Message = "error one", Detail = "detail one" },
            new MirrorError { Message = "error two", Detail = "detail two" }
        };
        var code = HttpStatusCode.BadRequest;
        var ex = new MirrorException(message, details, code);
        await Assert.That(ex.Message).IsEqualTo(message);
        await Assert.That(ex.StatusCode).IsEqualTo(code);
        await Assert.That(ex.Details).IsEqualTo(details);
    }

    [Test]
    public async Task Is_Exception()
    {
        var ex = new MirrorException("test", Array.Empty<MirrorError>(), HttpStatusCode.OK);
        await Assert.That(ex).IsAssignableTo<Exception>();
    }

    [Test]
    public async Task Empty_Details_Array()
    {
        var details = Array.Empty<MirrorError>();
        var ex = new MirrorException("No details", details, HttpStatusCode.NotFound);
        await Assert.That(ex.Details).IsNotNull();
        await Assert.That(ex.Details.Length).IsEqualTo(0);
        await Assert.That(ex.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

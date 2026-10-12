// SPDX-License-Identifier: Apache-2.0
using System.Numerics;
using System.Text;
using System.Text.Json;
using Hiero.Converters;
using Hiero.Mirror;
using Hiero.Mirror.Implementation;

namespace Hiero.Test.Unit.Mirror;

public class ContractResultDataTests
{
    [Test]
    public async Task Set_Code_Result_Reads_Its_Lists_Fees_And_State_Changes()
    {
        // List entries are the mirror node v0.164 OpenAPI examples.
        var json = """
        {
            "access_list": [
                {
                    "address": "0xa02457e5dfd32bda5fc7e1f1b008aa5979568150",
                    "storage_keys": ["0x0000000000000000000000000000000000000000000000000000000000000081"]
                }
            ],
            "authorization_list": [
                {
                    "address": "0x0000000000000000000000000000000000000001",
                    "chain_id": "0x127",
                    "nonce": 5,
                    "r": "0x2222222222222222222222222222222222222222222222222222222222222222",
                    "s": "0x3333333333333333333333333333333333333333333333333333333333333333",
                    "y_parity": "0x1"
                }
            ],
            "failed_initcode": null,
            "max_fee_per_gas": "0x59",
            "max_priority_fee_per_gas": "0x0",
            "result": "SUCCESS",
            "state_changes": [
                {
                    "address": "0x000000000000000000000000000000000000044d",
                    "contract_id": "0.0.1101",
                    "slot": "0x00000000000000000000000000000000000000000000000000000000000000fa",
                    "value_read": "0x97c1fc0a6ed5551bc831571325e9bdb365d06803100dc20648640ba24ce69750",
                    "value_written": null
                }
            ],
            "status": "0x1",
            "type": 4
        }
        """;
        var data = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.ContractResultData)!;

        await Assert.That(data.TransactionType).IsEqualTo(4);
        await Assert.That(data.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(data.MaxFeePerGas).IsEqualTo(new BigInteger(0x59));
        await Assert.That(data.MaxPriorityFeePerGas).IsEqualTo(BigInteger.Zero);
        await Assert.That(data.FailedInitcode.IsEmpty).IsTrue();

        var access = data.AccessList!;
        await Assert.That(access.Length).IsEqualTo(1);
        await Assert.That(access[0].Address).IsEqualTo(new EvmAddress(Convert.FromHexString("a02457e5dfd32bda5fc7e1f1b008aa5979568150").AsSpan()));
        await Assert.That(access[0].StorageKeys.Length).IsEqualTo(1);
        await Assert.That(access[0].StorageKeys[0].Length).IsEqualTo(32);
        await Assert.That(access[0].StorageKeys[0].Span[31]).IsEqualTo((byte)0x81);

        var authorization = data.AuthorizationList![0];
        await Assert.That(authorization.Address.ToString()).IsEqualTo("0x0000000000000000000000000000000000000001");
        await Assert.That(authorization.ChainId).IsEqualTo(new BigInteger(0x127));
        await Assert.That(authorization.Nonce).IsEqualTo(5L);
        await Assert.That(authorization.SignatureR).IsEqualTo(new BigInteger(Enumerable.Repeat((byte)0x22, 32).ToArray(), true, true));
        await Assert.That(authorization.SignatureS).IsEqualTo(new BigInteger(Enumerable.Repeat((byte)0x33, 32).ToArray(), true, true));
        await Assert.That(authorization.YParity).IsEqualTo(BigInteger.One);

        var change = data.StateChanges![0];
        await Assert.That(change.Contract).IsEqualTo(new EntityId(0, 0, 1101));
        await Assert.That(change.Slot).IsEqualTo(new BigInteger(0xfa));
        await Assert.That(change.ValueRead.Length).IsEqualTo(32);
        await Assert.That(change.ValueWritten.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Legacy_String_Access_List_Reads_As_Null_Without_Failing_The_Result()
    {
        // Mirror nodes up to at least v0.152 reported the access list as a
        // string. A strict array binding would throw and lose the result.
        var json = """{"access_list":"0x","gas_used":21000,"result":"SUCCESS","type":2}""";
        var data = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.ContractResultData)!;
        await Assert.That(data.AccessList).IsNull();
        await Assert.That(data.GasUsed).IsEqualTo(21000UL);
        await Assert.That(data.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(data.AuthorizationList).IsNull();
    }

    [Test]
    public async Task Failed_Create_Reports_Its_Initcode()
    {
        var json = """{"failed_initcode":"0x856739","result":"CONTRACT_REVERT_EXECUTED","max_fee_per_gas":null}""";
        var data = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.ContractResultData)!;
        await Assert.That(data.FailedInitcode.ToArray()).IsEquivalentTo(new byte[] { 0x85, 0x67, 0x39 });
        await Assert.That(data.Status).IsEqualTo(ResponseCode.ContractRevertExecuted);
        await Assert.That(data.MaxFeePerGas).IsEqualTo(BigInteger.Zero);
    }

    [Test]
    public async Task Access_List_Converter_Round_Trips()
    {
        var converter = new AccessListConverter();
        var entries = new[]
        {
            new AccessListEntryData
            {
                Address = new EvmAddress(Convert.FromHexString("a02457e5dfd32bda5fc7e1f1b008aa5979568150").AsSpan()),
                StorageKeys = [new byte[32], Enumerable.Repeat((byte)0xff, 32).ToArray()]
            }
        };
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            converter.Write(writer, entries, JsonSerializerOptions.Default);
        }
        var reader = new Utf8JsonReader(stream.ToArray());
        reader.Read();
        var copy = converter.Read(ref reader, typeof(AccessListEntryData[]), JsonSerializerOptions.Default)!;

        await Assert.That(Encoding.UTF8.GetString(stream.ToArray())).Contains("\"storage_keys\"");
        await Assert.That(copy.Length).IsEqualTo(1);
        await Assert.That(copy[0].Address).IsEqualTo(entries[0].Address);
        await Assert.That(copy[0].StorageKeys[1].ToArray()).IsEquivalentTo(entries[0].StorageKeys[1].ToArray());
    }
}

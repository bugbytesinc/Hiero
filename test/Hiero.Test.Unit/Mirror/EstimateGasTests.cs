// SPDX-License-Identifier: Apache-2.0
using Hiero.Extensions;
using Hiero.Mirror;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Hiero.Test.Unit.Mirror;

/// <summary>
/// EstimateGasAsync against a simulated mirror node whose /contracts/call
/// answers INSUFFICIENT_GAS below the gas the call truly needs and otherwise
/// returns an estimate — which, for calls earning a storage refund, is lower
/// than what the call needs (see the testnet observations in each test).
/// </summary>
public class EstimateGasTests
{
    private const long InitialCap = 15_000_000;
    private static readonly EvmAddress From = new(new byte[20]);
    private static readonly CallContractParams SetMessage = new()
    {
        Contract = new EntityId(0, 0, 1001),
        MethodName = "set_message",
        MethodArgs = ["Scheduled: ABCDEFGHIJKLMNOPQRST"]
    };

    private sealed class SimulatedMirror(Func<long, long?> estimateUnderCap) : HttpMessageHandler
    {
        public List<long> Caps { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var cap = body.RootElement.GetProperty("gas").GetInt64();
            Caps.Add(cap);
            var estimate = estimateUnderCap(cap);
            return estimate is long gas
                ? Json(HttpStatusCode.OK, $$"""{"result":"0x{{gas:x64}}"}""")
                : Json(HttpStatusCode.BadRequest, """{"_status":{"messages":[{"message":"INSUFFICIENT_GAS"}]}}""");
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
            new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private static (MirrorRestClient Client, SimulatedMirror Mirror) CreateMirror(Func<long, long?> estimateUnderCap)
    {
        var handler = new SimulatedMirror(estimateUnderCap);
        return (new MirrorRestClient(new HttpClient(handler) { BaseAddress = new Uri("https://mirror.test/") }), handler);
    }

    [Test]
    public async Task Converging_Estimates_Are_Followed_Down_To_The_Smallest_Accepted_Limit()
    {
        // Observed on testnet for a set_message that keeps the string in its
        // long form: each tighter cap yields a slightly lower estimate.
        var observed = new Dictionary<long, long>
        {
            [InitialCap] = 40_761,
            [40_761] = 40_418,
            [40_418] = 40_097,
            [40_097] = 40_087,
            [40_087] = 40_082,
            [40_082] = 40_082,
        };
        var (client, mirror) = CreateMirror(cap => observed.TryGetValue(cap, out var e) ? e : cap >= 40_082 ? 40_082 : null);

        var gas = await client.EstimateGasAsync(From, SetMessage);

        await Assert.That(gas).IsEqualTo(40_082L);
        await Assert.That(mirror.Caps.Count <= 10).IsTrue();
    }

    [Test]
    public async Task Refund_Earning_Call_Gets_A_Limit_Above_The_Mirror_Estimate()
    {
        // Observed on testnet for a set_message that shortens a long string
        // to 31 bytes: the estimate (38,424) is net of the storage refund and
        // is itself refused as a cap. Here the call truly needs 40,000.
        const long required = 40_000;
        var (client, mirror) = CreateMirror(cap => cap >= required ? 38_424 : null);

        var gas = await client.EstimateGasAsync(From, SetMessage);

        await Assert.That(gas >= required).IsTrue();
        await Assert.That(gas - required < 100).IsTrue();
        await Assert.That(mirror.Caps.Count <= 10).IsTrue();
        // The second call tested the mirror's own estimate as a cap, and it was refused.
        await Assert.That(mirror.Caps[1]).IsEqualTo(38_424L);
    }

    [Test]
    public async Task Refund_Bound_Refused_Falls_Back_To_Searching_Up_To_The_Initial_Cap()
    {
        // Needing more than five quarters of the estimate is beyond any
        // EIP-3529 refund, but the search must still end on an accepted limit.
        const long required = 60_000;
        var (client, mirror) = CreateMirror(cap => cap >= required ? 38_424 : null);

        var gas = await client.EstimateGasAsync(From, SetMessage);

        await Assert.That(gas >= required).IsTrue();
        await Assert.That(gas <= InitialCap).IsTrue();
        await Assert.That(mirror.Caps.Count <= 10).IsTrue();
    }

    [Test]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(6)]
    public async Task Any_Iteration_Budget_Above_One_Returns_An_Accepted_Limit(int maxIterations)
    {
        const long required = 40_000;
        var (client, mirror) = CreateMirror(cap => cap >= required ? 38_424 : null);

        var gas = await client.EstimateGasAsync(From, SetMessage, maxIterations);

        await Assert.That(gas >= required).IsTrue();
        await Assert.That(mirror.Caps.Count <= maxIterations).IsTrue();
    }

    [Test]
    public async Task A_Single_Iteration_Returns_The_Raw_Estimate()
    {
        var (client, mirror) = CreateMirror(cap => cap >= 40_000 ? 38_424 : null);

        var gas = await client.EstimateGasAsync(From, SetMessage, maxIterations: 1);

        await Assert.That(gas).IsEqualTo(38_424L);
        await Assert.That(mirror.Caps.Count).IsEqualTo(1);
    }

    [Test]
    public async Task A_Call_That_Fails_At_The_Initial_Cap_Still_Throws()
    {
        // Refused even at the generous initial cap: a genuine failure of the
        // call, not something a higher limit could fix.
        var (client, _) = CreateMirror(_ => null);

        var ex = await Assert.That(async () => await client.EstimateGasAsync(From, SetMessage)).ThrowsException();

        await Assert.That(ex).IsTypeOf<MirrorException>();
        await Assert.That(ex!.Message).IsEqualTo("INSUFFICIENT_GAS");
    }
}

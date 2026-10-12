// SPDX-License-Identifier: Apache-2.0
using Hiero.Extensions;
using Hiero.Mirror;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Hiero.Test.Unit.Mirror;

/// <summary>
/// Every MirrorRestClient method can be cancelled, by a token parameter or,
/// for paged results, by WithCancellation too. The sweeps below find the
/// methods by reflection, so a method added without a token, or one that
/// drops the token on its way to the request, fails here.
/// </summary>
public class MirrorCancellationTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    /// <summary>
    /// A mirror node that answers some requests and leaves the rest hanging
    /// until the request is cancelled.
    /// </summary>
    private sealed class StalledMirror(Func<HttpRequestMessage, string?>? answer = null) : HttpMessageHandler
    {
        private readonly TaskCompletionSource _stalled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Stalled => _stalled.Task;
        public bool SawCancelledToken { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SawCancelledToken |= cancellationToken.IsCancellationRequested;
            if (answer?.Invoke(request) is string json)
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }
            _stalled.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new UnreachableException();
        }
    }

    private static (MirrorRestClient Client, StalledMirror Mirror) CreateMirror(Func<HttpRequestMessage, string?>? answer = null)
    {
        var handler = new StalledMirror(answer);
        // No HttpClient timeout: a request that ignores the token must hang
        // (and fail the test), not end in a TaskCanceledException of its own.
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://mirror.test/"), Timeout = Timeout.InfiniteTimeSpan };
        return (new MirrorRestClient(http), handler);
    }

    private static MethodInfo[] MirrorMethods()
    {
        return typeof(MirrorRestClient).Assembly.GetExportedTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.IsDefined(typeof(ExtensionAttribute)) && m.GetParameters()[0].ParameterType == typeof(MirrorRestClient))
            .ToArray();
    }

    private static string Describe(MethodInfo method)
    {
        return $"{method.DeclaringType!.Name}.{method.Name}({string.Join(", ", method.GetParameters().Skip(1).Select(p => p.ParameterType.Name))})";
    }

    private static bool IsParamsArray(ParameterInfo parameter)
    {
        return parameter.IsDefined(typeof(ParamArrayAttribute));
    }

    private static object? SampleArgument(ParameterInfo parameter, MirrorRestClient client, CancellationToken token)
    {
        var type = parameter.ParameterType;
        if (type == typeof(MirrorRestClient))
        {
            return client;
        }
        if (type == typeof(CancellationToken))
        {
            return token;
        }
        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 0);
        }
        if (parameter.HasDefaultValue)
        {
            return type.IsEnum ? Enum.ToObject(type, parameter.DefaultValue!) : parameter.DefaultValue;
        }
        var contract = new EntityId(0, 0, 1001);
        object? value = type switch
        {
            _ when type == typeof(EntityId) => contract,
            _ when type == typeof(ConsensusTimeStamp) => new ConsensusTimeStamp(1_700_000_000, 0),
            _ when type == typeof(EvmHash) => new EvmHash(new byte[32]),
            _ when type == typeof(TransactionId) => new TransactionId(new EntityId(0, 0, 2), 1_700_000_000, 0),
            _ when type == typeof(Hiero.Nft) => new Hiero.Nft(contract, 1),
            _ when type == typeof(ReadOnlyMemory<byte>) => new ReadOnlyMemory<byte>(new byte[48]),
            _ when type == typeof(long) => 1L,
            _ when type == typeof(ulong) => 1UL,
            _ when type == typeof(int) => 1_000,
            _ when type == typeof(BigInteger) => BigInteger.One,
            _ when type == typeof(EvmAddress) => new EvmAddress(new byte[20]),
            _ when type == typeof(EvmCallData) => new EvmCallData { To = new EvmAddress(new byte[20]) },
            _ when type == typeof(QueryContractParams) => new QueryContractParams { Contract = contract, MethodName = "get_message" },
            _ when type == typeof(CallContractParams) => new CallContractParams { Contract = contract, MethodName = "set_message" },
            _ when type == typeof(CreateContractParams) => new CreateContractParams { ByteCode = new byte[] { 0x60, 0x00 } },
            _ => null
        };
        return value ?? throw new InvalidOperationException($"No sample value for {type.Name}: add one to {nameof(SampleArgument)}.");
    }

    /// <summary>
    /// Starts a mirror method and returns its completion as a task: the task
    /// itself, or the first step of a paged enumeration.
    /// </summary>
    private static Task Start(MethodInfo method, object?[] args, CancellationToken enumeratorToken)
    {
        object? result;
        try
        {
            result = method.Invoke(null, args);
        }
        catch (TargetInvocationException ex)
        {
            return Task.FromException(ex.InnerException!);
        }
        if (result is Task task)
        {
            return task;
        }
        var itemType = method.ReturnType.GetGenericArguments()[0];
        var first = typeof(MirrorCancellationTests).GetMethod(nameof(FirstAsync), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(itemType);
        return (Task)first.Invoke(null, [result, enumeratorToken])!;
    }

    private static async Task FirstAsync<T>(IAsyncEnumerable<T> source, CancellationToken enumeratorToken)
    {
        await foreach (var _ in source.WithCancellation(enumeratorToken))
        {
            break;
        }
    }

    /// <summary>
    /// What a finished call ended with: null for success, else its exception
    /// (a cancelled task carries none in Task.Exception, so await it).
    /// </summary>
    private static async Task<Exception?> OutcomeOf(Task call)
    {
        try
        {
            await call;
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static string Name(Exception? error)
    {
        return error?.GetType().Name ?? "success";
    }

    /// <summary>
    /// Runs the call until the mirror holds its request, cancels, and reports
    /// what went wrong, or null if the call ended in OperationCanceledException.
    /// </summary>
    private static async Task<string?> CancelInFlightAsync(StalledMirror mirror, CancellationTokenSource cts, Func<Task> start)
    {
        var call = start();
        if (await Task.WhenAny(mirror.Stalled, call, Task.Delay(Patience)) != mirror.Stalled)
        {
            return call.IsCompleted ? $"ended before reaching the mirror ({Name(await OutcomeOf(call))})" : "never reached the mirror";
        }
        await cts.CancelAsync();
        if (await Task.WhenAny(call, Task.Delay(Patience)) != call)
        {
            return "kept waiting after the token was cancelled";
        }
        var error = await OutcomeOf(call);
        return error is OperationCanceledException ? null : $"ended with {Name(error)} instead of OperationCanceledException";
    }

    [Test]
    public async Task Every_Mirror_Method_Takes_A_Cancellation_Token()
    {
        var methods = MirrorMethods();
        var problems = new List<string>();
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var tokenAt = Array.FindIndex(parameters, p => p.ParameterType == typeof(CancellationToken));
            if (tokenAt >= 0)
            {
                // The token comes last, and is optional; or it sits just
                // before a params list and is required (an optional token
                // there would capture the first filter).
                var last = parameters.Length - 1;
                var valid = (tokenAt == last && parameters[tokenAt].HasDefaultValue)
                    || (tokenAt == last - 1 && IsParamsArray(parameters[last]) && !parameters[tokenAt].HasDefaultValue);
                if (!valid)
                {
                    problems.Add($"{Describe(method)}: token is not last (optional) or just before params (required)");
                }
                continue;
            }
            // A method without a token must be the convenience overload of
            // one that takes it just before the params list.
            if (parameters.Length == 0 || !IsParamsArray(parameters[^1]))
            {
                problems.Add($"{Describe(method)}: takes no CancellationToken");
                continue;
            }
            var expected = parameters.Select(p => p.ParameterType).ToList();
            expected.Insert(expected.Count - 1, typeof(CancellationToken));
            var sibling = method.DeclaringType!.GetMethod(method.Name, BindingFlags.Public | BindingFlags.Static, [.. expected]);
            if (sibling is null || sibling.ReturnType != method.ReturnType)
            {
                problems.Add($"{Describe(method)}: no overload taking a CancellationToken before the params list");
            }
        }
        await Assert.That(methods.Length).IsGreaterThan(60);
        await Assert.That(string.Join("\n", problems)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Every_Mirror_Method_Stops_When_Its_Token_Is_Cancelled()
    {
        var problems = new List<string>();
        foreach (var method in MirrorMethods().Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken))))
        {
            var (client, mirror) = CreateMirror();
            using var cts = new CancellationTokenSource();
            var args = method.GetParameters().Select(p => SampleArgument(p, client, cts.Token)).ToArray();
            var problem = await CancelInFlightAsync(mirror, cts, () => Start(method, args, CancellationToken.None));
            if (problem is not null)
            {
                problems.Add($"{Describe(method)}: {problem}");
            }
        }
        await Assert.That(string.Join("\n", problems)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Every_Paged_Mirror_Method_Honors_WithCancellation()
    {
        var problems = new List<string>();
        // The forms without a token argument: the convenience overloads, and
        // the paged methods whose token is optional (passed as None here).
        var paged = MirrorMethods().Where(m => m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
        foreach (var method in paged.Where(m => m.GetParameters().All(p => p.ParameterType != typeof(CancellationToken) || p.HasDefaultValue)))
        {
            var (client, mirror) = CreateMirror();
            using var cts = new CancellationTokenSource();
            var args = method.GetParameters().Select(p => SampleArgument(p, client, CancellationToken.None)).ToArray();
            var problem = await CancelInFlightAsync(mirror, cts, () => Start(method, args, cts.Token));
            if (problem is not null)
            {
                problems.Add($"{Describe(method)}: {problem}");
            }
        }
        await Assert.That(string.Join("\n", problems)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Every_Mirror_Method_Sends_Its_Request_Already_Cancelled_When_Given_A_Cancelled_Token()
    {
        var problems = new List<string>();
        foreach (var method in MirrorMethods().Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken))))
        {
            var (client, mirror) = CreateMirror();
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var args = method.GetParameters().Select(p => SampleArgument(p, client, cts.Token)).ToArray();
            var call = Start(method, args, CancellationToken.None);
            if (await Task.WhenAny(call, Task.Delay(Patience)) != call)
            {
                problems.Add($"{Describe(method)}: kept waiting on a cancelled token");
            }
            else if (await OutcomeOf(call) is var error && error is not OperationCanceledException)
            {
                problems.Add($"{Describe(method)}: ended with {Name(error)} instead of OperationCanceledException");
            }
            else if (mirror.Stalled.IsCompleted && !mirror.SawCancelledToken)
            {
                problems.Add($"{Describe(method)}: sent a request that was not cancelled");
            }
        }
        await Assert.That(string.Join("\n", problems)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Cancelling_Between_Pages_Stops_The_Enumeration()
    {
        var (client, mirror) = CreateMirror(request => request.RequestUri!.Query.Contains("account.id=gt")
            ? null
            : """{"accounts":[{"account":"0.0.1001"}],"links":{"next":"/api/v1/accounts?limit=100&account.id=gt:0.0.1001"}}""");
        using var cts = new CancellationTokenSource();
        var seen = new List<AccountData>();
        var problem = await CancelInFlightAsync(mirror, cts, async () =>
        {
            await foreach (var account in client.GetAccountsAsync(cts.Token))
            {
                seen.Add(account);
            }
        });
        await Assert.That(problem).IsNull();
        await Assert.That(seen.Count).IsEqualTo(1);
        await Assert.That(seen[0].Account).IsEqualTo(new EntityId(0, 0, 1001));
    }

    [Test]
    public async Task Cancelling_A_Gas_Estimate_Stops_Its_Search_Mid_Way()
    {
        // The first estimate is answered; the search then asks again under a
        // tighter cap, and that request is left hanging.
        var calls = 0;
        var (client, mirror) = CreateMirror(_ => Interlocked.Increment(ref calls) == 1 ? $$"""{"result":"0x{{40_000L:x64}}"}""" : null);
        using var cts = new CancellationTokenSource();
        var call = new CallContractParams { Contract = new EntityId(0, 0, 1001), MethodName = "set_message" };
        var problem = await CancelInFlightAsync(mirror, cts, () => client.EstimateGasAsync(new EvmAddress(new byte[20]), call, cancellationToken: cts.Token));
        await Assert.That(problem).IsNull();
        await Assert.That(calls).IsEqualTo(2);
    }

    [Test]
    public async Task Cancelling_The_Chain_Id_Lookup_Stops_Its_Second_Request()
    {
        // The latest Ethereum transaction is answered; the contract result
        // lookup that follows it is left hanging.
        var (client, mirror) = CreateMirror(request => request.RequestUri!.AbsolutePath.EndsWith("/transactions")
            ? """{"transactions":[{"transaction_id":"0.0.2-1700000000-000000000"}],"links":{"next":null}}"""
            : null);
        using var cts = new CancellationTokenSource();
        var problem = await CancelInFlightAsync(mirror, cts, () => client.GetChainIdAsync(cts.Token));
        await Assert.That(problem).IsNull();
    }
}

# Hiero SDK Samples

Runnable .NET console applications demonstrating common Hiero SDK workflows.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- A Hedera testnet account ([portal.hedera.com](https://portal.hedera.com/))

## Samples

| Sample | Description | Payer Required |
|--------|-------------|:-:|
| [QueryBalance](QueryBalance/) | Query an account's hbar balance | No |
| [TransferCrypto](TransferCrypto/) | Transfer hbar between two accounts | Yes |
| [CreateToken](CreateToken/) | Create a fungible token and mint supply | Yes |
| [CreateNft](CreateNft/) | Create an NFT collection and mint NFTs | Yes |
| [TokenAirdrop](TokenAirdrop/) | Airdrop tokens to recipients | Yes |
| [ConsensusService](ConsensusService/) | Create an HCS topic and submit messages | Yes |
| [SmartContract](SmartContract/) | Call a deployed smart contract | Yes |
| [FileService](FileService/) | Create and read a file on the network | Yes |
| [ScheduleTransaction](ScheduleTransaction/) | Schedule a crypto transfer for later | Yes |
| [MirrorQueries](MirrorQueries/) | Query historical data from the Mirror Node | No |
| [TopicSubscription](TopicSubscription/) | Subscribe to an HCS topic stream | No |

## Running a Sample

```sh
cd samples/QueryBalance
dotnet run -- <endpointUrl> <nodeAccountNum> <queryAccountNum>
```

Each sample's `Program.cs` documents the required command-line arguments.

## Configuration

All samples accept network configuration via command-line arguments. For
testnet, typical values are:

- Endpoint: `https://2.testnet.hedera.com:50211`
- Node Account: `5` (for node 0.0.5)
- Mirror REST: `https://testnet.mirrornode.hedera.com`
- Mirror gRPC: `https://hcs.testnet.mirrornode.hedera.com:5600`

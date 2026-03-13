---
title: Concepts
---

# Concepts

## Entity ID

The first concept is the **Entity ID**.  An entity ID is an identifier for an account, contract, file, consensus topic or Hedera token.  The identifier consists of three parts: Shard, Realm and Number.  Currently, the Hedera network only has one instance of a shard and realm; for the time being, these values will be zero.  The third identifier is the entity number.  The network generates this number upon item creation.

Entity IDs can also take two alternate forms: a **Key Alias** (referencing an account by its public key endorsement) and an **EVM Address** (a 20-byte address compatible with Ethereum tooling).

## Consensus Node Endpoint

The **Consensus Node Endpoint** is an object provided by the library for identifying a Hedera gossip network node.  Each Hedera node has a public addressable internet endpoint and linked network account address.  The internet endpoint represents the publicly available gRPC service hosted by the network node, this is the service the .NET library will connect to when making requests.  The account address represents the Hedera crypto account paired with the node that receives funds from transaction requests requiring payment.  Hedera lists the address book of endpoints for the test networks at https://docs.hedera.com/hedera/networks/testnet/testnet-nodes.

## Consensus Client

The [`ConsensusClient`](xref:Hiero.ConsensusClient) object orchestrates communication with the Hedera network.  It takes on the role of encoding messages, sending them to the endpoint, waiting for a response and then decoding the results, returning normal .NET class objects.  It provides one or more methods relating to each possible network function; each following the standard .NET async/await pattern.  The client is resilient to busy network responses and generally waits for network consensus before returning results.

The library also provides a [`MirrorRestClient`](xref:Hiero.MirrorRestClient) for querying historical data from the Hedera Mirror Node REST API, and a [`MirrorGrpcClient`](xref:Hiero.MirrorGrpcClient) for subscribing to real-time streams such as HCS topic messages.

## Context

Some initial configuration is required when creating a Consensus Client object.  For example, for balance queries, the client must know which endpoint to contact to ask for the information.  Each client instance maintains a **Context** representing the client's configuration.  The client provides methods such as _Configure_ and _Clone_ enabling calling code to modify the client's configuration.  These methods accept a callback method, that when called, receives the context represented as an [`IConsensusContext`](xref:Hiero.IConsensusContext) interface containing properties that may be changed.

The context follows a hierarchical **stack** pattern: when a client is cloned, the child inherits the parent's configuration, and any changes to the child do not affect the parent.  Properties not set in the child context fall back to the parent's value.

## Payer

Whereas querying the Hedera network for balances is presently free (note: balance queries on gossip nodes are being deprecated in favor of the Mirror Node), other actions, particularly those changing network state, require the payment of a small fee to execute.  The .NET library refers to the account paying these transaction fees as the **Payer** (also known as the "Operator").  The payer consists of two pieces of information: the _Entity ID_ identifying the payer, and a _Signatory_ authorizing the spending of funds from the account.

## Signatory

The [`Signatory`](xref:Hiero.Signatory) is a private key, or callback method that can sign a transaction.  Typically, a signatory is an account holder's private key.  The .NET library will accept Ed25519 and ECDSA Secp256K1 keys as signatories and use them to sign transactions.  It also accepts callback functions for advanced scenarios such as distributed cooperative systems coordinating the signatures of a single transaction.  Multiple signatories can be combined into a single signatory representing all keys required to sign a transaction.

## Endorsement

Most accounts are secured by a single private Ed25519 key.  The Hedera network never sees these private keys but has been given the public key corresponding to each account's private key during creation.  Accounts are not the only thing protected by keys in the Hedera network.  Contracts, topics and tokens can be administered (modified) by parties holding administrative keys assigned to these assets.  For example, when creating a token, there is an opportunity to provide a public key enabling access to various administrative functions against the token.  The .NET SDK provides the [`Endorsement`](xref:Hiero.Endorsement) object to hold this public key value.  Endorsements can also represent N-of-M threshold key structures, where a subset of keys must sign to satisfy the endorsement requirement.

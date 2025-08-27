﻿using Hiero.Implementation;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hiero;

/// <summary>
/// Represents a keyholder or group of keyholders that
/// can sign a transaction for crypto transfer to support
/// file creation, contract creation and execution or pay
/// for consensus services among other network tasks.
/// </summary>
/// <remarks>
/// A <code>Signatory</code> is presently created with a pre-existing
/// Ed25519 private key or a callback action having the
/// information necessary to sucessfully sign the transaction
/// as described by its matching <see cref="Endorsement" />
/// requrements.  RSA-3072, ECDSA and <code>Contract</code> signatures
/// are not natively supported thru the <code>Signatory</code> at this 
/// time but can be achieved thru the callback functionality.
/// </remarks>
public sealed class Signatory : ISignatory, IEquatable<Signatory>
{
    /// <summary>
    /// Private helper type tracking the type of signing
    /// information held by this instance.
    /// </summary>
    private enum Type
    {
        /// <summary>
        /// Ed25519 Public Key (Stored as a <see cref="Org.BouncyCastle.Crypto.Parameters.Ed25519PublicKeyParameters"/>).
        /// </summary>
        Ed25519 = 1,
        /// <summary>
        /// ECDSASecp256K1 Public Key (Stored as a <see cref="Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters"/>).
        /// </summary>
        ECDSASecp256K1 = 2,
        /// <summary>
        /// A <code>Func<IInvoice, Task> signingCallback</code> callback function 
        /// having the knowledge to properly sign the binary representation of the 
        /// transaction as serialized using the grpc protocol.
        /// </summary>
        Callback = 4,
        /// <summary>
        /// This signatory holds a list of a number of other signatories that can
        /// in turn sign transactions.  This supports the sceneario where multiple
        /// keys must sign a transaction.
        /// </summary>
        List = 5,
        /// <summary>
        /// This signatory holds information delaying the immediate execution of
        /// the transaction upon submission and instead causing the transaction 
        /// to be scheduled instead.
        /// </summary>
        Pending = 6,
    }
    /// <summary>
    /// Internal type of this Signatory.
    /// </summary>
    private readonly Type _type;
    /// <summary>
    /// Internal union of the types of data this Signatory may hold.
    /// The contents are a function of the <code>Type</code>.  It can be a 
    /// list of other signatories, a reference to a callback method, 
    /// pending transaction schedule information or a private key.
    /// </summary>
    private readonly object _data;
    /// <summary>
    /// Create a signatory with a private Ed25519 key.  When transactions
    /// are signed, this signatory will automatically sign the transaction
    /// with this private key.
    /// </summary>
    /// <param name="privateKey">
    /// Bytes representing an Ed25519 private key signing transactions.  
    /// It is expected to be 48 bytes in length, prefixed with 
    /// <code>0x302e020100300506032b6570</code>.
    /// </param>
    public Signatory(ReadOnlyMemory<byte> privateKey)
    {
        var (type, data) = KeyUtils.ParsePrivateKey(privateKey);
        _data = data;
        _type = type switch
        {
            KeyType.Ed25519 => Type.Ed25519,
            KeyType.ECDSASecp256K1 => Type.ECDSASecp256K1,
            KeyType.List => throw new ArgumentOutOfRangeException(nameof(type), "Only signatories representing a single key are supported with this constructor, please use the list constructor instead."),
            _ => throw new ArgumentOutOfRangeException(nameof(type), "Not a presently supported Signatory key type, please consider the callback signatory as an alternative."),
        };
    }
    /// <summary>
    /// Create a signatory that is a combination of a number of other
    /// signatories.  When this signatory is called to sign a transaction
    /// it will in turn ask all the child signatories in turn to sign the 
    /// given transaction.
    /// </summary>
    /// <param name="Signatories">
    /// One or more signatories that when combined can form a
    /// multi key signature for the transaction.
    /// </param>
    public Signatory(params Signatory[] signatories)
    {
        if (signatories is null)
        {
            throw new ArgumentNullException(nameof(signatories), "The list of signatories may not be null.");
        }
        else if (signatories.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(signatories), "At least one Signatory in a list is required.");
        }
        for (int i = 0; i < signatories.Length; i++)
        {
            if (signatories[i] is null)
            {
                throw new ArgumentNullException(nameof(signatories), "No signatory within the list may be null.");
            }
        }
        _type = Type.List;
        _data = signatories;
    }
    /// <summary>
    /// Create a signatory having a private key of the specified type.
    /// </summary>
    /// <param name="type">
    /// The type of private key this <code>Signatory</code> should use to 
    /// sign transactions.
    /// </param>
    /// <param name="privateKey">
    /// The bytes of a private key corresponding to the specified type.
    /// </param>
    /// <remarks>
    /// At this time, the library only supports Ed25519 and 
    /// ECDSA Secp2516k key types.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If any key type other than Ed25519 or ECDSA Secp2516k is used.
    /// </exception>
    public Signatory(KeyType type, ReadOnlyMemory<byte> privateKey)
    {
        switch (type)
        {
            case KeyType.Ed25519:
                _type = Type.Ed25519;
                _data = KeyUtils.ParsePrivateEd25519Key(privateKey);
                break;
            case KeyType.ECDSASecp256K1:
                _type = Type.ECDSASecp256K1;
                _data = KeyUtils.ParsePrivateEcdsaSecp256k1Key(privateKey);
                break;
            case KeyType.List:
                throw new ArgumentOutOfRangeException(nameof(type), "Only signatories representing a single key are supported with this constructor, please use the list constructor instead.");
            default:
                throw new ArgumentOutOfRangeException(nameof(type), "Not a presently supported Signatory key type, please consider the callback signatory as an alternative.");
        }
    }
    /// <summary>
    /// Create a Signatory invoking the given async callback function
    /// when asked to sign a transaction.  The <code>Signatory</code> 
    /// will pass an instance of an <see cref="IInvoice"/> containing 
    /// details of the transaction to sign when needed.  The callback 
    /// function may add as many signatures as necessary to properly 
    /// sign the  transaction.
    /// </summary>
    /// <param name="signingCallback">
    /// An async callback method that is invoked when the library 
    /// asks this Signatory to sign a transaction.
    /// </param>
    /// <remarks>
    /// Note:  For a single transaction this method MAY BE CALLED TWICE
    /// in the event the library is being asked to retrieve a record as
    /// a part of the request.  This is because retrieving a record of 
    /// a transaction requires a separate payment.  So, if this Signatory
    /// is directly attached to the root <see cref="IConsensusContext"/> it will
    /// be used to sign the request to retrieve the record (since this 
    /// will typically represent the <see cref="IConsensusContext.Payer"/>'s 
    /// signature for the transaction).
    /// </remarks>
    public Signatory(Func<IInvoice, Task> signingCallback)
    {
        if (signingCallback is null)
        {
            throw new ArgumentNullException(nameof(signingCallback), "The signing callback must not be null.");
        }
        _type = Type.Callback;
        _data = signingCallback;
    }
    /// <summary>
    /// Creates a signatory that indicates the transaction should be 
    /// scheduled and not immediately executed.  The params include
    /// optional details on how to schedule the transaction.
    /// </summary> 
    /// <param name="pendingParams">
    /// The scheduling details of the pending transaction.
    /// </param>
    public Signatory(PendingParams pendingParams)
    {
        if (pendingParams is null)
        {
            throw new ArgumentNullException(nameof(pendingParams), "Pending Parameters object cannot be null.");
        }
        _type = Type.Pending;
        _data = pendingParams;
    }
    /// <summary>
    /// Convenience implict cast for creating a <code>Signatory</code> 
    /// directly from an Ed25519 private key.
    /// </summary>
    /// <param name="privateKey">
    /// Bytes representing an Ed25519 private key signing transactions.  
    /// It is expected to be 48 bytes in length, prefixed with 
    /// <code>0x302e020100300506032b6570</code>.
    /// </param>
    public static implicit operator Signatory(ReadOnlyMemory<byte> privateKey)
    {
        return new Signatory(privateKey);
    }
    /// <summary>
    /// Convenience implicit cast for creating a <code>Signatory</code> 
    /// directly from a <code>Func&lt;IInvoice, Task&gt; signingCallback</code> 
    /// callback
    /// method.
    /// </summary>
    /// <param name="signingCallback">
    /// An async callback method that is invoked when the library 
    /// asks this Signatory to sign a transaction.
    /// </param>
    public static implicit operator Signatory(Func<IInvoice, Task> signingCallback)
    {
        return new Signatory(signingCallback);
    }
    /// <summary>
    /// Convenience implicit cast for creating a <code>Signatory</code>
    /// directly from a <see cref="PendingParams"/> object.
    /// </summary>
    /// <param name="pendingParams">
    /// The scheduling details of the pending transaction.
    /// </param>
    public static implicit operator Signatory(PendingParams pendingParams)
    {
        return new Signatory(pendingParams);
    }
    /// <summary>
    /// Equality implementation.
    /// </summary>
    /// <param name="other">
    /// The other <code>Signatory</code> object to compare.
    /// </param>
    /// <returns>
    /// True if public key layout and requirements are equivalent to the 
    /// other <code>Signatory</code> object.
    /// </returns>
    public bool Equals(Signatory? other)
    {
        if (other is null)
        {
            return false;
        }
        if (_type != other._type)
        {
            return false;
        }
        switch (_type)
        {
            case Type.Ed25519:
                return ((Ed25519PrivateKeyParameters)_data).GetEncoded().SequenceEqual(((Ed25519PrivateKeyParameters)other._data).GetEncoded());
            case Type.ECDSASecp256K1:
                return ((ECPrivateKeyParameters)_data).Equals((ECPrivateKeyParameters)other._data);
            case Type.List:
                var thisList = (Signatory[])_data;
                var otherList = (Signatory[])other._data;
                if (thisList.Length == otherList.Length)
                {
                    for (int i = 0; i < thisList.Length; i++)
                    {
                        if (!thisList[i].Equals(otherList[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                break;
            case Type.Callback:
                return ReferenceEquals(_data, other._data);
            case Type.Pending:
                var thisPending = (PendingParams)_data;
                var otherPending = (PendingParams)other._data;
                return thisPending.Equals(otherPending);
        }
        return false;
    }
    /// <summary>
    /// Equality implementation.
    /// </summary>
    /// <param name="obj">
    /// The other <code>Signatory</code> object to compare (if it is
    /// an <code>Signatory</code>).
    /// </param>
    /// <returns>
    /// If the other object is an Signatory, then <code>True</code> 
    /// if key requirements are identical to the other 
    /// <code>Signatories</code> object, otherwise 
    /// <code>False</code>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (obj is Signatory other)
        {
            return Equals(other);
        }
        return false;
    }
    /// <summary>
    /// Equality implementation.
    /// </summary>
    /// <returns>
    /// A unique hash of the contents of this <code>Signatory</code> 
    /// object.  Only consistent within the current instance of 
    /// the application process.
    /// </returns>
    public override int GetHashCode()
    {
        return _type switch
        {
            Type.Ed25519 => $"Signatory:{_type}:{((Ed25519PrivateKeyParameters)_data).GetHashCode()}".GetHashCode(),
            Type.ECDSASecp256K1 => $"Signatory:{_type}:{((ECPrivateKeyParameters)_data).GetHashCode()}".GetHashCode(),
            Type.Callback => $"Signatory:{_type}:{_data.GetHashCode()}".GetHashCode(),
            Type.List => $"Signatory:{_type}:{string.Join(':', ((Signatory[])_data).Select(e => e.GetHashCode().ToString()))}".GetHashCode(),
            _ => "Signatory:Empty".GetHashCode(),
        };
    }
    /// <summary>
    /// Retrieves a list of Endorsment (Public Keys) held internally
    /// by this Signatory (and/or its child signatories). At this time
    /// only Endorsements backed by Ed25519 and ECDSA keys are
    /// exported.  If this signatory representes a Scheduled marker
    /// or Contract, the list returned will be empty.
    /// </summary>
    /// <returns>
    /// A collection of Ed25519 and/or ECDSA Endorsments, can
    /// be an empty list if none exist.
    /// </returns>
    public IReadOnlyList<Endorsement> GetEndorsements()
    {
        switch (_type)
        {
            case Type.Ed25519:
                return new[] { new Endorsement(KeyType.Ed25519, ((Ed25519PrivateKeyParameters)_data).GeneratePublicKey().GetEncoded()) };
            case Type.ECDSASecp256K1:
                var keyParam = (ECPrivateKeyParameters)_data;
                var publicKey = keyParam.Parameters.G.Multiply(keyParam.D).GetEncoded(true);
                return new[] { new Endorsement(KeyType.ECDSASecp256K1, publicKey) };
            case Type.List:
                var list = new List<Endorsement>();
                foreach (var child in (Signatory[])_data)
                {
                    list.AddRange(child.GetEndorsements());
                }
                return list;
            default:
                return Array.Empty<Endorsement>();
        }
    }
    /// <summary>
    /// Equals implementation.
    /// </summary>
    /// <param name="left">
    /// Left hand <code>Signatory</code> argument.
    /// </param>
    /// <param name="right">
    /// Right hand <code>Signatory</code> argument.
    /// </param>
    /// <returns>
    /// True if Key requirements are identical 
    /// within each <code>Signatory</code> objects.
    /// </returns>
    public static bool operator ==(Signatory left, Signatory right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.Equals(right);
    }
    /// <summary>
    /// Not equals implementation.
    /// </summary>
    /// <param name="left">
    /// Left hand <code>Signatory</code> argument.
    /// </param>
    /// <param name="right">
    /// Right hand <code>Signatory</code> argument.
    /// </param>
    /// <returns>
    /// <code>False</code> if the Key requirements are identical 
    /// within each <code>Signatory</code> object.  
    /// <code>True</code> if they are not identical.
    /// </returns>
    public static bool operator !=(Signatory left, Signatory right)
    {
        return !(left == right);
    }
    /// <summary>
    /// Implement the signing algorithm.  In the case of an Ed25519
    /// it will use the private key to sign the transaction and 
    /// return immediately.  In the case of the callback method, it 
    /// will pass the invoice to the async method and async await
    /// for the method to return.
    /// </summary>
    /// <param name="invoice">
    /// The information for the transaction, including the TransactionId 
    /// ID, Memo and serialized bytes of the crypto transfers and other
    /// embedded information making up the transaction.
    /// </param>
    /// <returns></returns>
    async Task ISignatory.SignAsync(IInvoice invoice)
    {
        switch (_type)
        {
            case Type.Ed25519:
                KeyUtils.Sign(invoice, (Ed25519PrivateKeyParameters)_data);
                break;
            case Type.ECDSASecp256K1:
                KeyUtils.Sign(invoice, (ECPrivateKeyParameters)_data);
                break;
            case Type.List:
                foreach (ISignatory signer in (Signatory[])_data)
                {
                    await signer.SignAsync(invoice).ConfigureAwait(false);
                }
                break;
            case Type.Callback:
                await ((Func<IInvoice, Task>)_data)(invoice).ConfigureAwait(false);
                break;
            case Type.Pending:
                // This will be called to sign the to-be-scheduled
                // transaction. In this context, we do nothing.
                break;
            default:
                throw new InvalidOperationException("Not a presently supported Signatory key type, please consider the callback signatory as an alternative.");
        }
    }

    PendingParams? ISignatory.GetSchedule()
    {
        switch (_type)
        {
            case Type.Pending:
                return (PendingParams)_data;
            case Type.List:
                PendingParams? result = null;
                foreach (ISignatory signer in (Signatory[])_data)
                {
                    var schedule = signer.GetSchedule();
                    if (schedule is not null)
                    {
                        if (result is null)
                        {
                            result = schedule;
                        }
                        else if (!result.Equals(schedule))
                        {
                            throw new InvalidOperationException("Found Multiple Pending Signatories, do not know which one to choose.");
                        }
                    }
                }
                return result;
            default:
                return null;
        }
    }
    (byte[] R, byte[] S, int RevoeryId) ISignatory.SignEvm(byte[] data)
    {
        if (_type != Type.ECDSASecp256K1)
        {
            throw new InvalidOperationException("This Signatory does not support EVM signing, it is not an ECDSA Secp256K1 key.");
        }
        return KeyUtils.Sign(data, (ECPrivateKeyParameters)_data);
    }
}
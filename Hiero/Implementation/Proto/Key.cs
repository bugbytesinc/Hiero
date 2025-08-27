﻿using Google.Protobuf;
using Hiero;
using Org.BouncyCastle.Crypto.Parameters;
using System;

namespace Proto;

public sealed partial class Key
{
    internal Key(Endorsement endorsement) : this()
    {
        switch (endorsement.Type)
        {
            case KeyType.Ed25519:
                Ed25519 = ByteString.CopyFrom(((Ed25519PublicKeyParameters)endorsement._data).GetEncoded());
                break;
            case KeyType.ECDSASecp256K1:
                ECDSASecp256K1 = ByteString.CopyFrom(((ECPublicKeyParameters)endorsement._data).Q.GetEncoded(true));
                break;
            case KeyType.Contract:
                ContractID = new ContractID((EntityId)endorsement._data);
                break;
            case KeyType.List:
                var list = (Endorsement[])endorsement._data;
                if (list.Length == 0)
                {
                    KeyList = new KeyList();
                }
                else if (endorsement.RequiredCount == list.Length)
                {
                    KeyList = new KeyList(list);
                }
                else
                {
                    ThresholdKey = new ThresholdKey
                    {
                        Threshold = endorsement.RequiredCount,
                        Keys = new KeyList(list)
                    };
                }
                break;
            default:
                throw new InvalidOperationException("Endorsement is Empty.");
        }
    }
    internal Endorsement ToEndorsement()
    {
        return KeyCase switch
        {
            KeyOneofCase.Ed25519 => new Endorsement(KeyType.Ed25519, Ed25519.Memory),
            KeyOneofCase.ECDSASecp256K1 => new Endorsement(KeyType.ECDSASecp256K1, ECDSASecp256K1.Memory),
            KeyOneofCase.ContractID => new Endorsement(ContractID.AsAddress()),
            KeyOneofCase.ThresholdKey => ThresholdKey.Keys.Keys.Count == 0 ? Endorsement.None : new Endorsement(ThresholdKey.Threshold, ThresholdKey.Keys.ToEndorsements()),
            KeyOneofCase.KeyList => KeyList.Keys.Count == 0 ? Endorsement.None : new Endorsement(KeyList.ToEndorsements()),
            _ => throw new InvalidOperationException($"Unsupported Key Type {KeyCase}.  Do we have a network/library version mismatch?"),
        };
    }
}
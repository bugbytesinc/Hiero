// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Crypto;

public class AccountRekeyTests
{
    [Test]
    public async Task Can_Rotate_Ed25519_To_Ed25519_Key()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            (fx.PublicKey, fx.PrivateKey) = Generator.Ed25519KeyPair();
            fx.CreateParams.Endorsement = fx.PublicKey;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Endorsement).IsEqualTo(fxAccount.CreateParams.Endorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(data!.Account).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(data.Endorsement).IsEqualTo(fxAccount.CreateParams.Endorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        await Assert.That(data.Alias).IsNull();

        var (newPublicKey, newPrivateKey) = Generator.Ed25519KeyPair();
        var newEndorsement = new Endorsement(newPublicKey);
        var newAlias = new EntityId(0, 0, newEndorsement);
        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Endorsement = newPublicKey,
            Signatory = new Signatory(newPrivateKey, fxAccount.PrivateKey)
        });
        info = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(data!.Account).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(data.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        await Assert.That(data.Alias).IsNull();
    }

    // Defect 0.54.1
    [Test]
    public async Task Can_Rotate_ECDSA_To_Ed25519_Key_Defect()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            (fx.PublicKey, fx.PrivateKey) = Generator.Secp256k1KeyPair();
            fx.CreateParams.Endorsement = fx.PublicKey;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var moniker = fxAccount.CreateReceipt!.Address.CastToEvmAddress();
        var info = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Endorsement).IsEqualTo(fxAccount.CreateParams.Endorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(moniker);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(data!.Account).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(data.Endorsement).IsEqualTo(fxAccount.CreateParams.Endorsement);
        // DEFECT 0.54.1 vvvvvvvvvvvvvvvvvv
        //await Assert.That(data.EvmAddress).IsEqualTo(moniker);
        await Assert.That(data.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        await Assert.That(data.Alias).IsNull();

        var (newPublicKey, newPrivateKey) = Generator.Ed25519KeyPair();
        var newEndorsement = new Endorsement(newPublicKey);
        var newAlias = new EntityId(0, 0, newEndorsement);
        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Endorsement = newPublicKey,
            Signatory = new Signatory(newPrivateKey, fxAccount.PrivateKey)
        });
        info = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Endorsement).IsEqualTo(newEndorsement);
        // DEFECT 0.54.1 vvvvvvvvvvvvvvvvvv  THIS IS EVEN WORSE, THE CONTRACT ADDRESS IS MUTABLE
        //await Assert.That(info.EvmAddress).IsEqualTo(moniker);
        await Assert.That(data.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(data!.Account).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(data.Endorsement).IsEqualTo(newEndorsement);
        // DEFECT 0.54.1 vvvvvvvvvvvvvvvvvv
        //await Assert.That(data.EvmAddress).IsEqualTo(moniker);
        await Assert.That(data.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        await Assert.That(data.Alias).IsNull();
    }

    [Test]
    public async Task Can_Rotate_Evm_Created_To_Ed25519_Key()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var (originalPublicKey, originalPrivateKey) = Generator.Secp256k1KeyPair();
        var originalEndorsement = new Endorsement(originalPublicKey);
        var originalMoniker = new EvmAddress(originalEndorsement);
        var receipt = await client.TransferAsync(TestNetwork.Payer, originalMoniker, 1_00_000_000);
        var receipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        var address = ((CreateAccountReceipt)receipts[1]).Address;

        // Note: Unmaterialized
        var info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(Endorsement.None);
        await Assert.That(info.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsNull();
        await Assert.That(data.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(data.Alias).IsNotNull();
        var originalAlias = data.Alias;

        await client.TransferAsync(address, TestNetwork.Payer, 1, ctx =>
        {
            ctx.Payer = address;
            ctx.Signatory = originalPrivateKey;
            ctx.SignaturePrefixTrimLimit = int.MaxValue;
            ctx.FeeLimit = 0_99_000_000;
        });

        // Now it is materialized
        info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(data.Alias).IsEqualTo(originalAlias);

        var (newPublicKey, newPrivateKey) = Generator.Ed25519KeyPair();
        var newEndorsement = new Endorsement(newPublicKey);
        var newAlias = new EntityId(0, 0, newEndorsement);
        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = address,
            Endorsement = newPublicKey,
            Signatory = new Signatory(newPrivateKey, originalPrivateKey)
        });
        info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(data.Alias).IsEqualTo(originalAlias);
    }

    [Test]
    public async Task Can_Rotate_Alias_Created_To_Ed25519_Key()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var (originalPublicKey, originalPrivateKey) = Generator.Ed25519KeyPair();
        var originalEndorsement = new Endorsement(originalPublicKey);
        var originalAlias = new EntityId(0, 0, originalEndorsement);
        var receipt = await client.TransferAsync(TestNetwork.Payer, originalAlias, 1_00_000_000);
        var receipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        var address = ((CreateAccountReceipt)receipts[1]).Address;

        // Note: Unmaterialized
        var info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That((EntityId)info.KeyAlias).IsEqualTo(originalAlias);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That(data.Alias).IsNotNull();
        var originalMirrorAlias = data.Alias;

        await client.TransferAsync(address, TestNetwork.Payer, 1, ctx =>
        {
            ctx.Payer = address;
            ctx.Signatory = originalPrivateKey;
            ctx.SignaturePrefixTrimLimit = int.MaxValue;
            ctx.FeeLimit = 0_99_000_000;
        });

        // Now it is materialized
        info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That((EntityId)info.KeyAlias).IsEqualTo(originalAlias);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That(data.Alias).IsEqualTo(originalMirrorAlias);

        var (newPublicKey, newPrivateKey) = Generator.Ed25519KeyPair();
        var newEndorsement = new Endorsement(newPublicKey);
        var newAlias = new EntityId(0, 0, newEndorsement);
        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = address,
            Endorsement = newPublicKey,
            Signatory = new Signatory(newPrivateKey, originalPrivateKey)
        });
        info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That((EntityId)info.KeyAlias).IsEqualTo(originalAlias);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That(data.Alias).IsEqualTo(originalMirrorAlias);
    }

    [Test]
    public async Task Can_Rotate_Ed25519_To_ECDSA_Key()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            (fx.PublicKey, fx.PrivateKey) = Generator.Ed25519KeyPair();
            fx.CreateParams.Endorsement = fx.PublicKey;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Endorsement).IsEqualTo(fxAccount.CreateParams.Endorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(data!.Account).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(data.Endorsement).IsEqualTo(fxAccount.CreateParams.Endorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        await Assert.That(data.Alias).IsNull();

        var (newPublicKey, newPrivateKey) = Generator.Secp256k1KeyPair();
        var newEndorsement = new Endorsement(newPublicKey);
        var newAlias = new EntityId(0, 0, newEndorsement);
        var newMoniker = new EvmAddress(newEndorsement);
        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Endorsement = newPublicKey,
            Signatory = new Signatory(newPrivateKey, fxAccount.PrivateKey)
        });
        info = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(data!.Account).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(data.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        await Assert.That(data.Alias).IsNull();
    }

    // Defect 0.54.1
    [Test]
    public async Task Can_Rotate_ECDSA_To_ECDSA_Key_Defect()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            (fx.PublicKey, fx.PrivateKey) = Generator.Secp256k1KeyPair();
            fx.CreateParams.Endorsement = fx.PublicKey;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var moniker = fxAccount.CreateReceipt!.Address.CastToEvmAddress();
        var info = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Endorsement).IsEqualTo(fxAccount.CreateParams.Endorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(moniker);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(data!.Account).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(data.Endorsement).IsEqualTo(fxAccount.CreateParams.Endorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(moniker);
        await Assert.That(data.Alias).IsNull();

        var (newPublicKey, newPrivateKey) = Generator.Secp256k1KeyPair();
        var newEndorsement = new Endorsement(newPublicKey);
        var newAlias = new EntityId(0, 0, newEndorsement);
        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Endorsement = newPublicKey,
            Signatory = new Signatory(newPrivateKey, fxAccount.PrivateKey)
        });
        info = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Endorsement).IsEqualTo(newEndorsement);
        // DEFECT 0.54.1 vvvvvvvvvvvvvvvvvv  THIS IS EVEN WORSE, THE CONTRACT ADDRESS IS MUTABLE
        //await Assert.That(info.EvmAddress).IsEqualTo(moniker);
        await Assert.That(data.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(data!.Account).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(data.Endorsement).IsEqualTo(newEndorsement);
        // DEFECT 0.54.1 vvvvvvvvvvvvvvvvvv
        //await Assert.That(data.EvmAddress).IsEqualTo(moniker);
        await Assert.That(data.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        await Assert.That(data.Alias).IsNull();
    }

    [Test]
    public async Task Can_Rotate_Evm_Created_To_ECDSA_Key()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var (originalPublicKey, originalPrivateKey) = Generator.Secp256k1KeyPair();
        var originalEndorsement = new Endorsement(originalPublicKey);
        var originalMoniker = new EvmAddress(originalEndorsement);
        var receipt = await client.TransferAsync(TestNetwork.Payer, originalMoniker, 1_00_000_000);
        var receipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        var address = ((CreateAccountReceipt)receipts[1]).Address;

        // Note: Unmaterialized
        var info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(Endorsement.None);
        await Assert.That(info.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsNull();
        await Assert.That(data.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(data.Alias).IsNotNull();
        var originalMirrorAlias = data.Alias;

        await client.TransferAsync(address, TestNetwork.Payer, 1, ctx =>
        {
            ctx.Payer = address;
            ctx.Signatory = originalPrivateKey;
            ctx.SignaturePrefixTrimLimit = int.MaxValue;
            ctx.FeeLimit = 0_99_000_000;
        });

        // Now it is materialized
        info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(data.Alias).IsEqualTo(originalMirrorAlias);

        var (newPublicKey, newPrivateKey) = Generator.Secp256k1KeyPair();
        var newEndorsement = new Endorsement(newPublicKey);
        var newAlias = new EntityId(0, 0, newEndorsement);
        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = address,
            Endorsement = newPublicKey,
            Signatory = new Signatory(newPrivateKey, originalPrivateKey)
        });
        info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(originalMoniker);
        await Assert.That(data.Alias).IsEqualTo(originalMirrorAlias);
    }

    [Test]
    public async Task Can_Rotate_Alias_Created_To_ECDSA_Key()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var (originalPublicKey, originalPrivateKey) = Generator.Ed25519KeyPair();
        var originalEndorsement = new Endorsement(originalPublicKey);
        var originalAlias = new EntityId(0, 0, originalEndorsement);
        var receipt = await client.TransferAsync(TestNetwork.Payer, originalAlias, 1_00_000_000);
        var receipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        var address = ((CreateAccountReceipt)receipts[1]).Address;

        // Note: Unmaterialized
        var info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That((EntityId)info.KeyAlias).IsEqualTo(originalAlias);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That(data.Alias).IsNotNull();
        var originalMirrorAlias = data.Alias;

        await client.TransferAsync(address, TestNetwork.Payer, 1, ctx =>
        {
            ctx.Payer = address;
            ctx.Signatory = originalPrivateKey;
            ctx.SignaturePrefixTrimLimit = int.MaxValue;
            ctx.FeeLimit = 0_99_000_000;
        });

        // Now it is materialized
        info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That((EntityId)info.KeyAlias).IsEqualTo(originalAlias);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsEqualTo(originalEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That(data.Alias).IsEqualTo(originalMirrorAlias);

        var (newPublicKey, newPrivateKey) = Generator.Secp256k1KeyPair();
        var newEndorsement = new Endorsement(newPublicKey);
        var newAlias = new EntityId(0, 0, newEndorsement);
        var newMoniker = new EvmAddress(newEndorsement);
        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = address,
            Endorsement = newPublicKey,
            Signatory = new Signatory(newPrivateKey, originalPrivateKey)
        });
        info = await client.GetAccountInfoAsync(address);
        await Assert.That(info.Address).IsEqualTo(address);
        await Assert.That(info.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(info.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That((EntityId)info.KeyAlias).IsEqualTo(originalAlias);
        data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(address);
        await Assert.That(data!.Account).IsEqualTo(address);
        await Assert.That(data.Endorsement).IsEqualTo(newEndorsement);
        await Assert.That(data.EvmAddress).IsEqualTo(address.CastToEvmAddress());
        await Assert.That(data.Alias).IsEqualTo(originalMirrorAlias);
    }
}

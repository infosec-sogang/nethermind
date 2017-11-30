﻿using Nevermind.Core.Crypto;
using Nevermind.Core.Extensions;
using NUnit.Framework;

namespace Nevermind.Core.Test
{
    [TestFixture]
    public class BlockHeaderTests
    {
        [Test]
        public void Hash_as_expected()
        {
            BlockHeader header = new BlockHeader();
            header.Bloom = new Bloom(
                Hex.ToBytes("0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")
                    .ToBigEndianBitArray2048());
            header.Beneficiary = new Address("0x8888f1f195afa192cfee860698584c030f4c9db1");
            header.Difficulty = Hex.ToBytes("0x020000").ToUnsignedBigInteger();
            header.ExtraData = Bytes.Empty;
            header.GasLimit = (long)Hex.ToBytes("0x2fefba").ToUnsignedBigInteger();
            header.GasUsed = (long)Hex.ToBytes("0x5208").ToUnsignedBigInteger();
            header.MixHash = new Keccak(Hex.ToBytes("0x00be1f287e0911ea2f070b3650a1a0346535895b6c919d7e992a0c255a83fc8b"));
            header.Nonce = (ulong)Hex.ToBytes("0xa0ddc06c6d7b9f48").ToUnsignedBigInteger();
            header.Number = Hex.ToBytes("0x01").ToUnsignedBigInteger();
            header.ParentHash = new Keccak(Hex.ToBytes("0x5a39ed1020c04d4d84539975b893a4e7c53eab6c2965db8bc3468093a31bc5ae"));
            header.ReceiptsRoot = new Keccak(Hex.ToBytes("0x056b23fbba480696b65fe5a59b8f2148a1299103c4f57df839233af2cf4ca2d2"));
            header.StateRoot = new Keccak(Hex.ToBytes("0x5c2e5a51a79da58791cdfe572bcfa3dfe9c860bf7fad7d9738a1aace56ef9332"));
            header.Timestamp = Hex.ToBytes("0x59d79f18").ToUnsignedBigInteger();
            header.TransactionsRoot = new Keccak(Hex.ToBytes("0x5c9151c2413d1cd25c51ffb4ac38948acc1359bf08c6b49f283660e9bcf0f516"));
            header.OmmersHash = new Keccak(Hex.ToBytes("0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347"));
            header.RecomputeHash();

            Assert.AreEqual(new Keccak(Hex.ToBytes("0x19a24085f6b1fb174aee0463264cc7163a7ffa165af04d3f40431ab3c3b08b98")), header.Hash);
        }

        [Test]
        public void Hash_as_expected_2()
        {
            BlockHeader header = new BlockHeader();
            header.Bloom = new Bloom(
                Hex.ToBytes("0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")
                    .ToBigEndianBitArray2048());
            header.Beneficiary = new Address("0x8888f1f195afa192cfee860698584c030f4c9db1");
            header.Difficulty = Hex.ToBytes("0x020080").ToUnsignedBigInteger();
            header.ExtraData = Bytes.Empty;
            header.GasLimit = (long)Hex.ToBytes("0x2fefba").ToUnsignedBigInteger();
            header.GasUsed = (long)Hex.ToBytes("0x5208").ToUnsignedBigInteger();
            header.MixHash = new Keccak(Hex.ToBytes("0x615bbf44eb133eab3cb24d5766ae9617d9e45ee00e7a5667db30672b47d22149"));
            header.Nonce = (ulong)Hex.ToBytes("0x4c4f3d3e055cb264").ToUnsignedBigInteger();
            header.Number = Hex.ToBytes("0x03").ToUnsignedBigInteger();
            header.ParentHash = new Keccak(Hex.ToBytes("0xde1457da701ef916533750d46c124e9ae50b974410bd590fbcf4c935a4d19465"));
            header.ReceiptsRoot = new Keccak(Hex.ToBytes("0x056b23fbba480696b65fe5a59b8f2148a1299103c4f57df839233af2cf4ca2d2"));
            header.StateRoot = new Keccak(Hex.ToBytes("0xfb4084a7f8b57e370fefe24a3da3aaea6c4dd8b6f6251916c32440336035160b"));
            header.Timestamp = Hex.ToBytes("0x59d79f1c").ToUnsignedBigInteger();
            header.TransactionsRoot = new Keccak(Hex.ToBytes("0x1722b8a91bfc4f5614ce36ee77c7cce6620ab4af36d3c54baa66d7dbeb7bce1a"));
            header.OmmersHash = new Keccak(Hex.ToBytes("0xe676a42c388d2d24bb2927605d5d5d82fba50fb60d74d44b1cd7d1c4e4eee3c0"));
            header.RecomputeHash();

            Assert.AreEqual(new Keccak(Hex.ToBytes("0x1423c2875714c31049cacfea8450f66a73ecbd61d7a6ab13089406a491aa9fc2")), header.Hash);
        }
    }
}
using System;
using static SharpCompress.Compressors.Rar.UnpackV2017.PackDef;
using static SharpCompress.Compressors.Rar.UnpackV2017.Unpack.Unpack20Local;

/*#if !Rar2017_64bit
#else
using nint = System.Int64;
using nuint = System.UInt64;
using size_t = System.UInt64;
#endif*/

namespace SharpCompress.Compressors.Rar.UnpackV2017;

internal partial class Unpack
{
    private void CopyString20(uint Length, uint Distance)
    {
        LastDist = OldDist[OldDistPtr++ & 3] = Distance;
        LastLength = Length;
        DestUnpSize -= Length;
        CopyString(Length, Distance);
    }

    internal static class Unpack20Local
    {
        public static readonly byte[] LDecode =
        {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            10,
            12,
            14,
            16,
            20,
            24,
            28,
            32,
            40,
            48,
            56,
            64,
            80,
            96,
            112,
            128,
            160,
            192,
            224,
        };
        public static readonly byte[] LBits =
        {
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            1,
            1,
            1,
            1,
            2,
            2,
            2,
            2,
            3,
            3,
            3,
            3,
            4,
            4,
            4,
            4,
            5,
            5,
            5,
            5,
        };
        public static readonly uint[] DDecode =
        {
            0,
            1,
            2,
            3,
            4,
            6,
            8,
            12,
            16,
            24,
            32,
            48,
            64,
            96,
            128,
            192,
            256,
            384,
            512,
            768,
            1024,
            1536,
            2048,
            3072,
            4096,
            6144,
            8192,
            12288,
            16384,
            24576,
            32768U,
            49152U,
            65536,
            98304,
            131072,
            196608,
            262144,
            327680,
            393216,
            458752,
            524288,
            589824,
            655360,
            720896,
            786432,
            851968,
            917504,
            983040,
        };
        public static readonly byte[] DBits =
        {
            0,
            0,
            0,
            0,
            1,
            1,
            2,
            2,
            3,
            3,
            4,
            4,
            5,
            5,
            6,
            6,
            7,
            7,
            8,
            8,
            9,
            9,
            10,
            10,
            11,
            11,
            12,
            12,
            13,
            13,
            14,
            14,
            15,
            15,
            16,
            16,
            16,
            16,
            16,
            16,
            16,
            16,
            16,
            16,
            16,
            16,
            16,
            16,
        };
        public static readonly byte[] SDDecode = { 0, 4, 8, 16, 32, 64, 128, 192 };
        public static readonly byte[] SDBits = { 2, 2, 3, 4, 5, 6, 6, 6 };
    }

    private void Unpack20(bool Solid)
    {
        uint Bits;

        if (Suspended)
        {
            UnpPtr = WrPtr;
        }
        else
        {
            UnpInitData(Solid);
            if (!UnpReadBuf())
            {
                return;
            }

            if ((!Solid || !TablesRead2) && !ReadTables20())
            {
                return;
            }

            --DestUnpSize;
        }

        while (DestUnpSize >= 0)
        {
            UnpPtr &= MaxWinMask;

            if (Inp.InAddr > ReadTop - 30)
            {
                if (!UnpReadBuf())
                {
                    break;
                }
            }

            if (((WrPtr - UnpPtr) & MaxWinMask) < 270 && WrPtr != UnpPtr)
            {
                UnpWriteBuf20();
                if (Suspended)
                {
                    return;
                }
            }
            if (UnpAudioBlock)
            {
                var AudioNumber = DecodeNumber(Inp, MD[UnpCurChannel]);

                if (AudioNumber == 256)
                {
                    if (!ReadTables20())
                    {
                        break;
                    }

                    continue;
                }
                Window[UnpPtr++] = DecodeAudio((int)AudioNumber);
                if (++UnpCurChannel == UnpChannels)
                {
                    UnpCurChannel = 0;
                }

                --DestUnpSize;
                continue;
            }

            var Number = DecodeNumber(Inp, BlockTables.LD);
            if (Number < 256)
            {
                Window[UnpPtr++] = (byte)Number;
                --DestUnpSize;
                continue;
            }
            if (Number > 269)
            {
                var Length = (uint)(LDecode[Number -= 270] + 3);
                if ((Bits = LBits[Number]) > 0)
                {
                    Length += Inp.getbits() >> (int)(16 - Bits);
                    Inp.addbits(Bits);
                }

                var DistNumber = DecodeNumber(Inp, BlockTables.DD);
                var Distance = DDecode[DistNumber] + 1;
                if ((Bits = DBits[DistNumber]) > 0)
                {
                    Distance += Inp.getbits() >> (int)(16 - Bits);
                    Inp.addbits(Bits);
                }

                if (Distance >= 0x2000)
                {
                    Length++;
                    if (Distance >= 0x40000L)
                    {
                        Length++;
                    }
                }

                CopyString20(Length, Distance);
                continue;
            }
            if (Number == 269)
            {
                if (!ReadTables20())
                {
                    break;
                }

                continue;
            }
            if (Number == 256)
            {
                CopyString20(LastLength, LastDist);
                continue;
            }
            if (Number < 261)
            {
                var Distance = OldDist[(OldDistPtr - (Number - 256)) & 3];
                var LengthNumber = DecodeNumber(Inp, BlockTables.RD);
                var Length = (uint)(LDecode[LengthNumber] + 2);
                if ((Bits = LBits[LengthNumber]) > 0)
                {
                    Length += Inp.getbits() >> (int)(16 - Bits);
                    Inp.addbits(Bits);
                }
                if (Distance >= 0x101)
                {
                    Length++;
                    if (Distance >= 0x2000)
                    {
                        Length++;
                        if (Distance >= 0x40000)
                        {
                            Length++;
                        }
                    }
                }
                CopyString20(Length, Distance);
                continue;
            }
            if (Number < 270)
            {
                var Distance = (uint)(SDDecode[Number -= 261] + 1);
                if ((Bits = SDBits[Number]) > 0)
                {
                    Distance += Inp.getbits() >> (int)(16 - Bits);
                    Inp.addbits(Bits);
                }
                CopyString20(2, Distance);
                continue;
            }
        }
        ReadLastTables();
        UnpWriteBuf20();
    }

    private void UnpWriteBuf20()
    {
        if (UnpPtr != WrPtr)
        {
            UnpSomeRead = true;
        }

        if (UnpPtr < WrPtr)
        {
            UnpIO_UnpWrite(Window, WrPtr, (uint)(-(int)WrPtr & MaxWinMask));
            UnpIO_UnpWrite(Window, 0, UnpPtr);
            UnpAllBuf = true;
        }
        else
        {
            UnpIO_UnpWrite(Window, WrPtr, UnpPtr - WrPtr);
        }

        WrPtr = UnpPtr;
    }

    private bool ReadTables20()
    {
        Span<byte> BitLength = stackalloc byte[checked((int)BC20)];
        Span<byte> Table = stackalloc byte[checked((int)MC20 * 4)];
        if (Inp.InAddr > ReadTop - 25)
        {
            if (!UnpReadBuf())
            {
                return false;
            }
        }

        var BitField = Inp.getbits();
        UnpAudioBlock = (BitField & 0x8000) != 0;

        if ((BitField & 0x4000) != 0)
        {
            new Span<byte>(UnpOldTable20).Clear();
        }

        Inp.addbits(2);

        uint TableSize;
        if (UnpAudioBlock)
        {
            UnpChannels = ((BitField >> 12) & 3) + 1;
            if (UnpCurChannel >= UnpChannels)
            {
                UnpCurChannel = 0;
            }

            Inp.addbits(2);
            TableSize = MC20 * UnpChannels;
        }
        else
        {
            TableSize = NC20 + DC20 + RC20;
        }

        for (int I = 0; I < checked((int)BC20); I++)
        {
            BitLength[I] = (byte)(Inp.getbits() >> 12);
            Inp.addbits(4);
        }
        MakeDecodeTables(BitLength, 0, BlockTables.BD, BC20);
        for (int I = 0; I < checked((int)TableSize); )
        {
            if (Inp.InAddr > ReadTop - 5)
            {
                if (!UnpReadBuf())
                {
                    return false;
                }
            }

            var Number = DecodeNumber(Inp, BlockTables.BD);
            if (Number < 16)
            {
                Table[I] = (byte)((Number + UnpOldTable20[I]) & 0xf);
                I++;
            }
            else if (Number == 16)
            {
                var N = (Inp.getbits() >> 14) + 3;
                Inp.addbits(2);
                if (I == 0)
                {
                    return false; // We cannot have "repeat previous" code at the first position.
                }
                else
                {
                    while (N-- > 0 && I < TableSize)
                    {
                        Table[I] = Table[I - 1];
                        I++;
                    }
                }
            }
            else
            {
                uint N;
                if (Number == 17)
                {
                    N = (Inp.getbits() >> 13) + 3;
                    Inp.addbits(3);
                }
                else
                {
                    N = (Inp.getbits() >> 9) + 11;
                    Inp.addbits(7);
                }
                while (N-- > 0 && I < TableSize)
                {
                    Table[I++] = 0;
                }
            }
        }
        TablesRead2 = true;
        if (Inp.InAddr > ReadTop)
        {
            return true;
        }

        if (UnpAudioBlock)
        {
            for (uint I = 0; I < UnpChannels; I++)
            {
                MakeDecodeTables(Table, (int)(I * MC20), MD[I], MC20);
            }
        }
        else
        {
            MakeDecodeTables(Table, 0, BlockTables.LD, NC20);
            MakeDecodeTables(Table, (int)NC20, BlockTables.DD, DC20);
            MakeDecodeTables(Table, (int)(NC20 + DC20), BlockTables.RD, RC20);
        }
        Table.CopyTo(this.UnpOldTable20);
        return true;
    }

    private void ReadLastTables()
    {
        if (ReadTop >= Inp.InAddr + 5)
        {
            if (UnpAudioBlock)
            {
                if (DecodeNumber(Inp, MD[UnpCurChannel]) == 256)
                {
                    ReadTables20();
                }
            }
            else if (DecodeNumber(Inp, BlockTables.LD) == 269)
            {
                ReadTables20();
            }
        }
    }

    private void UnpInitData20(bool Solid)
    {
        if (!Solid)
        {
            TablesRead2 = false;
            UnpAudioBlock = false;
            UnpChannelDelta = 0;
            UnpCurChannel = 0;
            UnpChannels = 1;

            //memset(AudV,0,sizeof(AudV));
            AudV = new AudioVariables[4];
            new Span<byte>(UnpOldTable20).Clear();
            //memset(MD,0,sizeof(MD));
            MD = new DecodeTable[4];
        }
    }

    private byte DecodeAudio(int Delta)
    {
        var V = AudV[UnpCurChannel];
        V.ByteCount++;
        V.D4 = V.D3;
        V.D3 = V.D2;
        V.D2 = V.LastDelta - V.D1;
        V.D1 = V.LastDelta;
        var PCh =
            (8 * V.LastChar)
            + (V.K1 * V.D1)
            + (V.K2 * V.D2)
            + (V.K3 * V.D3)
            + (V.K4 * V.D4)
            + (V.K5 * UnpChannelDelta);
        PCh = (PCh >> 3) & 0xFF;

        var Ch = (uint)(PCh - Delta);

        int D = (sbyte)Delta;
        // Left shift of negative value is undefined behavior in C++,
        // so we cast it to unsigned to follow the standard.
        D = (int)((uint)D << 3);

        V.Dif[0] += (uint)Math.Abs(D);
        V.Dif[1] += (uint)Math.Abs(D - V.D1);
        V.Dif[2] += (uint)Math.Abs(D + V.D1);
        V.Dif[3] += (uint)Math.Abs(D - V.D2);
        V.Dif[4] += (uint)Math.Abs(D + V.D2);
        V.Dif[5] += (uint)Math.Abs(D - V.D3);
        V.Dif[6] += (uint)Math.Abs(D + V.D3);
        V.Dif[7] += (uint)Math.Abs(D - V.D4);
        V.Dif[8] += (uint)Math.Abs(D + V.D4);
        V.Dif[9] += (uint)Math.Abs(D - UnpChannelDelta);
        V.Dif[10] += (uint)Math.Abs(D + UnpChannelDelta);

        UnpChannelDelta = V.LastDelta = (sbyte)(Ch - V.LastChar);
        V.LastChar = (int)Ch;

        if ((V.ByteCount & 0x1F) == 0)
        {
            uint MinDif = V.Dif[0],
                NumMinDif = 0;
            V.Dif[0] = 0;
            for (uint I = 1; I < V.Dif.Length; I++)
            {
                if (V.Dif[I] < MinDif)
                {
                    MinDif = V.Dif[I];
                    NumMinDif = I;
                }
                V.Dif[I] = 0;
            }
            switch (NumMinDif)
            {
                case 1:
                    if (V.K1 >= -16)
                    {
                        V.K1--;
                    }

                    break;
                case 2:
                    if (V.K1 < 16)
                    {
                        V.K1++;
                    }

                    break;
                case 3:
                    if (V.K2 >= -16)
                    {
                        V.K2--;
                    }

                    break;
                case 4:
                    if (V.K2 < 16)
                    {
                        V.K2++;
                    }

                    break;
                case 5:
                    if (V.K3 >= -16)
                    {
                        V.K3--;
                    }

                    break;
                case 6:
                    if (V.K3 < 16)
                    {
                        V.K3++;
                    }

                    break;
                case 7:
                    if (V.K4 >= -16)
                    {
                        V.K4--;
                    }

                    break;
                case 8:
                    if (V.K4 < 16)
                    {
                        V.K4++;
                    }

                    break;
                case 9:
                    if (V.K5 >= -16)
                    {
                        V.K5--;
                    }

                    break;
                case 10:
                    if (V.K5 < 16)
                    {
                        V.K5++;
                    }

                    break;
            }
        }
        return (byte)Ch;
    }
}

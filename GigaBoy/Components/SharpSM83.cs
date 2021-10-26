using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components
{
    [Flags]
    public enum InterruptType : byte { VBlank=1,Stat=2,Timer=4,Serial=8,Joypad=16 }
    public class SharpSM83 : ClockBoundDevice
    {
        public int DelayTicks { get; protected set; } = 0;
        public GBInstance GB { get; init; }
        public bool Running { get; set; } = true;
        public bool Debug { get; set; } = true;
        public bool PrintOperation { get; set; } = false;
        public byte LastOpcode { get; protected set; } = 0;
        protected IEnumerator<bool> InstructionProcessor;
        #region Registers
        public byte A { get; set; }
        public byte B { get; set; }
        public byte C { get; set; }
        public byte D { get; set; }
        public byte E { get; set; }
        public byte F { get { 
                var data = Zero ? 128 : 0;
                data |= SubFlag ? 64 : 0;
                data |= HalfCarry ? 32 : 0;
                return (byte)(data | (Carry ? 16 : 0));
            } set {
                Zero = (value & 128) != 0;
                SubFlag = (value & 64) != 0;
                HalfCarry = (value & 32) != 0;
                Carry = (value & 16) != 0;
            } }
        public byte H { get; set; }
        public byte L { get; set; }
        public ushort HL
        {
            get
            {
                return (ushort)((H << 8) | L);
            }
            set { H = (byte)((value & 0xFF00) >> 8); L = (byte)(value & 0xFF); }
        }
        public ushort BC
        {
            get
            {
                return (ushort)((B << 8) | C);
            }
            set { B = (byte)((value & 0xFF00) >> 8); C = (byte)(value & 0xFF); }
        }
        public ushort DE
        {
            get
            {
                return (ushort)((D << 8) | E);
            }
            set { D = (byte)((value & 0xFF00) >> 8); E = (byte)(value & 0xFF); }
        }
        public ushort AF
        {
            get
            {
                return (ushort)((A << 8) | F);
            }
            set { A = (byte)((value & 0xFF00) >> 8); F = (byte)(value & 0xFF); }
        }
        public ushort SP { get; set; }
        public ushort PC { get; set; }
        public bool Zero { get; set; }
        public bool Carry { get; set; }
        public bool SubFlag { get; set; }
        public bool HalfCarry { get; set; }
        public bool InterruptMasterEnable { get; set; }
        public InterruptType InterruptEnable { get; set; }
        public InterruptType InterruptFlags { get; set; }
        #endregion
        public SharpSM83(GBInstance gb) {
            GB = gb;
            InstructionProcessor = Processor().GetEnumerator();
            PC = 0x0100;
            SP = 0xFFFE;
            A = 0x01;
            BC = 0xFF13;
            DE = 0x00C1;
            HL = 0x8403;
            F = 0;
            var abc = (JObject)(JObject.Parse(System.IO.File.ReadAllText(Environment.CurrentDirectory + @"\GigaBoyTests\opcodes.json"))["unprefixed"]);
            if (abc == null) throw new NullReferenceException();
            OpcodeDictionary = abc;
        }
        public bool TickOnce() {
            if (DelayTicks-- <= 0) {
                DelayTicks = 3;
                if (!Running) return false;
                return InstructionProcessor.MoveNext();
            }
            return false;
        }
        public void Tick() {
            TickOnce();
        }
        public static JObject OpcodeDictionary { get; set; }
        protected IEnumerable<bool> Processor() {
            string s;
            while (true) {
                byte opcode = Fetch();
                LastOpcode = opcode;

                switch (opcode&0b11000000) {
                    case 0:
                        byte data;
                        sbyte sdata;
                        ushort address;
                        int idata;
                        switch (opcode & 0b00111111) {
                            case 0:
                                break;
                            case 1:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                C = data;
                                B = Fetch();
                                break;
                            case 2:
                                yield return false;
                                Store(BC, A);
                                break;
                            case 3:
                                yield return false;
                                ++BC;
                                break;

                            case 4:
                                SubFlag = false;
                                Zero = ++B == 0;
                                HalfCarry = (B & 15) == 0;
                                break;
                            case 5:
                                SubFlag = true;
                                Zero = --B == 0;
                                HalfCarry = (B & 15) == 15;
                                break;
                            case 6:
                                yield return false;
                                B = Fetch();
                                break;
                            case 7:
                                idata = (A & 128) >> 7;
                                A = (byte)((A << 1) | idata);
                                Zero = false;
                                HalfCarry = false;
                                SubFlag = false;
                                Carry = idata != 0;
                                break;


                            case 8:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)((data << 8) |Fetch());
                                yield return false;
                                Store(address,(byte)(SP & 0x00FF));//Wrong?
                                yield return false;
                                Store(++address,(byte)(SP & 0xFF00));
                                break;
                            case 9:
                                data = L;
                                idata = HL + BC;
                                yield return false;
                                HL = (ushort)(idata&0xFFFF);
                                HalfCarry = L < data;
                                Carry = (idata&0xFFFF0000)!=0;
                                SubFlag = false;
                                break;
                            case 0xA:
                                yield return false;
                                A = Fetch(BC);
                                break;
                            case 0xB:
                                yield return false;
                                --BC;
                                break;

                            case 0xC:
                                SubFlag = false;
                                Zero = ++C == 0;
                                HalfCarry = (C & 15) == 0;
                                break;
                            case 0xD:
                                SubFlag = true;
                                Zero = --C == 0;
                                HalfCarry = (C & 15) == 15;
                                break;
                            case 0xE:
                                yield return false;
                                C = Fetch();
                                break;
                            case 0xF:
                                idata = (A & 1) << 7;
                                A = (byte)((A >> 1) | idata);
                                Zero = false;
                                HalfCarry = false;
                                SubFlag = false;
                                Carry = idata == 1;
                                break;


                            case 0x10:
                                Running = false;
                                throw new NotImplementedException("STOP instruction has not been implemented yet.");
                                break;
                            case 0x11:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                E = data;
                                D = Fetch();
                                break;
                            case 0x12:
                                yield return false;
                                Store(DE, A);
                                break;
                            case 0x13:
                                yield return false;
                                ++DE;
                                break;

                            case 0x14:
                                SubFlag = false;
                                Zero = ++D == 0;
                                HalfCarry = (D & 15) == 0;
                                break;
                            case 0x15:
                                SubFlag = true;
                                Zero = --D == 0;
                                HalfCarry = (D & 15) == 15;
                                break;
                            case 0x16:
                                yield return false;
                                D = Fetch();
                                break;
                            case 0x17:
                                idata = A & 128;
                                A = (byte)((A << 1) | (Carry?1:0));
                                Zero = false;
                                HalfCarry = false;
                                SubFlag = false;
                                Carry = idata != 0;
                                break;

                            case 0x18:
                                yield return false;
                                sdata = (sbyte)Fetch();
                                yield return false;
                                PC = (ushort)(PC + sdata);
                                break;
                            case 0x19:
                                address = HL;
                                data = L;
                                idata = HL + DE;
                                yield return false;
                                HL = (ushort)(idata & 0xFFFF);
                                HalfCarry = L < data;
                                Carry = (idata & 0xFFFF0000) != 0;
                                SubFlag = false;
                                break;
                            case 0x1A:
                                yield return false;
                                A = Fetch(DE);
                                break;
                            case 0x1B:
                                yield return false;
                                --DE;
                                break;

                            case 0x1C:
                                SubFlag = false;
                                Zero = ++E == 0;
                                HalfCarry = (E & 15) == 0;
                                break;
                            case 0x1D:
                                SubFlag = true;
                                Zero = --E == 0;
                                HalfCarry = (E & 15) == 15;
                                break;
                            case 0x1E:
                                yield return false;
                                E = Fetch();
                                break;
                            case 0x1F:
                                idata = A & 1;
                                A = (byte)((A >> 1) | (Carry ? 128 : 0));
                                Zero = false;
                                HalfCarry = false;
                                SubFlag = false;
                                Carry = idata != 0;
                                break;


                            case 0x20:
                                yield return false;
                                sdata = (sbyte)Fetch();
                                if (!Zero)
                                {
                                    yield return false;
                                    PC = (ushort)(PC + sdata);
                                }
                                break;
                            case 0x21:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                L = data;
                                H = Fetch();
                                break;
                            case 0x22:
                                yield return false;
                                Store(HL++, A);
                                break;
                            case 0x23:
                                yield return false;
                                ++HL;
                                break;

                            case 0x24:
                                SubFlag = false;
                                Zero = ++H == 0;
                                HalfCarry = (H & 15) == 0;
                                break;
                            case 0x25:
                                SubFlag = true;
                                Zero = --H == 0;
                                HalfCarry = (H & 15) == 15;
                                break;
                            case 0x26:
                                yield return false;
                                H = Fetch();
                                break;
                            case 0x27://DAA ?????
                                data = 0;
                                if (HalfCarry || (A & 0xF) > 9) data = 6;
                                if (Carry || (A & 0xF0) > 0x90) data |= 6;
                                idata = A + data;
                                A = (byte)idata;
                                HalfCarry = false;
                                Carry = (idata & 0xFFFFFF00) != 0;
                                Zero = A == 0;
                                break;

                            case 0x28:
                                yield return false;
                                sdata = (sbyte)Fetch();
                                if (Zero)
                                {
                                    yield return false;
                                    PC = (ushort)(PC + sdata);
                                }
                                break;
                            case 0x29:
                                address = HL;
                                data = L;
                                idata = HL + HL;
                                yield return false;
                                HL = (ushort)(idata & 0xFFFF);
                                HalfCarry = L < data;
                                Carry = (idata & 0xFFFF0000) != 0;
                                SubFlag = false;
                                break;
                            case 0x2A:
                                yield return false;
                                A = Fetch(HL++);
                                break;
                            case 0x2B:
                                yield return false;
                                --HL;
                                break;

                            case 0x2C:
                                SubFlag = false;
                                Zero = ++L == 0;
                                HalfCarry = (L & 15) == 0;
                                break;
                            case 0x2D:
                                SubFlag = true;
                                Zero = --L == 0;
                                HalfCarry = (L & 15) == 15;
                                break;
                            case 0x2E:
                                yield return false;
                                L = Fetch();
                                break;
                            case 0x2F:
                                A = (byte)~A;
                                HalfCarry = true;
                                SubFlag = true;
                                break;



                            case 0x30:
                                yield return false;
                                sdata = (sbyte)Fetch();
                                if (!Carry)
                                {
                                    yield return false;
                                    PC = (ushort)(PC + sdata);
                                }
                                break;
                            case 0x31:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                SP = (ushort)((Fetch() << 8) | data);
                                break;
                            case 0x32:
                                yield return false;
                                Store(HL--, A);
                                break;
                            case 0x33:
                                yield return false;
                                ++SP;
                                break;

                            case 0x34:
                                yield return false;
                                data = Fetch(HL);
                                yield return false;
                                Store(HL, ++data);
                                SubFlag = false;
                                Zero = data == 0;
                                HalfCarry = (data & 15) == 0;
                                break;
                            case 0x35:
                                yield return false;
                                data = Fetch(HL);
                                yield return false;
                                Store(HL,--data);
                                SubFlag = true;
                                Zero = data == 0;
                                HalfCarry = (data & 15) == 15;
                                break;
                            case 0x36:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                Store(HL,data);
                                break;
                            case 0x37:
                                Carry = true;
                                HalfCarry = false;
                                SubFlag = false;
                                break;

                            case 0x38:
                                yield return false;
                                sdata = (sbyte)Fetch();
                                if (Carry)
                                {
                                    yield return false;
                                    PC = (ushort)(PC + sdata);
                                }
                                break;
                            case 0x39:
                                data = L;
                                idata = HL + SP;
                                yield return false;
                                HL = (ushort)(idata & 0xFFFF);
                                HalfCarry = L < data;
                                Carry = (idata & 0xFFFF0000) != 0;
                                SubFlag = false;
                                break;
                            case 0x3A:
                                yield return false;
                                A = Fetch(HL--);
                                break;
                            case 0x3B:
                                yield return false;
                                --SP;
                                break;

                            case 0x3C:
                                SubFlag = false;
                                Zero = ++A == 0;
                                HalfCarry = (A & 15) == 0;
                                break;
                            case 0x3D:
                                SubFlag = true;
                                Zero = --A == 0;
                                HalfCarry = (L & 15) == 15;
                                break;
                            case 0x3E:
                                yield return false;
                                A = Fetch();
                                break;
                            case 0x3F:
                                Carry = !Carry;
                                HalfCarry = false;
                                SubFlag = false;
                                break;

                            default:
                                throw new NotImplementedException($"Invalid Opcode {opcode:X}");
                        }
                        break;
                    case 64:
                        byte val;
                        var source = opcode & 7;
                        var dest = (opcode >> 3) & 7;
                        if (source == dest&&source==6) {
                            //TODO: Implement Halt
                            throw new NotImplementedException("HALT");
                        }
                        switch (source)
                        {
                            case 0:
                                val = B;
                                break;
                            case 1:
                                val = C;
                                break;
                            case 2:
                                val = D;
                                break;
                            case 3:
                                val = E;
                                break;
                            case 4:
                                val = H;
                                break;
                            case 5:
                                val = L;
                                break;
                            case 6:
                                yield return false;
                                val = Fetch(HL);
                                break;
                            case 7:
                                val = A;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        switch (dest)
                        {
                            case 0:
                                B = val;
                                break;
                            case 1:
                                C = val;
                                break;
                            case 2:
                                D = val;
                                break;
                            case 3:
                                E = val;
                                break;
                            case 4:
                                H = val;
                                break;
                            case 5:
                                L = val;
                                break;
                            case 6:
                                yield return false;
                                Store(HL,val);
                                break;
                            case 7:
                                A = val;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        if (source == dest && Debug) { 
                            GB.BreakpointHit();
                        }
                        break;
                    case 128:
                        switch (opcode & 7) {
                            case 0:
                                data = B;
                                break;
                            case 1:
                                data = C;
                                break;
                            case 2:
                                data = D;
                                break;
                            case 3:
                                data = E;
                                break;
                            case 4:
                                data = H;
                                break;
                            case 5:
                                data = L;
                                break;
                            case 6:
                                yield return false;
                                data = Fetch(HL);
                                break;
                            case 7:
                                data = A;
                                break;
                            default:
                                throw new NotImplementedException($"Invalid Opcode {opcode:X}");
                        }
                        switch (opcode & 0b00111000) {
                            case 0:
                                idata = A + data;
                                Carry = (idata & 0xFFFFFF00) != 0;
                                HalfCarry = (idata & 0xF) < (A & 0xF);
                                A = (byte)idata;
                                Zero = A == 0;
                                SubFlag = false;
                                break;
                            case 8:
                                idata = A + data + (Carry?1:0);
                                Carry = (idata & 0xFFFFFF00) != 0;
                                HalfCarry = (idata & 0xF) < (A & 0xF);
                                A = (byte)idata;
                                Zero = A == 0;
                                SubFlag = false;
                                break;
                            case 16:
                                idata = A - data;
                                Carry = (idata & 0xFFFFFF00) != 0;
                                HalfCarry = (idata & 0xF) > (A & 0xF);
                                A = (byte)idata;
                                Zero = A == 0;
                                SubFlag = true;
                                break;
                            case 24:
                                idata = A - data-(Carry?1:0);
                                Carry = (idata & 0xFFFFFF00) != 0;
                                HalfCarry = (idata & 0xF) > (A & 0xF);
                                A = (byte)idata;
                                Zero = A == 0;
                                SubFlag = true;
                                break;
                            case 32:
                                A = (byte)(A & data);
                                Carry = false;
                                HalfCarry = true;
                                Zero = A == 0;
                                SubFlag = false;
                                break;
                            case 40:
                                A = (byte)(A ^ data);
                                Carry = false;
                                HalfCarry = false;
                                Zero = A == 0;
                                SubFlag = false;
                                break;
                            case 48:
                                A = (byte)(A | data);
                                Carry = false;
                                HalfCarry = false;
                                Zero = A == 0;
                                SubFlag = false;
                                break;
                            case 56:
                                idata = A - data;
                                Carry = (idata & 0xFFFFFF00) != 0;
                                HalfCarry = (idata & 0xF) > (A & 0xF);
                                Zero = A == data;
                                SubFlag = true;
                                break;
                            default:
                                throw new NotImplementedException($"Invalid Opcode {opcode:X}");
                        }
                        break;
                    case 192:
                        switch (opcode & 0b00111111) {
                            case 0:
                                yield return false;
                                if (Zero) break;
                                yield return false;
                                data = Fetch(SP++);
                                yield return false;
                                PC = (ushort)((Fetch(SP++) << 8) | data);
                                yield return false;
                                break;
                            case 1:
                                yield return false;
                                data = Fetch(SP++);
                                yield return false;
                                BC = (ushort)((Fetch(SP++) << 8) | data);
                                break;
                            case 2:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)(data | (Fetch() << 8));
                                if (!Zero)
                                {
                                    yield return false;
                                    PC = address;
                                }
                                break;
                            case 3:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)(data | (Fetch() << 8));
                                yield return false;
                                PC = address;
                                break;
                            case 4:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)(data | (Fetch() << 8));
                                if (!Zero)
                                {
                                    yield return false;
                                    yield return false;
                                    Store(--SP, (byte)(((PC + 3) & 0xFF00) >> 8));
                                    yield return false;
                                    Store(--SP, (byte)((PC + 3) & 0xFF));
                                    PC = address;
                                }
                                break;
                            case 5:
                                yield return false;
                                yield return false;
                                Store(--SP, B);
                                yield return false;
                                Store(--SP, C);
                                break;
                            case 6:
                                yield return false;
                                data = Fetch();
                                idata = A + data;
                                Carry = (idata & 0xFFFFFF00) != 0;
                                HalfCarry = (idata & 0xF) < (A & 0xF);
                                A = (byte)idata;
                                Zero = A == 0;
                                SubFlag = false;
                                break;
                            case 7:
                                yield return false;
                                address = (ushort)(opcode & 0b00111000);
                                yield return false;
                                Store(--SP, (byte)((PC & 0xFF00) >> 8));
                                yield return false;
                                Store(--SP, (byte)(PC & 0xFF));
                                PC = address;
                                break;

                            case 8:
                                yield return false;
                                if (!Zero) break;
                                yield return false;
                                data = Fetch(SP++);
                                yield return false;
                                PC = (ushort)((Fetch(SP++) << 8) | data);
                                yield return false;
                                break;
                            case 9:
                                yield return false;
                                data = Fetch(SP++);
                                yield return false;
                                address = (ushort)((Fetch(SP++) << 8) | data);
                                yield return false;
                                PC = address;
                                break;
                            case 0xA:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)(data | (Fetch() << 8));
                                if (Zero)
                                {
                                    yield return false;
                                    PC = address;
                                }
                                break;
                            case 0xB:   //Prefix 0xCB command
                                yield return false;
                                byte operand = Fetch();
                                switch (operand&7) {
                                    case 0:
                                        data = B;
                                        break;
                                    case 1:
                                        data = C;
                                        break;
                                    case 2:
                                        data = D;
                                        break;
                                    case 3:
                                        data = E;
                                        break;
                                    case 4:
                                        data = H;
                                        break;
                                    case 5:
                                        data = L;
                                        break;
                                    case 6:
                                        yield return false;
                                        data = Fetch(HL);
                                        break;
                                    case 7:
                                        data = A;
                                        break;
                                    default:
                                        throw new NotImplementedException($"Invalid Opcode CB{operand:X}");
                                }

                                int mask = 1 << ((operand>>3)&7);

                                switch (operand&0b11000000) {
                                    case 0:
                                        switch (operand&0b00111000) {
                                            case 0:
                                                idata = (data & 128) >> 7;
                                                data = (byte)((data << 1) | idata);
                                                Zero = data == 0;
                                                HalfCarry = false;
                                                SubFlag = false;
                                                Carry = idata != 0;
                                                break;
                                            case 8:
                                                idata = (data & 1) << 7;
                                                data = (byte)((data >> 1) | idata);
                                                Zero = data == 0;
                                                HalfCarry = false;
                                                SubFlag = false;
                                                Carry = idata != 0;
                                                break;
                                            case 16:
                                                idata = data & 128;
                                                data = (byte)((data << 1) | (Carry?1:0));
                                                Zero = data == 0;
                                                HalfCarry = false;
                                                SubFlag = false;
                                                Carry = idata != 0;
                                                break;
                                            case 24:
                                                idata = data & 1;
                                                data = (byte)((data >> 1) | (Carry ? 1 : 0));
                                                Zero = data == 0;
                                                HalfCarry = false;
                                                SubFlag = false;
                                                Carry = idata != 0;
                                                break;
                                            case 32:
                                                SubFlag = false;
                                                HalfCarry = false;
                                                Carry = (data & 0b10000000) != 0;
                                                data = (byte)(data << 1);
                                                Zero = data == 0;
                                                break;
                                            case 40:
                                                SubFlag = false;
                                                HalfCarry = false;
                                                Carry = (data & 1) != 0;
                                                idata = data & 0b10000000;
                                                data = (byte)((data >> 1) | idata);
                                                Zero = data == 0;
                                                break;
                                            case 48:
                                                SubFlag = false;
                                                HalfCarry = false;
                                                Carry = false;
                                                Zero = data == 0;
                                                data = (byte)((data<<4) | (data>>4));
                                                break;
                                            case 56:
                                                SubFlag = false;
                                                HalfCarry = false;
                                                Carry = (data&1) != 0;
                                                data = (byte)(data >> 1);
                                                break;
                                            default:
                                                throw new NotImplementedException($"Invalid Opcode CB{operand:X}");
                                        }
                                        break;
                                    case 64:
                                        SubFlag = false;
                                        HalfCarry = true;
                                        Zero = (mask&data) == 0;
                                        break;
                                    case 128:
                                        data = (byte)(data & (~mask));
                                        break;
                                    case 192:
                                        data = (byte)(data | mask);
                                        break;
                                }

                                if ((operand & 0b11000000) != 0b01000000) {
                                    switch (operand & 7)
                                    {
                                        case 0:
                                            B = data;
                                            break;
                                        case 1:
                                            C = data;
                                            break;
                                        case 2:
                                            D = data;
                                            break;
                                        case 3:
                                            E = data;
                                            break;
                                        case 4:
                                            H = data;
                                            break;
                                        case 5:
                                            L = data;
                                            break;
                                        case 6:
                                            yield return false;
                                            Store(HL,data);
                                            break;
                                        case 7:
                                            A = data;
                                            break;
                                        default:
                                            throw new NotImplementedException($"Invalid Opcode CB{operand:X}");
                                    }
                                }
                                break;
                            case 0xC:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)((Fetch() << 8) | data);
                                if (Zero)
                                {
                                    yield return false;
                                    yield return false;
                                    Store(--SP, (byte)(((PC + 3) & 0xFF00) >> 8));
                                    yield return false;
                                    Store(--SP, (byte)((PC + 3) & 0xFF));
                                    PC = address;
                                }
                                break;
                            case 0xD:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)((Fetch() << 8) | data);
                                yield return false;
                                yield return false;
                                Store(--SP, (byte)(((PC) & 0xFF00) >> 8));
                                yield return false;
                                Store(--SP, (byte)((PC) & 0xFF));
                                PC = address;
                                break;
                            case 0xE:
                                yield return false;
                                data = Fetch();
                                idata = A + data + (Carry ? 1 : 0);
                                Carry = (idata & 0xFFFFFF00) != 0;
                                HalfCarry = (idata & 0xF) < (A & 0xF);
                                A = (byte)idata;
                                Zero = A == 0;
                                SubFlag = false;
                                break;
                            case 0xF:
                                yield return false;
                                address = (ushort)(opcode & 0b00111000);
                                yield return false;
                                Store(--SP, (byte)((PC & 0xFF00) >> 8));
                                yield return false;
                                Store(--SP, (byte)(PC & 0xFF));
                                PC = address;
                                break;


                            case 0x10:
                                yield return false;
                                if (Carry) break;
                                yield return false;
                                data = Fetch(SP++);
                                yield return false;
                                PC = (ushort)((Fetch(SP++) << 8) | data);
                                yield return false;
                                break;
                            case 0x11:
                                yield return false;
                                data = Fetch(SP++);
                                yield return false;
                                DE = (ushort)((Fetch(SP++) << 8) | data);
                                break;
                            case 0x12:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)((Fetch() << 8) | data);
                                if (!Carry)
                                {
                                    yield return false;
                                    PC = address;
                                }
                                break;
                            case 0x14:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)((Fetch() << 8) | data);
                                if (!Carry)
                                {
                                    yield return false;
                                    yield return false;
                                    Store(--SP, (byte)(((PC + 3) & 0xFF00) >> 8));
                                    yield return false;
                                    Store(--SP, (byte)((PC + 3) & 0xFF));
                                    PC = address;
                                }
                                break;
                            case 0x15:
                                yield return false;
                                yield return false;
                                Store(--SP, D);
                                yield return false;
                                Store(--SP, E);
                                break;
                            case 0x16:
                                yield return false;
                                data = Fetch();
                                idata = A - data;
                                Carry = (idata & 0xFFFFFF00) != 0;
                                HalfCarry = (idata & 0xF) > (A & 0xF);
                                A = (byte)idata;
                                Zero = A == 0;
                                SubFlag = true;
                                break;
                            case 0x17:
                                yield return false;
                                address = (ushort)(opcode & 0b00111000);
                                yield return false;
                                Store(--SP, (byte)((PC & 0xFF00) >> 8));
                                yield return false;
                                Store(--SP, (byte)(PC & 0xFF));
                                PC = address;
                                break;

                            case 0x18:
                                yield return false;
                                if (!Carry) break;
                                yield return false;
                                data = Fetch(SP++);
                                yield return false;
                                PC = (ushort)((Fetch(SP++) << 8) | data);
                                yield return false;
                                break;
                            case 0x19:
                                yield return false;
                                data = Fetch(SP++);
                                yield return false;
                                address = (ushort)((Fetch(SP++) << 8) | data);
                                yield return false;
                                PC = address;
                                InterruptMasterEnable = true;
                                break;
                            case 0x1A:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)((Fetch() << 8) | data);
                                if (Carry)
                                {
                                    yield return false;
                                    PC = address;
                                }
                                break;
                            case 0x1C:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)((Fetch() << 8) | data);
                                if (Carry)
                                {
                                    yield return false;
                                    yield return false;
                                    Store(--SP, (byte)(((PC + 3) & 0xFF00) >> 8));
                                    yield return false;
                                    Store(--SP, (byte)((PC + 3) & 0xFF));
                                    PC = address;
                                }
                                break;
                            case 0x1E:
                                yield return false;
                                data = Fetch();
                                idata = A - data - (Carry ? 1 : 0);
                                Carry = (idata & 0xFFFFFF00) != 0;
                                HalfCarry = (idata & 0xF) > (A & 0xF);
                                A = (byte)idata;
                                Zero = A == 0;
                                SubFlag = true;
                                break;
                            case 0x1F:
                                yield return false;
                                address = (ushort)(opcode & 0b00111000);
                                yield return false;
                                Store(--SP, (byte)((PC & 0xFF00) >> 8));
                                yield return false;
                                Store(--SP, (byte)(PC & 0xFF));
                                PC = address;
                                break;


                            case 0x20:  //LDH (a8), A
                                yield return false;
                                address = (ushort)(Fetch()|0xFF00);
                                yield return false;
                                Store(address,A);
                                break;
                            case 0x21:
                                yield return false;
                                data = Fetch(SP++);
                                yield return false;
                                HL = (ushort)((Fetch(SP++) << 8) | data);
                                break;
                            case 0x22:
                                yield return false;
                                Store((ushort)(0xFF00|C),A);
                                break;
                            case 0x25:
                                yield return false;
                                yield return false;
                                Store(--SP, H);
                                yield return false;
                                Store(--SP, L);
                                break;
                            case 0x26:
                                yield return false;
                                data = Fetch();
                                A = (byte)(A & data);
                                Carry = false;
                                HalfCarry = true;
                                Zero = A == 0;
                                SubFlag = false;
                                break;
                            case 0x27:
                                yield return false;
                                address = (ushort)(opcode & 0b00111000);
                                yield return false;
                                Store(--SP, (byte)((PC & 0xFF00) >> 8));
                                yield return false;
                                Store(--SP, (byte)(PC & 0xFF));
                                PC = address;
                                break;

                            case 0x28:      //ADD SP, signed const byte
                                yield return false;
                                sdata = (sbyte)Fetch();
                                yield return false;
                                Zero = false;
                                SubFlag = false;
                                Carry = (((SP & 0xFF) + sdata) & 0xFFFFFF00) != 0;
                                HalfCarry = (((SP & 0xF) + (sdata & 0xF)) & 0xFFFFFFF0) != 0;
                                yield return false;
                                SP = (ushort)(SP + sdata);
                                break;
                            case 0x29:
                                PC = HL;
                                break;
                            case 0x2A:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)((Fetch() << 8) | data);
                                yield return false;
                                Store(address,A);
                                break;
                            case 0x2E:
                                yield return false;
                                data = Fetch();
                                A = (byte)(A ^ data);
                                Carry = false;
                                HalfCarry = false;
                                Zero = A == 0;
                                SubFlag = false;
                                break;
                            case 0x2F:
                                yield return false;
                                address = (ushort)(opcode & 0b00111000);
                                yield return false;
                                Store(--SP, (byte)((PC & 0xFF00) >> 8));
                                yield return false;
                                Store(--SP, (byte)(PC & 0xFF));
                                PC = address;
                                break;

                            case 0x30:
                                yield return false;
                                address = (ushort)(0xFF00|Fetch());
                                yield return false;
                                Store(address, A);
                                break;
                            case 0x31:
                                yield return false;
                                data = Fetch(SP++);
                                yield return false;
                                AF = (ushort)((Fetch(SP++) << 8) | data);
                                break;
                            case 0x32:
                                yield return false;
                                address = (ushort)(0xFF00 | C);
                                A = Fetch(address);
                                break;
                            case 0x33:
                                InterruptMasterEnable = false;
                                break;
                            case 0x35:
                                yield return false;
                                yield return false;
                                Store(--SP, A);
                                yield return false;
                                Store(--SP, F);
                                break;
                            case 0x36:
                                yield return false;
                                data = Fetch();
                                A = (byte)(A | data);
                                Carry = false;
                                HalfCarry = false;
                                Zero = A == 0;
                                SubFlag = false;
                                break;
                            case 0x37:
                                yield return false;
                                address = (ushort)(opcode & 0b00111000);
                                yield return false;
                                Store(--SP, (byte)((PC & 0xFF00) >> 8));
                                yield return false;
                                Store(--SP, (byte)(PC & 0xFF));
                                PC = address;
                                break;

                            case 0x38:      //LD HL, SP + signed byte
                                yield return false;
                                sdata = (sbyte)Fetch();
                                yield return false;
                                Zero = false;
                                SubFlag = false;
                                Carry = (((SP & 0xFF) + sdata) & 0xFFFFFF00) != 0;
                                HalfCarry = (((SP & 0xF) + (sdata & 0xF)) & 0xFFFFFFF0) != 0;
                                HL = (ushort)(SP + sdata);
                                break;
                            case 0x39:
                                yield return false;
                                SP = HL;
                                break;
                            case 0x3A:
                                yield return false;
                                data = Fetch();
                                yield return false;
                                address = (ushort)(data | (Fetch() << 8));
                                yield return false;
                                A = Fetch(address);
                                break;
                            case 0x3B:   //EI
                                InterruptMasterEnable = true;
                                yield return true;
                                continue;       //the EI instruction is bugged: The processor won't handle any interrupts until the instruction after EI is done executing. This means that if the DI instruction is executed directly after EI no interrupts will be handled, even if there are interrupts scheduled. To emulate this behaviour I made the EI instruction jump to the start of the main loop, as the interrupts are handled at the end of it.
                            case 0x3E:
                                yield return false;
                                data = Fetch();
                                idata = A - data;
                                Carry = (idata & 0xFFFFFF00) != 0;
                                HalfCarry = (idata & 0xF) > (A & 0xF);
                                Zero = A == data;
                                SubFlag = true;
                                break;
                            case 0x3F:
                                yield return false;
                                address = (ushort)(opcode & 0b00111000);
                                yield return false;
                                Store(--SP, (byte)((PC & 0xFF00) >> 8));
                                yield return false;
                                Store(--SP, (byte)(PC & 0xFF));
                                PC = address;
                                break;

                            default:
                                throw new NotImplementedException($"Invalid Opcode {opcode:X}");
                        }
                        break;
                    default:
                        throw new NotImplementedException($"Invalid Opcode {opcode:X}");
                }
                if (Debug && PrintOperation)
                {
                    s = opcode.ToString("X").ToUpper();
                    if (s.Length < 2) s = "0" + s;
                    if (s.Length < 2) s = "0" + s;
                    var obj = ((JObject)OpcodeDictionary["0x" + s]);
                    s = obj["mnemonic"].ToString();
                    foreach (JObject operandData in (JArray)obj["operands"]) {
                        s += " " + operandData["name"].ToString();
                    }
                    switch (opcode)
                    {
                        case 0x20:
                        case 0x30:
                        case 0x18:
                        case 0x28:
                        case 0x38:
                        case 0xC2:
                        case 0xD2:
                        case 0xC3:
                        case 0xCA:
                        case 0xCB:
                        case 0xC4:
                        case 0xD4:
                        case 0xCC:
                        case 0xDC:
                        case 0xCD:
                        case 0xC0:
                        case 0xD0:
                        case 0xC8:
                        case 0xD8:
                        case 0xC9:
                        case 0xD9:
                            s = s + ' ' + PC.ToString("X");
                            break;
                        default:
                            break;
                    }
                    GB.Log(s);
                }
                yield return true;
                if (InterruptMasterEnable && (((int)InterruptEnable & (int)InterruptFlags & 0x1F) != 0)) {
                    ushort address;
                    var interrupts = InterruptEnable & InterruptFlags;
                    if (interrupts.HasFlag(InterruptType.VBlank))
                    {
                        InterruptFlags = interrupts & ~InterruptType.VBlank;
                        address = 0x0040;
                    }
                    else if (interrupts.HasFlag(InterruptType.Stat))
                    {
                        InterruptFlags = interrupts & ~InterruptType.Stat;
                        address = 0x0048;
                    }
                    else if (interrupts.HasFlag(InterruptType.Timer))
                    {
                        InterruptFlags = interrupts & ~InterruptType.Timer;
                        address = 0x0050;
                    }
                    else if (interrupts.HasFlag(InterruptType.Serial))
                    {
                        InterruptFlags = interrupts & ~InterruptType.Serial;
                        address = 0x0058;
                    }
                    else
                    {
                        InterruptFlags = interrupts & ~InterruptType.Joypad;
                        address = 0x0060;
                    }

                    yield return false;
                    yield return false;
                    Store(--SP, (byte)((PC & 0xFF00) >> 8));
                    yield return false;
                    Store(--SP, (byte)(PC & 0xFF));
                    yield return false;
                    PC = address;
                    yield return true;
                }
            }
        }
        protected byte Fetch(ushort address)
        {
            return GB.MemoryMapper.GetByte(address);
        }
        protected void Store(ushort address,byte value)
        {
            GB.MemoryMapper.SetByte(address,value);
        }
        protected byte Fetch() {
            return Fetch(PC++);
        }
        public void SetInterrupt(InterruptType interrupt) {
            InterruptFlags = InterruptFlags | interrupt;
        }
    }
}

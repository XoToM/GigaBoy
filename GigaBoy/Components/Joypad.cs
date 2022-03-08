using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components
{
    [Flags]
    public enum GameboyInput : byte { Right = 1, Left = 2, Up = 4, Down = 8, A = 16, B = 32, Select = 64, Start = 128 }
    public class Joypad : MMIODevice
    {
        public GBInstance GB { get; init; }
        public bool JoypadBankHigher = false;
        
        GameboyInput Buttons { get; set; }

        public Joypad(GBInstance gb) {
            GB = gb;
        }

        public void SetButton(GameboyInput button, bool state)
        {
            //System.Diagnostics.Debug.WriteLine("Btn Press!");
            lock (GB)
            {
                if (state)
                {
                    var original = DirectRead(0);
                    Buttons |= button;
                    if (original != DirectRead(0)) {
                        GB.CPU.SetInterrupt(InterruptType.Joypad);
                    }
                    return;
                }
                Buttons &= ~button;
            }
        }
        public bool GetButton(GameboyInput button) {
            return (Buttons & button) != 0;
        }

        public byte DirectRead(ushort address)
        {
            int value;
            value = (int)Buttons;
            
            if (JoypadBankHigher) value = ((int)value >> 4);
            return (byte)~(value & 0b00001111);
        }

        public void DirectWrite(ushort address, byte value)
        {
            if ((value & 0b00100000) != 0) JoypadBankHigher = true;
            if ((value & 0b00010000) != 0) JoypadBankHigher = false;
        }

        public byte Read(ushort address)
        {
            return DirectRead(address);
        }

        public void Write(ushort address, byte value)
        {
            DirectWrite(address, value);
        }
    }
}

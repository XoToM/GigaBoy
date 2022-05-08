using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components
{
    public class Timers : ClockBoundDevice
    {
        public GBInstance GB { get; init; }

        public enum TimerSpacingMode { _1024 = 0, _16 = 1, _64 = 2, _256 = 3 }

        public byte DivCount;
        public byte Div { get; set; }

        public byte Timer { get; set; }
        public byte TMA { get; set; }
        public int TimerCounter = 1024;
        public TimerSpacingMode TimerSpacing = TimerSpacingMode._1024;
        public bool TimerEnable = false;
        public void Tick() {
            ++DivCount;
            if (DivCount == 0) ++Div;
            if (--TimerCounter == 0) {
                ResetTimerCounter();
                if (++Timer == 0) {
                    Timer = TMA;
                    GB.CPU.SetInterrupt(InterruptType.Timer);
                }
            }
        }
        public void ResetDIV()
        {
            DivCount = 0;
            Div = 0;
        }
        public void ResetTimer()
        {
            TimerCounter = 0;
            Timer = 0;
        }
        public void ResetTimerCounter() {
            switch (TimerSpacing) {
                case TimerSpacingMode._1024:
                    TimerCounter = 1024;
                    break;
                case TimerSpacingMode._16:
                    TimerCounter = 16;
                    break;
                case TimerSpacingMode._64:
                    TimerCounter = 64;
                    break;
                case TimerSpacingMode._256:
                    TimerCounter = 256;
                    break;
            }
        }
        public byte GetDIV() { 
            return Div;
        }
        public byte GetTAC() {
            return (byte)(((TimerEnable ? 1 : 0) << 2) | (int)TimerSpacing);
        }
        public void SetTAC(Byte value) {
            TimerEnable = (value & 4) != 0;
            TimerSpacing = (TimerSpacingMode)(value & 3);
            ResetTimerCounter();
        }
        public byte GetTIMA()
        {
            return Timer;
        }
        public void GetTIMA(byte value)
        {
            Timer = value;
        }
        public byte GetTMA()
        {
            return TMA;
        }
        public void SetTMA(byte value)
        {
            TMA = value;
        }

    }
}

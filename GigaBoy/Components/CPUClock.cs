using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components
{
    public class CPUClock
    {
        public GBInstance GB { get; init; }
        public CPUClock(GBInstance gb) {
            GB = gb;
        }
        public bool StopRequested { get; set; }
        public bool Running { get; protected set; } = false;
        private double _speedMultiplier = 1;

        public double SpeedMultiplier
        {
            get => 1 / _speedMultiplier;
            set => _speedMultiplier = 1 / value;
        }

        public DateTime AutoBreakpoint { get; set; } = DateTime.MinValue;
        public void RunClock(bool step) {
            double durationTicks;
            lock (GB)
            {
                StopRequested = false;
                Running = true;
                durationTicks = Math.Round(0.00000023841857910156 * SpeedMultiplier * Stopwatch.Frequency);
            }
            try
            {
                var sw = Stopwatch.StartNew();
                bool lastResult = false;
                while (!(step&lastResult))
                {
                    bool breakpoint = false;
                    sw.Restart();
                    while (sw.ElapsedTicks < durationTicks) { }
                    lock (GB)
                    {
                        GB.PPU.Tick();
                        lastResult = GB.CPU.TickOnce();
                        if (AutoBreakpoint != DateTime.MinValue)
                        {
                            if (AutoBreakpoint < DateTime.Now)
                            {
                                breakpoint = true;
                            }
                        }
                    }
                    if (breakpoint) {
                        GB.BreakpointHit();
                    }
                    lock (GB) {
                        if (StopRequested)
                        {
                            GB.Log("Stopping");
                            return;
                        }
                        durationTicks = Math.Round(0.00000023841857910156 * SpeedMultiplier * Stopwatch.Frequency);
                    }
                }
            }
            finally {
                lock (GB) { 
                    Running = false;
                    StopRequested = false;
                }
            }
        }
    }
}

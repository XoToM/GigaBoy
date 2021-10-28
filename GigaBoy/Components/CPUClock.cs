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
        public DateTime AutoBreakpoint { get; set; } = DateTime.MinValue;
        public void RunClock() {
            lock (GB)
            {
                StopRequested = false;
                Running = true;
            }
            bool doAutoBreakpoint = AutoBreakpoint != DateTime.MinValue;
            var sw = Stopwatch.StartNew();
            try
            {
                while (true)
                {
                    var durationTicks = Math.Round(0.00000023841857910156 * Stopwatch.Frequency);
                    sw.Restart();
                    while (sw.ElapsedTicks < durationTicks) { }
                    lock (GB)
                    {
                        GB.PPU.Tick();
                        GB.CPU.Tick();
                        if (doAutoBreakpoint)
                        {
                            if (AutoBreakpoint < DateTime.Now)
                            {
                                GB.BreakpointHit();
                            }
                        }

                        if (StopRequested)
                        {
                            StopRequested = false;
                            return;
                        }
                    }
                }
            }
            finally {
                lock(GB) Running = false;
            }
        }
        public void Step()
        {
            lock (GB)
            {
                if (Running) return;
                Running = true;
                try
                {
                    do
                    {
                        GB.PPU.Tick();
                    } while (!GB.CPU.TickOnce());

                }
                finally
                {
                    Running = false;
                }
            }
        }
    }
}

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
        public DateTime AutoBreakpoint { get; set; } = DateTime.MinValue;
        public void RunClock() {
            StopRequested = false;
            bool doAutoBreakpoint = AutoBreakpoint != DateTime.MinValue;
            while (true) {
                if (StopRequested) {
                    StopRequested = false;
                    return; 
                }
                
                var durationTicks = Math.Round(0.00000023841857910156 * Stopwatch.Frequency);
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedTicks < durationTicks){ }
                GB.PPU.Tick();
                GB.CPU.Tick();
                if (doAutoBreakpoint)
                {
                    if (AutoBreakpoint < DateTime.Now) {
                        GB.BreakpointHit();
                    }
                }
            }
        }
        public void Step()
        {
            do {
                GB.PPU.Tick();
            } while (!GB.CPU.TickOnce());
        }
    }
}

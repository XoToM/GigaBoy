using System;
using System.Diagnostics;
using GigaBoy.Components.Graphics;
using GigaBoy.Components;
using GigaBoy.Components.Mappers;
using System.Collections.Generic;
using System.Text;

namespace GigaBoy
{
    public class GBInstance
    {
        public const bool DEBUG = false; //Enable/Disable to disable all debug features. When this is false the compiler wil likely optimise out all debug features.

        public static GBInstance? LastInstance = null;
        public event EventHandler? Breakpoint;
        public PPU PPU { get; init; }
        public CRAM[] CRAMBanks { get; init; }
        public TMRAM[] TMRAMBanks { get; init; }
        public RAM WRam { get; init; }
        public RAM HRam { get; init; }
        public RAM SRam { get { return MemoryMapper.SRam; } }
        public byte[] Rom { get { return MemoryMapper.RomImage; } set { MemoryMapper.RomImage = value; } }
        public SharpSM83 CPU { get; init; }
        public Timers Timers { get; init; }
        public CPUClock Clock { get; init; }
        public Joypad Joypad { get; init; }
        public MemoryMapper MemoryMapper { get; init; }
        public bool Running { get => Clock.Running; }
        public double SpeedMultiplier { get => Clock.SpeedMultiplier; set => Clock.SpeedMultiplier = value; }
        public double FrameAutoRefreshTreshold { get; set; } = 0;
        public bool DebugLogging { get; set; } = false;
        public bool LoggingBuffer { get; set; } = false;
        public bool CurrentlyStepping { get; protected set; } = false;
        public bool BreakpointsEnable = true;
        public Dictionary<ushort,LinkedList<BreakpointInfo>> Breakpoints { get; protected set; } = new();
        public GBInstance(string filename)
        {
            LastInstance = this;
            LogBacklog = new(BacklogMaxSize);
            CRAMBanks = new CRAM[] { new(this), new(this), new(this) };
            TMRAMBanks = new TMRAM[] { new(this), new(this)};
            WRam = new(this, 0x2000) { Type = RAMType.RAM };
            HRam = new(this, 128) { Type = RAMType.HRAM };
            PPU = new(this);
            CPU = new(this);
            Timers = new() { GB = this };
            Clock = new(this);
            Joypad = new(this);
            MemoryMapper = MemoryMapper.GetMemoryMapper(this, filename);
            MemoryMapper.initRegisters();
        }
        public GBInstance()
        {
            LastInstance = this;
            LogBacklog = new(BacklogMaxSize);
            CRAMBanks = new CRAM[] { new(this), new(this), new(this) };
            TMRAMBanks = new TMRAM[] { new(this), new(this) };
            WRam = new(this, 0x2000) { Type = RAMType.RAM };
            HRam = new(this, 128) { Type = RAMType.HRAM };
            PPU = new(this);
            CPU = new(this);
            Timers = new() { GB = this };
            Clock = new(this);
            Joypad = new(this);
            MemoryMapper = MemoryMapper.GetMapperObject(this, 0, new byte[0x8000]);

            PPU.Enabled = true;
            Clock.StopRequested = false;
            Clock.AutoBreakpoint = DateTime.MinValue;
            MemoryMapper.initRegisters();
        }
        public bool BacklogOnlyLogging { get; set; } = true;

        public int BacklogMaxSize = 500;
        private static Queue<string> LogBacklog;
        private StringBuilder logBuilder = new();
        public void Log(string data)
        {
            if (DebugLogging && DEBUG) {
                if (!BacklogOnlyLogging)
                {
                    if (LoggingBuffer) {
                        LogBacklog.Enqueue(data);
                        if (LogBacklog.Count >= BacklogMaxSize) {
                            logBuilder.AppendJoin("\n",LogBacklog);
                            Debug.WriteLine(logBuilder);
                            logBuilder.Clear();
                            LogBacklog.Clear();
                        }
                        return;
                    }
                    Debug.WriteLine(data);
                    return;
                }
                if (LogBacklog.Count == BacklogMaxSize) LogBacklog.Dequeue();
                LogBacklog.Enqueue(data);
            }
        }
        public void Error(string data)
        {
            Error(new Exception(data));
        }
        public void Error(Exception e)
        {
            Clock.StopRequested = true;
            CPU.Running = false;
            PPU.Enabled = false;
            Debug.WriteLine("  ---  Emulation Exception  ---  ");

            PrintBackLog();

            Debug.WriteLine("  ---  Emulation Exception  ---  ");
            Debug.WriteLine("   --  " + e.GetType().Name+"  --");
            Debug.WriteLine(e.Message);
            throw e;
        }
        public void MainLoop(bool step=false) {
            try
            {
                CurrentlyStepping = step;
                CPU.Running = true;
                Clock.RunClock(step);
            }
            catch (Exception e)
            {
                Error(e);
            }
            finally {
                CurrentlyStepping = false;
            }
        }
        public void Step() {
            MainLoop(true);
        }
        public void PrintBackLog() {
            if (!DEBUG) return;
            if (BacklogOnlyLogging)
            {
                Debug.WriteLine("   --  Backlog  --");
                while (LogBacklog.Count != 0) Debug.WriteLine(LogBacklog.Dequeue());
                LogBacklog.Clear();
            }
            Debug.WriteLine("   --  Register States  --   ");
            Debug.WriteLine($"AF={CPU.AF:X}  BC={CPU.BC:X}  DE={CPU.DE:X}  HL={CPU.HL:X}  SP={CPU.SP:X}  PC={CPU.PC:X}  LastPC={CPU.LastPC:X}\n\n");
        }
        protected internal void BreakpointHit() {
            if (!DEBUG) return;
            if (!BreakpointsEnable) return;

            PrintBackLog();
            Debug.WriteLine("Breakpoint Hit!");

            EventHandler? temp = Breakpoint;
            if (temp != null)
            {
                temp.Invoke(null,new EventArgs());
            }

        }
        /// <summary>
        /// Stops the emulator. This method should be thread-safe.
        /// </summary>
        /// <param name="block">If true, this method will block until the emulator stops, otherwise it will return immediately. 
        /// This should never be true when this method is called by the same thread the emulator runs on.</param>
        public void Stop(bool block=false) {
            if (!Clock.Running) return;
            Clock.StopRequested = true;
            if (block) {
                while (!Clock.Running) { }
            }
        }
        public void AddBreakpoint(ushort address,BreakpointInfo breakpoint)
        {
            if (!DEBUG) return;
            if (Breakpoints.TryGetValue(address, out var breakpoints)) {
                breakpoints.AddLast(breakpoint);
                return;
            }
            var list = new LinkedList<BreakpointInfo>();
            list.AddLast(breakpoint);
            Breakpoints.Add(address,list);
        }
    }
}

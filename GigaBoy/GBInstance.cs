using System;
using System.Diagnostics;
using GigaBoy.Components.Graphics;
using GigaBoy.Components;
using GigaBoy.Components.Mappers;

namespace GigaBoy
{
    public class GBInstance
    {
        public static GBInstance? LastInstance = null;
        public event EventHandler? Breakpoint;
        public PPU PPU { get; init; }
        public RAM VRam { get; init; }
        public RAM WRam { get; init; }
        public RAM HRam { get; init; }
        public RAM SRam { get { return MemoryMapper.SRam; } }
        public byte[] Rom { get { return MemoryMapper.RomImage; } set { MemoryMapper.RomImage = value; } }
        public SharpSM83 CPU { get; init; }
        public CPUClock Clock { get; init; }
        public MemoryMapper MemoryMapper { get; init; }
        public GBInstance(string filename)
        {
            LastInstance = this;
            VRam = new RAM(this, 0x2000) { Type = RAMType.VRAM };
            WRam = new(this, 0x2000) { Type = RAMType.RAM };
            HRam = new(this, 128) { Type = RAMType.HRAM };
            PPU = new(this);
            CPU = new(this);
            Clock = new(this);
            MemoryMapper = MemoryMapper.GetMemoryMapper(this, filename);
        }
        public GBInstance()
        {
            LastInstance = this;
            VRam = new RAM(this, 0x2000) { Type = RAMType.VRAM };
            WRam = new(this, 0x2000) { Type = RAMType.RAM };
            HRam = new(this, 128) { Type = RAMType.HRAM };
            PPU = new(this);
            CPU = new(this);
            Clock = new(this);
            MemoryMapper = MemoryMapper.GetMapperObject(this,0,new byte[0x8000]);
        }
        public void Log(string data)
        {
            Debug.WriteLine(data);
        }
        public void Error(string data)
        {
            Debug.WriteLine(data);
            throw new Exception(data);
        }
        public void MainLoop() {
            Clock.RunClock();
        }
        internal void BreakpointHit() {
            EventHandler temp = Breakpoint;
            if (temp != null)
            {
                temp.Invoke(null,new EventArgs());
            }
        }
    }
}

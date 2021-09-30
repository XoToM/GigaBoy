using System;
using System.Diagnostics;
using GigaBoy.Components.Graphics;

namespace GigaBoy
{
    public class GBInstance
    {
        public static GBInstance? LastInstance = null;
        public PPU PPU { get; init; }
        public RAM VRam { get; init; }
        public GBInstance() {
            LastInstance = this;
            VRam = new(this,0x2000);
            PPU = new(this);
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
    }
}

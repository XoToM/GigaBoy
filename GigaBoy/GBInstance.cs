﻿using System;
using GigaBoy.Components.Graphics;

namespace GigaBoy
{
    public class GBInstance
    {
        public PPU PPU { get; init; }
        public VRAM VRam { get; init; }
        public GBInstance() {
            VRam = new(this);
            PPU = new(this);
        }
    }
}

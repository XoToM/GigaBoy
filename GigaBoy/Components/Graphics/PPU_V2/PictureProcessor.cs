﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics.PPU_V2
{
	public class PictureProcessor
	{
		public GBInstance GB { get; init; }
		public PPU PPU { get; init; }

		public enum FifoState { Stopped, DummyT, Dummy0, Dummy1, Character, Plane0, Plane1, Sprite }
		public enum WindowMode { Off, Rendering, Glitch_A6 }
		private enum WindowCheckMode { Off, Preparing, Active }

		public bool Debug { get => PPU.Debug; set => PPU.Debug = value; }
		public byte LY { get => PPU.LY; }
		
		public ushort fetcher_address;
		public ushort character_address;
		public byte character_plane1;
		public byte character_plane2;
		public int xPixel = 0;
		public byte spritePhaseDelay = 0;
		public bool WindowYCondition = false;
		private bool timerToggle = true;
		private WindowCheckMode windowCheck = WindowCheckMode.Off;
		public FifoState State = FifoState.Stopped;
		public WindowMode WindowState = WindowMode.Off;

		public int BackgroundPixels { get; protected set; } = 0;
		public uint backgroundQueue = 0;
		public (byte, PaletteType)[] spriteQueue = new (byte, PaletteType)[8];

		public PictureProcessor(PPU ppu) {
			PPU = ppu;
			GB = PPU.GB;
			Reset();
		}

		public void Tick()
		{
			if (!PPU.WindowEnable) windowCheck = WindowCheckMode.Off;
			if (BackgroundPixels > 8) ShiftOut();
			if (windowCheck == WindowCheckMode.Active) {    //This check and several other checks in this function have been added to allow the WX=00 glitch to happen.
				if (WindowYCondition && PPU.WindowEnable && PPU.WX == xPixel)
				{       //The WX=A6 glitch is currently not supported.
					windowCheck = WindowCheckMode.Off;
					WindowState = WindowMode.Rendering;
					State = FifoState.DummyT;
				}
			}
			if (timerToggle) {
				switch (WindowState)
				{
					case WindowMode.Off:
						switch (State)
						{
							case FifoState.DummyT:
								FIFO_BackgroundLatch();
								FIFO_B();
								State = FifoState.Dummy0;
								break;
							case FifoState.Character:
								FIFO_B();
								IncrementFetcherAddress();
								State = FifoState.Plane0;
								if (windowCheck == WindowCheckMode.Preparing) windowCheck = WindowCheckMode.Active;
								break;

							case FifoState.Dummy0:
								FIFO_0();
								State = FifoState.Dummy1;
								break;
							case FifoState.Plane0:
								FIFO_0();
								State = FifoState.Plane1;
								if (windowCheck == WindowCheckMode.Off && WindowYCondition && PPU.WindowEnable && xPixel >= PPU.WX) {
									State = FifoState.DummyT;				//I don't know how the gameboy handles changes to LCDC bit 5 mid scanline
									WindowState = WindowMode.Rendering;		//This check is here to force the emulator to draw the window anyway
								}
								break;

							case FifoState.Dummy1:
								FIFO_1();
								ClearPixelQueue();
								State = FifoState.Character;
								windowCheck = WindowCheckMode.Preparing;
								break;
							case FifoState.Plane1:
								FIFO_1();
								State = FifoState.Sprite;
								break;

							case FifoState.Sprite:
								FIFO_S();
								State = FifoState.Character;
								break;

							case FifoState.Stopped:
								break;
						}
						break;
					case WindowMode.Rendering:
						switch (State)
						{
							case FifoState.DummyT:
								ClearPixelQueue();
								FIFO_WindowLatch();
								FIFO_W();
								IncrementFetcherAddress();
								State = FifoState.Dummy0;
								break;
							case FifoState.Character:
								FIFO_W();                           //Add window handling code here.
								IncrementFetcherAddress();
								State = FifoState.Plane0;
								break;

							case FifoState.Dummy0:
								FIFO_0();
								State = FifoState.Dummy1;
								break;
							case FifoState.Plane0:
								FIFO_0();
								State = FifoState.Plane1;
								break;

							case FifoState.Dummy1:
								FIFO_1();
								State = FifoState.Character;
								break;
							case FifoState.Plane1:
								FIFO_1();
								State = FifoState.Sprite;
								break;

							case FifoState.Sprite:
								FIFO_S();
								State = FifoState.Character;
								break;

							case FifoState.Stopped:
								break;
						}
						break;
					case WindowMode.Glitch_A6:
						throw new NotImplementedException();
						break;
				}
				
			}
			timerToggle = !timerToggle;
		}


		public void FIFO_B() {
			byte ntbyte = PPU.Fetch(fetcher_address);
			byte ysub = (byte)(PPU.SCY + LY);
			if (PPU.TileData)   //One of these tiling modes is bugged.
			{
				character_address = (ushort)((ntbyte << 4) | ((ysub & 0x7) << 1));
				character_address |= 0x8000;
			}
			else
			{
				if (ntbyte < 128)
				{
					character_address = (ushort)(((ntbyte) << 4) | ((ysub & 0x7) << 1));

					character_address |= 0x9000;
				}
				else 
				{
					character_address = (ushort)(((ntbyte & 0x7F) << 4) | ((ysub & 0x7) << 1));

					character_address |= 0x8800;
				}
			}
		}
		public void FIFO_W() {
			byte ntbyte = PPU.Fetch(fetcher_address);
			byte ysub = (byte)(PPU.WY + LY);
			if (PPU.TileData)   //One of these tiling modes is bugged.
			{
				character_address = (ushort)((ntbyte << 4) | ((ysub & 0x7) << 1));
				character_address |= 0x8000;
			}
			else
			{
				if (ntbyte < 128)
				{
					character_address = (ushort)(((ntbyte) << 4) | ((ysub & 0x7) << 1));

					character_address |= 0x9000;
				}
				else 
				{
					character_address = (ushort)(((ntbyte & 0x7F) << 4) | ((ysub & 0x7) << 1));

					character_address |= 0x8800;
				}
			}
		}
		public void FIFO_0() {
			character_plane1 = PPU.Fetch(character_address);
		}
		public void FIFO_1() {
			character_plane2 = PPU.Fetch((ushort)(character_address + 1));
			if (!PPU.BGWindowPriority) {
				character_plane1 = 0;
				character_plane2 = 0;
			}
			EnqueuePixels(character_plane1, character_plane2);
		}
		public void FIFO_S() {
			if (spritePhaseDelay != 0) {
				--spritePhaseDelay;
				timerToggle = false;
				return;
			}

		}

		public void FIFO_BackgroundLatch()
		{
			byte ybase = (byte)((PPU.SCY + LY) & 0xFF);   // calculates the effective vis. scanline
			fetcher_address = (ushort)((0x9800 | ((PPU.BackgroundTileMap ? 1 : 0) << 10) | ((ybase & 0xf8) << 2) | (PPU.SCX & 0xf8) >> 3));
			xPixel = -(PPU.SCX % 8);
		}
		public void FIFO_WindowLatch() {

			byte basew = (byte)((LY - PPU.WY) & 0xFF);   // calculates the effective window scanline

			fetcher_address = (ushort)(0x9800 | ((PPU.WindowTileMap ? 1 : 0) << 10) | ((basew & 0xf8) << 2));
			if (PPU.WX == 0)
			{
				xPixel = -(PPU.SCX % 8);
				if (xPixel == -7)
				{
					xPixel = -6;
					spritePhaseDelay = 1;
				}
			}
		}
		public void IncrementFetcherAddress()
		{
			fetcher_address = (ushort)((fetcher_address & 0b1111111111100000) | ((fetcher_address + 1) & 0b0000000000011111));
		}


		public void EnqueuePixels(byte plane1, byte plane2)
		{
			uint data1 = plane1;
			uint data2 = plane2;
			ClearLastBits();
			BackgroundPixels += 8;
			for (int i = 0; i < 8; i++)
			{
				uint color = data1 & 1;
				color = color | ((data2 & 1) << 1);
				color = color << (i * 2);
				backgroundQueue = backgroundQueue | color;
				data1 = data1 >> 1;
				data2 = data2 >> 1;
			}
		}
		protected byte DequeuePixel()
		{
			if (BackgroundPixels <= 8) throw new DataMisalignedException("Cannot shift out pixels if the pixel FIFO contains 8 pixels or less.");
			const uint MASK = 0xC0000000;
			uint color = backgroundQueue & MASK;
			backgroundQueue = backgroundQueue << 2;
			--BackgroundPixels;
			return (byte)(color >> 30);
		}
		public void ClearLastBits()
		{
			backgroundQueue = backgroundQueue & 0xFFFF0000;
		}
		public void ShiftOut()
		{
			byte color = DequeuePixel();
			//System.Diagnostics.Debug.WriteLine($"queue: {Convert.ToString(backgroundQueue,2)}, pxl: {Convert.ToString(color,2)} ({color}) ");
			var palette = PaletteType.Background;
			/*      //Legacy Sprite Mixing Code
			if (spriteQueue[0].Item1 != 0)
			{
				color = spriteQueue[0].Item1;
				palette = spriteQueue[0].Item2;
			}
			for (int i = 1; i < 8; i++) spriteQueue[i - 1] = spriteQueue[i];
			spriteQueue[7] = (0, PaletteType.Background);
			*/
			SetPixel(PPU.Palette.GetTrueColor(color, palette));
		}
		public void SetPixel(ColorContainer pixel) {
			if (xPixel < 0) {
				++xPixel;
				return;
			}
			PPU.SetPixel(xPixel++,LY,pixel);
			if (xPixel == 160)
			{
				Reset();
				PPU.State = PPUStatus.HBlank;
				if (PPU.Mode0InterruptEnable) GB.CPU.SetInterrupt(InterruptType.Stat);
			}
		}

		public void ClearPixelQueue() {
			backgroundQueue = 0;
			BackgroundPixels = 0;
		}
		public void FullReset()
		{
			PPU.Log($"BG ({PPU.SCX:X}, {PPU.SCY:X},  WIN ({PPU.WX:X}, {PPU.WY:X}))");
			WindowYCondition = false;
			Reset();
		}
		public void Reset()
		{
			timerToggle = true;
			windowCheck = WindowCheckMode.Off;
			xPixel = 0;
			spritePhaseDelay = 0;
			ClearPixelQueue();
			State = FifoState.Stopped; 
			WindowState = WindowMode.Off;

			var cv = ((byte)0, PaletteType.Background);
			Array.Fill(spriteQueue, cv);
		}
		public void Start()
		{
			State = FifoState.DummyT;
			if (LY == PPU.WY && PPU.WindowEnable) WindowYCondition = true;
			//Scan OAM Here
		}
	}
}

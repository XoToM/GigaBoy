using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components.Graphics
{
	public class PictureProcessor
	{
		public GBInstance GB { get; init; }
		public PPU PPU { get; init; }
		public OamRam OAM { get => PPU.OAM; }

		public enum FifoState { Stopped, DummyT, Dummy0, Dummy1, Character, Plane0, Plane1, Sprite, Wait }
		public enum WindowMode { Off, Rendering, Glitch_A6 }
		private enum WindowCheckMode { Off, Preparing, Active }
		private enum SpriteFetcherMode { Off, TileId, Plane1, Plane2 }

		public bool Debug { get => PPU.Debug; set => PPU.Debug = value; }
		public byte LY { get => PPU.LY; }
		
		public ushort fetcher_address;
		public ushort character_address;
		public byte character_plane1;
		public byte character_plane2;
		public int xPixel = 0;
		public byte spritePhaseDelay = 0;
		public bool WindowYCondition = false;
		public int WindowLYCounter = 0;
		private bool timerToggle = true;

		private WindowCheckMode windowCheck = WindowCheckMode.Off;
		public FifoState State = FifoState.Stopped;
		public WindowMode WindowState = WindowMode.Off;

		public int BackgroundPixels { get; protected set; } = 0;
		public (uint,uint) backgroundQueue = (0,0);

		public FixedSizeQueue<SpritePixelData> spritePixelQueue = new(8);
		public FixedSizeQueue<OamSprite> scanlineSprites = new(10);

		public bool performBackgroundPush = false;
		public (byte, byte) backgroundPushData = (0, 0);


		byte spritePlane1;
		byte spritePlane2;
		public ushort sprite_character_address;
		SpriteFetcherMode spriteFetcherStatus = SpriteFetcherMode.Off;

		public PictureProcessor(PPU ppu) {
			PPU = ppu;
			GB = PPU.GB;
			Reset();
		}

		public void Tick()
		{
			if ((spriteFetcherStatus == SpriteFetcherMode.Off) && (scanlineSprites.Count > 0) && (scanlineSprites.Peek(0).PosX == xPixel + 8 )) {
				if (!PPU.ObjectEnable)
				{
					while ((scanlineSprites.Count > 0) && (scanlineSprites.Peek(0).PosX == xPixel + 8))
					{
						scanlineSprites.Dequeue();
					}
				}
				else
				{
					//GB.Log("Sprite Hit Registered");
					if(PPU.ObjectEnable)spriteFetcherStatus = SpriteFetcherMode.TileId;
				}
			}
			if (spriteFetcherStatus == SpriteFetcherMode.Off)
			{
				//if (!PPU.WindowEnable) windowCheck = WindowCheckMode.Off;
				if (BackgroundPixels > 8) ShiftOut();
				if (windowCheck == WindowCheckMode.Active)
				{    //This check and several other checks in this function have been added to allow the WX=00 glitch to happen.
					if (WindowYCondition && PPU.WindowEnable && PPU.WX == xPixel + 15)   //ToDo: Fix window glitch: Window renders 8 pixels to the right of where it should be, and leaves a color 0 block. xPixel + 15 is a temporary fix. Should be xPixel + 7
					{       //The WX=A6 glitch is currently not supported.
						windowCheck = WindowCheckMode.Off;
						WindowState = WindowMode.Rendering;
						State = FifoState.DummyT;
						timerToggle = true;
						ClearLastBits();
					}
				}
			}
			if (timerToggle) {              //TODO: Implement the finite state machine for fetching sprite tiles
				switch (spriteFetcherStatus) {
					case SpriteFetcherMode.Off:
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
										if (windowCheck == WindowCheckMode.Off && WindowYCondition && PPU.WindowEnable && xPixel + 15 >= PPU.WX)    //ToDo: Fix window glitch: Window renders 8 pixels to the right of where it should be, and leaves a color 0 block. xPixel + 15 i9s a temporary fix. Should be xPixel + 7
										{
											State = FifoState.DummyT;               //I don't know how the gameboy handles changes to LCDC bit 5 mid scanline
											WindowState = WindowMode.Rendering;     //This check is here to force the emulator to draw the window anyway
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
									case FifoState.Wait:
										State = FifoState.Stopped;
										break;
								}
								break;
							case WindowMode.Rendering:
								switch (State)
								{
									case FifoState.DummyT:
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
										State = FifoState.Wait;
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
									case FifoState.Wait:
										State = FifoState.Character;
										break;
								}
								break;
							case WindowMode.Glitch_A6:
								throw new NotImplementedException();
								//break;
						}
						break;
					case SpriteFetcherMode.TileId:
						FIFO_SprF();
						spriteFetcherStatus = SpriteFetcherMode.Plane1;
						break;
					case SpriteFetcherMode.Plane1:
						FIFO_S0();
						spriteFetcherStatus = SpriteFetcherMode.Plane2;
						break;
					case SpriteFetcherMode.Plane2:
						FIFO_S1();
						spriteFetcherStatus = SpriteFetcherMode.Off;
						break;
				}
			}
			if (performBackgroundPush && BackgroundPixels <= 8) {
				EnqueuePixels(backgroundPushData.Item1,backgroundPushData.Item2);
				performBackgroundPush = false;
			}
			timerToggle = !timerToggle;
		}


		public void FIFO_B() {
			byte ntbyte = PPU.Fetch(fetcher_address);
			byte ysub = (byte)(LY + PPU.SCY);	//This seems to be correct, but there is still some weirdness going on with the first few scanlines when the background is scrolling...
			if (PPU.TileData)   
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
			byte ysub = (byte)(LY-PPU.WY);
			if (PPU.TileData)
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
			backgroundPushData = (character_plane1, character_plane2);
			performBackgroundPush = true;
		}
		public void FIFO_S() {
			if (spritePhaseDelay != 0) {
				--spritePhaseDelay;
				timerToggle = false;
				return;
			}
		}

		public void FIFO_SprF()
		{
			byte ysub, ntbyte;
			var spr = scanlineSprites.Peek(0);
			ysub = (byte)(LY - spr.PosY);
			if (PPU.ObjectSize)
			{
				if (spr.YFlip) ysub = (byte)(15 - ysub);

				ntbyte = (byte)((spr.TileID & 0b11111110) | ((ysub / 8) & 1));
				
				ysub = (byte)(ysub % 8);
			}
			else
			{
				if (spr.YFlip) ysub = (byte)(7 - ysub);
				ntbyte = spr.TileID;
			}

			sprite_character_address = (ushort)((ntbyte << 4) | ((ysub & 0x7) << 1));
			sprite_character_address |= 0x8000;
			
		}
		public void FIFO_S0()
		{
			var spr = scanlineSprites.Peek(0);
			//if (PPU.ObjectSize) GB.Log($"Sprite {spr.PosX:X}, Address = {sprite_character_address:X}");
			spritePlane1 = PPU.Fetch(sprite_character_address);
		}
		public void FIFO_S1()
		{
			spritePlane2 = PPU.Fetch((ushort)(sprite_character_address + 1));
			SpritePixelData.MixSprite(PPU,spritePlane1,spritePlane2,scanlineSprites.Dequeue());
		}

		public void FIFO_BackgroundLatch()
		{
			byte ybase = (byte)((PPU.SCY + LY) & 0xFF);   // calculates the effective vis. scanline
			fetcher_address = (ushort)((0x9800 | ((PPU.BackgroundTileMap ? 1 : 0) << 10) | ((ybase & 0xf8) << 2) | (PPU.SCX & 0xf8) >> 3));
			xPixel = -(PPU.SCX & 7);
		}
		public void FIFO_WindowLatch() {

			byte basew = (byte)((WindowLYCounter++) & 0xFF);

			fetcher_address = (ushort)(0x9800 | ((PPU.WindowTileMap ? 1 : 0) << 10) | ((basew & 0xf8) << 2));
			if (PPU.WX == 0 && xPixel < 1)
			{
				xPixel = -(PPU.SCX & 7);
				if (xPixel == -7)
				{
					xPixel = -6;
					spritePhaseDelay = 1;
				}
			}
		}
		public void FIFO_Push() {
			if (BackgroundPixels <= 8 && performBackgroundPush) {
				performBackgroundPush = false;
				EnqueuePixels(backgroundPushData.Item1, backgroundPushData.Item2);
			}
		}
		
		public void IncrementFetcherAddress()
		{
			fetcher_address = (ushort)((fetcher_address & 0b1111111111100000) | ((fetcher_address + 1) & 0b0000000000011111));
		}


		public void EnqueuePixels(byte plane1, byte plane2)
		{
			//ClearLastBits();
			backgroundQueue = (backgroundQueue.Item1 | plane1, backgroundQueue.Item2 | plane2);
			if(BackgroundPixels == 0) backgroundQueue = (backgroundQueue.Item1 << 8, backgroundQueue.Item2 << 8);
			BackgroundPixels += 8;
			/*for (int i = 0; i < 8; i++)
			{
				uint color = data1 & 1;
				color = color | ((data2 & 1) << 1);
				color = color << (i * 2);
				backgroundQueue = backgroundQueue | color;
				data1 = data1 >> 1;
				data2 = data2 >> 1;
			}//*/
		}
		protected byte DequeuePixel()
		{
			if (BackgroundPixels <= 8) throw new DataMisalignedException("Cannot shift out pixels if the pixel FIFO contains 8 pixels or less.");
			backgroundQueue = (backgroundQueue.Item1 << 1, backgroundQueue.Item2 << 1);
			uint color = ((backgroundQueue.Item1 >> 16) & 1) | ((backgroundQueue.Item2 >> 15) & 2);
			--BackgroundPixels;
			return (byte)color;
		}
		public void ClearLastBits()
		{
			backgroundQueue = (backgroundQueue.Item1 & 0xFF00, backgroundQueue.Item2 & 0xFF00);
			BackgroundPixels = BackgroundPixels > 8 ? 8 : BackgroundPixels;
			performBackgroundPush = false;
		}
		public void ShiftOut()
		{
			byte color = DequeuePixel();
			if (!PPU.BGWindowPriority) color = 0;
			var palette = PaletteType.Background;

			if (!spritePixelQueue.TryDequeue(out var spritePixel)) spritePixel = new SpritePixelData() { BGPriority = false, Color = 0, Palette = PaletteType.Sprite1, GB = GB };

			if (spritePixel.Color != 0 && ((!spritePixel.BGPriority) || (spritePixel.BGPriority && (color == 0))))
			{
				color = spritePixel.Color;
				palette = spritePixel.Palette;
			}

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
			backgroundQueue = (0,0);
			BackgroundPixels = 0;
			performBackgroundPush = false; 
		}
		public void FullReset()
		{
			PPU.Log($"BG ({PPU.SCX:X}, {PPU.SCY:X}),  WIN ({PPU.WX:X}, {PPU.WY:X}))");
			WindowYCondition = false;
			WindowLYCounter = 0;
			Reset();
		}
		public void Reset()
		{
			timerToggle = true;
			performBackgroundPush = false;
			xPixel = 0;
			spritePhaseDelay = 0;

			State = FifoState.Stopped; 
			WindowState = WindowMode.Off;
			spriteFetcherStatus = SpriteFetcherMode.Off;
			windowCheck = WindowCheckMode.Off;

			ClearPixelQueue();
			spritePixelQueue.Clear();
			scanlineSprites.Clear();
		}
		public void Start()
		{
			State = FifoState.DummyT;
			if (LY == PPU.WY && PPU.WindowEnable) WindowYCondition = true;
		}
		public void SearchOAM() {
			//GB.Log("OAM Scan Start");
			if (scanlineSprites.Count != 0) scanlineSprites.Clear();
			int miny,maxy;
			miny = LY+9;


			if (PPU.ObjectSize)
			{
				maxy = miny + 15;
			}
			else {
				maxy = miny + 7;
			}

			var oamQuerry = from spr in OAM where (spr.PosY >= miny) && (spr.PosY <= maxy) orderby spr.PosX select spr;
			foreach(var spr in oamQuerry) {
				scanlineSprites.Enqueue(spr);
				if (scanlineSprites.Count == 10) break;
			}
			//GB.Log("OAM Scan End");
		}
	}
}

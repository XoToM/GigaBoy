using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GigaBoy;

namespace GigaBoyTests
{
    class Program
    {
        [DllImport("msvcrt.dll")]
        private static extern int memcmp(IntPtr b1, IntPtr b2, long count);

        public static bool CompareMemCmp(Bitmap b1, Bitmap b2)
        {
            if ((b1 == null) != (b2 == null)) return false;
            if (b1.Size != b2.Size) return false;

            var bd1 = b1.LockBits(new Rectangle(new Point(0, 0), b1.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bd2 = b2.LockBits(new Rectangle(new Point(0, 0), b2.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                IntPtr bd1scan0 = bd1.Scan0;
                IntPtr bd2scan0 = bd2.Scan0;

                int stride = bd1.Stride;
                int len = stride * b1.Height;

                return memcmp(bd1scan0, bd2scan0, len) == 0;
            }
            finally
            {
                b1.UnlockBits(bd1);
                b2.UnlockBits(bd2);
            }
        }
        static void Main(string[] args)
        {
            ConcurrentBag<string> results = new();
            //Parallel.ForEach(Directory.GetFiles(Environment.CurrentDirectory + @"\GigaBoyTests\mooneye_test_roms\", "*.gb"),
            Parallel.ForEach(Directory.GetFiles(Environment.CurrentDirectory + @"\GigaBoyTests\my_test_roms\", "*.gb"),
            //Parallel.ForEach(Directory.GetFiles(Environment.CurrentDirectory + @"\GigaBoyTests\age_test_roms\", "*.gb"),
            //Parallel.ForEach(Directory.GetFiles(Environment.CurrentDirectory + @"\GigaBoyTests\blargg_test_roms\", "*.gb"),
            (string f)=> {
                try
                {
                    Console.WriteLine($"Running Test Rom {f}");
                    var gb = new GBInstance(f);
                    gb.Breakpoint += (sender,args) => { gb.Clock.StopRequested = true; };
                    gb.Clock.AutoBreakpoint = DateTime.Now.AddSeconds(120);
                    gb.MainLoop();
                    Console.WriteLine($"Rom {f} finished");
                    Console.WriteLine("Registers:");
                    Console.WriteLine($"A = {gb.CPU.A:X}    F = {gb.CPU.F:X}");
                    Console.WriteLine($"B = {gb.CPU.B:X}    C = {gb.CPU.C:X}");
                    Console.WriteLine($"D = {gb.CPU.D:X}    E = {gb.CPU.E:X}");
                    Console.WriteLine($"H = {gb.CPU.D:X}    L = {gb.CPU.E:X}");
                    
                    Console.WriteLine();
                    Console.WriteLine($"PC = {gb.CPU.PC:X}    SP = {gb.CPU.SP:X}");
                    Console.WriteLine();
                    Console.WriteLine();
                    var cpu = gb.CPU;
                    if (cpu.B == 3 && cpu.C == 5 && cpu.D == 8 && cpu.E == 13 && cpu.H == 21 && cpu.L == 34) {
                        results.Add($"{f} = PASS");
                    } else if (File.Exists(f+".png")) {
                        Bitmap correct = new Bitmap(f + ".png");
                        var result = gb.PPU.GetInstantImage();
                        if (CompareMemCmp(correct, result))
                        {
                            results.Add($"{f} = IMAGE PASS");
                        }
                        else
                        {
                            results.Add($"{f} = IMAGE FAIL");
                        }
                    }
                    else {
                        results.Add($"{f} = FAIL");
                    }
                }
                catch (Exception e) {
                    results.Add($"{f} = CRASH        ({e.GetType().Name}: {e.Message})");
                    Console.WriteLine(e.ToString());
                }
            });
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            foreach (var r in results) {
                Console.WriteLine(r);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GigaboyDemo
{
    public partial class HexView : UserControl
    {
        public static string HEX_SYMBOLS = "0123456789ABCDEF";
        private IList<byte>? _hexData;

        public IList<byte>? HexData
        {
            get { return _hexData; }
            set { _hexData = value; Init(); }
        }
        private int _bytesPerLine = 16;

        public int BytesPerLine
        {
            get { return _bytesPerLine; }
            set { _bytesPerLine = value; Reload(); }
        }
        private int _bytesPerAddress = 4;

        public int BytesPerAddress
        {
            get { return _bytesPerAddress; }
            set { _bytesPerAddress = value; Reload(); }
        }


        public HexView()
        {
            
            InitializeComponent();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_SelectionChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_SizeChanged(object sender, EventArgs e)
        {
        }

        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void richTextBox1_VScroll(object sender, EventArgs e)
        {
            Reload();
        }
        public void Init() {
            if (HexData == null) return;

            StringBuilder sb = new();
            
            for (int j = 0; j < BytesPerAddress; j++)
            {
                sb.Append("  ");
            }
            sb.Append("       ");
            
            for(int i=0;i<BytesPerLine;i++) sb.Append("   ");
            sb.Append('\n');
            string lineString = sb.ToString();
            sb.Clear();

            for (int i = 0; i < (HexData.Count/BytesPerLine); i++) {
                sb.Append(lineString);
            }
            if ((HexData.Count % BytesPerLine) != 0) sb.Append(lineString);
            richTextBox1.Text = sb.ToString();
            Debug.WriteLine("Initialised: " + sb.Length);
        }
        /*private void AppendHexByte(byte data,bool space = true) {
            if (space) sb.Append(' ');
            sb.Append(HEX_SYMBOLS[(data&0xF0)>>4]);
            sb.Append(HEX_SYMBOLS[data&0x0F]);
        }//*/
        public void Reload()
        {
            if (HexData == null) return;
            ReloadScreenView();
        }
        public void UpdateByte(int address)
        {
            if (HexData == null) return;
            byte data = HexData[address];
            int vOffset = address / BytesPerLine;
            int hOffset = address % BytesPerLine;
            hOffset = BytesPerAddress * 2 + 8 + hOffset * 3;
            Span<char> line = stackalloc char[richTextBox1.Lines[vOffset].Length];
            for (int i = 0; i < richTextBox1.Lines[vOffset].Length; i++) line[i] = richTextBox1.Lines[vOffset][i];
            line[hOffset] = HEX_SYMBOLS[(data & 0xF0) >> 4];
            line[++hOffset] = HEX_SYMBOLS[data & 0x0F];
            richTextBox1.Lines[vOffset] = new string(line);
        }
        public void UpdateByte(int address,Span<char> line)
        {
            if (HexData == null) return;
            byte data = HexData[address];
            int vOffset = address / BytesPerLine;
            int hOffset = address % BytesPerLine;
            hOffset = BytesPerAddress * 2 + 8 + hOffset * 3;
            line[hOffset] = HEX_SYMBOLS[(data & 0xF0) >> 4];
            line[++hOffset] = HEX_SYMBOLS[data & 0x0F];
            richTextBox1.Lines[vOffset] = new string(line);
        }
        public void UpdateAddress(int vOffset, int address)
        {
            Span<char> line = stackalloc char[richTextBox1.Lines[vOffset].Length];
            for (int i = 0; i < richTextBox1.Lines[vOffset].Length; i++) line[i] = richTextBox1.Lines[vOffset][i];

            int hOffset = 0;

            for (int i = 0; i > BytesPerAddress; i++)
            {
                int data = address & 0xFF;
                address = address >> 8;

                line[hOffset++] = HEX_SYMBOLS[(data & 0xF0) >> 4];
                line[hOffset++] = HEX_SYMBOLS[data & 0x0F];
            }
        }
        public void UpdateAddress(int address, Span<char> line)
        {
            int hOffset = BytesPerAddress * 2;

            for (int i = 0; i < BytesPerAddress; i++)
            {
                int data = address & 0x000000FF;
                address = address >> 8;

                line[--hOffset] = HEX_SYMBOLS[data & 0x0F];
                line[--hOffset] = HEX_SYMBOLS[(data & 0xF0) >> 4];
            }
        }
        public void ReloadScreenView() {
            int startIndex = richTextBox1.GetLineFromCharIndex(richTextBox1.GetCharIndexFromPosition(new Point(0,0)));
            int endIndex = richTextBox1.GetLineFromCharIndex(richTextBox1.GetCharIndexFromPosition(new Point(0, richTextBox1.ClientSize.Height)));
            int www = 0;
            for (int y = startIndex; y <= endIndex; y++)
            {
                void updateLine() { //The compiler gets angry when the stackalloc statement is inside a loop, so I put the stackalloc statement inside a function(which is inside a loop) instead.
                    int address = y * BytesPerLine;
                    Span<char> line = stackalloc char[richTextBox1.Lines[y].Length];
                    richTextBox1.Lines[y].AsSpan().CopyTo(line);
                    UpdateAddress(address, line);
                    for (int x = 0; x < BytesPerLine; x++) {
                        UpdateByte(address++, line);
                    }
                }
                updateLine();
                www = y;
            }
            Debug.WriteLine("Reloaded lines: "+www);
        }
    }
}

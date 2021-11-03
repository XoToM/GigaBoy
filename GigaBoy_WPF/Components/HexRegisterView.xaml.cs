using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GigaBoy_WPF.Components
{
    /// <summary>
    /// Interaction logic for HexRegisterView.xaml
    /// </summary>
    public partial class HexRegisterView : UserControl
    {
        public enum RegisterType { Memory8Register, Memory16Register, A,F,AF,B,C,BC,D,E,DE,H,L,HL,SP,PC }
        public enum RegisterDisplayType { Hexadecimal, HexadecimalSpaced, Decimal,Binary}


        public string GBRegisterName
        {
            get { return (string)GetValue(GBRegisterNameProperty); }
            set { SetValue(GBRegisterNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GBRegisterName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GBRegisterNameProperty = DependencyProperty.Register("GBRegisterName", typeof(string), typeof(HexRegisterView), new PropertyMetadata("Register"));

        public ushort MemoryAddress
        {
            get { return (ushort)GetValue(MemoryAddressProperty); }
            set { SetValue(MemoryAddressProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MemoryAddress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MemoryAddressProperty =
            DependencyProperty.Register("MemoryAddress", typeof(ushort), typeof(HexRegisterView), new PropertyMetadata((ushort)0));


        public RegisterType GBRegisterType
        {
            get { return (RegisterType)GetValue(GBRegisterTypeProperty); }
            set { SetValue(GBRegisterTypeProperty, value); Reload(); }
        }

        // Using a DependencyProperty as the backing store for GBRegisterType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GBRegisterTypeProperty =
            DependencyProperty.Register("GBRegisterType", typeof(RegisterType), typeof(HexRegisterView), new PropertyMetadata(HexRegisterView.RegisterType.Memory8Register));



        public RegisterDisplayType GBRegisterDisplayType
        {
            get { return (RegisterDisplayType)GetValue(GBRegisterDisplayTypeProperty); }
            set { SetValue(GBRegisterDisplayTypeProperty, value); if(Emulation.GB is not null) lock (Emulation.GB) Refresh(); }
        }

        // Using a DependencyProperty as the backing store for GBRegisterDisplayType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GBRegisterDisplayTypeProperty =
            DependencyProperty.Register("GBRegisterDisplayType", typeof(RegisterDisplayType), typeof(HexRegisterView), new PropertyMetadata(RegisterDisplayType.Hexadecimal));




        /// <summary>
        /// Gets the value of the register. Should only be used when Emulation.GB is locked
        /// </summary>
        public Func<ushort> Getter { get; protected set; }
        public Action<ushort> Setter { get; protected set; }

        public HexRegisterView()
        {
            Getter = () => { return 0; };
            Setter = (ushort value) => {  };
            Emulation.GBFrameReady += OnFrameRefresh;
            InitializeComponent();
        }
        public void OnFrameRefresh(object? sender,EventArgs args) {
            Refresh();
        }
        public void Refresh() {
            int bitCount=0;
            ushort value = Getter.Invoke();
            switch (GBRegisterType) {
                case RegisterType.Memory8Register:
                case RegisterType.A:
                case RegisterType.B:
                case RegisterType.C:
                case RegisterType.D:
                case RegisterType.E:
                case RegisterType.H:
                case RegisterType.L:
                    bitCount = 8;
                    break;
                case RegisterType.Memory16Register:
                case RegisterType.AF:
                case RegisterType.BC:
                case RegisterType.DE:
                case RegisterType.HL:
                case RegisterType.PC:
                case RegisterType.SP:
                    bitCount = 8;
                    break;
                case RegisterType.F:
                    bitCount = 4;
                    break;
            }
            switch (GBRegisterDisplayType) {
                case RegisterDisplayType.Decimal:
                    inputBox.Text = value.ToString();
                    break;
                case RegisterDisplayType.Hexadecimal:
                    if (bitCount == 16)
                    {
                        inputBox.Text = '$' + value.ToString("X4");
                    }
                    else
                    {
                        inputBox.Text = '$' + value.ToString("X2");
                    }
                    break;
                case RegisterDisplayType.HexadecimalSpaced:
                    stringConverter.Clear();
                    if (bitCount == 16)
                    {
                        stringConverter.Append(((value & 0xFF00) >> 8).ToString("X2"));
                        stringConverter.Append(' ');
                    }
                    stringConverter.Append((value&0x00FF).ToString("X2"));
                    
                    break;
                case RegisterDisplayType.Binary:
                    if (GBRegisterType == RegisterType.F) value = (ushort)(value >> 4);
                    string binary = Convert.ToString(value, 2);
                    stringConverter.Clear().Append('b');
                    for (int i = 0; i < Math.Max(bitCount - binary.Length, 0); i++) {
                        stringConverter.Append('0');
                    }
                    stringConverter.Append(binary);
                    break;
            }

        }
        private static StringBuilder stringConverter = new();
        public void SetValue(int value) {
            if (Emulation.GB is null) return;
            lock (Emulation.GB)
            {
                Setter((ushort)value);
                Refresh();
            }
        }
        public void Reload() {
            switch (GBRegisterType) {
                case RegisterType.Memory8Register:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.MemoryMapper.GetByte(MemoryAddress);
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.MemoryMapper.SetByte(MemoryAddress, (byte)value);
                    };
                    break;
                case RegisterType.Memory16Register:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return (ushort)((Emulation.GB.MemoryMapper.GetByte(MemoryAddress) << 8) | (Emulation.GB.MemoryMapper.GetByte(++MemoryAddress)));
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.MemoryMapper.SetByte(MemoryAddress, (byte)((value&0xFF00)>>8));
                        Emulation.GB.MemoryMapper.SetByte(++MemoryAddress, (byte)(value&0x00FF));
                    };
                    break;
                case RegisterType.A:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.A;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.A = (byte)(value & 0xFF);
                    };
                    break;
                case RegisterType.F:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.F;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.F = (byte)(value & 0xFF);
                    };
                    break;
                case RegisterType.B:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.B;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.B = (byte)(value & 0xFF);
                    };
                    break;
                case RegisterType.C:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.C;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.C = (byte)(value & 0xFF);
                    };
                    break;
                case RegisterType.D:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.D;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.D = (byte)(value & 0xFF);
                    };
                    break;
                case RegisterType.E:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.E;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.E = (byte)(value & 0xFF);
                    };
                    break;
                case RegisterType.H:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.H;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.H = (byte)(value & 0xFF);
                    };
                    break;
                case RegisterType.L:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.L;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.L = (byte)(value & 0xFF);
                    };
                    break;

                case RegisterType.AF:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.AF;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.AF = (byte)(value & 0xFFFF);
                    };
                    break;
                case RegisterType.BC:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.BC;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.BC = (byte)(value & 0xFFFF);
                    };
                    break;
                case RegisterType.DE:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.DE;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.DE = (byte)(value & 0xFFFF);
                    };
                    break;
                case RegisterType.HL:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.HL;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.HL = (byte)(value & 0xFFFF);
                    };
                    break;
                case RegisterType.PC:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.PC;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.PC = (byte)(value & 0xFFFF);
                    };
                    break;
                case RegisterType.SP:
                    Getter = () => {
                        if (Emulation.GB is null) return 0;
                        return Emulation.GB.CPU.SP;
                    };
                    Setter = (ushort value) => {
                        if (Emulation.GB is null) return;
                        Emulation.GB.CPU.SP = (byte)(value & 0xFFFF);
                    };
                    break;
            }
            if(Emulation.GB is not null) lock(Emulation.GB) Refresh();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Emulation.GBFrameReady -= OnFrameRefresh;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Reload();
        }
    }
}

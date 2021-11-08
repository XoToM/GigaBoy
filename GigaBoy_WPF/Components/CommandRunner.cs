using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GigaBoy_WPF.Components
{
    public enum EmulatorAction {Start,Step,Restart,Reset,Stop,Crash }
    public class EmulatorCommandRunner : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public EmulatorCommandRunner() {
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            if (parameter is not string) return;
            EmulatorAction action = Enum.Parse<EmulatorAction>((string)parameter,true);
            Debug.WriteLine($"Emulator Action: {parameter}");
            switch (action) {
                case EmulatorAction.Restart:
                    Emulation.Restart(Emulation.RomFilePath);
                    break;
                case EmulatorAction.Reset:
                    Emulation.Stop();
                    Emulation.Init(Emulation.RomFilePath);
                    break;
                case EmulatorAction.Stop:
                    Emulation.Stop();
                    break;
                case EmulatorAction.Start:
                    Emulation.Start();
                    break;
                case EmulatorAction.Step:
                    Emulation.Step();
                    break;
                case EmulatorAction.Crash:
                    if (Emulation.GB is null) break;
                    lock (Emulation.GB)
                    {
                        try
                        {
                            Emulation.GB.Error(new Exception("User triggered exception"));
                        }
                        catch (Exception) {
                            Emulation.Stop();
                            Emulation.GB = null;
                        }
                    }
                    break;
            }
        }
    }
}

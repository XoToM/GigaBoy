using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components
{
    public interface MMIODevice
    {
        public byte Read(ushort address);
        public byte DirectRead(ushort address);
        public void Write(ushort address,byte value);
        public void DirectWrite(ushort address,byte value);
    }
}

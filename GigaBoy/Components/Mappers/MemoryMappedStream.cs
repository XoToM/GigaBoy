using System;
using System.IO;

namespace GigaBoy.Components.Mappers
{
    internal class MemoryMappedStream : Stream
    {
        private MemoryMapper mapper;

        public MemoryMappedStream(MemoryMapper mapper)
        {
            this.mapper = mapper;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;//Currently Writing hasn't been implemented.

        public override long Length => ushort.MaxValue+1;

        public override long Position { get; set; } = 0;

        public override void Flush()
        {
            return;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (mapper.GB)
            {
                var byteCount = Math.Min(Math.Max(0, count + Position), ushort.MaxValue);
                byteCount = byteCount - Position;
                for (int i = 0; i < byteCount; i++)
                {
                    buffer[offset + i] = mapper.GetByte((ushort)Position++, true);
                }
                return (int)byteCount;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin) {
                case SeekOrigin.Begin:
                    Position = Math.Min(ushort.MaxValue, offset);
                    break;
                case SeekOrigin.Current:
                    Position += Math.Max(0, Math.Min(ushort.MaxValue, offset+Position));
                    break;
                case SeekOrigin.End:
                    Position = Math.Max(0, ushort.MaxValue - offset);
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            return;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }
    }
}
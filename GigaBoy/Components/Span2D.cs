using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components
{
    public ref struct Span2D<T>
    {
        public Span<T> Buffer { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }

        public Span2D(Span<T> buffer,int width,int height) {
            if (width < 0 || height < 0) throw new ArgumentOutOfRangeException("Width and height cannot be negative");
            if (buffer.Length < width * height) throw new OutOfMemoryException($"Buffer has to be at least {width*height} elements long");
            Buffer = buffer;
            Width = width;
            Height = height;
        }

        public T this[int y,int x]{
            get {
                if (x > Width) throw new IndexOutOfRangeException("X cannot be bigger than the Width of the span");
                return Buffer[y * Width + x];
            }
            set
            {
                if (x > Width) throw new IndexOutOfRangeException("X cannot be bigger than the Width of the span");
                Buffer[y * Width + x] = value;
            }
        }
        /// <summary>
        /// Copies data from <paramref name="source"/> to the Span2D object (from <paramref name="x"/> to the max width). When the edge of the bitmap is reached, continues from the start of the next row.
        /// </summary>
        /// <param name="x">Starting X Coordinate</param>
        /// <param name="y">Starting Y Coordinate</param>
        /// <param name="source">Span to copy from</param>
        public void SetBlockHorizontal(int x, int y, Span<T> source)
        {
            source.CopyTo(Buffer.Slice(y * Width + x));
        }
        /// <summary>
        /// Copies data from the Span2D object to <paramref name="destination"/>(from <paramref name="x"/> to the max width). When the edge of the bitmap is reached, continues from the start of the next row.
        /// </summary>
        /// <param name="x">Starting X Coordinate</param>
        /// <param name="y">Starting Y Coordinate</param>
        /// <param name="destination">Span to copy to</param>
        public void GetBlockHorizontal(int x, int y, Span<T> destination)
        {
            Buffer.Slice(y * Width + x).CopyTo(destination);
        }
        /// <summary>
        /// Copies data from <paramref name="source"/> to the Span2D object (from <paramref name="y"/> to the max height). When the edge of the bitmap is reached, continues from the start of the next column.
        /// </summary>
        /// <param name="x">Starting X Coordinate</param>
        /// <param name="y">Starting Y Coordinate</param>
        /// <param name="source">Span to copy from</param>
        public void SetBlockVertical(int x, int y, Span<T> source)
        {
            for (int i = 0; i < source.Length; i++)
            {
                this[x, y] = source[i];
                y = (++y) % Height;
                if (y == 0) ++x;
            }
        }
        /// <summary>
        /// Copies data from the Span2D object to <paramref name="destination"/>(from <paramref name="y"/> to the max height). When the edge of the bitmap is reached, continues from the start of the next column.
        /// </summary>
        /// <param name="x">Starting X Coordinate</param>
        /// <param name="y">Starting Y Coordinate</param>
        /// <param name="destination">Span to copy to</param>
        public void GetBlockVertical(int x, int y, Span<T> destination)
        {
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = this[x, y];
                y = (++y) % Height;
                if (y == 0) ++x;
            }
        }
    }
}

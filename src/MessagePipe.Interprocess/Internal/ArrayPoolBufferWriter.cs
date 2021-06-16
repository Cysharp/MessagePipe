using System;
using System.Buffers;

namespace MessagePipe.Interprocess.Internal
{
    internal class ArrayPoolBufferWriter : IBufferWriter<byte>, IDisposable
    {
        byte[] buffer;
        int index;

        const int MinBufferSize = 256;

        public int WrittenCount => index;
        public int Capacity => buffer.Length;
        public int FreeCapacity => buffer.Length - index;

        public ReadOnlySpan<byte> WrittenSpan => buffer.AsSpan(0, index);

        public ArrayPoolBufferWriter()
        {
            buffer = Array.Empty<byte>();
        }

        public void Advance(int count)
        {
            index += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return buffer.AsMemory(index);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return buffer.AsSpan(index);
        }

        void EnsureCapacity(int sizeHint)
        {
            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            if (sizeHint > FreeCapacity)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(index + sizeHint, MinBufferSize));
                if (buffer.Length != 0)
                {
                    Array.Copy(buffer, 0, newBuffer, 0, index);
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                buffer = newBuffer;
            }
        }

        public void Dispose()
        {
            if (buffer.Length != 0)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                buffer = Array.Empty<byte>();
            }
        }
    }

}

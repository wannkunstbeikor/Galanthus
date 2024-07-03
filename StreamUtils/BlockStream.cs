using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace StreamUtils;

public class BlockStream : DataStream
{
    private readonly Block<byte> m_block;
    private readonly bool m_leaveOpen;

    public BlockStream()
    {
        m_block = new Block<byte>(0);
        m_stream = m_block.ToStream();
    }

    public BlockStream(int inSize)
    {
        m_block = new Block<byte>(inSize);
        m_stream = m_block.ToStream();
    }

    public BlockStream(Block<byte> inBuffer, bool inLeaveOpen = false)
    {
        m_block = inBuffer;
        m_stream = m_block.ToStream();
        m_leaveOpen = inLeaveOpen;
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ResizeStream(Position + buffer.Length);
        base.Write(buffer);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ResizeStream(Position + count);
        base.Write(buffer, offset, count);
    }

    public override void WriteByte(byte value)
    {
        ResizeStream(Position + sizeof(byte));
        base.WriteByte(value);
    }

    public override unsafe void CopyTo(DataStream destination, int bufferSize)
    {
        if (destination is not BlockStream stream)
        {
            base.CopyTo(destination, bufferSize);
            return;
        }

        if (bufferSize <= 0)
        {
            return;
        }

        stream.ResizeStream(stream.Position + bufferSize);

        if (stream.Length < stream.Position + bufferSize)
        {
            stream.m_stream.SetLength((int)stream.Position + bufferSize);
        }

        using (Block<byte> a = new(m_block.BasePtr + Position, bufferSize))
        using (Block<byte> b = new(stream.m_block.BasePtr + stream.Position, bufferSize))
        {
            a.MarkMemoryAsFragile();
            b.MarkMemoryAsFragile();
            a.CopyTo(b);
        }

        stream.Position += bufferSize;

        Position += bufferSize;
    }

    public override unsafe string ReadNullTerminatedString()
    {
        string retVal = new((sbyte*)(m_block.Ptr + Position));
        Position += retVal.Length + 1;
        return retVal;
    }

    public override unsafe DataStream CreateSubStream(long inStartOffset, int inSize)
    {
        Block<byte> sub = new(m_block.BasePtr + inStartOffset, inSize);
        sub.MarkMemoryAsFragile();
        return new BlockStream(sub);
    }

    /// <summary>
    /// Loads whole file into memory and deobfuscates it if necessary.
    /// </summary>
    /// <param name="inPath">The path of the file</param>
    /// <returns>A <see cref="BlockStream"/> that has the file loaded.</returns>
    public static BlockStream FromFile(string inPath)
    {
        using (FileStream stream = new(inPath, FileMode.Open, FileAccess.Read))
        {
            BlockStream retVal = new((int)stream.Length);
            stream.ReadExactly(retVal.m_block);
            return retVal;
        }
    }

    /// <summary>
    /// Loads part of a file into memory.
    /// </summary>
    /// <param name="inPath">The path of the file.</param>
    /// <param name="inOffset">The offset of the data to load.</param>
    /// <param name="inSize">The size of the data to load</param>
    /// <returns>A <see cref="BlockStream"/> that has the data loaded.</returns>
    public static BlockStream FromFile(string inPath, long inOffset, int inSize)
    {
        using (FileStream stream = new(inPath, FileMode.Open, FileAccess.Read))
        {
            stream.Position = inOffset;

            BlockStream retVal = new(inSize);

            stream.ReadExactly(retVal.m_block);
            return retVal;
        }
    }

    /// <summary>
    /// <see cref="Aes"/> decrypt this <see cref="BlockStream"/>.
    /// </summary>
    /// <param name="inKey">The key to use for the decryption.</param>
    /// <param name="inPaddingMode">The <see cref="PaddingMode"/> to use for the decryption.</param>
    public void Decrypt(byte[] inKey, PaddingMode inPaddingMode)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = inKey;
            Span<byte> span = m_block.ToSpan((int)Position);
            aes.DecryptCbc(span, inKey, span, inPaddingMode);
        }
    }

    /// <summary>
    /// <see cref="Aes"/> decrypt part of this <see cref="BlockStream"/>.
    /// </summary>
    /// <param name="inKey">The key to use for the decryption.</param>
    /// <param name="inSize">The size of the data to decrypt.</param>
    /// <param name="inPaddingMode">The <see cref="PaddingMode"/> to use for the decryption.</param>
    public void Decrypt(byte[] inKey, int inSize, PaddingMode inPaddingMode)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = inKey;
            Span<byte> span = m_block.ToSpan((int)Position, inSize);
            aes.DecryptCbc(span, inKey, span, inPaddingMode);
        }
    }

    public override void Dispose()
    {
        if (m_leaveOpen && Position > Length)
        {
            Span<byte> padding = new byte[Position - Length];
            Position = Length;
            m_stream.Write(padding);
        }
        if (m_leaveOpen && m_block.Size != Length)
        {
            // resize the block if needed
            m_block.Resize((int)Length);
        }

        base.Dispose();
        if (!m_leaveOpen)
        {
            m_block.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    private unsafe void ResizeStream(long inDesiredMinLength)
    {
        if (inDesiredMinLength > m_block.Size)
        {
            long position = Position;
            int oldSize = m_block.Size;
            int neededLength = (int)Math.Max(inDesiredMinLength, Environment.SystemPageSize + position);
            neededLength = neededLength + 15 & ~15;
            m_block.Resize(neededLength);

            // make sure resized memory is 0
            uint size = (uint)(neededLength - oldSize);
            if (size > 0)
            {
                NativeMemory.Clear(m_block.BasePtr + oldSize, size);
            }

            m_stream = m_block.ToStream();
            m_stream.SetLength(inDesiredMinLength);
            m_stream.Position = position;
        }
    }
}
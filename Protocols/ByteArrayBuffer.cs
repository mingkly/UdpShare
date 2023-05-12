using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public class ByteArrayBuffer
    {
        byte[] data;
        int position;
        public byte[] Data => data;
        public int Count => position;
        
        public ByteArrayBuffer(byte[] data)
        {
            this.data = data;
            position= 0;
        }
        public ByteArrayBuffer(byte[] data,int position)
        {
            this.data = data;
            this.position = position;
        }
        public void Reset()
        {
            position = 0;
        }
        public void WriteInt32(uint value)
        {
            int length = 4;
            for(int i=0; i<length; i++)
            {
                data[position + i] = (byte)(value >>8* i);
            }
            position+= length;
        }
        public void WriteInt32(int value)
        {
            WriteInt32((uint)value);
        }
        public int ReadInt32()
        {
            int length = 4;
            int value = 0;
            for (int i = 0; i < length; i++)
            {
                value |= (data[position + i] << 8 * i);
            }
            position+= length;
            return value;
        }
        public uint ReadUInt()
        {
            return (uint)ReadInt32();
        }
        public void WriteString(string value)
        {
            if(value == null)
            {
                WriteInt32(0);
            }
            else
            {
                var buffer = Encoding.UTF8.GetBytes(value);
                WriteInt32(buffer.Length);
                WriteBytes(buffer, 0, buffer.Length);
            }

        }
        public string ReadString()
        {
            var length = ReadInt32();
            if(length == 0)
            {
                return null;
            }
            var buffer = new byte[length];
            ReadBytes(buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }
        public void WriteInt16(short value)
        {
            data[position]= (byte)(value);
            data[position + 1] = (byte)(value >> 8);
            position += 2;
        }
        public short ReadInt16()
        {
            return (short)(data[position]|data[position+1]<<8);
        }
        public void WriteByte(byte value)
        {
            data[position]= (byte)(value);
            position += 1;
        }
        public byte ReadByte()
        {
            var value = data[position];
            position += 1;
            return value;
        }
        public void WriteBytes(byte[] data,int offset,int count)
        {
            for(int i = 0; i < count; i++)
            {
                this.data[i + position] = data[offset+i];
            }
            position+= count;
        }
        public void ReadBytes(byte[] data, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                data[offset + i] = this.data[i + position];                 
            }
            position += count;
        }

        public void WriteStream(Stream stream,int count)
        {
            stream.Read(data, position, count);   
            position+= count;
        }
        public void ReadStream(Stream stream,int count)
        {
            stream.Write(data,position, count);
        }
    }
}

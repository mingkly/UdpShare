
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public class DefaultEncoder : IEncoder
    {
        public void Encode(ByteArrayBuffer buffer, ISegement segement)
        {
            if (segement is HeadSegement head)
            {
                EncoderHead(buffer,head);
            }
            else if(segement is ReplySegement reply)
            {
                EncodeReply(buffer,reply);
            }
            else if (segement is ByteBodySegement byteSege)
            {
                EncodeByteBody(buffer, byteSege);
            }
            else if (segement is StreamBodySegement streamSege)
            {
                EncodeStreamBody(buffer, streamSege);
            }
            else if (segement is FootSegement foot)
            {
                EncodeFoot(buffer, foot);
            }
            else if(segement is FoundDeviceHead foundDevice)
            {
                EncodeFoundDevice(buffer,foundDevice);
            }
            else if(segement is CommandSegement command)
            {
                EncodeCommand(buffer, command);
            }
        }

        private void EncodeCommand(ByteArrayBuffer buffer, CommandSegement command)
        {
            buffer.WriteInt32((int)command.SegementType);
            buffer.WriteInt32(command.FileId);
            buffer.WriteInt32((int)command.Command);
            buffer.WriteString(command.Value);
        }

        private void EncodeFoundDevice(ByteArrayBuffer buffer, FoundDeviceHead foundDevice)
        {
            buffer.WriteInt32((int)foundDevice.SegementType);
            buffer.WriteInt32(foundDevice.Id);
            buffer.WriteString(foundDevice.DeviceName);
        }

        private void EncodeReply(ByteArrayBuffer buffer, ReplySegement reply)
        {
            buffer.WriteInt32((int)reply.SegementType);
            buffer.WriteInt32(reply.FileId);
        }

        void EncoderHead(ByteArrayBuffer buffer,HeadSegement segement)
        {
            buffer.WriteInt32((int)segement.SegementType);
            buffer.WriteInt32(segement.FileId);
            buffer.WriteInt64(segement.FileLength);
            buffer.WriteInt32((int)segement.FileType);
            buffer.WriteString(segement.FileName);
        }
        void EncodeByteBody(ByteArrayBuffer buffer,ByteBodySegement segement)
        {
            buffer.WriteInt32((int)segement.SegementType);
            buffer.WriteInt32(segement.FileId);
            buffer.WriteInt32(segement.FileOrder);
            buffer.WriteBytes(segement.RawData,0,segement.RawData.Length);
        }
        void EncodeStreamBody(ByteArrayBuffer buffer,StreamBodySegement segement)
        {
            buffer.WriteInt32((int)segement.SegementType);
            buffer.WriteInt32(segement.FileId);
            buffer.WriteInt32(segement.FileOrder);
            if (segement.Stream.CanSeek)
            {
                segement.Stream.Position = segement.Index;
            }
            buffer.WriteStream(segement.Stream, (int)segement.Length);
        }
        void EncodeFoot(ByteArrayBuffer buffer,FootSegement segement)
        {
            buffer.WriteInt32((int)segement.SegementType);
            buffer.WriteInt32(segement.FileId);
        }


    }
}

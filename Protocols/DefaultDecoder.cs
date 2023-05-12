
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;

namespace UdpQuickShare.Protocols
{
    public class DefaultDecoder : IDecoder
    {
        public ISegement Decode(ByteArrayBuffer buffer)
        {
            try
            {
                var typeInt=buffer.ReadInt32();
                try
                {
                    SegementType segementType = (SegementType)typeInt;
                    if (segementType == SegementType.Head)
                    {
                        return DecodeHeader(buffer);
                    }
                    else if (segementType == SegementType.Reply)
                    {
                        return DecodeReply(buffer);
                    }
                    else if (segementType == SegementType.Body)
                    {
                        return DecodeBody(buffer);
                    }
                    else if (segementType == SegementType.Foot)
                    {
                        return DecodeFoot(buffer);
                    }
                    else if(segementType == SegementType.DeviceFound)
                    {
                        return DecodeDeviceFound(buffer);
                    }
                    else if (segementType == SegementType.Command)
                    {
                        return DecodeCommand(buffer);
                    }
                }
                catch { }
            }
            catch { }
            return null;
        }

        private ISegement DecodeCommand(ByteArrayBuffer buffer)
        {
            var id=buffer.ReadUInt();
            CommandType command = (CommandType)buffer.ReadInt32();
            var value = buffer.ReadString();
            return new CommandSegement(id,value, command);
        }

        private ISegement DecodeDeviceFound(ByteArrayBuffer buffer)
        {
            if (buffer.ReadInt32() != FoundDeviceHead.DefaultId)
            {
                return null;
            }
            return new FoundDeviceHead(buffer.ReadString());
        }

        private ISegement DecodeReply(ByteArrayBuffer buffer)
        {
            var fileId = buffer.ReadUInt();
            return new ReplySegement(fileId);
        }

        public HeadSegement DecodeHeader(ByteArrayBuffer buffer)
        {
            var fileId = buffer.ReadUInt();
            var fileLength=buffer.ReadUInt();
            var fileTypeInt=buffer.ReadInt32();
            FileType fileType;
            try
            {
                fileType= (FileType)fileTypeInt;
            }
            catch
            {
                fileType = FileType.Any;
            }
            var fileName=buffer.ReadString();
            return new HeadSegement(fileName,fileId,fileLength,fileType);
        }
        public PartByteSegement DecodeBody(ByteArrayBuffer buffer)
        {
            var fileId = buffer.ReadUInt();
            var fileOrder = buffer.ReadUInt();
            return new PartByteSegement(fileId,fileOrder,buffer.Data,buffer.Count,buffer.Data.Length-buffer.Count);
        }
        public FootSegement DecodeFoot(ByteArrayBuffer buffer)
        {
            var fileId = buffer.ReadUInt();
            return new FootSegement(fileId);
        }
    }
}

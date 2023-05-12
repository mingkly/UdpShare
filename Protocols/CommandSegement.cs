using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public class CommandSegement : ISegement
    {
        public SegementType SegementType => SegementType.Command;

        public uint FileId { get; }
        public string Value { get; }
        public CommandType Command { get; }
        public CommandSegement(string value, CommandType command)
        {
            Value = value;
            Command = command;
        }
        public CommandSegement(uint fileId,string value, CommandType command)
        {
            Value = value;
            Command = command;
            FileId= fileId;
        }
    }
}

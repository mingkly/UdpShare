using Android.OS;
using Android.Runtime;
using Java.IO;
using Java.Nio;
using Java.Nio.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Platforms.Android.Services
{
    internal class JavaStreamWrapper : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek=>true;

        public override bool CanWrite => true;

        public override long Length
        {
            get
            {
                return channel.Size();
            }
        }

        public override long Position
        {
            get
            {
                return channel.Position();
            }
            set
            {
                channel.Position(value);
            }
        }
        readonly ParcelFileDescriptor fileDescriptor;
        readonly RandomAccessFile randomAccessFile;
        readonly FileChannel channel;
        readonly FileOutputStream outputStream;
        public JavaStreamWrapper(Java.IO.File file)
        {
            randomAccessFile = new RandomAccessFile(file, "rw");
            channel = randomAccessFile.Channel;           
        }
        public JavaStreamWrapper( ParcelFileDescriptor fileDescriptor)
        {
            this.fileDescriptor = fileDescriptor;
            outputStream = new FileOutputStream(fileDescriptor.FileDescriptor);
            channel= outputStream.Channel;
        }

        public override void Flush()
        {
            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(Position+count > Length)
            {
                count = (int)(Length - Position);
            }
            if (randomAccessFile != null)
            {
                return randomAccessFile.Read(buffer, offset, count);
            }
            var javaBuffer = ByteBuffer.Wrap(buffer, offset, count);
            var readed = channel.Read(javaBuffer);
            javaBuffer.Get(buffer, offset, readed);
            return readed;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch(origin)
            {
                case SeekOrigin.Begin:
                    channel.Position(offset);
                    break;
                case SeekOrigin.Current:
                    channel.Position(channel.Position()+offset);
                    break;
                case SeekOrigin.End:
                    channel.Position(channel.Size() - offset);
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            channel.Truncate(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var javaBuffer = ByteBuffer.Wrap(buffer, offset, count);
            channel.Write(javaBuffer);
            
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try
            {
                fileDescriptor?.Close();
                fileDescriptor?.Dispose();
                randomAccessFile?.Dispose();
                outputStream?.Dispose();
                channel?.Dispose();
            }
            catch { }
        }
    }
}

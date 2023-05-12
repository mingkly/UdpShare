using Android.OS;
using Android.Runtime;
using Java.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Platforms.Android.Services
{
    internal class JavaStreamWrapper : Stream
    {
        public override bool CanRead => inputStream != null;

        public override bool CanSeek
        {
            get
            {
                if (inputStream != null)
                {
                    return true;
                }
                else if (outputStream != null)
                {
                    return true;
                }
                return false;
            }
        }

        public override bool CanWrite => outputStream != null;

        public override long Length
        {
            get
            {
                if (inputStream != null)
                {
                    return inputStream.Channel.Size();
                }
                else if(outputStream != null)
                {
                    return outputStream.Channel.Size();
                }
                return 0;
            }
        }

        public override long Position
        {
            get
            {
                if (outputStream != null)
                {                    
                    return outputStream.Channel.Position();
                }
                if (inputStream != null)
                {
                    return inputStream.Channel.Position();
                }
                return 0;
            }
            set
            {
                if (inputStream != null)
                {
                    inputStream.Channel.Position(value);
                }
                if (outputStream != null)
                {
                    outputStream.Channel.Position(value);
                }
            }
        }

        readonly FileOutputStream outputStream;
        readonly FileInputStream inputStream;
        readonly ParcelFileDescriptor fileDescriptor;
        public JavaStreamWrapper( FileOutputStream outputStream)
        {
            this.outputStream = outputStream;
        }
        public JavaStreamWrapper(FileInputStream inputStream )
        {
            this.inputStream = inputStream;
        }
        public JavaStreamWrapper(FileOutputStream outputStream,FileInputStream inputStream)
        {
            this.inputStream = inputStream;
            this.outputStream = outputStream;
        }
        public JavaStreamWrapper( ParcelFileDescriptor fileDescriptor):this(new FileOutputStream(fileDescriptor.FileDescriptor),new FileInputStream(fileDescriptor.FileDescriptor))
        {
            this.fileDescriptor = fileDescriptor;
        }

        public override void Flush()
        {
            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readed= inputStream?.Read(buffer, offset, count) ?? 0;
            outputStream?.Channel.Position(inputStream.Channel.Position());
            return readed;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch(origin)
            {
                case SeekOrigin.Begin:
                    inputStream?.Channel.Position(offset);
                    outputStream?.Channel.Position(offset);
                    break;
                case SeekOrigin.Current:
                    inputStream?.Channel.Position(inputStream.Channel.Position()+offset);
                    outputStream?.Channel.Position(outputStream.Channel.Position()+offset);
                    break;
                case SeekOrigin.End:
                    inputStream?.Channel.Position(inputStream.Channel.Size()-offset);
                    outputStream?.Channel.Position(outputStream.Channel.Size() - offset);
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            outputStream?.Channel.Truncate(value);
            inputStream?.Channel.Truncate(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            outputStream?.Write(buffer, offset, count);
            inputStream?.Channel.Position(outputStream.Channel.Position());
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try
            {
                fileDescriptor?.Dispose();
                inputStream?.Dispose();
                outputStream?.Dispose();
            }
            catch { }
        }
    }
}

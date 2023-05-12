using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.Protocols;

namespace UdpQuickShare.Clients
{
    public class FileResultEventArgs
    {
        SendingFile SendingFile { get; }
        RecievingFile RecievingFile { get; }
        HeadSegement HeadSegement { get; }

        public FileResultState State { get; }

        public uint FileId
        {
            get 
            {
                if (SendingFile != null)
                {
                    return SendingFile.FileId;
                }
                else if(RecievingFile!= null)
                {
                    return RecievingFile.FileId;
                }
                else if(HeadSegement!=null)
                {
                    return HeadSegement.FileId;
                }
                return 0;
            }
        }
        public string TextValue { get; set; }
        string savedPath;
        public string SavedPath
        {
            get
            {
                if(savedPath == null)
                {
                    return RecievingFile?.SavedPath;
                }
                return savedPath;
            }
        }
        public string FileName
        {
            get
            {
                if (SendingFile != null)
                {
                    return SendingFile.FileName;
                }
                else if (RecievingFile != null)
                {
                    return RecievingFile.FileName;
                }
                else if (HeadSegement != null)
                {
                    return HeadSegement.FileName;
                }
                return null;
            }
        }
        public long FileLength
        {
            get
            {
                if (SendingFile != null)
                {
                    return SendingFile.FileLength;
                }
                else if (RecievingFile != null)
                {
                    return RecievingFile.FileLength;
                }
                else if (HeadSegement != null)
                {
                    return HeadSegement.FileLength;
                }
                return 0;
            }
        }

        public long Position { get; }


        public FileResultEventArgs(SendingFile sendingFile,FileResultState state,long position=0)
        {
            SendingFile= sendingFile;
            State= state;
            Position= position;
        }
        public FileResultEventArgs(RecievingFile recievingFile, FileResultState state, long position = 0)
        {
            RecievingFile= recievingFile;
            State = state;
            Position = position;
        }
        public FileResultEventArgs(HeadSegement headSegement,FileResultState state)
        {
            HeadSegement = headSegement;
            this.State= state;
        }
        public FileResultEventArgs(HeadSegement headSegement,string savedPath, FileResultState state)
        {
            HeadSegement = headSegement;
            this.State = state;
            this.savedPath= savedPath;
        }
    }
}

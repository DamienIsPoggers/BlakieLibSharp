using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace BlakieLibSharp
{
    public class DPArc : IDisposable
    {
        Dictionary<string, ArchiveFile> files = new Dictionary<string, ArchiveFile>();
        List<string> fileNames = new List<string>();
        
        public DPArc(byte[] data)
        {
            BinaryReader file = new BinaryReader(new MemoryStream(data));
            ParseData(file);
            file.Close();
        }

        public DPArc(BinaryReader data)
        {
            ParseData(data);
        }

        void ParseData(BinaryReader file)
        {
            if(Encoding.ASCII.GetString(file.ReadBytes(5)) != "DPARC")
            {
                Console.WriteLine("Invalid DPArc file");
                return;
            }

            int fileCount = file.ReadInt32();
            int headerSize = file.ReadInt32();

            for(int i = 0; i < fileCount; i++)
            {
                ArchiveFile archiveFile = new ArchiveFile();
                archiveFile.fileName = Encoding.ASCII.GetString(file.ReadBytes(file.ReadByte()));
                int dataOffset = file.ReadInt32();
                archiveFile.dataSize = file.ReadInt32();
                long position = file.BaseStream.Position;
                file.BaseStream.Position = headerSize + dataOffset;
                if(archiveFile.dataSize > 0)
                    archiveFile.data = file.ReadBytes(archiveFile.dataSize);
                else
                    archiveFile.data = new byte[0];
                file.BaseStream.Position = position;
                files.Add(archiveFile.fileName, archiveFile);
                fileNames.Add(archiveFile.fileName);
            }
        }

        public bool FileExists(string fileName)
        {
            return files.ContainsKey(fileName);
        }

        public ArchiveFile GetFile(string fileName)
        {
            if(files.ContainsKey(fileName))
                return files[fileName];
            return null;
        }

        public void Dispose()
        {
            files.Clear();
            GC.Collect();
        }
    }

    [Serializable]
    public class ArchiveFile
    {
        public string fileName;
        public int dataSize;
        public byte[] data;

        public string DataAsString()
        {
            return Encoding.ASCII.GetString(data);
        }
    }
}

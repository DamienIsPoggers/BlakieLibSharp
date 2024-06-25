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

        int headerSize = 0;
        BinaryReader reader;
        
        public DPArc(byte[] data)
        {
            reader = new BinaryReader(new MemoryStream(data));
            ParseData(reader);
        }

        public DPArc(BinaryReader data)
        {
            reader = data;
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
            headerSize = file.ReadInt32();

            for(int i = 0; i < fileCount; i++)
            {
                ArchiveFile archiveFile = new ArchiveFile(reader);
                archiveFile.fileName = Encoding.ASCII.GetString(file.ReadBytes(file.ReadByte()));
                archiveFile.dataPos = file.ReadInt32() + headerSize;
                archiveFile.dataSize = file.ReadInt32();
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

        public ArchiveFile[] GetFolder(string folder)
        {
            List<ArchiveFile> rtrn = new List<ArchiveFile>();
            foreach (ArchiveFile file in files.Values)
                if (file.fileName.StartsWith(folder))
                    rtrn.Add(file);
            return rtrn.ToArray();
        }

        public void Dispose()
        {
            foreach (ArchiveFile file in files.Values)
                file.Dispose();
            files.Clear();
            reader.Dispose();
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }

    [Serializable]
    public class ArchiveFile : IDisposable
    { 
        public string fileName = "";
        public int dataSize = 0;
        public int dataPos = 0;
        public bool dataLoaded = false;
        public byte[] data { get { if (!dataLoaded) LoadData(); return bytes; } set { bytes = value; } }
        byte[] bytes;

        BinaryReader reader;

        public ArchiveFile(BinaryReader reader)
        {
            this.reader = reader;
        }

        public string DataAsString()
        {
            LoadData();
            return Encoding.ASCII.GetString(data);
        }

        public void Dispose()
        {
            data = new byte[0];
            GC.SuppressFinalize(this);
        }

        public void LoadData()
        {
            dataLoaded = true;
            reader.BaseStream.Position = dataPos;
            data = reader.ReadBytes(dataSize);
        }

        public void UnloadData()
        {
            data = new byte[0];
            GC.SuppressFinalize(this);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using ZLibDotNet;

namespace BlakieLibSharp
{
    public class DPSpr : IDisposable
    {
        public Dictionary<string, Sprite> sprites { get; private set; } = new Dictionary<string, Sprite>();

        public DPSpr(BinaryReader file, bool useBasePal = false)
        {
            ParseData(file, useBasePal);
        }

        public DPSpr(byte[] data, bool useBasePal = false)
        {
            using (BinaryReader file = new BinaryReader(new MemoryStream(data)))
                ParseData(file, useBasePal);
        }

        void ParseData(BinaryReader file, bool useBasePal)
        {
            if (Encoding.ASCII.GetString(file.ReadBytes(5)) != "DPSpr")
            {
                Console.WriteLine("Invalid DPSpr file, not loading");
                return;
            }

            int spriteCount = file.ReadInt32();
            int paletteCount = file.ReadInt32();
            CompressionType compressed = (CompressionType)file.ReadByte();

            int[,] palettes = new int[0,0];
            byte[,][] palettesBytes = new byte[0, 0][];
            if (!useBasePal)
                file.ReadBytes(256 * sizeof(int) * paletteCount);
            else
            {
                //Console.WriteLine("Loading dpsprite with palette");
                palettes = new int[paletteCount, 256];
                for (int i = 0; i < paletteCount; i++)
                {
                    for (int j = 0; j < 256; j++)
                        palettes[i, j] = file.ReadInt32();
                    palettes[i, 0] = 0; //set the first color to be transparent
                }
                palettesBytes = new byte[paletteCount, 256][];
                for (int i = 0; i < paletteCount; i++)
                    for (int j = 0; j < 256; j++)
                        palettesBytes[i, j] = BitConverter.GetBytes(palettes[i, j]);
            }

            for(int i = 0; i < spriteCount; i++)
            {
                Sprite header = new Sprite
                {
                    name = Encoding.ASCII.GetString(file.ReadBytes(file.ReadByte())),
                    indexed = file.ReadBoolean(),
                    palNum = file.ReadByte(),
                    width = file.ReadInt32(),
                    height = file.ReadInt32(),
                    size = file.ReadInt32()
                };
                sprites.Add(header.name, header);
            }

            foreach(Sprite sprite in sprites.Values)
            {
                int texSize = sprite.width * sprite.height;
                List<byte> pixels = new List<byte>();
                int bytesRead = 0;

                if (compressed == CompressionType.None)
                {
                    if (!sprite.indexed)
                        sprite.imageData = file.ReadBytes(texSize * sizeof(int));
                    else
                    {
                        for (int i = 0; i < texSize; i++)
                        {
                            byte index = file.ReadByte();
                            pixels.AddRange(useBasePal ? BitConverter.GetBytes(palettes[sprite.palNum, index]) : new byte[] { index, index, index, 255 });
                        }
                        sprite.imageData = pixels.ToArray();
                    }
                }
                else if(compressed == CompressionType.RLE)
                {
                    if (useBasePal || !sprite.indexed)
                        texSize *= sizeof(int);
                    while (pixels.Count < texSize)
                    {
                        byte pixelLength = file.ReadByte();
                        bytesRead++;
                        if (pixelLength == 0)
                        {
                            ushort repeatCount = file.ReadUInt16();
                            bytesRead += 2;
                            int pixel = sprite.indexed ? (useBasePal ? palettes[sprite.palNum, file.ReadByte()] : file.ReadByte()) : file.ReadInt32();
                            bytesRead++;
                            byte[] pixelBytes = BitConverter.GetBytes(pixel);
                            if (useBasePal || !sprite.indexed)
                                for (int i = 0; i < repeatCount; i++)
                                    pixels.AddRange(pixelBytes);
                            else
                                for (int i = 0; i < repeatCount; i++)
                                    pixels.Add(pixelBytes[0]);
                        }
                        else
                        {
                            bytesRead += pixelLength;
                            for (int i = 0; i < pixelLength; i++)
                            {
                                int pixel = sprite.indexed ? (useBasePal ? palettes[sprite.palNum, file.ReadByte()] : file.ReadByte()) : file.ReadInt32();
                                byte[] pixelBytes = BitConverter.GetBytes(pixel);
                                if (useBasePal || !sprite.indexed)
                                    pixels.AddRange(pixelBytes);
                                else
                                    pixels.Add(pixelBytes[0]);
                            }
                        }
                    }

                    if (bytesRead > sprite.size)
                    {
                        //Console.WriteLine("Error reading sprite " + sprite.name);
                        //Console.WriteLine(bytesRead);
                        //Console.WriteLine(sprite.size);
                        file.BaseStream.Position -= bytesRead - sprite.size;
                    }

                    sprite.imageData = pixels.ToArray();
                    if (useBasePal)
                        sprite.indexed = false;
                }
                else if(compressed == CompressionType.ZLib)
                {
                    if (!sprite.indexed)
                        texSize *= sizeof(int);
                    byte[] data = new byte[texSize];
                    byte[] compressedData = file.ReadBytes(sprite.size);
                    ZLib zlib = new ZLib();
                    zlib.Uncompress(data, out _, compressedData);
                    if (useBasePal && sprite.indexed)
                    {
                        for (int i = 0; i < data.Length; i++)
                            pixels.AddRange(palettesBytes[sprite.palNum, data[i]]);
                        sprite.indexed = false;
                    }
                    else
                        pixels.AddRange(data);

                    sprite.imageData = pixels.ToArray();
                }
            }


        }

        public bool HasSprite(string sprite)
        {
            return sprites.ContainsKey(sprite);
        }

        public Sprite GetSprite(string sprite)
        {
            return sprites[sprite];
        }

        public Vector2 GetSpriteSize(string sprite)
        {
            return new Vector2(sprites[sprite].width, sprites[sprite].height);
        }

        public uint GetSpriteGLId(string sprite)
        {
            return sprites[sprite].glTexId;
        }

        /// <summary>
        /// Very important. Lets sprite know that it can remove itself from ram cause its on gpu and will set its opengl tex id
        /// </summary>
        public void SpriteIsOnGPU(string sprite, uint glId)
        {
            Array.Clear(sprites[sprite].imageData);
            sprites[sprite].imageData = null;
            sprites[sprite].glTexId = glId;
        }

        public void Dispose()
        {
            sprites.Clear();
            GC.SuppressFinalize(this);
        }

        public class Sprite
        {
            public string name = "";
            public bool indexed = false;
            public byte palNum = 0;
            public int width = 0;
            public int height = 0;
            public int size = 0; //compressed size if file is compressed
            public byte[] imageData = new byte[0];
            public unsafe void* imageDataPtr { get { if(imageData.Length > 0) fixed(void* rtrn = &imageData[0]) return rtrn; return null; } }
            public uint glTexId = 0;
            public Sprite() { }
        }

        public enum CompressionType : byte
        {
            None = 0,
            RLE = 1,
            ZLib = 2,
        }
    }
}

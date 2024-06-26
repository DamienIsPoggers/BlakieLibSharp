﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace BlakieLibSharp
{
    public class PrmAn : IDisposable
    {
        public Dictionary<string, Frame> frames = new Dictionary<string, Frame>();
        public Dictionary<int, Texture> textures = new Dictionary<int, Texture>();
        public Dictionary<string, Animation> animations = new Dictionary<string, Animation>();

#if DEBUG
        public PrmAn() { }
#endif

        public PrmAn(BinaryReader file)
        {
            ParseData(file);
        }

        public PrmAn(byte[] data)
        {
            using (BinaryReader file = new BinaryReader(new MemoryStream(data)))
                ParseData(file);
        }

        private void ParseData(BinaryReader file)
        {
            if (Encoding.ASCII.GetString(file.ReadBytes(5)) != "PRMAN")
            {
                Console.WriteLine("Invalid primitive animation file");
                return;
            }

            int frameCount = file.ReadInt32();
            int textureCount = file.ReadInt32();
            int animCount = file.ReadInt32();

            for (int i = 0; i < frameCount; i++)
            {
                Frame frame = new Frame();
                frame.name = Encoding.ASCII.GetString(file.ReadBytes(file.ReadByte()));
                frame.layerCount = file.ReadInt32();
                frame.layers = new Layer[frame.layerCount];

                for (int j = 0; j < frame.layerCount; j++)
                {
                    Layer layer = new Layer();
                    byte indicator = file.ReadByte();
                    while(indicator != 0xFF)
                    {
                        switch(indicator)
                        {
                            default:
                                Console.WriteLine("Unknown indicator in prman " + Convert.ToString(indicator, 16));
                                break;
                            case 0x00:
                                layer.position = new Vector3(file.ReadSingle(), file.ReadSingle(), file.ReadSingle());
                                break;
                            case 0x01:
                                layer.rotation = new Vector3(file.ReadSingle(), file.ReadSingle(), file.ReadSingle());
                                break;
                            case 0x02:
                                layer.scale = new Vector3(file.ReadSingle(), file.ReadSingle(), file.ReadSingle());
                                break;
                            case 0x03:
                                layer.uv = new Vector4(file.ReadSingle(), file.ReadSingle(), file.ReadSingle(), file.ReadSingle());
                                break;
                            case 0x04:
                                layer.radius = file.ReadSingle();
                                break;
                            case 0x10:
                                layer.colMult = BitConverter.GetBytes(file.ReadInt32());
                                break;
                            case 0x11:
                                layer.colAdd = BitConverter.GetBytes(file.ReadInt32());
                                break;
                            case 0x12:
                                layer.palNum = file.ReadSByte();
                                break;
                            case 0x20:
                                layer.additive = true;
                                break;
                            case 0x21:
                                layer.drawIn2d = true;
                                break;
                            case 0x22:
                                layer.blendUv = false;
                                break;
                            case 0x23:
                                layer.allowBlending = false;
                                break;
                            case 0x24:
                                layer.blendTransform = false;
                                break;
                            case 0x30:
                                layer.primitiveType = (PrimitiveType)file.ReadByte();
                                break;
                            case 0x31:
                                layer.texId = file.ReadInt32();
                                break;
                        }
                        indicator = file.ReadByte();
                    }
                    frame.layers[j] = layer;
                }
                frames.Add(frame.name, frame);
            }

            for(int i = 0; i < textureCount; i++)
            {
                Texture texture = new Texture();
                texture.name = Encoding.ASCII.GetString(file.ReadBytes(file.ReadByte()));
                texture.id = file.ReadInt32();
                texture.width = file.ReadInt32();
                texture.height = file.ReadInt32();
                texture.texDatSize = file.ReadInt32();
                texture.texDat = file.ReadBytes(texture.texDatSize);
                textures.Add(texture.id, texture);
            }

            for(int i = 0; i < animCount; i++)
            {
                Animation anim = new Animation();
                anim.name = Encoding.ASCII.GetString(file.ReadBytes(file.ReadByte()));
                anim.frameCount = file.ReadInt32();
                anim.frames = new string[anim.frameCount];
                anim.frameTimes = new int[anim.frameCount];
                for(int j = 0; j < anim.frameCount; j++)
                {
                    anim.frames[j] = Encoding.ASCII.GetString(file.ReadBytes(file.ReadByte()));
                    anim.frameTimes[j] = file.ReadInt32();
                }
                animations.Add(anim.name, anim);
            }
        }

#if DEBUG
        public void SaveToFile(string fileName)
        {
            List<byte> file = new List<byte>();
            file.AddRange(Encoding.ASCII.GetBytes("PRMAN"));
            file.AddRange(BitConverter.GetBytes(frames.Count));
            file.AddRange(BitConverter.GetBytes(textures.Count));
            file.AddRange(BitConverter.GetBytes(animations.Count));

            foreach (Frame frame in frames.Values)
            {
                file.Add(BitConverter.GetBytes(frame.name.Length)[0]);
                file.AddRange(Encoding.ASCII.GetBytes(frame.name));
                file.AddRange(BitConverter.GetBytes(frame.layerCount));

                for(int j = 0; j < frame.layerCount; j++)
                {
                    Layer layer = frame.layers[j];
                    if(layer.position != Vector3.Zero)
                    {
                        file.Add(0x00);
                        file.AddRange(BitConverter.GetBytes(layer.position.X));
                        file.AddRange(BitConverter.GetBytes(layer.position.Y));
                        file.AddRange(BitConverter.GetBytes(layer.position.Z));
                    }
                    if(layer.rotation != Vector3.Zero)
                    {
                        file.Add(0x01);
                        file.AddRange(BitConverter.GetBytes(layer.rotation.X));
                        file.AddRange(BitConverter.GetBytes(layer.rotation.Y));
                        file.AddRange(BitConverter.GetBytes(layer.rotation.Z));
                    }
                    if(layer.scale != Vector3.One)
                    {
                        file.Add(0x02);
                        file.AddRange(BitConverter.GetBytes(layer.scale.X));
                        file.AddRange(BitConverter.GetBytes(layer.scale.Y));
                        file.AddRange(BitConverter.GetBytes(layer.scale.Z));
                    }
                    if(layer.uv != Vector4.Zero && layer.primitiveType == PrimitiveType.Plane)
                    {
                        file.Add(0x03);
                        file.AddRange(BitConverter.GetBytes(layer.uv.X));
                        file.AddRange(BitConverter.GetBytes(layer.uv.Y));
                        file.AddRange(BitConverter.GetBytes(layer.uv.Z));
                        file.AddRange(BitConverter.GetBytes(layer.uv.W));
                    }
                    if(layer.radius != 0.0f && layer.primitiveType == PrimitiveType.Sphere)
                    {
                        file.Add(0x04);
                        file.AddRange(BitConverter.GetBytes(layer.radius));
                    }
                    if (!layer.colMult.SequenceEqual(new byte[4] { 255, 255, 255, 255 }))
                    {
                        file.Add(0x10);
                        file.AddRange(layer.colMult);
                    }
                    if(!layer.colAdd.SequenceEqual(new byte[4] { 0, 0, 0, 0 }))
                    {
                        file.Add(0x11);
                        file.AddRange(layer.colAdd);
                    }
                    if (layer.palNum >= 0)
                    {
                        file.Add(0x12);
                        file.Add((byte)layer.palNum);
                    }
                    if (layer.additive)
                        file.Add(0x20);
                    if (layer.drawIn2d)
                        file.Add(0x21);
                    if (!layer.blendTransform)
                        file.Add(0x24);
                    if (!layer.blendUv)
                        file.Add(0x22);
                    if (!layer.allowBlending)
                        file.Add(0x23);
                    file.Add(0x30);
                    file.Add((byte)layer.primitiveType);
                    file.Add(0x31);
                    file.AddRange(BitConverter.GetBytes(layer.texId));
                    file.Add(0xFF);
                }
            }

            foreach(Texture tex in textures.Values)
            {
                file.Add(BitConverter.GetBytes(tex.name.Length)[0]);
                file.AddRange(Encoding.ASCII.GetBytes(tex.name));
                file.AddRange(BitConverter.GetBytes(tex.id));
                file.AddRange(BitConverter.GetBytes(tex.width));
                file.AddRange(BitConverter.GetBytes(tex.height));
                file.AddRange(BitConverter.GetBytes(tex.texDatSize));
                file.AddRange(tex.texDat);
            }

            foreach(Animation anim in animations.Values)
            {
                file.Add(BitConverter.GetBytes(anim.name.Length)[0]);
                file.AddRange(Encoding.ASCII.GetBytes(anim.name));
                file.AddRange(BitConverter.GetBytes(anim.frameCount));
                for(int i = 0; i < anim.frameCount; i++)
                {
                    file.Add(BitConverter.GetBytes(anim.frames[i].Length)[0]);
                    file.AddRange(Encoding.ASCII.GetBytes(anim.frames[i]));
                    file.AddRange(BitConverter.GetBytes(anim.frameTimes[i]));
                }
            }

            BinaryWriter writer = new BinaryWriter(File.OpenWrite(fileName));
            writer.Write(file.ToArray());
            writer.Close();
        }
#endif

        public void TexOnGPU(int texId, uint glId)
        {
            if (textures.ContainsKey(texId))
            {
                textures[texId].texDat = null;
                GC.Collect();
                textures[texId].glTexId = glId;
            }
        }

        public Texture GetTexture(string name)
        {
            foreach (Texture tex in textures.Values)
                if (tex.name == name)
                    return tex;
            return null;
        }

        public Texture GetTexture(int id)
        {
            return textures.ContainsKey(id) ? textures[id] : null;
        }

        public Frame GetFrame(string frame)
        {
            return frames.ContainsKey(frame) ? frames[frame] : new Frame();
        }

        public Animation GetAnim(string anim)
        {
            return animations.ContainsKey(anim) ? animations[anim] : null;
        }

        public Frame BlendFrames(string frameA, string frameB, float time)
        {
            Frame a, b, rtrn = new Frame();
            if (frames.ContainsKey(frameA))
                a = frames[frameA];
            else
                return rtrn;
            if (frames.ContainsKey(frameB))
                b = frames[frameB];
            else
                return a;

            if (frameA == frameB)
                return a;

            if (time <= 0.0f)
                return a;
            else if (time >= 1.0f)
                return b;

            rtrn.layerCount = a.layerCount;
            rtrn.layers = new Layer[rtrn.layerCount];
            rtrn.name = "Blend";

            int layer = 0;
            while (layer < a.layers.Length && layer < b.layers.Length)
            {
                Layer layerA = a.layers[layer];
                Layer layerB = b.layers[layer];
                Layer blend = layerA.Blend(layerB, time);
                rtrn.layers[layer] = blend;
                layer++;
            }
            return rtrn;
        }

        public void Combine(PrmAn other)
        {
            Dictionary<int, int> texIdUpdates = new Dictionary<int, int>();
            foreach (KeyValuePair<int, Texture> tex in other.textures)
                if(!textures.TryAdd(tex.Key, tex.Value))
                {
                    //you ready for the most stupid ass fucking thing ever to ensure unique keys
                    int oldId = tex.Value.id;
                    tex.Value.id = DateTime.Now.GetHashCode();
                    textures.TryAdd(tex.Value.id, tex.Value);
                    texIdUpdates.Add(oldId, tex.Value.id);
                }
            foreach (KeyValuePair<string, Frame> frame in other.frames)
            {
                for (int i = 0; i < frame.Value.layerCount; i++)
                    if (texIdUpdates.ContainsKey(frame.Value.layers[i].texId))
                        frame.Value.layers[i].texId = texIdUpdates[frame.Value.layers[i].texId];
                frames.TryAdd(frame.Key, frame.Value);
            }
            foreach (KeyValuePair<string, Animation> anim in other.animations)
                animations.TryAdd(anim.Key, anim.Value);
        }

        public void Dispose()
        {
            frames.Clear();
            textures.Clear();
            animations.Clear();
            GC.SuppressFinalize(this);
        }

        public struct Frame
        {
            public string name = "";
            public int layerCount = 0;
            public Layer[] layers = new Layer[0];

            public Frame() { }

            public Frame Copy()
            {
                Frame rtrn = new Frame();
                rtrn.name = name + "_copy";
                rtrn.layerCount = layerCount;
                rtrn.layers = new Layer[layerCount];
                int i = 0;
                foreach(Layer layer in layers)
                {
                    rtrn.layers[i] = layer;
                    rtrn.layers[i].colMult = new byte[4];
                    Array.Copy(layer.colMult, rtrn.layers[i].colMult, 4);
                    rtrn.layers[i].colAdd = new byte[4];
                    Array.Copy(layer.colAdd, rtrn.layers[i].colAdd, 4);
                    i++;
                }
                return rtrn;
            }
        }

        public struct Layer
        {
            public Vector3 position = Vector3.Zero;
            public Vector3 rotation = Vector3.Zero;
            public Vector3 scale = Vector3.One;
            public Vector4 uv = Vector4.Zero; //XYWH
            public float radius = 0.0f;
            public byte[] colMult = new byte[] { 255, 255, 255, 255 };
            public byte[] colAdd = new byte[] { 0, 0, 0, 0 };
            public sbyte palNum = -1;
            public bool additive = false;
            public bool drawIn2d = false;
            public bool blendUv = true;
            public bool blendTransform = true;
            public bool allowBlending = true;
            public PrimitiveType primitiveType = PrimitiveType.Plane;
            public int texId = 0;

            public Layer() { }

            public Layer Blend(Layer layerB, float time)
            {
                if (!allowBlending)
                    return this;

                if (time <= 0.0f)
                    return this;
                else if (time >= 1.0f)
                    return layerB;
                Layer rtrn = new Layer();

                if (blendTransform)
                {
                    rtrn.position = this.position + (layerB.position - this.position) * time;
                    rtrn.rotation = this.rotation + (layerB.rotation - this.rotation) * time;
                    rtrn.scale = this.scale + (layerB.scale - this.scale) * time;
                }
                else
                {
                    rtrn.position = this.position;
                    rtrn.rotation = this.rotation;
                    rtrn.scale = this.scale;
                }
                if (blendUv)
                    rtrn.uv = this.uv + (layerB.uv - this.uv) * time;
                else
                    rtrn.uv = this.uv;
                rtrn.radius = this.radius + (layerB.radius - this.radius) * time;
                rtrn.colMult[0] = LerpByte(this.colMult[0], layerB.colMult[0], time);
                rtrn.colMult[1] = LerpByte(this.colMult[1], layerB.colMult[1], time);
                rtrn.colMult[2] = LerpByte(this.colMult[2], layerB.colMult[2], time);
                rtrn.colMult[3] = LerpByte(this.colMult[3], layerB.colMult[3], time);
                rtrn.colAdd[0] = LerpByte(this.colAdd[0], layerB.colAdd[0], time);
                rtrn.colAdd[1] = LerpByte(this.colAdd[1], layerB.colAdd[1], time);
                rtrn.colAdd[2] = LerpByte(this.colAdd[2], layerB.colAdd[2], time);
                rtrn.colAdd[3] = LerpByte(this.colAdd[3], layerB.colAdd[3], time);
                rtrn.palNum = palNum;
                rtrn.additive = additive;
                rtrn.drawIn2d = drawIn2d;
                rtrn.primitiveType = primitiveType;
                rtrn.texId = texId;

                return rtrn;
            }

            byte LerpByte(byte byteA, byte byteB, float time)
            {
                int a = byteA;
                int b = byteB;
                return (byte)(a + (b - a) * time);
            }
        }

        public enum PrimitiveType : byte
        {
            Plane,
            Sphere,
            Cube,
        }

        public class Texture : IDisposable
        {
            public uint glTexId;
            public string name;
            public int id;
            public int width;
            public int height;
            public int texDatSize;
            public byte[] texDat;
            public unsafe void* texDatPtr { get { if (texDat.Length > 0) fixed (void* rtrn = &texDat[0]) return rtrn; return null; } }

            public void Dispose()
            {
                texDat = null;
                GC.Collect();
            }
        }

        public class Animation
        {
            public string name = "";
            public int frameCount = 0;
            public string[] frames = new string[0];
            public int[] frameTimes = new int[0];
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Linq;

namespace BlakieLibSharp
{
    public class SprAn
    {
        Dictionary<string, SprAnState> states = new Dictionary<string, SprAnState>();

#if DEBUG
        public SprAn()
        {

        }
#endif

        public SprAn(byte[] data)
        {
            BinaryReader file = new BinaryReader(new MemoryStream(data));
            LoadData(file);
            file.Close();
        }

        public SprAn(BinaryReader file)
        {
            LoadData(file);
        }

        //Will Return null if state doesnt exist. Ensure cases to prevent reading null data are there
        public SprAnState GetState(string name)
        {
            if (states.ContainsKey(name))
                return states[name];
            return null;
        }

        //Will Return null if state doesnt exist. Ensure cases to prevent reading null data are there
        public SprAnFrame GetFrame(string state, int frame)
        {
            if (states.ContainsKey(state))
                return states[state].frames[frame];
            return null;
        }

        public bool HasState(string name)
        {
            return states.ContainsKey(name);
        }

        public string[] GetAllStateNames()
        {
            return states.Keys.ToArray();
        }

        void LoadData(BinaryReader file)
        {
            if (Encoding.ASCII.GetString(file.ReadBytes(5)) != "SPRAN")
            {
                Console.WriteLine("Invalid SprAn file");
                file.Close();
                return;
            }

            int stateCount = file.ReadInt32();
            for (int i = 0; i < stateCount; i++)
            {
                SprAnState state = new SprAnState();
                state.name = Encoding.ASCII.GetString(file.ReadBytes(file.ReadByte()));
                state.frameCount = file.ReadByte();
                state.frames = new SprAnFrame[state.frameCount];
                for (int j = 0; j < state.frameCount; j++)
                {
                    SprAnFrame frame = new SprAnFrame();
                    frame.uvCount = file.ReadByte();
                    frame.uvs = new FrameUv[frame.uvCount];
                    for (int k = 0; k < frame.uvCount; k++)
                    {
                        FrameUv uv = new FrameUv();
                        uv.textureName = Encoding.ASCII.GetString(file.ReadBytes(file.ReadByte()));
                        uv.position.X = file.ReadSingle(); uv.position.Y = file.ReadSingle();
                        uv.rotation.X = file.ReadSingle(); uv.rotation.Y = file.ReadSingle(); uv.rotation.Z = file.ReadSingle();
                        uv.scale.X = file.ReadSingle(); uv.scale.Y = file.ReadSingle();
                        uv.uv = new Vector4(file.ReadSingle(), file.ReadSingle(), file.ReadSingle(), file.ReadSingle());
                        frame.uvs[k] = uv;
                    }

                    frame.frameLength = file.ReadUInt16();
                    frame.colliderCount = file.ReadByte();
                    frame.colliders = new RectCollider[frame.colliderCount];

                    for (int k = 0; k < frame.colliderCount; k++)
                    {
                        RectCollider col = new RectCollider();
                        col.colliderType = (RectCollider.ColliderType)file.ReadInt32();
                        col.x = file.ReadSingle();
                        col.y = file.ReadSingle();
                        col.width = file.ReadSingle();
                        col.height = file.ReadSingle();
                        frame.colliders[k] = col;
                    }

                    state.frames[j] = frame;
                }

                states.Add(state.name, state);
            }
        }

#if DEBUG
        public void Save(string filePath)
        {
            List<byte> data = new List<byte>();
            data.AddRange(Encoding.ASCII.GetBytes("SPRAN"));
            data.AddRange(BitConverter.GetBytes(states.Count));
            foreach(SprAnState state in states.Values)
            {
                data.Add(BitConverter.GetBytes(state.name.Length)[0]);
                data.AddRange(Encoding.ASCII.GetBytes(state.name));
                data.Add(state.frameCount);
                foreach(SprAnFrame frame in state.frames)
                {
                    data.Add(frame.uvCount);
                    foreach(FrameUv uv in frame.uvs)
                    {
                        data.Add(BitConverter.GetBytes(uv.textureName.Length)[0]);
                        data.AddRange(Encoding.ASCII.GetBytes(uv.textureName));
                        data.AddRange(BitConverter.GetBytes(uv.position.X));
                        data.AddRange(BitConverter.GetBytes(uv.position.Y));
                        data.AddRange(BitConverter.GetBytes(uv.rotation.X));
                        data.AddRange(BitConverter.GetBytes(uv.rotation.Y));
                        data.AddRange(BitConverter.GetBytes(uv.rotation.Z));
                        data.AddRange(BitConverter.GetBytes(uv.scale.X));
                        data.AddRange(BitConverter.GetBytes(uv.scale.Y));
                        data.AddRange(BitConverter.GetBytes(uv.uv.X));
                        data.AddRange(BitConverter.GetBytes(uv.uv.Y));
                        data.AddRange(BitConverter.GetBytes(uv.uv.Z));
                        data.AddRange(BitConverter.GetBytes(uv.uv.W));
                    }
                    data.AddRange(BitConverter.GetBytes(frame.frameLength));
                    data.Add(frame.colliderCount);
                    foreach(RectCollider collider in frame.colliders)
                    {
                        data.AddRange(BitConverter.GetBytes((int)collider.colliderType));
                        data.AddRange(BitConverter.GetBytes(collider.x));
                        data.AddRange(BitConverter.GetBytes(collider.y));
                        data.AddRange(BitConverter.GetBytes(collider.width));
                        data.AddRange(BitConverter.GetBytes(collider.height));
                    }
                }
            }
            BinaryWriter file = new BinaryWriter(File.OpenWrite(filePath));
            file.Write(data.ToArray());
            file.Close();
        }

        public void ReKeyState(string oldName, string newName)
        {
            SprAnState state = states[oldName];
            states.Remove(oldName);
            states.Add(newName, state);
        }

        public void AddState(SprAnState state)
        {
            states.TryAdd(state.name, state);
        }

        public void RemoveState(string state)
        {
            states.Remove(state);
        }
#endif

        public class SprAnState
        {
            public string name = "";
            public byte frameCount = 0;
            public SprAnFrame[] frames = new SprAnFrame[0];

            public SprAnState Copy()
            {
                SprAnState rtrn = new SprAnState();
                rtrn.name = name + "_Copy";
                rtrn.frameCount = frameCount;
                rtrn.frames = new SprAnFrame[frames.Length];
                for (int i = 0; i < frames.Length; i++)
                    rtrn.frames[i] = frames[i].Copy();
                return rtrn;
            }
        }

        public class SprAnFrame
        {
            public byte uvCount = 0;
            public FrameUv[] uvs = new FrameUv[0];
            public ushort frameLength = 0;
            public byte colliderCount = 0;
            public RectCollider[] colliders = new RectCollider[0];

            public SprAnFrame BlendFrames(SprAnFrame frameB, float time)
            {
                SprAnFrame rtrn = new SprAnFrame();
                rtrn.uvCount = uvCount;
                rtrn.uvs = new FrameUv[uvCount];
                rtrn.frameLength = frameLength;
                rtrn.colliderCount = colliderCount;
                rtrn.colliders = colliders;
                for(int i = 0; i < uvCount && i < frameB.uvCount; i++)
                {
                    FrameUv uv = new FrameUv();
                    uv.textureName = uvs[i].textureName;
                    uv.position = uvs[i].position + (frameB.uvs[i].position - uvs[i].position) * time;
                    uv.rotation = uvs[i].rotation + (frameB.uvs[i].rotation - uvs[i].rotation) * time;
                    uv.scale = uvs[i].scale + (frameB.uvs[i].scale - uvs[i].scale) * time;
                    rtrn.uvs[i] = uv;
                }
                return rtrn;
            }

            public SprAnFrame Copy()
            {
                SprAnFrame rtrn = new SprAnFrame();
                rtrn.uvCount = uvCount;
                rtrn.uvs = new FrameUv[uvCount];
                Array.Copy(uvs, rtrn.uvs, uvCount);
                rtrn.frameLength = frameLength;
                rtrn.colliderCount = colliderCount;
                rtrn.colliders = new RectCollider[colliderCount];
                Array.Copy(colliders, rtrn.colliders, colliderCount);
                return rtrn;
            }
        }

        public struct FrameUv
        {
            public string textureName = "";
            public Vector2 position = Vector2.Zero;
            public Vector3 rotation = Vector3.Zero;
            public Vector2 scale = Vector2.One;
            public Vector4 uv = Vector4.Zero;

            public FrameUv() { }
        }
    }
}

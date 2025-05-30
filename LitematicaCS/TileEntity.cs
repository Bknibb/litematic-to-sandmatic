using SharpNBT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace litematic_to_sandmatic.LitematicaCS
{
    internal class TileEntity
    {
        private CompoundTag data;
        private Tuple<int, int, int> position;
        public CompoundTag Data
        {
            get { return data; }
            set
            {
                TileEntity tileEntity = new TileEntity(value);
                position = tileEntity.position;
            }
        }
        public Tuple<int, int, int> Position
        {
            get { return position; }
            set
            {
                position = value;
                data["x"] = new IntTag("x", position.Item1);
                data["y"] = new IntTag("y", position.Item2);
                data["z"] = new IntTag("z", position.Item3);
            }
        }
        public TileEntity(CompoundTag nbt)
        {
            data = nbt;
            ICollection<string> keys = data.Keys;
            if (!keys.Contains("x"))
            {
                data["x"] = new IntTag("x", 0);
            }
            if (!keys.Contains("y"))
            {
                data["y"] = new IntTag("y", 0);
            }
            if (!keys.Contains("z"))
            {
                data["z"] = new IntTag("z", 0);
            }
            position = new Tuple<int, int, int>(data.Get<IntTag>("x").Value, data.Get<IntTag>("y").Value, data.Get<IntTag>("z").Value);
        }
        public CompoundTag toNbt()
        {
            return data;
        }
        public static TileEntity fromNbt(CompoundTag nbt)
        {
            return new TileEntity(nbt);
        }
        public void addTag(string key, Tag tag)
        {
            if (key == "x")
            {
                if (tag is IntTag intTag)
                {
                    this.position = new Tuple<int, int, int>(intTag.Value, position.Item2, position.Item3);
                }
                else
                {
                    throw new ArgumentException("Tried to add \"x\" tag as not a IntTag");
                }
            }
            if (key == "y")
            {
                if (tag is IntTag intTag)
                {
                    this.position = new Tuple<int, int, int>(position.Item1, intTag.Value, position.Item3);
                }
                else
                {
                    throw new ArgumentException("Tried to add \"y\" tag as not a IntTag");
                }
            }
            if (key == "z")
            {
                if (tag is IntTag intTag)
                {
                    this.position = new Tuple<int, int, int>(position.Item1, position.Item2, intTag.Value);
                }
                else
                {
                    throw new ArgumentException("Tried to add \"z\" tag as not a IntTag");
                }
            }
            data[key] = tag;
        }
        public Tag getTag(string key)
        {
            return data[key];
        }
    }
}

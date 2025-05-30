using SharpNBT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace litematic_to_sandmatic.LitematicaCS
{
    internal class Entity
    {
        private string id;
        private CompoundTag data;
        private Tuple<float, float, float> position;
        private Tuple<float, float> rotation;
        private Tuple<float, float, float> motion;
        public CompoundTag Data { get { return data; }
            set
            {
                Entity entity = new Entity(value);
                id = entity.id;
                position = entity.position;
                rotation = entity.rotation;
                motion = entity.motion;
            }
        }
        public string Id { get { return id; }
            set
            {
                id = value;
                data["id"] = new StringTag("id", id);
            }
        }
        public Tuple<float, float, float> Position
        {
            get { return position; }
            set
            {
                position = value;
                data["Pos"] = new ListTag("Pos", TagType.Double, [new DoubleTag(null, position.Item1), new DoubleTag(null, position.Item2), new DoubleTag(null, position.Item3)]);
            }
        }
        public Tuple<float, float> Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
                data["Rotation"] = new ListTag("Rotation", TagType.Double, [new DoubleTag(null, rotation.Item1), new DoubleTag(null, rotation.Item2)]);
            }
        }
        public Tuple<float, float, float> Motion
        {
            get { return motion; }
            set
            {
                motion = value;
                data["Motion"] = new ListTag("Motion", TagType.Double, [new DoubleTag(null, motion.Item1), new DoubleTag(null, motion.Item2), new DoubleTag(null, motion.Item3)]);
            }
        }
        public Entity(string str)
        {
            data = new TagBuilder().AddString("id", str).Create();
            ICollection<string> keys = data.Keys;
            if (!keys.Contains("id"))
            {
                throw new RequiredKeyMissionException("id");
            }
            if (!keys.Contains("Pos"))
            {
                data["Pos"] = new ListTag("Pos", TagType.Double, [new DoubleTag(null, 0), new DoubleTag(null, 0), new DoubleTag(null, 0)]);
            }
            if (!keys.Contains("Rotation"))
            {
                data["Rotation"] = new ListTag("Rotation", TagType.Double, [new DoubleTag(null, 0), new DoubleTag(null, 0)]);
            }
            if (!keys.Contains("Motion"))
            {
                data["Motion"] = new ListTag("Motion", TagType.Double, [new DoubleTag(null, 0), new DoubleTag(null, 0), new DoubleTag(null, 0)]);
            }
            id = MinecraftUtils.assertValidIdentifier(data.Get<StringTag>("id").Value);
            List<float> position = data.Get<ListTag>("Pos").Select(tag => (float)((DoubleTag)tag).Value).ToList();
            this.position = new Tuple<float, float, float>(position[0], position[1], position[2]);
            List<float> rotation = data.Get<ListTag>("Rotation").Select(tag => (float)((DoubleTag)tag).Value).ToList();
            this.rotation = new Tuple<float, float>(rotation[0], rotation[1]);
            List<float> motion = data.Get<ListTag>("Motion").Select(tag => (float)((DoubleTag)tag).Value).ToList();
            this.motion = new Tuple<float, float, float>(motion[0], motion[1], motion[2]);
        }
        public Entity(CompoundTag nbt)
        {
            data = nbt;
            ICollection<string> keys = data.Keys;
            if (!keys.Contains("id"))
            {
                throw new RequiredKeyMissionException("id");
            }
            if (!keys.Contains("Pos"))
            {
                data["Pos"] = new ListTag("Pos", TagType.Double, [new DoubleTag(null, 0), new DoubleTag(null, 0), new DoubleTag(null, 0)]);
            }
            if (!keys.Contains("Rotation"))
            {
                data["Rotation"] = new ListTag("Rotation", TagType.Double, [new DoubleTag(null, 0), new DoubleTag(null, 0)]);
            }
            if (!keys.Contains("Motion"))
            {
                data["Motion"] = new ListTag("Motion", TagType.Double, [new DoubleTag(null, 0), new DoubleTag(null, 0), new DoubleTag(null, 0)]);
            }
            id = MinecraftUtils.assertValidIdentifier(data.Get<StringTag>("id").Value);
            List<float> position = data.Get<ListTag>("Pos").Select(tag => (float)((DoubleTag)tag).Value).ToList();
            this.position = new Tuple<float, float, float>(position[0], position[1], position[2]);
            List<float> rotation = data.Get<ListTag>("Rotation").Select(tag => (float)((DoubleTag)tag).Value).ToList();
            this.rotation = new Tuple<float, float>(rotation[0], rotation[1]);
            List<float> motion = data.Get<ListTag>("Motion").Select(tag => (float)((DoubleTag)tag).Value).ToList();
            this.motion = new Tuple<float, float, float>(motion[0], motion[1], motion[2]);
        }
        public CompoundTag toNbt()
        {
            return data;
        }
        public static Entity fromNbt(CompoundTag nbt)
        {
            return new Entity(nbt);
        }
        public void addTag(string key, Tag tag)
        {
            if (key == "id")
            {
                if (tag is StringTag stringTag)
                {
                    id = ((StringTag)tag).Value;
                }
                else
                {
                    throw new ArgumentException("Tried to add \"id\" tag as not a StringTag");
                }
            }
            if (key == "Pos")
            {
                if (tag is ListTag listTag)
                {
                    List<float> position = listTag.Select(tag => (float)((DoubleTag)tag).Value).ToList();
                    if (position.Count < 3)
                    {
                        throw new ArgumentException("Tried to add \"Pos\" tag but the ListTag does not contain enough values");
                    }
                    this.position = new Tuple<float, float, float>(position[0], position[1], position[2]);
                } else
                {
                    throw new ArgumentException("Tried to add \"Pos\" tag as not a ListTag");
                }
            }
            if (key == "Rotation")
            {
                if (tag is ListTag listTag)
                {
                    List<float> rotation = listTag.Select(tag => (float)((DoubleTag)tag).Value).ToList();
                    if (rotation.Count < 2)
                    {
                        throw new ArgumentException("Tried to add \"Rotation\" tag but the ListTag does not contain enough values");
                    }
                    this.rotation = new Tuple<float, float>(rotation[0], rotation[1]);
                }
                else
                {
                    throw new ArgumentException("Tried to add \"Rotation\" tag as not a ListTag");
                }
            }
            if (key == "Motion")
            {
                if (tag is ListTag listTag)
                {
                    List<float> motion = listTag.Select(tag => (float)((DoubleTag)tag).Value).ToList();
                    if (motion.Count < 3)
                    {
                        throw new ArgumentException("Tried to add \"Motion\" tag but the ListTag does not contain enough values");
                    }
                    this.motion = new Tuple<float, float, float>(motion[0], motion[1], motion[2]);
                }
                else
                {
                    throw new ArgumentException("Tried to add \"Motion\" tag as not a ListTag");
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

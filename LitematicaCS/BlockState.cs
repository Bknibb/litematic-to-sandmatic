using Newtonsoft.Json;
using SharpNBT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace litematic_to_sandmatic.LitematicaCS
{
    internal class BlockState
    {
        private string blockId;
        private DiscriminatingDictionary<string, string> properties;
        private string? identifierCache;
        public string id => blockId;
        public BlockState(string blockId, IDictionary<string, string>? initialData = null)
        {
            this.blockId = MinecraftUtils.assertValidIdentifier(blockId);
            properties = new DiscriminatingDictionary<string, string>(initialData);
            identifierCache = null;
        }
        public CompoundTag toNbt()
        {
            TagBuilder root = new TagBuilder();
            root.AddString("Name", id);
            Dictionary<string, string> props = properties.Select(kvp => KeyValuePair.Create((string)kvp.Key, (string)kvp.Value)).ToDictionary();
            if (properties.Count > 0)
            {
                root.BeginCompound("Properties");
                foreach (KeyValuePair<string, string> kvp in props)
                {
                    root.AddString(kvp.Key, kvp.Value);
                }
                root.EndCompound();
            }
            return root.Create();
        }
        public static BlockState fromNbt(CompoundTag nbt)
        {
            string blockId = MinecraftUtils.assertValidIdentifier(nbt.Get<StringTag>("Name").Value);
            Dictionary<string, string> properties = new Dictionary<string, string>();
            if (nbt.ContainsKey("Properties"))
            {
                properties = new Dictionary<string, string>(nbt.Get<CompoundTag>("Properties").Values.Select(tag => new KeyValuePair<string, string>(tag.Name, ((StringTag)tag).Value)));
            }
            BlockState block = new BlockState(blockId, properties);
            return block;
        }
        public BlockState withProperties(IDictionary<string, string?> props)
        { 
            List<string> noneProperties = props.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();
            BlockState other = new BlockState(blockId);
            other.properties.Update(properties);
            foreach (string propName in noneProperties)
            {
                other.properties.Remove(propName);
                properties.Remove(propName);
            }
            other.properties.Update(props);
            return other;
        }
        public string toBlockStateIdentifier(bool skipEmpty = true)
        {
            if (skipEmpty && identifierCache != null)
            {
                return identifierCache;
            }
            string identifier = blockId;
            if (!skipEmpty || properties.Count > 0)
            {
                string state = "[" + string.Join(",", properties
                                .OrderBy(kvp => kvp.Key)
                                .Select(kvp => $"{kvp.Key}={kvp.Value}")) + "]";
                identifier += state;
            }
            if (skipEmpty)
            {
                identifierCache = identifier;
            }
            return identifier;
        }
        public override bool Equals(object? obj)
        {
            if (obj is not BlockState other)
            {
                return false;
            }
            return blockId == other.blockId && properties.OrderBy(kv => kv.Key).SequenceEqual(other.properties.OrderBy(kv => kv.Key));
        }
        public override int GetHashCode()
        {
            return toBlockStateIdentifier().GetHashCode();
        }
        public override string ToString()
        {
            return toBlockStateIdentifier(skipEmpty: true);
        }
        public string? this[string key] => properties.ContainsKey(key) ? properties[key] : null;
        public int Length => properties.Count;
    }
}

using SharpNBT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static litematic_to_sandmatic.LitematicaCS.Statics;

namespace litematic_to_sandmatic.LitematicaCS
{
    internal class Region
    {
        private int x;
        private int y;
        private int z;
        private int width;
        private int height;
        private int length;
        private List<BlockState> palette;
        private uint[,,] blocks;
        private List<Entity> entities;
        private List<CompoundTag> blockTicks;
        private List<CompoundTag> fluidTicks;
        private List<TileEntity> tileEntities;
        public int X => x;
        public int Y => y;
        public int Z => z;
        public int Width => width;
        public int Height => height;
        public int Length => length;
        public List<Entity> Entities => entities;
        public List<TileEntity> TileEntities => tileEntities;
        public List<CompoundTag> BlockTicks => blockTicks;
        public List<CompoundTag> FluidTicks => fluidTicks;
        public List<BlockState> Palette { get
            {
                optimizePalette();
                return palette;
            } }
        public Region(int x, int y, int z, int width, int height, int length)
        {
            if (width == 0 || height == 0 || length == 0)
            {
                throw new ArgumentException("Region dimensions cannot be 0");
            }
            this.x = x;
            this.y = y;
            this.z = z;
            this.width = width;
            this.height = height;
            this.length = length;
            this.palette = [AIR,];
            this.blocks = new uint[Math.Abs(width), Math.Abs(height), Math.Abs(length)];
            this.entities = new List<Entity>();
            this.tileEntities = new List<TileEntity>();
            this.blockTicks = new List<CompoundTag>();
            this.fluidTicks = new List<CompoundTag>();
        }
        public CompoundTag toNbt(string name = null)
        {
            optimizePalette();
            TagBuilder builder = new TagBuilder(name);
            builder.NewCompound("Position");
            builder.AddInt("x", x);
            builder.AddInt("y", y);
            builder.AddInt("z", z);
            builder.EndCompound();
            builder.NewCompound("Size");
            builder.AddInt("x", width);
            builder.AddInt("y", height);
            builder.AddInt("z", length);
            builder.EndCompound();
            builder.NewList(TagType.Compound, "BlockStatePalette");
            foreach (BlockState state in palette)
            {
                builder.AddTag(state.toNbt());
            }
            builder.EndList();
            builder.NewList(TagType.Compound, "Entities");
            foreach (Entity entity in entities)
            {
                builder.AddTag(entity.toNbt());
            }
            builder.EndList();
            builder.NewList(TagType.Compound, "TileEntities");
            foreach (TileEntity tileEntity in tileEntities)
            {
                builder.AddTag(tileEntity.toNbt());
            }
            builder.EndList();
            builder.NewList(TagType.Compound, "PendingBlockTicks");
            foreach (CompoundTag compoundTag in blockTicks)
            {
                builder.AddTag(compoundTag);
            }
            builder.EndList();
            builder.NewList(TagType.Compound, "PendingFluidTicks");
            foreach (CompoundTag compoundTag in fluidTicks)
            {
                builder.AddTag(compoundTag);
            }
            builder.EndList();
            LitematicaBitArray arr = new LitematicaBitArray(volume(), getNeededNBits());
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < length; z++)
                    {
                        int ind = (y * Math.Abs(width * length)) + z * Math.Abs(width) + x;
                        arr[ind] = blocks[x, y, z];
                    }
                }
            }
            builder.AddLongArray("BlockStates", arr.toNbtLongArray());
            return builder.Create();
        }
        public CompoundTag toSpongeNbt(int mcVersion = MC_DATA_VERSION)
        {
            optimizePalette();
            TagBuilder builder = new TagBuilder();
            builder.AddInt("DataVersion", mcVersion);
            builder.AddInt("Version", SPONGE_VERSION);

            builder.AddShort("Width", Math.Abs(width));
            builder.AddShort("Height", Math.Abs(height));
            builder.AddShort("Length", Math.Abs(length));

            builder.AddIntArray("Offset", [0, 0, 0]);

            builder.BeginList(TagType.Compound, "Entities");
            foreach (Entity entity in entities)
            {
                builder.BeginCompound();
                foreach (Tag tag in entity.Data)
                {
                    if (tag.Name == "TileX" || tag.Name == "TileY" || tag.Name == "TileZ" || tag.Name == "id") continue;
                    builder.AddTag(tag);
                }
                builder.BeginList(TagType.Double, "Pos");
                builder.AddDouble(entity.Position.Item1 - (width > 0 ? 0 : width + 1));
                builder.AddDouble(entity.Position.Item2 - (height > 0 ? 0 : height + 1));
                builder.AddDouble(entity.Position.Item3 - (length > 0 ? 0 : length + 1));
                builder.EndList();
                if (entity.Data.ContainsKey("TileX"))
                {
                    builder.AddInt("TileX", entity.Data.Get<IntTag>("TileX").Value);
                    builder.AddInt("TileY", entity.Data.Get<IntTag>("TileY").Value);
                    builder.AddInt("TileZ", entity.Data.Get<IntTag>("TileZ").Value);
                }
                builder.AddString("Id", entity.Id);
                builder.EndCompound();
            }
            builder.EndList();

            builder.BeginList(TagType.Compound, "BlockEntities");
            foreach (TileEntity tileEntity in tileEntities)
            {
                builder.BeginCompound();
                foreach (Tag tag in tileEntity.Data)
                {
                    if (tag.Name == "x" || tag.Name == "y" || tag.Name == "z") continue;
                    builder.AddTag(tag);
                }
                builder.AddIntArray("Pos", [tileEntity.Position.Item1, tileEntity.Position.Item2, tileEntity.Position.Item3]);
                builder.EndCompound();
            }
            builder.EndList();

            builder.AddInt("PaletteMax", palette.Count);
            builder.NewCompound("Palette");
            for (int i = 0; i < palette.Count; i++)
            {
                string state = palette[i].toBlockStateIdentifier();
                builder.AddInt(state, i);
            }
            builder.EndCompound();

            List<byte> blockArray = new List<byte>();
            for (int i = 0; i < Math.Abs(width * height * length); i++)
            {
                int blocksPerLayer = Math.Abs(width * length);
                int y = i / blocksPerLayer;
                int iInLayer = i % blocksPerLayer;
                int z = iInLayer / Math.Abs(width);
                int x = iInLayer % width;
                blockArray.Add((byte)blocks[x, y, z]);
            }
            builder.AddByteArray("BlockData", blockArray);
            return builder.Create();
        }
        public static Tuple<Region, int> fromSpongeNbt(CompoundTag nbt)
        {
            int mcVersion = nbt.Get<IntTag>("DataVersion").Value;
            int width = nbt.Get<ShortTag>("Width").Value;
            int height = nbt.Get<ShortTag>("Height").Value;
            int length = nbt.Get<ShortTag>("Length").Value;
            Region region = new Region(0, 0, 0, width, height, length);
            int[] offset = nbt.Get<IntArrayTag>("Offset").ToArray();
            foreach (Tag tag in nbt.Get<ListTag>("Entities"))
            {
                if (tag is CompoundTag compoundTag)
                {
                    if (!compoundTag.ContainsKey("Id"))
                    {
                        throw new KeyNotFoundException("Id");
                    }
                    compoundTag["id"] = compoundTag["Id"];
                    compoundTag.Remove("Id");
                    Entity ent = new Entity(compoundTag);
                    ent.Position = new Tuple<float, float, float>(ent.Position.Item1 - offset[0], ent.Position.Item2 - offset[1], ent.Position.Item3 - offset[2]);
                    region.entities.Add(ent);
                }
            }
            foreach (Tag tag in nbt.Get<ListTag>("BlockEntities"))
            {
                if (tag is CompoundTag compoundTag)
                {
                    if (!compoundTag.ContainsKey("Id"))
                    {
                        throw new KeyNotFoundException("Id");
                    }
                    compoundTag["id"] = compoundTag["Id"];
                    compoundTag.Remove("Id");
                    TileEntity tent = TileEntity.fromNbt(compoundTag);
                    IntArrayTag pos = tent.Data.Get<IntArrayTag>("Pos");
                    tent.Position = new Tuple<int, int, int>(pos[0], pos[1], pos[2]);
                    tent.Data.Remove("Pos");
                    region.tileEntities.Add(tent);
                }
            }
            Dictionary<int, BlockState> paletteDict = new Dictionary<int, BlockState>();
            foreach (Tag tag in nbt.Get<CompoundTag>("Palette"))
            {
                if (tag is IntTag intTag)
                {
                    Dictionary<string, string> propertyDict = new Dictionary<string, string>();
                    string blockId = intTag.Name;
                    if (intTag.Name.IndexOf('[') > -1)
                    {
                        string[] entries = intTag.Name.Split('[');
                        blockId = entries[0];
                        string[] properties = entries[1].Replace("]", "").Split(',');
                        foreach (string property in properties)
                        {
                            string[] kv = property.Split('=');
                            string key = kv[0], value = kv[1];
                            propertyDict[key] = value;
                        }
                    }
                    BlockState blockState = new BlockState(blockId, propertyDict);
                    paletteDict[intTag.Value] = blockState;
                }
            }
            ByteArrayTag blockData = nbt.Get<ByteArrayTag>("BlockData");
            for (int i = 0; i < blockData.Count; i++)
            {
                int blocksPerLayer = width * height;
                int y = i / blocksPerLayer;
                int iInLayer = i & blocksPerLayer;
                int z = iInLayer / width;
                int x = iInLayer % width;
                region[x, y, z] = paletteDict[blockData[i]];
            }
            return new Tuple<Region, int>(region, mcVersion);
        }
        public CompoundTag toStructureNbt(int mcVersion = MC_DATA_VERSION)
        {
            optimizePalette();
            TagBuilder builder = new TagBuilder();
            builder.AddIntArray("size", [Math.Abs(width), Math.Abs(height), Math.Abs(length)]);
            builder.AddInt("DataVersion", mcVersion);

            builder.BeginList(TagType.Compound, "entities");
            foreach (Entity entity in entities)
            {
                builder.BeginCompound();
                builder.BeginCompound("nbt");
                foreach (Tag tag in entity.Data)
                {
                    builder.AddTag(tag);
                }
                builder.EndCompound();
                builder.BeginList(TagType.Double, "pos");
                builder.AddDouble(entity.Position.Item1 - (width > 0 ? 0 : width + 1));
                builder.AddDouble(entity.Position.Item2 - (height > 0 ? 0 : height + 1));
                builder.AddDouble(entity.Position.Item3 - (length > 0 ? 0 : length + 1));
                builder.EndList();
                builder.BeginList(TagType.Double, "blockPos");
                builder.AddInt((int)(entity.Position.Item1 - (width > 0 ? 0 : width + 1)));
                builder.AddInt((int)(entity.Position.Item2 - (height > 0 ? 0 : height + 1)));
                builder.AddInt((int)(entity.Position.Item3 - (length > 0 ? 0 : length + 1)));
                builder.EndList();
                builder.EndCompound();
            }
            builder.EndList();

            Dictionary<Tuple<int, int, int>, CompoundTag> tileEntityDict = new Dictionary<Tuple<int, int, int>, CompoundTag>();
            foreach (TileEntity tileEntity in tileEntities)
            {
                CompoundTag tileEntityTag = new CompoundTag("nbt");
                foreach (Tag tag in tileEntity.Data)
                {
                    if (tag.Name != "x" && tag.Name != "y" && tag.Name != "z")
                    {
                        tileEntityTag[tag.Name] = tag;
                    }
                }
                tileEntityDict[tileEntity.Position] = tileEntityTag;
            }

            builder.NewList(TagType.Compound, "palette");
            foreach (BlockState block in palette)
            {
                builder.AddTag(block.toNbt());
            }
            builder.EndList();

            builder.NewList(TagType.Compound, "blocks");
            for (int x = 0; x < blocks.GetLength(0); x++)
            {
                for (int y = 0; y < blocks.GetLength(1); y++)
                {
                    for (int z = 0; z < blocks.GetLength(2); z++)
                    {
                        TagBuilder block = new TagBuilder();
                        Tuple<int, int, int> position = new Tuple<int, int, int>(x, y, z);
                        if (tileEntityDict.ContainsKey(position))
                        {
                            block.AddTag(tileEntityDict[position]);
                        }
                        block.AddIntArray("pos", [x, y, z]);
                        block.AddInt("state", blocks[x, y, z]);
                        builder.AddTag(block.Create());
                    }
                }
            }
            builder.EndList();
            return builder.Create();
        }
        public static Tuple<Region, int> fromStructionNbt(CompoundTag structure)
        {
            int mcVersion = structure.Get<IntTag>("DataVersion").Value;
            IntArrayTag size = structure.Get<IntArrayTag>("size");
            int width = size[0];
            int height = size[1];
            int length = size[2];
            Region region = new Region(0, 0, 0, width, height, length);
            foreach (Tag tag in structure.Get<ListTag>("entities"))
            {
                if (tag is CompoundTag compoundTag)
                {
                    Entity ent = new Entity(compoundTag.Get<CompoundTag>("nbt"));
                    IntArrayTag pos = compoundTag.Get<IntArrayTag>("pos");
                    ent.Position = new Tuple<float, float, float>(pos[0], pos[1], pos[2]);
                    region.entities.Add(ent);
                }
            }
            ListTag palette = structure.Get<ListTag>("palette");
            foreach (Tag tag in structure.Get<CompoundTag>("blocks"))
            {
                if (tag is CompoundTag compoundTag)
                {
                    int[] pos = compoundTag.Get<IntArrayTag>("pos");
                    int x = pos[0], y = pos[1], z = pos[2];
                    int state = compoundTag.Get<IntTag>("state").Value;
                    region[x, y, z] = BlockState.fromNbt((CompoundTag)palette[state]);
                    if (compoundTag.ContainsKey("nbt"))
                    {
                        TileEntity tileEntity = new TileEntity(compoundTag.Get<CompoundTag>("nbt"));
                        tileEntity.Position = new Tuple<int, int, int>(x, y, z);
                        region.tileEntities.Add(tileEntity);
                    }
                }
            }
            return new Tuple<Region, int>(region, mcVersion);
        }
        public BlockState this[int x, int y, int z]
        {
            get
            {
                var (ix, iy, iz) = regionCoordinatesToStoreCoordinates(x, y, z);
                int index = (int)blocks[ix, iy, iz];
                return palette[index];
            }
            set
            {
                var (ix, iy, iz) = regionCoordinatesToStoreCoordinates(x, y, z);
                int index = palette.IndexOf(value);
                if (index == -1)
                {
                    palette.Add(value);
                    index = palette.Count - 1;
                }
                blocks[ix, iy, iz] = (uint)index;
            }
        }
        public bool Contains(BlockState block)
        {
            int index = palette.IndexOf(block);
            if (index == -1) return false;

            foreach (int val in blocks)
            {
                if (val == index)
                    return true;
            }
            return false;
        }
        public int countBlocks()
        {
            int count = 0;
            for (int x = 0; x < blocks.GetLength(0); x++)
            {
                for (int y = 0; y < blocks.GetLength(1); y++)
                {
                    for (int z = 0; z < blocks.GetLength(2); z++)
                    {
                        if (blocks[x, y, z] != 0) // or != null or != default, depending on type
                            count++;
                    }
                }
            }
            return count;
        }
        private Tuple<int, int, int> regionCoordinatesToStoreCoordinates(int x, int y, int z)
        {
            if (width < 0) x -= width + 1;
            if (height < 0) y -= height + 1;
            if (length < 0) z -= length + 1;
            return new Tuple<int, int, int>(x, y, z);
        }
        public int volume()
        {
            return Math.Abs(width * height * length);
        }
        private int getNeededNBits()
        {
            return Math.Max((int)Math.Ceiling(Math.Log(palette.Count, 2)), 2);
        }
        public static Region fromNbt(CompoundTag nbt)
        {
            CompoundTag pos = nbt.Get<CompoundTag>("Position");
            int x = pos.Get<IntTag>("x").Value;
            int y = pos.Get<IntTag>("y").Value;
            int z = pos.Get<IntTag>("z").Value;
            CompoundTag size = nbt.Get<CompoundTag>("Size");
            int width = size.Get<IntTag>("x").Value;
            int height = size.Get<IntTag>("y").Value;
            int length = size.Get<IntTag>("z").Value;
            Region region = new Region(x, y, z, width, height, length);
            region.palette.RemoveAt(0);
            foreach (Tag tag in nbt.Get<ListTag>("BlockStatePalette"))
            {
                if (tag is CompoundTag compoundTag)
                {
                    BlockState block = BlockState.fromNbt(compoundTag);
                    region.palette.Add(block);
                }
            }
            foreach (Tag tag in nbt.Get<ListTag>("Entities"))
            {
                if (tag is CompoundTag compoundTag)
                {
                    Entity entity = Entity.fromNbt(compoundTag);
                    region.entities.Add(entity);
                }
            }
            foreach (Tag tag in nbt.Get<ListTag>("TileEntities"))
            {
                if (tag is CompoundTag compoundTag)
                {
                    TileEntity tileEntity = TileEntity.fromNbt(compoundTag);
                    region.tileEntities.Add(tileEntity);
                }
            }
            LongArrayTag blocks = nbt.Get<LongArrayTag>("BlockStates");
            int nbits = region.getNeededNBits();
            LitematicaBitArray bitArray = LitematicaBitArray.fromNbtLongArray(blocks, region.volume(), nbits);
            for (int ix = 0; ix < Math.Abs(width); ix++)
            {
                for (int iy = 0; iy < Math.Abs(height); iy++)
                {
                    for (int iz = 0; iz < Math.Abs(length); iz++)
                    {
                        int ind = (iy * Math.Abs(width * length)) + (iz * Math.Abs(width)) + ix;
                        region.blocks[ix, iy, iz] = (uint)bitArray[ind];
                    }
                }
            }
            foreach (Tag tag in nbt.Get<ListTag>("PendingBlockTicks"))
            {
                if (tag is CompoundTag compoundTag)
                {
                    region.blockTicks.Add(compoundTag);
                }
            }
            foreach (Tag tag in nbt.Get<ListTag>("PendingFluidTicks"))
            {
                if (tag is CompoundTag compoundTag)
                {
                    region.fluidTicks.Add(compoundTag);
                }
            }
            return region;
        }
        public int minSchemX()
        {
            return Math.Min(x, x + width + 1);
        }
        public int maxSchemX()
        {
            return Math.Max(x, x + width - 1);
        }
        public int minSchemY()
        {
            return Math.Min(y, y + height + 1);
        }
        public int maxSchemY()
        {
            return Math.Max(y, y + height - 1);
        }
        public int minSchemZ()
        {
            return Math.Min(z, z + length + 1);
        }
        public int maxSchemZ()
        {
            return Math.Max(z, z + length - 1);
        }
        public int minX()
        {
            return Math.Min(0, width + 1);
        }
        public int maxX()
        {
            return Math.Max(0, width - 1);
        }
        public int minY()
        {
            return Math.Min(0, height + 1);
        }
        public int maxY()
        {
            return Math.Max(0, height - 1);
        }
        public int minZ()
        {
            return Math.Min(0, length + 1);
        }
        public int maxZ()
        {
            return Math.Max(0, length - 1);
        }
        public IEnumerable<int> rangeX()
        {
            return Enumerable.Range(minX(), maxX() - minX() + 1);
        }
        public IEnumerable<int> rangeY()
        {
            return Enumerable.Range(minY(), maxY() - minY() + 1);
        }
        public IEnumerable<int> rangeZ()
        {
            return Enumerable.Range(minZ(), maxZ() - minZ() + 1);
        }
        public IEnumerable<Tuple<int, int, int>> blockPositions()
        {
            foreach (int x in rangeX())
            {
                foreach(int y in rangeY())
                {
                    foreach (int z in rangeZ())
                    {
                        yield return new Tuple<int, int, int>(x, y, z);
                    }
                }
            }
        }
        public Schematic asSchematic(string name = DEFAULT_NAME, string author = "", string description = "", int mcVersion = MC_DATA_VERSION)
        {
            Dictionary<string, Region> regions = new Dictionary<string, Region>();
            regions[name] = this;
            return new Schematic(name: name, author: author, description: description, regions: regions, mcVersion: mcVersion);
        }
        private void replacePaletteIndex(int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex) return;
            for (int x = 0; x < blocks.GetLength(0); x++)
            {
                for (int y = 0; y < blocks.GetLength(1); y++)
                {
                    for (int z = 0; z < blocks.GetLength(2); z++)
                    {
                        if (blocks[x, y, z] == oldIndex)
                        {
                            blocks[x, y, z] = (uint)newIndex;
                        }
                    }
                }
            }
        }
        private bool blocksContainsIndex(int index)
        {
            foreach (int val in blocks)
            {
                if (val == index)
                    return true;
            }
            return false;
        }
        private void optimizePalette()
        {
            List<BlockState> newPalette = new List<BlockState>();
            int count = 0;
            for (int oldIndex = 0; oldIndex < palette.Count; oldIndex++)
            {
                BlockState state = palette[oldIndex];
                if (oldIndex != 0 && !blocksContainsIndex(oldIndex))
                    continue;

                int newIndex = newPalette.IndexOf(state);
                if (newIndex == -1)
                {
                    newIndex = newPalette.Count;
                    newPalette.Add(state);
                    count++;
                }

                replacePaletteIndex(oldIndex, newIndex);
            }
            palette = newPalette;
        }
        public void filter(Func<BlockState, BlockState> function)
        {
            palette = palette.Select(function).ToList();

            if (!palette[0].Equals(AIR))
            {
                palette.Add(palette[0]);
                replacePaletteIndex(0, palette.Count - 1);
                palette[0] = AIR;
            }
        }
        public void replace(BlockState replace, BlockState replaceWith)
        {
            int index = palette.IndexOf(replace);
            if (index == -1) return;
            if (index == 0)
            {
                palette.Append(replaceWith);
                replacePaletteIndex(0, palette.Count - 1);
            } else
            {
                palette[index] = replaceWith;
            }
        }
    }
}

using SharpNBT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static litematic_to_sandmatic.LitematicaCS.Statics;

namespace litematic_to_sandmatic.LitematicaCS
{
    internal class Schematic
    {
        public string name;
        public string author;
        public string description;
        public Dictionary<string, Region> region;
        public int lmVersion;
        public int lmSubversion;
        public int mcVersion;
        public long created;
        public long modified;
        private DiscriminatingDictionary<string, Region> regions;
        private IntArrayTag preview;
        private int? xMin;
        private int? yMin;
        private int? zMin;
        private int? xMax;
        private int? yMax;
        private int? zMax;
        public DiscriminatingDictionary<string, Region> Regions => regions;
        public int width
        {
            get
            {
                if (xMin == null || xMax == null) return 0;
                return xMax.Value - xMin.Value + 1;
            }
        }
        public int height
        {
            get
            {
                if (yMin == null || yMax == null) return 0;
                return yMax.Value - yMin.Value + 1;
            }
        }
        public int length
        {
            get
            {
                if (zMin == null || zMax == null) return 0;
                return zMax.Value - zMin.Value + 1;
            }
        }
        public IntArrayTag Preview { get { return preview; } set { preview = value; } }
        public Schematic(string name = DEFAULT_NAME, string author = "", string description = "", Dictionary<string, Region>? regions = null, int lmVersion = LITEMATIC_VERSION, int lmSubversion = LITEMATIC_SUBVERSION, int mcVersion = MC_DATA_VERSION)
        {
            if (regions == null)
            {
                regions = new Dictionary<string, Region>();
            }
            this.author = author;
            this.description = description;
            this.name = name;
            this.created = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.modified = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.regions = new DiscriminatingDictionary<string, Region>(onAdd: onRegionAdd, onRemove: onRegionRemove);
            computeEnclosure();
            if (regions != null && regions.Count > 0) {
                this.regions.Update(regions);
            }
            this.mcVersion = mcVersion;
            this.lmVersion = lmVersion;
            this.lmSubversion = lmSubversion;
            this.preview = new IntArrayTag("PreviewImageData", []);
        }
        public void save(string filePath, bool updateMeta = true, bool saveSoft = true, CompressionType compression = CompressionType.GZip, FormatOptions format = FormatOptions.BigEndian)
        {
            if (updateMeta) updateMetadata();
            NbtFile.Write(filePath, toNbt(saveSoft: saveSoft), format, compression);
        }
        public CompoundTag toNbt(bool saveSoft = true)
        {
            if (regions.Count < 1)
            {
                throw new InvalidOperationException("Empty schematic does not have any regions");
            }
            TagBuilder tagBuilder = new TagBuilder();
            tagBuilder.AddInt("Version", lmVersion);
            tagBuilder.AddInt("SubVersion", lmSubversion);
            tagBuilder.AddInt("MinecraftDataVersion", mcVersion);
            tagBuilder.BeginCompound("Metadata");
            tagBuilder.BeginCompound("EnclosingSize");
            tagBuilder.AddInt("x", width);
            tagBuilder.AddInt("y", height);
            tagBuilder.AddInt("z", length);
            tagBuilder.EndCompound();
            tagBuilder.AddString("Author", author);
            tagBuilder.AddString("Description", description);
            tagBuilder.AddString("Name", name);
            if (saveSoft)
            {
                tagBuilder.AddString("Software", LITEMATICACS_NAME + "_" + LITEMATICACS_VERSION);
            }
            tagBuilder.AddInt("RegionCount", regions.Count);
            tagBuilder.AddLong("TimeCreated", created);
            tagBuilder.AddLong("TimeModified", modified);
            tagBuilder.AddInt("TotalBlocks", region.Sum(kvp => kvp.Value.countBlocks()));
            tagBuilder.AddInt("TotalVolume", region.Sum(kvp => kvp.Value.volume()));
            tagBuilder.AddTag(preview);
            tagBuilder.EndCompound();
            tagBuilder.BeginCompound("Regions");
            foreach (var kvp in regions)
            {
                tagBuilder.AddTag(kvp.Value.toNbt(kvp.Key));
            }
            tagBuilder.EndCompound();
            return tagBuilder.Create();
        }
        public static Schematic fromNbt(CompoundTag nbt)
        {
            CompoundTag meta = nbt.Get<CompoundTag>("Metadata");
            int lmVersion = nbt.Get<IntTag>("Version").Value;
            int lmSubversion = 0;
            if (nbt.TryGetValue("SubVersion", out IntTag subversionTag))
            {
                lmSubversion = subversionTag.Value;
            }
            int mcVersion = nbt.Get<IntTag>("MinecraftDataVersion");
            int width = 0;
            int height = 0;
            int length = 0;
            if (meta.ContainsKey("EnclosingSize"))
            {
                CompoundTag enclosingSize = meta.Get<CompoundTag>("EnclosingSize");
                width = enclosingSize.Get<IntTag>("x").Value;
                height = enclosingSize.Get<IntTag>("y").Value;
                length = enclosingSize.Get<IntTag>("z").Value;
            }
            string author = meta.Get<StringTag>("Author").Value;
            string name = meta.Get<StringTag>("Name").Value;
            string desc = meta.Get<StringTag>("Description").Value;
            Dictionary<string, Region> regions = new Dictionary<string, Region>();
            foreach (Tag tag in nbt.Get<CompoundTag>("Regions"))
            {
                if (tag is CompoundTag compoundTag)
                {
                    Region reg = Region.fromNbt(compoundTag);
                    regions[compoundTag.Name] = reg;
                }
            }
            Schematic schematic = new Schematic(name: name, author: author, description: desc, regions: regions, lmVersion: lmVersion, lmSubversion: lmSubversion, mcVersion: mcVersion);
            if (meta.ContainsKey("EnclosingSize") && schematic.width != width)
            {
                throw new CorruptedSchematicError($"Invalid schematic width in metadata, excepted {schematic.width} was {width}");
            }
            if (meta.ContainsKey("EnclosingSize") && schematic.height != height)
            {
                throw new CorruptedSchematicError($"Invalid schematic height in metadata, excepted {schematic.height} was {height}");
            }
            if (meta.ContainsKey("EnclosingSize") && schematic.length != length)
            {
                throw new CorruptedSchematicError($"Invalid schematic length in metadata, excepted {schematic.length} was {length}");
            }
            schematic.created = meta.Get<LongTag>("TimeCreated").Value;
            schematic.modified = meta.Get<LongTag>("TimeModified").Value;
            if (meta.ContainsKey("RegionCount") && schematic.regions.Count != meta.Get<IntTag>("RegionCount").Value)
            {
                throw new CorruptedSchematicError("Number of regions in metadata does not match the number of parsed regions");
            }
            if (meta.ContainsKey("PreviewImageData"))
            {
                schematic.preview = meta.Get<IntArrayTag>("PreviewImageData");
            }
            return schematic;
        }
        public void updateMetadata()
        {
            modified = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        public static Schematic load(string filePath)
        {
            CompoundTag nbt = NbtFile.Read(filePath, FormatOptions.BigEndian);
            return Schematic.fromNbt(nbt);
        }
        private void onRegionAdd(string name, Region region)
        {
            if (xMin == null)
            {
                xMin = region.minSchemX();
            } else
            {
                xMin = Math.Min(xMin.Value, region.minSchemX());
            }
            if (xMax == null)
            {
                xMax = region.maxSchemX();
            }
            else
            {
                xMax = Math.Max(xMax.Value, region.maxSchemX());
            }
            if (yMin == null)
            {
                yMin = region.minSchemY();
            }
            else
            {
                yMin = Math.Min(yMin.Value, region.minSchemY());
            }
            if (yMax == null)
            {
                yMax = region.maxSchemY();
            }
            else
            {
                yMax = Math.Max(yMax.Value, region.maxSchemY());
            }
            if (zMin == null)
            {
                zMin = region.minSchemZ();
            }
            else
            {
                zMin = Math.Min(zMin.Value, region.minSchemZ());
            }
            if (zMax == null)
            {
                zMax = region.maxSchemZ();
            }
            else
            {
                zMax = Math.Max(zMax.Value, region.maxSchemZ());
            }
        }
        private void onRegionRemove(string name, Region region)
        {
            bool boundingBoxChanged = xMin == region.minSchemX();
            boundingBoxChanged = boundingBoxChanged || xMax == region.maxSchemX();
            boundingBoxChanged = boundingBoxChanged || yMin == region.minSchemY();
            boundingBoxChanged = boundingBoxChanged || yMax == region.maxSchemY();
            boundingBoxChanged = boundingBoxChanged || zMin == region.minSchemZ();
            boundingBoxChanged = boundingBoxChanged || zMax == region.maxSchemZ();
            if (boundingBoxChanged)
            {
                computeEnclosure();
            }
        }
        private void computeEnclosure()
        {
            int? xmin = null, xmax = null, ymin = null, ymax = null, zmin = null, zmax = null;
            foreach (Region region in regions.Values)
            {
                xmin = (xmin != null) ? Math.Min(xmin.Value, region.minSchemX()) : region.minSchemX();
                xmax = (xmax != null) ? Math.Max(xmax.Value, region.maxSchemX()) : region.maxSchemX();
                ymin = (ymin != null) ? Math.Min(ymin.Value, region.minSchemY()) : region.minSchemY();
                ymax = (ymax != null) ? Math.Max(ymax.Value, region.maxSchemY()) : region.maxSchemY();
                zmin = (zmin != null) ? Math.Min(zmin.Value, region.minSchemZ()) : region.minSchemZ();
                zmax = (zmax != null) ? Math.Max(zmax.Value, region.maxSchemZ()) : region.maxSchemZ();
            }
            xMin = xmin;
            xMax = xmax;
            yMin = ymin;
            yMax = ymax;
            zMin = zmin;
            zMax = zmax;
        }
    }
}

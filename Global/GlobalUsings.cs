global using RawBuffer = NativeMemoryOwner<byte>;

global using ReadonlyBufferMap = System.Collections.Generic.IReadOnlyDictionary<string, NativeBuffer>;

global using BufferMap = System.Collections.Generic.Dictionary<string, NativeBuffer>;

// RSB Type Aliases

global using RsbManifest = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamBundle.ManifestCompositeInfo_Json>;

global using PoolMap = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamBundle.RsbPoolInfo>;

global using CompositeMap = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamBundle.RsbCompositeInfo>;

global using GroupMap = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamBundle.RsbGroupInfo>;

global using SubGroupMap = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamBundle.RsbSubGroupInfo>;

global using PtxGlobalMap = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamBundle.RsbTextureInfo>;

// RSB Manifest sub types

global using RsbManifestGroupMap = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamBundle.ManifestGroupInfo_Json>;

global using RsbManifestResMap = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamBundle.ManifestResInfo_Json>;

// RSG Type Aliases

global using CommonResMap = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamGroup.RsgResidentInfo>;

global using CommonResEntries = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, SexyPackages.ResourceStreamGroup.RsgResidentInfo>>;

global using CommonResList = System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, SexyPackages.ResourceStreamGroup.RsgResidentInfo>>;

global using PtxMap = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamGroup.RsgTextureInfo>;

global using PtxExportInfo = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamGroup.TextureFileMetadata>;

global using CodecInfo = System.Collections.Generic.Dictionary<string, SexyPackages.ResourceStreamGroup.CodecContext>;

global using CodecSharedInfo = System.Collections.Concurrent.ConcurrentDictionary<string, SexyPackages.ResourceStreamGroup.CodecContext>;
namespace oscdimg_wrapprt_gui
{
    internal enum ImageFormatKind
    {
        Iso9660,
        Joliet,
        Udf,
        DvdVideo,
    }

    internal enum IsoNameMode
    {
        Dos8Dot3,
        LongNames,
        Nt351,
    }

    internal enum JolietMode
    {
        WithIso9660,
        JolietOnly,
    }

    internal enum UdfNamespace
    {
        WithIso9660,
        UdfOnly,
    }

    internal sealed class OscdimgRequest
    {
        public ImageFormatKind Format { get; init; }
        public string SourceDirectory { get; init; } = "";
        public string DestinationFile { get; init; } = "";
        public string Label { get; init; } = "";
        public bool IgnoreMaxSize { get; init; }

        public IsoNameMode IsoNameMode { get; init; }
        public bool PreserveLowerCase { get; init; }

        public JolietMode JolietMode { get; init; }
        public string? JolietReadme { get; init; }

        public UdfNamespace UdfNamespace { get; init; }
        public string UdfVersion { get; init; } = "1.02";
        public bool UdfEmbedData { get; init; }
        public bool UdfEmbedIdentifier { get; init; }
        public bool UdfSparse { get; init; }
        public bool UdfLongAllocation { get; init; }
        public string? UdfReadme { get; init; }

        public bool DvdVideoZone { get; init; }
        public bool DvdTruncateIso { get; init; }
    }
}

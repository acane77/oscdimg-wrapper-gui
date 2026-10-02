using System.IO;
using System.Text;

namespace oscdimg_wrapprt_gui
{
    internal static class OscdimgCommand
    {
        public static IReadOnlyList<string> Build(OscdimgRequest request)
        {
            var args = new List<string>();
            switch (request.Format)
            {
                case ImageFormatKind.Iso9660:
                    AddIso(args, request);
                    break;
                case ImageFormatKind.Joliet:
                    AddJoliet(args, request);
                    break;
                case ImageFormatKind.Udf:
                    AddUdf(args, request);
                    break;
                case ImageFormatKind.DvdVideo:
                    AddDvd(args, request);
                    break;
            }

            if (request.Label.Length > 0)
                args.Add("-l" + request.Label);
            if (request.IgnoreMaxSize)
                args.Add("-m");
            if (request.SourceDirectory.Length > 0)
                args.Add(request.SourceDirectory);
            if (request.DestinationFile.Length > 0)
                args.Add(request.DestinationFile);
            return args;
        }

        public static string? Validate(OscdimgRequest request)
        {
            if (request.SourceDirectory.Length == 0)
                return "请选择源文件夹。";
            if (!TryFullPath(request.SourceDirectory, out var sourceFull))
                return "源文件夹路径无效。";
            if (!Directory.Exists(sourceFull))
                return "源文件夹不存在。";

            if (request.Label.Length == 0)
                return "请输入卷标。";
            if (request.Label.Length > 32)
                return "卷标最长 32 个字符。";
            if (request.Label.Any(char.IsWhiteSpace))
                return "卷标不能包含空格。";
            if (request.Label.Contains('"'))
                return "卷标不能包含引号。";

            if (request.DestinationFile.Length == 0)
                return "请指定输出 ISO 文件。";
            if (!TryFullPath(request.DestinationFile, out var destinationFull))
                return "输出文件路径无效。";
            var parent = Path.GetDirectoryName(destinationFull);
            if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                return "输出文件所在的文件夹不存在。";
            if (IsInsideDirectory(sourceFull, destinationFull))
                return "输出文件不能放在源文件夹里。";

            if (request.Format == ImageFormatKind.DvdVideo
                && !request.DvdVideoZone
                && !request.DvdTruncateIso)
            {
                return "请至少选择一项 DVD 视频选项。";
            }

            var readme = request.Format switch
            {
                ImageFormatKind.Joliet when request.JolietMode == JolietMode.JolietOnly => request.JolietReadme,
                ImageFormatKind.Udf when request.UdfNamespace == UdfNamespace.UdfOnly => request.UdfReadme,
                _ => null,
            };
            if (!string.IsNullOrEmpty(readme))
            {
                if (!TryFullPath(readme, out var readmeFull))
                    return "说明文件路径无效。";
                if (!File.Exists(readmeFull))
                    return "说明文件不存在。";
            }

            return null;
        }

        public static string FormatCommandLine(string executable, IReadOnlyList<string> arguments)
        {
            var parts = new string[arguments.Count + 1];
            parts[0] = Quote(executable);
            for (var i = 0; i < arguments.Count; i++)
                parts[i + 1] = Quote(arguments[i]);
            return string.Join(" ", parts);
        }

        private static void AddIso(List<string> args, OscdimgRequest request)
        {
            switch (request.IsoNameMode)
            {
                case IsoNameMode.LongNames:
                    args.Add("-n");
                    break;
                case IsoNameMode.Nt351:
                    args.Add("-nt");
                    break;
            }

            if (request.PreserveLowerCase && request.IsoNameMode != IsoNameMode.Nt351)
                args.Add("-d");
        }

        private static void AddJoliet(List<string> args, OscdimgRequest request)
        {
            if (request.JolietMode == JolietMode.JolietOnly)
            {
                args.Add("-j2");
                if (!string.IsNullOrEmpty(request.JolietReadme))
                    args.Add("-js" + request.JolietReadme);
            }
            else
            {
                args.Add("-j1");
            }
        }

        private static void AddUdf(List<string> args, OscdimgRequest request)
        {
            args.Add(request.UdfNamespace == UdfNamespace.UdfOnly ? "-u2" : "-u1");
            args.Add(UdfVersionFlag(request.UdfVersion));
            if (request.UdfNamespace == UdfNamespace.UdfOnly)
            {
                if (request.UdfEmbedData)
                    args.Add("-ue");
                if (request.UdfEmbedIdentifier)
                    args.Add("-uf");
                if (request.UdfSparse)
                    args.Add("-us");
                if (!string.IsNullOrEmpty(request.UdfReadme))
                    args.Add("-ur" + request.UdfReadme);
            }

            if (request.UdfLongAllocation)
                args.Add("-yl");
        }

        private static void AddDvd(List<string> args, OscdimgRequest request)
        {
            if (request.DvdVideoZone)
                args.Add("-uv");
            if (request.DvdTruncateIso)
                args.Add("-ut");
        }

        private static string UdfVersionFlag(string version)
        {
            if (version == "1.02")
                return "-udfver102";
            throw new InvalidOperationException("未知的 UDF 版本。");
        }

        private static bool TryFullPath(string path, out string fullPath)
        {
            try
            {
                fullPath = Path.GetFullPath(path);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                fullPath = "";
                return false;
            }
        }

        private static bool IsInsideDirectory(string directory, string filePath)
        {
            var root = Path.GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var full = Path.GetFullPath(filePath);
            var prefix = root + Path.DirectorySeparatorChar;
            return full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string Quote(string value)
        {
            if (!NeedsQuotes(value))
                return value;

            var builder = new StringBuilder();
            builder.Append('"');
            var slashes = 0;
            foreach (var ch in value)
            {
                if (ch == '\\')
                {
                    slashes++;
                    continue;
                }

                if (ch == '"')
                {
                    builder.Append('\\', slashes * 2 + 1);
                    builder.Append('"');
                    slashes = 0;
                    continue;
                }

                if (slashes > 0)
                {
                    builder.Append('\\', slashes);
                    slashes = 0;
                }

                builder.Append(ch);
            }

            if (slashes > 0)
                builder.Append('\\', slashes * 2);
            builder.Append('"');
            return builder.ToString();
        }

        private static bool NeedsQuotes(string value)
        {
            if (value.Length == 0)
                return true;

            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                    continue;
                switch (ch)
                {
                    case '.':
                    case '_':
                    case '-':
                    case ':':
                    case '\\':
                    case '/':
                    case '+':
                        continue;
                    default:
                        return true;
                }
            }

            return false;
        }
    }
}

using System.Diagnostics;
using System.IO;

namespace oscdimg_wrapprt_gui
{
    internal sealed class OscdimgRunner : IDisposable
    {
        private Process? _process;

        public static string ExePath =>
            Path.Combine(AppContext.BaseDirectory, "bundled", "oscdimg.exe");

        public async Task<int> RunAsync(
            IReadOnlyList<string> arguments,
            Action<string?> onOutput,
            CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ExePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory,
            };
            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            var process = new Process { StartInfo = startInfo };
            _process = process;
            process.OutputDataReceived += (_, e) => onOutput(e.Data);
            process.ErrorDataReceived += (_, e) => onOutput(e.Data);

            if (!process.Start())
                throw new InvalidOperationException("无法启动 oscdimg。");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                }
            });

            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            process.WaitForExit();
            return process.ExitCode;
        }

        public void Dispose()
        {
            _process?.Dispose();
            _process = null;
        }
    }
}

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RevitToIfcScheduler.Utilities
{
    public static class ViteServerMiddleware
    {
        public const string ViteServerUrl = "http://localhost:5173";
        private const int MaxWaitMs = 120_000;

        private static Process? _viteProcess;

        // Call once during Configure(). Blocks until Vite is ready.
        // If Vite is already running externally the start is skipped.
        public static void EnsureStarted(string appRootPath, CancellationToken stoppingToken = default)
        {
            if (IsViteReadyAsync(CancellationToken.None).GetAwaiter().GetResult())
                return;

            _viteProcess = StartViteProcess(appRootPath);
            stoppingToken.Register(KillViteProcess);

            WaitForViteAsync(stoppingToken).GetAwaiter().GetResult();
        }

        private static Process StartViteProcess(string appRootPath)
        {
            var workingDir = System.IO.Path.Combine(appRootPath, "ClientApp");

            ProcessStartInfo psi;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi = new ProcessStartInfo("cmd.exe", "/c npm run start")
                {
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                };
            }
            else
            {
                psi = new ProcessStartInfo("npm", "run start")
                {
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                };
            }

            return Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start Vite dev server process.");
        }

        private static void KillViteProcess()
        {
            try
            {
                if (_viteProcess != null && !_viteProcess.HasExited)
                    _viteProcess.Kill(entireProcessTree: true);
            }
            catch { /* best-effort cleanup */ }
        }

        private static async Task WaitForViteAsync(CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(MaxWaitMs);
            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await IsViteReadyAsync(cancellationToken))
                    return;
                await Task.Delay(500, cancellationToken);
            }
            throw new TimeoutException($"Vite dev server did not start within {MaxWaitMs / 1000} seconds.");
        }

        private static async Task<bool> IsViteReadyAsync(CancellationToken cancellationToken)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
            try
            {
                var response = await client.GetAsync(ViteServerUrl, cancellationToken);
                return (int)response.StatusCode < 500;
            }
            catch
            {
                return false;
            }
        }
    }
}

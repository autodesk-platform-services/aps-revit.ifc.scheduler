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
        public const string DefaultViteServerUrl = "http://localhost:5173";
        private const int MaxWaitMs = 120_000;

        private static Process? _viteProcess;

        // Call once during Configure(). Blocks until Vite is ready.
        // Returns the URL actually used so the caller can pass it straight to UseProxyToSpaDevelopmentServer.
        // If Vite is already running externally the start is skipped.
        public static string EnsureStarted(
            string appRootPath,
            CancellationToken stoppingToken = default,
            string viteServerUrl = DefaultViteServerUrl)
        {
            if (IsViteReadyAsync(viteServerUrl, CancellationToken.None).GetAwaiter().GetResult())
                return viteServerUrl;

            _viteProcess = StartViteProcess(appRootPath);
            stoppingToken.Register(KillViteProcess);

            WaitForViteAsync(viteServerUrl, stoppingToken).GetAwaiter().GetResult();
            return viteServerUrl;
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

        private static async Task WaitForViteAsync(string viteServerUrl, CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(MaxWaitMs);
            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await IsViteReadyAsync(viteServerUrl, cancellationToken))
                    return;
                await Task.Delay(500, cancellationToken);
            }
            throw new TimeoutException($"Vite dev server did not start within {MaxWaitMs / 1000} seconds.");
        }

        private static async Task<bool> IsViteReadyAsync(string viteServerUrl, CancellationToken cancellationToken)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
            try
            {
                var response = await client.GetAsync(viteServerUrl, cancellationToken);
                return (int)response.StatusCode < 500;
            }
            catch
            {
                return false;
            }
        }
    }
}

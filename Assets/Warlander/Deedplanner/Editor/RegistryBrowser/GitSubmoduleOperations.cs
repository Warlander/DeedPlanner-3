using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public static class GitSubmoduleOperations
    {
        private const string EmbedsRelativePath = "Packages/Embeds";

        public static string GetProjectRoot()
            => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static string GetEmbedRelativePath(string packageId)
            => $"{EmbedsRelativePath}/{packageId}";

        public static string GetEmbedAbsolutePath(string packageId)
            => Path.Combine(GetProjectRoot(), EmbedsRelativePath, packageId);

        public static bool IsEmbeddedAsSubmodule(string packageId)
            => Directory.Exists(GetEmbedAbsolutePath(packageId));

        public static async Task AddSubmoduleAsync(string packageId, string repoUrl, string commitSha)
        {
            string projectRoot = GetProjectRoot();
            string cleanUrl = CleanRepoUrl(repoUrl);
            string relativePath = GetEmbedRelativePath(packageId);

            string embedsAbsPath = Path.Combine(projectRoot, EmbedsRelativePath);
            if (!Directory.Exists(embedsAbsPath))
                Directory.CreateDirectory(embedsAbsPath);

            await RunGitAsync($"submodule add {QuoteArg(cleanUrl)} {QuoteArg(relativePath)}", projectRoot);

            string absPath = GetEmbedAbsolutePath(packageId);
            await RunGitAsync($"checkout {commitSha}", absPath);

            await RunGitAsync($"add {QuoteArg(relativePath)} .gitmodules", projectRoot);
        }

        public static async Task<bool> SubmoduleHasChangesAsync(string packageId)
        {
            string absPath = GetEmbedAbsolutePath(packageId);
            if (!Directory.Exists(absPath))
                return false;

            string output = await RunGitOutputAsync("status --porcelain", absPath);
            return !string.IsNullOrWhiteSpace(output);
        }

        public static async Task RemoveSubmoduleAsync(string packageId)
        {
            string projectRoot = GetProjectRoot();
            string relativePath = GetEmbedRelativePath(packageId);

            await RunGitAsync($"submodule deinit -f {QuoteArg(relativePath)}", projectRoot);
            await RunGitAsync($"rm -f {QuoteArg(relativePath)}", projectRoot);

            string gitModulesDir = Path.Combine(
                projectRoot, ".git", "modules",
                EmbedsRelativePath.Replace('/', Path.DirectorySeparatorChar),
                packageId);
            if (Directory.Exists(gitModulesDir))
                Directory.Delete(gitModulesDir, recursive: true);
        }

        private static string CleanRepoUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;
            if (url.StartsWith("git+"))
                url = url.Substring(4);
            return url;
        }

        private static string QuoteArg(string arg)
            => $"\"{arg.Replace("\"", "\\\"")}\"";

        private static Task RunGitAsync(string args, string workingDir)
        {
            var tcs = new TaskCompletionSource<bool>();
            var proc = CreateGitProcess(args, workingDir, captureStdout: false);
            var stderr = new StringBuilder();

            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
            proc.Exited += (_, __) =>
            {
                proc.WaitForExit();
                int code = proc.ExitCode;
                proc.Dispose();
                if (code == 0)
                    tcs.TrySetResult(true);
                else
                    tcs.TrySetException(new Exception($"git {args} failed (exit {code}): {stderr}"));
            };

            proc.Start();
            proc.BeginErrorReadLine();
            return tcs.Task;
        }

        private static Task<string> RunGitOutputAsync(string args, string workingDir)
        {
            var tcs = new TaskCompletionSource<string>();
            var proc = CreateGitProcess(args, workingDir, captureStdout: true);
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
            proc.Exited += (_, __) =>
            {
                proc.WaitForExit();
                int code = proc.ExitCode;
                proc.Dispose();
                if (code == 0)
                    tcs.TrySetResult(stdout.ToString());
                else
                    tcs.TrySetException(new Exception($"git {args} failed (exit {code}): {stderr}"));
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            return tcs.Task;
        }

        private static Process CreateGitProcess(string args, string workingDir, bool captureStdout)
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = captureStdout,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            return new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true,
            };
        }
    }
}

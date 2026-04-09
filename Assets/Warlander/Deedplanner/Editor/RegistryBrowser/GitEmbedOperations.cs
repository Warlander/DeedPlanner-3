using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Warlander.Deedplanner.Editor.RegistryBrowser
{
    public static class GitEmbedOperations
    {
        private const string EmbedsRelativePath = "Packages/Embeds";
        private const string EmbedsFolderGitIgnoreEntry = "Packages/Embeds/";

        public static string GetProjectRoot()
            => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static string GetEmbedRelativePath(string packageId)
            => $"{EmbedsRelativePath}/{packageId}";

        public static string GetEmbedAbsolutePath(string packageId)
            => Path.Combine(GetProjectRoot(), EmbedsRelativePath, packageId);

        public static bool IsEmbedded(string packageId)
            => Directory.Exists(GetEmbedAbsolutePath(packageId));

        public static bool IsEmbedFolderInGitIgnore()
        {
            string gitIgnorePath = Path.Combine(GetProjectRoot(), ".gitignore");
            if (!File.Exists(gitIgnorePath))
                return false;
            foreach (string line in File.ReadAllLines(gitIgnorePath))
            {
                string trimmed = line.Trim();
                if (trimmed == "Packages/Embeds/" || trimmed == "Packages/Embeds")
                    return true;
            }
            return false;
        }

        public static void AddEmbedFolderToGitIgnore()
        {
            if (IsEmbedFolderInGitIgnore())
                return;
            string gitIgnorePath = Path.Combine(GetProjectRoot(), ".gitignore");
            string existing = File.Exists(gitIgnorePath) ? File.ReadAllText(gitIgnorePath) : "";
            if (existing.Length > 0 && !existing.EndsWith("\n"))
                existing += "\n";
            existing += "\n";
            existing += "# Embedded packages are temporary local git repos used for development — do not commit.\n";
            existing += EmbedsFolderGitIgnoreEntry + "\n";
            File.WriteAllText(gitIgnorePath, existing);
        }

        public static async Task CloneAndCheckoutAsync(string packageId, string repoUrl, string commitSha)
        {
            string projectRoot = GetProjectRoot();
            string cleanUrl = CleanRepoUrl(repoUrl);
            string relativePath = GetEmbedRelativePath(packageId);
            string absPath = GetEmbedAbsolutePath(packageId);

            string embedsAbsPath = Path.Combine(projectRoot, EmbedsRelativePath);
            if (!Directory.Exists(embedsAbsPath))
                Directory.CreateDirectory(embedsAbsPath);

            // Clean up any leftover directory from a previous failed attempt.
            if (Directory.Exists(absPath))
                DeleteDirectoryForce(absPath);

            await RunGitAsync($"clone {QuoteArg(cleanUrl)} {QuoteArg(relativePath)}", projectRoot);
            await RunGitAsync($"checkout {commitSha}", absPath);
        }

        public static async Task<bool> EmbedHasChangesAsync(string packageId)
        {
            string absPath = GetEmbedAbsolutePath(packageId);
            if (!Directory.Exists(absPath))
                return false;

            string output = await RunGitOutputAsync("status --porcelain", absPath);
            return !string.IsNullOrWhiteSpace(output);
        }

        public static Task RemoveEmbedAsync(string packageId)
        {
            string absPath = GetEmbedAbsolutePath(packageId);
            if (Directory.Exists(absPath))
                DeleteDirectoryForce(absPath);
            return Task.CompletedTask;
        }

        // Directory.Delete with recursive:true throws on read-only files (common for git pack files on Windows).
        // Strip read-only attributes first so deletion succeeds.
        private static void DeleteDirectoryForce(string path)
        {
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(path, recursive: true);
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

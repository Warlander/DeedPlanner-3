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

            // Clean up any stale git-modules directory left by a previous failed add or incomplete remove.
            string gitModulesDir = Path.Combine(
                projectRoot, ".git", "modules",
                EmbedsRelativePath.Replace('/', Path.DirectorySeparatorChar),
                packageId);
            if (Directory.Exists(gitModulesDir))
                DeleteDirectoryForce(gitModulesDir);

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

            // deinit clears the working directory and removes the .git/config entry.
            // Fails when the submodule was never committed (staged-only state).
            try
            {
                await RunGitAsync($"submodule deinit -f {QuoteArg(relativePath)}", projectRoot);
            }
            catch (Exception ex) when (ex.Message.Contains("did not match"))
            {
                // Not in the committed index; clean .git/config manually and fall through.
                await TryRunGitAsync($"config --remove-section submodule.{relativePath}", projectRoot);
            }

            // git rm removes the gitlink from the index and strips the entry from .gitmodules.
            // Fails when the path was never staged as a gitlink (staged-only embed).
            try
            {
                await RunGitAsync($"rm -f {QuoteArg(relativePath)}", projectRoot);
            }
            catch
            {
                // No gitlink in the index; clean .gitmodules directly and delete the directory.
                string gitmodulesSection = $"submodule.{relativePath}";
                await TryRunGitAsync(
                    $"config --file .gitmodules --remove-section {QuoteArg(gitmodulesSection)}",
                    projectRoot);
                await TryRunGitAsync("add .gitmodules", projectRoot);

                string absPath = GetEmbedAbsolutePath(packageId);
                if (Directory.Exists(absPath))
                    DeleteDirectoryForce(absPath);
            }

            string gitModulesDir = Path.Combine(
                projectRoot, ".git", "modules",
                EmbedsRelativePath.Replace('/', Path.DirectorySeparatorChar),
                packageId);
            if (Directory.Exists(gitModulesDir))
                DeleteDirectoryForce(gitModulesDir);
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

        private static async Task TryRunGitAsync(string args, string workingDir)
        {
            try { await RunGitAsync(args, workingDir); }
            catch { /* best-effort, ignore */ }
        }

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

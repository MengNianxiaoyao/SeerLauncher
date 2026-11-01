using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using VisualBasicFileSystem = Microsoft.VisualBasic.FileIO.FileSystem;

namespace SeerLauncher.Infrastructure.FileSystem
{
    /// <summary>启动结果，用于区分失败原因并给出对应提示。</summary>
    public enum LaunchResult
    {
        Success,
        FileNotFound,
        AccessDenied,
        InvalidExecutable,
        Failed
    }

    /// <summary>删除结果，用于区分被删程序的当前状态并给出对应提示。</summary>
    public enum DeleteResult
    {
        Success,
        FileNotFound,
        FileInUse,
        AccessDenied,
        Unknown
    }

    public class FileOperationsService : IFileOperationsService
    {
        public LaunchResult Launch(string fullPath)
        {
            if (!File.Exists(fullPath)) return LaunchResult.FileNotFound;
            var workingDirectory = Path.GetDirectoryName(fullPath) ?? string.Empty;

            // 启动器是管理员身份，直接 Process.Start 会把子进程也拉成管理员。
            // 先借资源管理器（普通用户）的令牌启动；程序自身要求提权时该调用会失败（错误 740），
            // 再回退到当前身份，让需要管理员的程序照常拿到管理员权限。
            if (TryStartDeElevated(fullPath, workingDirectory))
                return LaunchResult.Success;

            try
            {
                // 工作目录设为程序自身所在目录，避免目标程序把配置/存档写进启动器目录
                Process.Start(new ProcessStartInfo(fullPath)
                {
                    UseShellExecute = true,
                    WorkingDirectory = workingDirectory
                });
                return LaunchResult.Success;
            }
            catch (Win32Exception exception)
            {
                switch (exception.NativeErrorCode)
                {
                    case 2:   // ERROR_FILE_NOT_FOUND
                    case 3:   // ERROR_PATH_NOT_FOUND
                        return LaunchResult.FileNotFound;
                    case 5:   // ERROR_ACCESS_DENIED
                    case 740: // ERROR_ELEVATION_REQUIRED
                    case 225: // ERROR_VIRUS_INFECTED / 被安全软件拦截
                        return LaunchResult.AccessDenied;
                    case 193: // ERROR_BAD_EXE_FORMAT
                        return LaunchResult.InvalidExecutable;
                    default:
                        return LaunchResult.Failed;
                }
            }
            catch
            {
                return LaunchResult.Failed;
            }
        }

        public void OpenDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;
            try
            {
                var args = directory.IndexOf(' ') >= 0 ? "\"" + directory + "\"" : directory;
                Process.Start(new ProcessStartInfo("explorer.exe", args) { UseShellExecute = true });
            }
            catch
            {
            }
        }

        public DeleteResult DeleteToRecycleBin(string fullPath)
        {
            if (!File.Exists(fullPath)) return DeleteResult.FileNotFound;
            try
            {
                VisualBasicFileSystem.DeleteFile(fullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return DeleteResult.Success;
            }
            catch (Exception exception)
            {
                return ClassifyDeleteFailure(fullPath, exception);
            }
        }

        public DeleteResult DeletePermanently(string fullPath)
        {
            if (!File.Exists(fullPath)) return DeleteResult.FileNotFound;
            try
            {
                File.Delete(fullPath);
                return DeleteResult.Success;
            }
            catch (Exception exception)
            {
                return ClassifyDeleteFailure(fullPath, exception);
            }
        }

        private static DeleteResult ClassifyDeleteFailure(string fullPath, Exception exception)
        {
            // 等待期间文件可能已被其他途径删除
            if (!File.Exists(fullPath)) return DeleteResult.FileNotFound;

            // 运行中的 exe 也会抛“拒绝访问”，必须先按“正在运行”识别，否则会误报成权限问题
            if (IsExecutableRunning(fullPath)) return DeleteResult.FileInUse;

            if (exception is UnauthorizedAccessException || exception is System.Security.SecurityException)
                return DeleteResult.AccessDenied;
            if (exception is IOException)
                return DeleteResult.FileInUse;

            return DeleteResult.Unknown;
        }

        private static bool IsExecutableRunning(string fullPath)
        {
            try
            {
                var target = Path.GetFullPath(fullPath);
                var processName = Path.GetFileNameWithoutExtension(fullPath);
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        var modulePath = process.MainModule == null ? null : process.MainModule.FileName;
                        if (string.Equals(modulePath, target, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    catch
                    {
                        // 本程序以管理员身份运行，通常可读到 MainModule；
                        // 系统进程/受保护进程仍可能拒绝，此时按“未运行”处理，避免误报
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public static void OpenUrl(string url)
        {
            if (!IsSafeUrl(url)) return;
            try
            {
                // 经 explorer 中转，避免提权后把浏览器也拉成管理员身份
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + url + "\"") { UseShellExecute = true });
            }
            catch
            {
            }
        }

        public static bool IsSafeUrl(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && !string.IsNullOrEmpty(uri.Host)
                && string.IsNullOrEmpty(uri.UserInfo)
                && uri.IsDefaultPort;
        }

        /// <summary>
        /// 用资源管理器（普通用户）的身份启动目标程序，失败返回 false。
        /// 程序自身 manifest 要求提权时这里会失败（ERROR_ELEVATION_REQUIRED），由调用方回退。
        /// </summary>
        private static bool TryStartDeElevated(string fullPath, string workingDirectory)
        {
            var shellToken = IntPtr.Zero;
            var primaryToken = IntPtr.Zero;
            var processInfo = new ProcessInformation();
            var created = false;
            try
            {
                shellToken = GetShellToken();
                if (shellToken == IntPtr.Zero) return false;

                if (!DuplicateTokenEx(shellToken, MaximumAllowed, IntPtr.Zero,
                        SecurityImpersonation, TokenPrimary, out primaryToken))
                    return false;
                if (primaryToken == IntPtr.Zero) return false;

                var startupInfo = new StartupInfo();
                startupInfo.cb = Marshal.SizeOf(typeof(StartupInfo));
                startupInfo.lpDesktop = "winsta0\\default";

                created = CreateProcessWithTokenW(primaryToken, 0, fullPath,
                    "\"" + fullPath + "\"", CreateUnicodeEnvironment, IntPtr.Zero,
                    workingDirectory, ref startupInfo, ref processInfo);
                return created;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (created)
                {
                    CloseHandle(processInfo.hThread);
                    CloseHandle(processInfo.hProcess);
                }

                if (primaryToken != IntPtr.Zero) CloseHandle(primaryToken);
                if (shellToken != IntPtr.Zero) CloseHandle(shellToken);
            }
        }

        /// <summary>取当前会话中 explorer.exe 的访问令牌（普通用户权限）。</summary>
        private static IntPtr GetShellToken()
        {
            var sessionId = Process.GetCurrentProcess().SessionId;
            foreach (var process in Process.GetProcessesByName("explorer"))
            {
                try
                {
                    if (process.SessionId != sessionId) continue;
                    IntPtr token;
                    if (OpenProcessToken(process.Handle, TokenDuplicate | TokenQuery, out token))
                        return token;
                }
                catch
                {
                    // 跨会话或受保护进程，跳过
                }
                finally
                {
                    process.Dispose();
                }
            }

            return IntPtr.Zero;
        }

        private const int TokenQuery = 0x0008;
        private const int TokenDuplicate = 0x0002;
        private const int MaximumAllowed = 0x02000000;
        private const int SecurityImpersonation = 2;
        private const int TokenPrimary = 1;
        private const int CreateUnicodeEnvironment = 0x00000400;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessWithTokenW(IntPtr hToken, int dwLogonFlags,
            string lpApplicationName, string lpCommandLine, int dwCreationFlags, IntPtr lpEnvironment,
            string lpCurrentDirectory, ref StartupInfo lpStartupInfo, ref ProcessInformation lpProcessInformation);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, int desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DuplicateTokenEx(IntPtr existingToken, int desiredAccess,
            IntPtr tokenAttributes, int impersonationLevel, int tokenType, out IntPtr newToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}

﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;

public class Program
{
    private const int InitDelay = 2500;
    static StringBuilder LogBuilder = new();
    static void SaveLog()
    {
        try
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            // 检查目录是否可写
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            // 测试文件写入权限
            var testFile = Path.Combine(logDir, ".test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);

            // 真正写入日志
            File.WriteAllText(Path.Combine(logDir, "updater_log.txt"), LogBuilder.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"日志保存失败: {ex.Message}");
        }
    }

    static Version GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version("1.0.8.0");
    }

    static void Main(string[] args)
    {
        try
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            Version version = GetCurrentVersion();
            if (args.Length > 0 && args[0].Equals("--version", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(version); // 直接输出（不写入日志，便于外部程序解析）
                return;
            }
            Log("MFAUpdater Version: v" + version);
            Log("Command Line Arguments: " + string.Join(", ", args));

            ValidateArguments(args);
            int mainProcessId = ParseMainProcessId(args);
            WaitForMainProcessExit(mainProcessId);
            HandleFileOperations(args);
        }
        catch (Exception ex)
        {
            Log($"更新过程发生错误: {ex.Message}");
            SaveLog();
        }
        finally
        {
            SaveLog();
        }
    }

    private static void Log(object message)
    {
        string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        Console.WriteLine(logEntry);
        LogBuilder.AppendLine(logEntry);
    }

    /// <summary>
    /// 验证参数格式（兼容原有2-4个业务参数+1个PID参数）
    /// </summary>
    private static void ValidateArguments(string[] args)
    {
        if ((args.Length < 2 || args.Length > 4) && (args.Length < 3 || args.Length > 5))
        {
            Log("参数格式错误，正确用法:");
            Log("新格式（含主程序PID）: MFAUpdater [源路径] [目标路径] [原程序名(可选)] [新程序名(可选)] [主程序PID]");
            Log("老格式（固定延迟）: MFAUpdater [源路径] [目标路径] [程序名(可选)]");
            SaveLog();
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// 解析最后一个参数作为主程序PID（处理引号转义）
    /// </summary>
    private static int ParseMainProcessId(string[] args)
    {
        // 判断是否包含PID参数（新格式：最后一个参数为数字）
        if (args.Length >= 3)
        {
            try
            {
                string lastArg = args[^1].Trim('"');
                if (int.TryParse(lastArg, out int pid))
                {
                    Log($"解析到主程序PID: {pid}");
                    return pid; // 返回有效PID
                }
                // 最后一个参数不是数字 → 视为老格式（无PID）
                Log("未检测到有效的PID参数，将使用固定延迟等待");
                return -1;
            }
            catch (Exception ex)
            {
                Log($"PID解析失败，将使用固定延迟等待: {ex.Message}");
                return -1;
            }
        }
        // 参数数量不足 → 老格式
        Log("未提供PID参数，将使用固定延迟等待");
        return -1;
    }

    /// <summary>
    /// 等待主程序完全退出后再执行文件操作
    /// </summary>
    private static void WaitForMainProcessExit(int mainProcessId)
    {
        if (mainProcessId != -1)
        {
            // 新逻辑：等待指定PID进程退出
            try
            {
                Log($"开始等待主程序退出 (PID: {mainProcessId})");
                var mainProcess = Process.GetProcessById(mainProcessId);

                if (mainProcess.HasExited)
                {
                    Log("主程序已退出");
                    return;
                }

                // 最多等待30秒
                var exited = mainProcess.WaitForExit(5000);
                if (exited)
                {
                    Log("主程序已成功退出");
                }
                else
                {
                    Log("警告：主程序在5秒内未正常退出，尝试强制继续");
                }
            }
            catch (ArgumentException)
            {
                Log("主程序已退出（进程未找到）");
            }
            catch (Exception ex)
            {
                Log($"等待主程序退出时发生错误: {ex.Message}");
            }
        }
        else
        {
            // 老逻辑：固定延迟等待
            Log($"使用固定延迟等待 {InitDelay} 毫秒...");
            Thread.Sleep(InitDelay);
            Log("固定延迟等待结束");
        }
    }


    /// <summary>
    /// 处理文件复制、目录迁移等核心操作（排除PID参数）
    /// </summary>
    private static void HandleFileOperations(string[] args)
    {
        try
        {
            // 判断是否包含PID参数（影响业务参数索引）
            bool hasPid = args.Length >= 3 && int.TryParse(args[^1].Trim('"'), out _);
            int pidOffset = hasPid ? 1 : 0; // 有PID时业务参数少1个（最后一个是PID）

            // 解析源路径和目标路径（索引不受PID影响）
            string source = Path.GetFullPath(args[0].Replace("\\\"", "\"").Replace("\"", ""))
                .Replace('\\', Path.DirectorySeparatorChar);
            string dest = Path.GetFullPath(args[1].Replace("\\\"", "\"").Replace("\"", ""))
                .Replace('\\', Path.DirectorySeparatorChar);

            Log($"源路径: {source}");
            Log($"目标路径: {dest}");

            // 处理文件/目录迁移（逻辑不变）
            if (File.Exists(source))
            {
                HandleFileTransfer(source, dest);
            }
            else if (Directory.Exists(source))
            {
                HandleDirectoryTransfer(source, dest);
            }
            else
            {
                throw new FileNotFoundException($"源路径不存在: {source}");
            }

            // 处理程序重命名（有PID时参数索引+1）
            if (args.Length - pidOffset >= 4) // 原参数长度>=4（含oldName和newName）
            {
                string oldName = args[2].Replace("\\\"", "\"").Replace("\"", "");
                string newName = args[3 - pidOffset].Replace("\\\"", "\"").Replace("\"", ""); // 适配PID偏移
                HandleAppRename(dest, oldName, newName);
            }

            // 处理程序启动（有PID时参数索引+1）
            if (args.Length - pidOffset == 3) // 原参数长度==3（含启动程序名）
            {
                string appName = args[2 - pidOffset].Replace("\\\"", "\"").Replace("\"", ""); // 适配PID偏移
                StartCrossPlatformProcess(appName);
            }

            // 清理源目录
            HandleDeleteDirectoryTransfer(source);
        }
        catch (Exception ex)
        {
            HandlePlatformSpecificErrors(ex);
            Environment.Exit(ex.HResult);
        }
    }

    private static void HandleFileTransfer(string source, string dest)
    {
        try
        {
            Log($"复制文件: {source} -> {dest}");
            // 创建目标目录（如果不存在）
            var destDir = Path.GetDirectoryName(dest);
            if (destDir != null)
                Directory.CreateDirectory(destDir);
            File.Copy(source, dest, true); // 覆盖已存在的文件

        }
        catch (Exception ex)
        {
            Log($"{source} 文件复制失败: {ex.Message}");
        }

        SetUnixPermissions(dest);
    }

    private static void HandleDirectoryTransfer(string source, string dest)
    {
        try
        {
            Log($"开始迁移目录: {source} -> {dest}");
            Directory.CreateDirectory(dest);

            // 递归创建子目录
            foreach (string sourceDir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                string destDir = sourceDir.Replace(source, dest);
                Directory.CreateDirectory(destDir);
                Log($"创建子目录: {destDir}");
            }

            // 递归复制文件
            foreach (string sourceFile in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string destFile = sourceFile.Replace(source, dest);
                HandleFileTransfer(sourceFile, destFile);
            }

            Log($"目录迁移完成: {dest}");
        }
        catch (Exception ex)
        {
            Log($"目录迁移失败: {ex.Message}");
            throw;
        }
    }

    private static void HandleDeleteDirectoryTransfer(string source)
    {
        try
        {
            if (Directory.Exists(source))
            {
                Directory.Delete(source, true);
                Log($"源目录已删除: {source}");
            }
            else if (File.Exists(source))
            {
                File.Delete(source);
                Log($"源文件已删除: {source}");
            }
        }
        catch (Exception ex)
        {
            Log($"源路径删除失败: {ex.Message}");
            // 非致命错误，不中断流程
        }
    }

    private static void HandleAppRename(string destDir, string oldName, string newName)
    {
        try
        {
            string oldPath = Path.Combine(destDir, oldName);
            string newPath = Path.Combine(destDir, newName);

            if (File.Exists(oldPath))
            {
                Log($"重命名程序: {oldPath} -> {newPath}");
                File.Move(oldPath, newPath, true);
                SetUnixPermissions(newPath);
                StartCrossPlatformProcess(newName);
            }
            else
            {
                Log($"待重命名的程序不存在: {oldPath}");
            }
        }
        catch (Exception ex)
        {
            Log($"程序重命名失败: {ex.Message}");
            throw;
        }
    }

    private static void SetUnixPermissions(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var chmodProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+rwx \"{path}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (chmodProcess != null)
                {
                    chmodProcess.WaitForExit();
                    if (chmodProcess.ExitCode != 0)
                    {
                        Log($"chmod执行失败，退出码: {chmodProcess.ExitCode}");
                    }
                    else
                    {
                        Log($"已设置文件权限: {path}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"权限设置失败: {ex.Message}");
                Log($"建议手动执行: chmod +rwx \"{path}\"");
            }
        }
    }

    private static void StartCrossPlatformProcess(string appName)
    {
        try
        {
            string fullAppPath = Path.Combine(AppContext.BaseDirectory, appName);
            if (!File.Exists(fullAppPath))
            {
                Log($"程序文件不存在: {fullAppPath}");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? appName
                    : $"./{appName}"
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                Log($"程序已启动 [PID: {process.Id}]");
            }
            else
            {
                Log("程序启动失败（进程未创建）");
            }
        }
        catch (Exception ex)
        {
            Log($"程序启动失败: {ex.Message}");
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log($"建议手动启动: chmod +x {appName} && ./{appName}");
            }
        }
    }

    private static void HandlePlatformSpecificErrors(Exception ex)
    {
        Log($"错误详情: {ex}");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && ex is UnauthorizedAccessException)
        {
            Log("Linux/macOS 权限问题解决方案:");
            Log("1. 尝试使用sudo权限运行");
            Log("2. 检查文件所有权: ls -l [目标目录]");
            Log("3. 手动设置权限: chmod -R 755 [目标目录]");
        }
        else if (ex is IOException && ex.Message.Contains("被另一进程使用", StringComparison.OrdinalIgnoreCase))
        {
            Log("文件被占用，可能的原因:");
            Log("1. 主程序未完全退出");
            Log("2. 其他进程正在使用目标文件");
            Log("建议: 关闭相关进程后重试");
        }
    }
}

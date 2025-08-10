﻿using Avalonia.Controls;
using MFAAvalonia.Configuration;
using MFAAvalonia.ViewModels.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MFAAvalonia.Helper;

public static class DirectoryMerger
{
    // 用于传递进度状态的内部类（替代ref参数）
    private class MergeProgressState
    {
        /// <summary>已处理的总大小（字节）</summary>
        public long ProcessedSize { get; set; }
    }

    public class DirectoryMergeProgress
    {
        /// <summary>当前处理的文件路径</summary>
        public string CurrentFilePath { get; set; }
        /// <summary>已处理大小(字节)</summary>
        public long ProcessedSize { get; set; }
        /// <summary>总大小(字节)</summary>
        public long TotalSize { get; set; }
        /// <summary>进度百分比</summary>
        public double ProgressPercentage => TotalSize > 0
            ? Math.Round((double)ProcessedSize / TotalSize * 100, 2)
            : 0;
    }

    /// <summary>
    /// 异步合并目录并按文件大小报告进度
    /// </summary>
    public static async Task DirectoryMergeAsync(
        string sourceDirName,
        string destDirName,
        ProgressBar? progressBar = null,
        bool overwriteMFA = true,
        bool saveAnnouncement = false,
        CancellationToken cancellationToken = default)
    {
        // 预扫描计算总文件大小
        var totalSize = CalculateTotalSize(sourceDirName);
        // 用类实例存储已处理大小（替代ref参数）
        var progressState = new MergeProgressState { ProcessedSize = 0 };

        await MergeDirectoryAsync(
            sourceDirName,
            destDirName,
            progressBar,
            totalSize,
            progressState,
            overwriteMFA,
            saveAnnouncement,
            cancellationToken);
    }

    /// <summary>递归计算所有文件的总大小</summary>
    private static long CalculateTotalSize(string directory)
    {
        if (!Directory.Exists(directory)) return 0;

        long totalSize = 0;

        // 累加当前目录文件大小
        foreach (var file in Directory.GetFiles(directory))
        {
            try
            {
                totalSize += new FileInfo(file).Length;
            }
            catch (Exception ex)
            {
                LoggerHelper.Warning($"计算文件大小失败: {file}，错误: {ex.Message}");
            }
        }

        // 递归累加子目录文件大小
        foreach (var subDir in Directory.GetDirectories(directory))
        {
            totalSize += CalculateTotalSize(subDir);
        }

        return totalSize;
    }

    /// <summary>异步合并目录的核心逻辑（按大小计算进度）</summary>
    async private static Task MergeDirectoryAsync(
        string sourceDirName,
        string destDirName,
        ProgressBar? progressBar,
        long totalSize,
        MergeProgressState progressState, // 用实例传递已处理大小
        bool overwriteMFA = true,
        bool saveAnnouncement = false,
        CancellationToken cancellationToken = default)
    {
        var dir = new DirectoryInfo(sourceDirName);
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"源目录不存在或无法找到: {sourceDirName}");
        }

        // 创建目标目录（如果不存在）
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // 处理当前目录下的文件
        foreach (var file in dir.GetFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tempPath = Path.Combine(destDirName, file.Name);
            var fileSize = file.Length; // 当前文件大小

            try
            {
                // 处理公告文件逻辑
                if (saveAnnouncement
                    && tempPath.Contains(AnnouncementViewModel.AnnouncementFolder)
                    && Path.GetExtension(file.Name).Equals(".md", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(tempPath))
                    {
                        var sourceContent = await File.ReadAllTextAsync(file.FullName, cancellationToken);
                        var destContent = await File.ReadAllTextAsync(tempPath, cancellationToken);

                        if (!string.Equals(sourceContent, destContent, StringComparison.Ordinal))
                        {
                            GlobalConfiguration.SetValue(ConfigurationKeys.DoNotShowAnnouncementAgain, bool.FalseString);
                        }
                    }
                    else
                    {
                        GlobalConfiguration.SetValue(ConfigurationKeys.DoNotShowAnnouncementAgain, bool.FalseString);
                    }
                }

                // 判断是否需要跳过文件
                if (!(overwriteMFA
                        || !Path.GetFileName(tempPath).Contains("MFAUpdater")
                        && !Path.GetFileName(tempPath).Contains("MFAAvalonia")
                        && !Path.GetFileName(tempPath).Contains(Process.GetCurrentProcess().MainModule?.ModuleName ?? "MFAAvalonia.exe"))
                    || Path.GetExtension(tempPath).Equals(".dll", StringComparison.OrdinalIgnoreCase) && OperatingSystem.IsWindows()
                    || !Path.GetFileName(tempPath).Contains("minicap.so", StringComparison.OrdinalIgnoreCase)
                    && Path.GetExtension(tempPath).Equals(".so", StringComparison.OrdinalIgnoreCase)
                    && OperatingSystem.IsLinux()
                    || Path.GetExtension(tempPath).Equals(".dylib", StringComparison.OrdinalIgnoreCase)
                    && (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS()))
                {
                    LoggerHelper.Info($"跳过文件: {tempPath}");
                    // 跳过的文件计入已处理大小
                    progressState.ProcessedSize += fileSize;
                    UpdateProgress(progressBar, progressState.ProcessedSize, totalSize);
                    continue;
                }

                // 复制文件（分块读取以实时更新进度）
                LoggerHelper.Info($"Copying file: {tempPath} (Size: {FormatSize(fileSize)})");
                await using var sourceStream = File.OpenRead(file.FullName);
                await using var destStream = File.Create(tempPath);

                var buffer = new byte[81920]; // 80KB缓冲区
                int bytesRead;

                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await destStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    progressState.ProcessedSize += bytesRead; // 更新已处理大小
                    UpdateProgress(progressBar, progressState.ProcessedSize, totalSize);
                }
            }
            catch (IOException ex)
            {
                LoggerHelper.Error($"IO错误: {ex.Message}，文件: {tempPath}");
                progressState.ProcessedSize += fileSize; // 失败文件也计入进度
                UpdateProgress(progressBar, progressState.ProcessedSize, totalSize);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"处理文件错误: {ex.Message}，文件: {tempPath}");
                progressState.ProcessedSize += fileSize;
                UpdateProgress(progressBar, progressState.ProcessedSize, totalSize);
            }
        }

        // 递归处理子目录
        foreach (var subDir in dir.GetDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();
            string tempPath = Path.Combine(destDirName, subDir.Name);
            await MergeDirectoryAsync(
                subDir.FullName,
                tempPath,
                progressBar,
                totalSize,
                progressState, // 传递同一个状态实例
                overwriteMFA,
                saveAnnouncement,
                cancellationToken);
        }
    }

    /// <summary>更新进度条（确保UI线程安全）</summary>
    private static void UpdateProgress(ProgressBar? progressBar, long processedSize, long totalSize)
    {
        if (progressBar == null || totalSize <= 0) return;

        double percentage = (double)processedSize / totalSize * 100;
        percentage = Math.Min(percentage, 100); // 限制最大进度为100%

        DispatcherHelper.RunOnMainThread(() => progressBar.Value = percentage);
    }

    /// <summary>格式化文件大小为易读格式（B/KB/MB）</summary>
    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F2} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F2} KB";
        return $"{bytes} B";
    }
}
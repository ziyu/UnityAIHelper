using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityLLMAPI.Utils.Json;

namespace UnityAIHelper.Editor.Tools.SystemTools
{
    /// <summary>
    /// 文件工具基类
    /// </summary>
    public abstract class FileToolBase : UnityToolBase
    {
        public override ToolType Type => ToolType.System;
        public override PermissionType RequiredPermissions => PermissionType.None;

        protected string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            // 处理相对路径
            if (!Path.IsPathRooted(path))
            {
                path=path.TrimStart('/');
                if (path.StartsWith("Assets"))
                {
                    path = path.Substring(7);
                }
                path = Path.Combine(Application.dataPath, path);
            }
            var finalPath= Path.GetFullPath(path);
            return finalPath;
        }
    }

    /// <summary>
    /// 读取文件工具
    /// </summary>
    public class ReadFileTool : FileToolBase
    {
        public override string Name => "ReadFile";
        public override string Description => "读取文件内容";
        public override PermissionType RequiredPermissions => PermissionType.Read;

        protected override void InitializeParameters()
        {
            AddParameter("path", typeof(string), "文件路径");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var path = NormalizePath(GetParameterValue<string>(parameters, "path"));
            
            if (!File.Exists(path))
            {
                throw new Exception($"File not found: {path}");
            }

            return await File.ReadAllTextAsync(path);
        }
    }

    /// <summary>
    /// 删除文件工具
    /// </summary>
    public class DeleteFileTool : FileToolBase
    {
        public override string Name => "DeleteFile";
        public override string Description => "删除文件";
        public override PermissionType RequiredPermissions => PermissionType.Delete;

        protected override void InitializeParameters()
        {
            AddParameter("path", typeof(string), "文件路径");
            AddParameter("recursive", typeof(bool), "是否递归删除目录", false, false);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var path = NormalizePath(GetParameterValue<string>(parameters, "path"));
            var recursive = GetParameterValueOrDefault(parameters, "recursive", false);

            if (Directory.Exists(path))
            {
                if (recursive)
                {
                    Directory.Delete(path, true);
                }
                else
                {
                    throw new Exception($"Path is a directory. Set recursive=true to delete directories: {path}");
                }
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                throw new Exception($"File or directory not found: {path}");
            }

            AssetDatabase.Refresh();
            return true;
        }
    }

    /// <summary>
    /// 复制文件工具
    /// </summary>
    public class CopyFileTool : FileToolBase
    {
        public override string Name => "CopyFile";
        public override string Description => "复制文件";
        public override PermissionType RequiredPermissions => PermissionType.Read | PermissionType.Write;

        protected override void InitializeParameters()
        {
            AddParameter("sourcePath", typeof(string), "源文件路径");
            AddParameter("targetPath", typeof(string), "目标文件路径");
            AddParameter("overwrite", typeof(bool), "是否覆盖已存在的文件", false, false);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var sourcePath = NormalizePath(GetParameterValue<string>(parameters, "sourcePath"));
            var targetPath = NormalizePath(GetParameterValue<string>(parameters, "targetPath"));
            var overwrite = GetParameterValueOrDefault(parameters, "overwrite", false);

            if (!File.Exists(sourcePath))
            {
                throw new Exception($"Source file not found: {sourcePath}");
            }

            if (File.Exists(targetPath) && !overwrite)
            {
                throw new Exception($"Target file already exists: {targetPath}");
            }

            var targetDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Copy(sourcePath, targetPath, overwrite);
            AssetDatabase.Refresh();
            
            return targetPath;
        }
    }

    /// <summary>
    /// 搜索文件工具
    /// </summary>
    public class SearchFilesTool : FileToolBase
    {
        public override string Name => "SearchFiles";
        public override string Description => "搜索文件";
        public override PermissionType RequiredPermissions => PermissionType.Read;

        protected override void InitializeParameters()
        {
            AddParameter("directory", typeof(string), "搜索目录");
            AddParameter("pattern", typeof(string), "搜索模式", false, "*.*");
            AddParameter("recursive", typeof(bool), "是否递归搜索", false, true);
            AddParameter("includeHidden", typeof(bool), "是否包含隐藏文件", false, false);
            AddParameter("page", typeof(int), "页码", false, 1);
            AddParameter("pageSize", typeof(int), "每页数量", false, 100);
            AddParameter("sortBy", typeof(string), "排序字段(name/size/lastModified)", false, "name");
            AddParameter("sortOrder", typeof(string), "排序顺序(asc/desc)", false, "asc");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var directory = NormalizePath(GetParameterValue<string>(parameters, "directory"));
            var pattern = GetParameterValueOrDefault(parameters, "pattern", "*.*");
            var recursive = GetParameterValueOrDefault(parameters, "recursive", true);
            var includeHidden = GetParameterValueOrDefault(parameters, "includeHidden", false);
            var page = GetParameterValueOrDefault(parameters, "page", 1);
            var pageSize = GetParameterValueOrDefault(parameters, "pageSize", 100);
            var sortBy = GetParameterValueOrDefault(parameters, "sortBy", "name");
            var sortOrder = GetParameterValueOrDefault(parameters, "sortOrder", "asc");

            if (!Directory.Exists(directory))
            {
                throw new Exception($"Directory not found: {directory}");
            }

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(directory, pattern, searchOption);

            if (!includeHidden)
            {
                files = files.Where(f => !new FileInfo(f).Attributes.HasFlag(FileAttributes.Hidden)).ToArray();
            }

            // Apply sorting
            files = sortBy.ToLower() switch
            {
                "size" => sortOrder == "asc" ?
                    files.OrderBy(f => new FileInfo(f).Length).ToArray() :
                    files.OrderByDescending(f => new FileInfo(f).Length).ToArray(),
                "lastmodified" => sortOrder == "asc" ?
                    files.OrderBy(f => new FileInfo(f).LastWriteTime).ToArray() :
                    files.OrderByDescending(f => new FileInfo(f).LastWriteTime).ToArray(),
                _ => sortOrder == "asc" ?
                    files.OrderBy(f => f).ToArray() :
                    files.OrderByDescending(f => f).ToArray()
            };

            // Apply pagination
            var totalCount = files.Length;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));
            
            var pagedFiles = files
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            var collapsedMessage = totalPages > 1 ?
                $"共 {totalPages} 页结果 (使用 page 参数查看更多)" :
                null;
            var result = new
            {
                files = pagedFiles,
                totalCount,
                totalPages,
                currentPage = page,
                pageSize,
                collapsedMessage
            };
            return result;
        }
    }

    /// <summary>
    /// 监视文件变化工具
    /// </summary>
    public class WatchFileTool : FileToolBase
    {
        private static readonly Dictionary<string, FileSystemWatcher> watchers = new();

        public override string Name => "WatchFile";
        public override string Description => "监视文件变化";
        public override PermissionType RequiredPermissions => PermissionType.Read;

        protected override void InitializeParameters()
        {
            AddParameter("path", typeof(string), "监视路径");
            AddParameter("filter", typeof(string), "文件过滤器", false, "*.*");
            AddParameter("action", typeof(string), "操作(start/stop)");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var path = NormalizePath(GetParameterValue<string>(parameters, "path"));
            var filter = GetParameterValueOrDefault(parameters, "filter", "*.*");
            var action = GetParameterValue<string>(parameters, "action").ToLower();

            switch (action)
            {
                case "start":
                    return StartWatching(path, filter);
                case "stop":
                    return StopWatching(path);
                default:
                    throw new ArgumentException($"Invalid action: {action}");
            }
        }

        private bool StartWatching(string path, string filter)
        {
            if (watchers.ContainsKey(path))
            {
                return false;
            }

            var watcher = new FileSystemWatcher(path, filter)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };

            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileRenamed;

            watchers[path] = watcher;
            return true;
        }

        private bool StopWatching(string path)
        {
            if (!watchers.TryGetValue(path, out var watcher))
            {
                return false;
            }

            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            watchers.Remove(path);
            return true;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Debug.Log($"File {e.ChangeType}: {e.FullPath}");
            AssetDatabase.Refresh();
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Debug.Log($"File renamed from {e.OldFullPath} to {e.FullPath}");
            AssetDatabase.Refresh();
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Profiling;
using VLib.Utility;
using Debug = UnityEngine.Debug;

namespace VLib
{
    public static class VUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetOrThrow<T>(this T reference, string exceptionMessageOnNull)
            where T : class
        {
            return reference ?? throw new NullReferenceException(exceptionMessageOnNull);
        }

        public static class IO
        {
            public static string PathNavUpDirToAbsolute(string path, int stepsUpward = 1)
            {
                for (int i = 0; i < stepsUpward; i++)
                    path = Path.Combine(path, @"../");

                return Path.GetFullPath(path);
            }

            /// <summary> Step up one directory </summary>
            public static string PathNavUpDir(string path)
            {
                if (string.IsNullOrEmpty(path))
                    return path;
                // Check first separator
                var lastSeparatorIndex = path.LastIndexOf(Path.DirectorySeparatorChar);
                if (lastSeparatorIndex < 0)
                {
                    // Oh no, check for alt separator
                    lastSeparatorIndex = path.LastIndexOf(Path.AltDirectorySeparatorChar);
                    if (lastSeparatorIndex < 0)
                        return path;
                }
                return path.Substring(0, lastSeparatorIndex);
            }

            /// <summary> Compatible with relative paths, will not navigate above root. </summary>
            public static string PathNavUpDir(string path, int stepsUpward)
            {
                for (int i = 0; i < stepsUpward; i++)
                    path = PathNavUpDir(path);
                return path;
            }
            
            public static bool PathsAreEqual(string path1, string path2)
            {
                var fullPath1 = Path.GetFullPath(path1);
                var fullPath2 = Path.GetFullPath(path2);

                var fullPath1EndsWithSeparator = fullPath1.EndsWith(Path.DirectorySeparatorChar);
                var fullPath2EndsWithSeparator = fullPath2.EndsWith(Path.DirectorySeparatorChar);
                
                // If one path ends with a separator, they both must, before the equality check
                if (fullPath1EndsWithSeparator != fullPath2EndsWithSeparator)
                {
                    if (!fullPath1EndsWithSeparator)
                        fullPath1 += Path.DirectorySeparatorChar;
                    else
                        fullPath2 += Path.DirectorySeparatorChar;
                }
                
                return fullPath1.Equals(fullPath2);
            }
            
            public static string ChangeAllDirSeparatorsTo(string path, char separator)
            {
                if (separator == Path.DirectorySeparatorChar)
                    path = path.Replace(Path.AltDirectorySeparatorChar, separator);
                else if (separator == Path.AltDirectorySeparatorChar)
                    path = path.Replace(Path.DirectorySeparatorChar, separator);
                else
                {
                    path = path.Replace(Path.DirectorySeparatorChar, separator);
                    path = path.Replace(Path.AltDirectorySeparatorChar, separator);
                }
                return path;
            }

            public static string PathCombineToAbsolute(string pathBase, string pathAddition)
            {
                return Path.GetFullPath(Path.Combine(pathBase, pathAddition));
            }

            public static string RelativizePathToUnity(string absolutePath, bool keepAssetsFolder)
            {
                if (absolutePath.IsNullOrWhitespace())
                    return absolutePath;
                if (!Path.IsPathRooted(absolutePath))
                {
                    Debug.LogError($"Path is not rooted: {absolutePath}");
                    return absolutePath;
                }
                
                // Find Assets
                var assetsIndex = absolutePath.IndexOf("Assets");
                if (assetsIndex < 0)
                {
                    Debug.LogError($"Path does not contain 'Assets': {absolutePath}");
                    return absolutePath;
                }
                
                return absolutePath.Substring(assetsIndex + (keepAssetsFolder ? 0 : "Assets/".Length));
            }

            /// <summary> Defensive method to ensure a directory exists. </summary>
            /// <returns> True if created or already exists. False if could not create the directory. </returns>
            public static bool TryCreateDirectory(string path, bool logOnFail = true, bool logOnAlreadyExists = false, string customAddErrorMsg = "")
            {
                if (path.IsNullOrWhitespace())
                {
                    if (logOnFail)
                        Debug.LogError("Input path is null or empty...");
                    return false;
                }

                if (Directory.Exists(path))
                {
                    if (logOnAlreadyExists)
                        Debug.LogError($"Directory already exists at '{path}'");
                    return true;
                }

                Directory.CreateDirectory(path);

                if (!Directory.Exists(path))
                {
                    if (logOnFail)
                    {
                        string msg = $"Could not create directory: {path}";
                        if (!customAddErrorMsg.IsNullOrWhitespace())
                            msg += $" - Msg: {customAddErrorMsg}";
                        Debug.LogError(msg);
                    }
                    return false;
                }
                
                return true;
            }
            
            public static bool TryRemoveDirectory(string path, bool logOnFail = true, bool logOnNoDir = false, string customAddErrorMsg = "", bool returnOnNoDir = false)
            {
                try
                {
                    if (Directory.Exists(path))
                        Directory.Delete(path, true);
                    else
                    {
                        if (logOnNoDir)
                            Debug.LogError($"Cannot remove directory that does not exist: {path}");
                        return returnOnNoDir;
                    }
                    if (Directory.Exists(path))
                    {
                        if (logOnFail)
                        {
                            string msg = $"Could not remove directory: {path}";
                            if (!customAddErrorMsg.IsNullOrWhitespace())
                                msg += $" - Msg: {customAddErrorMsg}";
                            Debug.LogError(msg);
                        }
                        return false;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception while trying to remove directory...");
                    Debug.LogException(e);
                }

                return false;
            }

            public static bool TryMoveDirectory(string path, string newPath, bool deleteExistingAtNewPath = false, bool logOnFail = true, bool logToCloud = true)
            {
                if (path.IsNullOrWhitespace())
                {
                    LogError("MoveDir: Path is invalid!");
                    return false;
                }
                if (newPath.IsNullOrWhitespace())
                {
                    LogError("MoveDir: New Path is invalid!");
                    return false;
                }
                if (path.Equals(newPath))
                {
                    //LogError("MoveDir: Paths are the same!");
                    Debug.LogWarning("MoveDir: Paths are the same!");
                    return true; // We can't move anything, but technically the goal is achieved
                }

                if (!Directory.Exists(path))
                {
                    LogError($"MoveDir: Directory does not exist at path {path}!");
                    return false;
                }

                var newPathContainingDir = Path.GetDirectoryName(newPath);
                string retainedExistingDirTempPath = null;
                
                if (Directory.Exists(newPath))
                {
                    if (!deleteExistingAtNewPath)
                    {
                        LogError("MoveDir: Target path already has a folder and 'deleteExistingAtNewPath' is false!");
                        return false;
                    }

                    // Remove existing
                    // Move it to a temp path, delete afterward if move is successful
                    int attempts = 16;
                    while (--attempts >= 0)
                    {
                        var tempPath = Path.Combine(newPathContainingDir, Path.GetRandomFileName());
                        if (!Directory.Exists(tempPath))
                        {
                            retainedExistingDirTempPath = tempPath;
                            break;
                        }
                    }
                    if (retainedExistingDirTempPath.IsNullOrWhitespace())
                    {
                        LogError("MoveDir: Could not generate a temp path for existing directory!");
                        return false;
                    }
                    Directory.Move(newPath, retainedExistingDirTempPath);
                }
                
                // Perform actual move
                try
                {
                    Directory.Move(path, newPath);
                }
                catch (Exception e)
                {
                    LogError($"MoveDir: Exception while trying to move directory:\n{e.Message}");
                    return false;
                }

                if (!AssertDirectoryExists(newPath))
                {
                    RestoreExistingDir();
                    return false;
                }
                
                FinalDeletePriorExistingDirAtTempLocation();
                return true;
                
                void LogError(string msg)
                {
                    if (logOnFail)
                    {
                        if (logToCloud)
                            Debug.LogException(new UnityException(msg));
                        else
                            Debug.LogError(msg);
                    }
                }
                
                void RestoreExistingDir()
                {
                    if (retainedExistingDirTempPath.IsNullOrWhitespace())
                        return;
                    if (Directory.Exists(newPath))
                    {
                        LogError($"Cannot restore previous directory at path {retainedExistingDirTempPath} because something else exists at the new path now!");
                        return;
                    }
                    
                    Directory.Move(retainedExistingDirTempPath, newPath);
                }

                void FinalDeletePriorExistingDirAtTempLocation()
                {
                    if (retainedExistingDirTempPath.IsNullOrWhitespace())
                        return;
                    if (!Directory.Exists(retainedExistingDirTempPath))
                        return;
                    if (!TryRemoveDirectory(retainedExistingDirTempPath))
                        LogError($"Could not remove retained existing directory at path {retainedExistingDirTempPath}!");
                }
            }
            
            public static bool TryRemoveFile(string path, bool logOnFail = true, bool logOnNoFile = false, string customAddErrorMsg = "")
            {
                if (File.Exists(path))
                    File.Delete(path);
                else
                {
                    if (logOnNoFile)
                        Debug.LogError($"Cannot remove file that does not exist: {path}");
                    return true;
                }
                if (File.Exists(path))
                {
                    if (logOnFail)
                    {
                        string msg = $"Could not remove file: {path}";
                        if (!customAddErrorMsg.IsNullOrWhitespace())
                            msg += $" - Msg: {customAddErrorMsg}";
                        Debug.LogError(msg);
                    }
                    return false;
                }

                return true;
            }

            public static bool AssertFileExists(string path, [CallerMemberName]string caller = "")
            {
                if (File.Exists(path))
                    return true;
                
                Debug.LogException(new UnityException($"{caller}: File MUST exist, but does not, at path: {path}"));
                return false;
            }

            public static bool AssertDirectoryExists(string path, [CallerMemberName] string caller = "")
            {
                if (Directory.Exists(path))
                    return true;

                Debug.LogException(new UnityException($"{caller}: Directory MUST exist, but does not, at path: {path}"));
                return false;
            }
        }

        [Conditional("UNITY_EDITOR")]
        public static void CheckIsPlayMode()
        {
            if (!VApplicationMonitor.IsPlayingSafe)
                throw new InvalidOperationException("This method can only be called in play mode.");
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void CheckIsEditMode()
        {
            if (VApplicationMonitor.IsPlayingSafe)
                throw new InvalidOperationException("This method can only be called in edit mode.");
        }
    }
}
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Sirenix.Utilities;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace VLib
{
    public static class VUtil
    {
        public static T GetOrThrow<T>(this T reference, string exceptionMessageOnNull)
        {
            return reference != null ? reference : throw new NullReferenceException(exceptionMessageOnNull);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BeginProfileSampleAuto<T>(this T obj, [CallerMemberName] string methodName = "")
        {
            Profiler.BeginSample($"{obj.GetType().Name}.{methodName}");
        }

        public static class IO
        {
            public static string PathNavUpDir(string path, int stepsUpward = 1)
            {
                for (int i = 0; i < stepsUpward; i++)
                    path = Path.Combine(path, @"../");

                return Path.GetFullPath(path);
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

            public static string PathCombineToAbsolute(string pathBase, string pathAddition)
            {
                return Path.GetFullPath(Path.Combine(pathBase, pathAddition));
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

            public static bool TryMoveDirectory(string path, string newPath, bool deleteExistingAtNewPath = false, bool logOnFail = true)
            {
                if (path.IsNullOrWhitespace())
                {
                    if (logOnFail)
                        Debug.LogError("MoveDir: Path is invalid!");
                    return false;
                }
                if (newPath.IsNullOrWhitespace())
                {
                    if (logOnFail)
                        Debug.LogError("MoveDir: New Path is invalid!");
                    return false;
                }
                if (path.Equals(newPath))
                {
                    if (logOnFail)
                        Debug.LogError("MoveDir: Paths are the same!");
                    return false;
                }

                if (!Directory.Exists(path))
                {
                    if (logOnFail)
                        Debug.LogError($"MoveDir: Directory does not exist at path {path}!");
                    return false;
                }

                if (Directory.Exists(newPath))
                {
                    if (!deleteExistingAtNewPath)
                    {
                        if (logOnFail)
                            Debug.LogError("MoveDir: Target path already has a folder and 'deleteExistingAtNewPath' is false!");
                        return false;
                    }
                    else
                    {
                        if (!TryRemoveDirectory(newPath))
                            return false;
                    }
                }
                
                Directory.Move(path, newPath);

                return AssertDirectoryExists(newPath);
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
                
                Debug.LogError($"{caller}: File MUST exist, but does not, at path: {path}");
                return false;
            }

            public static bool AssertDirectoryExists(string path, [CallerMemberName] string caller = "")
            {
                if (Directory.Exists(path))
                    return true;

                Debug.LogError($"{caller}: Directory MUST exist, but does not, at path: {path}");
                return false;
            }
        }
    }
}
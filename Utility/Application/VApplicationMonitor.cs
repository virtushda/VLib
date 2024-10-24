using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VLib.Utility
{
    /// <summary> A system that reacts to all scenes being unloaded </summary>
    public static class VApplicationMonitor
    {
        public static event Action OnQuitAndAllScenesUnloaded;
        static bool applicationQuittingHasCalled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void StaticInit()
        {
            Debug.Log("VApplicationMonitor.StaticInit called");
            
            applicationQuittingHasCalled = false;
            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;
            
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= EditorCleanup;
            EditorApplication.playModeStateChanged += EditorCleanup;
#endif
        }
        
        static void OnSceneUnloaded(Scene scene)
        {
            if (SceneManager.loadedSceneCount == 0 && applicationQuittingHasCalled)
            {
                Debug.Log("VApplicationMonitor.OnQuitAndAllScenesUnloaded invoked");
                OnQuitAndAllScenesUnloaded?.Invoke();
            }
        }
        
        static void OnQuit() => applicationQuittingHasCalled = true;
        
#if UNITY_EDITOR
        static void EditorCleanup(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange != PlayModeStateChange.EnteredEditMode)
                return;
            
            Debug.Log("VApplicationMonitor.EditorCleanup called");
            Application.quitting -= OnQuit;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            OnQuitAndAllScenesUnloaded = null;
        }
#endif
    }
}
﻿using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace VLib.Utility
{
    /// <summary> A system that reacts to all scenes being unloaded </summary>
    public static class VApplicationMonitor
    {
        /// <summary> Called when OnQuit has called and the last scene is unloaded. <br/>
        /// Also invoked by the editor when you exit play mode. <br/> 
        /// All events are cleared on invocation! </summary>
        public static OnQuitAndAllScenesUnloadedEventRelay OnQuitAndAllScenesUnloaded = new();
        static readonly VSortedList<SortedAction> OnQuitAndAllScenesUnloadedActions = new(16);
        static bool onQuitHasCalled;

        static bool applicationPlaying;

        /// <summary> A thread-safe way to check if the application is playing, I cannot believe this is not thread-safe already... </summary>
        public static bool IsPlayingSafe => applicationPlaying;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StaticInit()
        {
            Debug.Log("VApplicationMonitor.StaticInit called");
            Assert.IsNotNull(OnQuitAndAllScenesUnloaded);
            
            onQuitHasCalled = false;
            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;
            
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= EditorCleanup;
            EditorApplication.playModeStateChanged += EditorCleanup;
#endif

            applicationPlaying = true;
        }
        
        static void OnSceneUnloaded(Scene scene)
        {
            if (SceneManager.loadedSceneCount == 0 && onQuitHasCalled)
            {
                Debug.Log("VApplicationMonitor.OnQuitAndAllScenesUnloadedActions invoked");
                InvokeAll();
            }
        }
        
        static void OnQuit()
        {
            onQuitHasCalled = true;
            OnSceneUnloaded(default);
        }

        static void InvokeAll()
        {
            Debug.Log("Invoking OnQuitAndAllScenesUnloadedActions during editor cleanup");
            OnQuitAndAllScenesUnloadedActions.InvokeAll();
            OnQuitAndAllScenesUnloadedActions.Clear();
        }
        
#if UNITY_EDITOR
        static void EditorCleanup(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange is not PlayModeStateChange.EnteredEditMode)
                return;
            
            Debug.Log("VApplicationMonitor.EditorCleanup called");
            
            applicationPlaying = false;
            
            Application.quitting -= OnQuit;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            
            // Call actions during editor cleanup if not already called.
            InvokeAll();
        }
#endif

        public class OnQuitAndAllScenesUnloadedEventRelay
        {
            /// <summary> Returns the number of events after addition </summary>
            public static OnQuitAndAllScenesUnloadedEventRelay operator +(OnQuitAndAllScenesUnloadedEventRelay _, SortedAction action)
            {
                AddQuitEvent(action);
                return OnQuitAndAllScenesUnloaded;
            }
            public static OnQuitAndAllScenesUnloadedEventRelay operator -(OnQuitAndAllScenesUnloadedEventRelay _, SortedAction action)
            {
                RemoveFirstEventBySignature(action);
                return OnQuitAndAllScenesUnloaded;
            }
        }
        
        public static void AddQuitEvent(SortedAction action) => OnQuitAndAllScenesUnloadedActions.Add(action);

        public static void RemoveEventByReference(SortedAction action) => OnQuitAndAllScenesUnloadedActions.Remove(action);

        public static void RemoveFirstEventBySignature(SortedAction action)
        {
            if (OnQuitAndAllScenesUnloadedActions.Count < 1)
                return;
            int matchingIndex = OnQuitAndAllScenesUnloadedActions.IndexOfComparableMatch(action);
            if (matchingIndex >= 0)
                OnQuitAndAllScenesUnloadedActions.RemoveAt(matchingIndex);
        }
        
        public static void RemoveAllEventsWithSignature(SortedAction action)
        {
            if (OnQuitAndAllScenesUnloadedActions.Count < 1)
                return;
            int matchingIndex = OnQuitAndAllScenesUnloadedActions.IndexOfComparableMatch(action);
            while (matchingIndex >= 0 && OnQuitAndAllScenesUnloadedActions.Count > 0)
            {
                OnQuitAndAllScenesUnloadedActions.RemoveAt(matchingIndex);
                matchingIndex = OnQuitAndAllScenesUnloadedActions.IndexOfComparableMatch(action);
            }
        }
    }
}
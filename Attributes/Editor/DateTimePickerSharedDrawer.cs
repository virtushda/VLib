#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace VLib.Attributes
{
    public static class DateTimePickerSharedDrawer
    {
        // Static state for foldouts, etc.
        static HashSet<string> expandedKeys = new();
        internal static bool edited;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearOnDomainReload() => expandedKeys.Clear();

        static DateTimePickerSharedDrawer()
        {
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
                    expandedKeys.Clear();
            };
        }

        // Shared drawer logic for DateTime UI
        public static DateTime DrawDateTimePicker(DateTime value, GUIContent label, string key = null)
        {
            edited = false;
            // Use provided key (typically Odin Property.Path) or fallback to label string if null
            key ??= label?.text ?? "DateTime";
            string titleString = value.ToString("yyyy-MM-dd HH:mm:ss");

            if (value.Kind is DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
                edited = true;
            }
            string kindText = value.Kind == DateTimeKind.Utc ? "UTC" : "local";

            EditorGUILayout.BeginVertical(GUI.skin.box);
            bool expanded = expandedKeys.Contains(key);
            bool newExpanded = SirenixEditorGUI.Foldout(expanded, $"{label?.text ?? "DateTime"}: {titleString} ({kindText})");
            if (newExpanded) expandedKeys.Add(key); else expandedKeys.Remove(key);

            if (newExpanded)
            {
                EditorGUI.indentLevel++;
                value = DrawIncrementRow("Year", value.Year, 1, 9999, value,
                    (dt, v) => dt.AddYears(v - dt.Year), new[] { 10, 5, 1 }, (dt, inc) => dt.AddYears(inc),
                    dt => new DateTime(1, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind), _ => DateTime.Now.Year);

                value = DrawIncrementRow("Month", value.Month, 1, 12, value,
                    (dt, v) => {
                        int maxDay = DateTime.DaysInMonth(dt.Year, v);
                        int newDay = Mathf.Clamp(dt.Day, 1, maxDay);
                        return new DateTime(dt.Year, v, newDay, dt.Hour, dt.Minute, dt.Second, dt.Kind);
                    }, new[] { 6, 3, 1 }, (dt, inc) => dt.AddMonths(inc),
                    dt => new DateTime(dt.Year, 1, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind), _ => DateTime.Now.Month);

                value = DrawIncrementRow("Day", value.Day, 1, DateTime.DaysInMonth(value.Year, value.Month), value,
                    (dt, v) => new DateTime(dt.Year, dt.Month, v, dt.Hour, dt.Minute, dt.Second, dt.Kind),
                    new[] { 14, 7, 1 }, (dt, inc) => dt.AddDays(inc),
                    dt => new DateTime(dt.Year, dt.Month, 1, dt.Hour, dt.Minute, dt.Second, dt.Kind), _ => DateTime.Now.Day);

                value = DrawIncrementRow("Hour", value.Hour, 0, 23, value,
                    (dt, v) => new DateTime(dt.Year, dt.Month, dt.Day, v, dt.Minute, dt.Second, dt.Kind),
                    new[] { 8, 4, 1 }, (dt, inc) => dt.AddHours(inc),
                    dt => new DateTime(dt.Year, dt.Month, dt.Day, 0, dt.Minute, dt.Second, dt.Kind), _ => DateTime.Now.Hour);

                value = DrawIncrementRow("Minute", value.Minute, 0, 59, value,
                    (dt, v) => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, v, dt.Second, dt.Kind),
                    new[] { 15, 5, 1 }, (dt, inc) => dt.AddMinutes(inc),
                    dt => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, dt.Second, dt.Kind), _ => DateTime.Now.Minute);

                value = DrawIncrementRow("Second", value.Second, 0, 59, value,
                    (dt, v) => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, v, dt.Kind),
                    new[] { 15, 5, 1 }, (dt, inc) => dt.AddSeconds(inc),
                    dt => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind), _ => DateTime.Now.Second);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Now")) { value = DateTime.Now; edited = true; }
                if (GUILayout.Button("To UTC") && value.Kind != DateTimeKind.Utc) { value = value.ToUniversalTime(); edited = true; }
                if (GUILayout.Button("As UTC")) { value = DateTime.SpecifyKind(value, DateTimeKind.Utc); edited = true; }
                if (GUILayout.Button("To Local") && value.Kind != DateTimeKind.Local) { value = value.ToLocalTime(); edited = true; }
                if (GUILayout.Button("As Local")) { value = DateTime.SpecifyKind(value, DateTimeKind.Local); edited = true; }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            return value;
        }

        static DateTime DrawIncrementRow(
            string label, int value, int min, int max, DateTime current,
            Func<DateTime, int, DateTime> setValueFunc,
            int[] increments, Func<DateTime, int, DateTime> addFunc,
            Func<DateTime, DateTime> zeroFunc,
            Func<DateTime, int> nowFunc)
        {
            float labelWidth = 90f, btnWidth = 32f, valueWidth = 90f, sideBtnWidth = 55f, pad = 2f;
            float indent = EditorGUI.indentLevel * 15f, rowHeight = EditorGUIUtility.singleLineHeight + 2f;
            Rect rowRect = EditorGUILayout.GetControlRect(false, rowHeight);
            var labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            EditorGUI.LabelField(labelRect, label);
            float x = rowRect.x + labelWidth + indent + pad;
            for (int i = 0; i < increments.Length; ++i)
            {
                var minusRect = new Rect(x, rowRect.y, btnWidth, rowHeight);
                if (GUI.Button(minusRect, $"−{increments[i]}")) { current = addFunc(current, -increments[i]); edited = true; }
                x += btnWidth + pad;
            }
            var valueRect = new Rect(x, rowRect.y, valueWidth, rowHeight);
            int newValue = EditorGUI.IntField(valueRect, GUIContent.none, value);
            newValue = Mathf.Clamp(newValue, min, max);
            if (newValue != value) { current = setValueFunc(current, newValue); edited = true; }
            x += valueWidth + pad;
            for (int i = increments.Length - 1; i >= 0; --i)
            {
                var plusRect = new Rect(x, rowRect.y, btnWidth, rowHeight);
                if (GUI.Button(plusRect, $"+{increments[i]}")) { current = addFunc(current, increments[i]); edited = true; }
                x += btnWidth + pad;
            }
            var zeroRect = new Rect(x, rowRect.y, sideBtnWidth, rowHeight);
            if (GUI.Button(zeroRect, "To Zero")) { current = zeroFunc(current); edited = true; }
            x += sideBtnWidth + pad;
            var nowRect = new Rect(x, rowRect.y, sideBtnWidth, rowHeight);
            if (GUI.Button(nowRect, "Now")) { current = setValueFunc(current, nowFunc(current)); edited = true; }
            return current;
        }
    }
}
#endif
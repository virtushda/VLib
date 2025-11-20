#if UNITY_EDITOR
using System;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace VLib.Attributes
{
    public class DateTimeSerializablePickerDrawer : OdinValueDrawer<DateTimeSerializable>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var value = DateTimePickerSharedDrawer.DrawDateTimePicker(ValueEntry.SmartValue, label, Property?.Path);

            if (DateTimePickerSharedDrawer.edited)
                ValueEntry.Values.ForceSetValue(0, value);
        }
    }
}
#endif
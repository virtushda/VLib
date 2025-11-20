#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace VLib.Attributes
{
    public class DateTimeSpanDrawer : OdinValueDrawer<DateTimeSpan>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            bool edited = false;
            var startValue = DateTimePickerSharedDrawer.DrawDateTimePicker(ValueEntry.SmartValue.Start, label);
            if (DateTimePickerSharedDrawer.edited)
                edited = true;
            var endValue = DateTimePickerSharedDrawer.DrawDateTimePicker(ValueEntry.SmartValue.End, label);
            if (DateTimePickerSharedDrawer.edited)
                edited = true;

            if (edited)
                ValueEntry.Values.ForceSetValue(0, new DateTimeSpan(startValue, endValue));
        }
    }
}
#endif
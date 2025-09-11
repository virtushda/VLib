#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VLib;

// Property drawer for the custom attribute
[CustomPropertyDrawer(typeof(QuaternionEulerAttribute))]
public class QuaternionEulerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.Quaternion)
        {
            // Convert quaternion to euler angles
            Quaternion quat = property.quaternionValue;
            Vector3 euler = quat.eulerAngles;

            // Draw Vector3 field for euler angles
            euler = EditorGUI.Vector3Field(position, label, euler);

            // Convert back to quaternion and assign
            property.quaternionValue = Quaternion.Euler(euler);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use [QuaternionEuler] with Quaternion.");
        }
    }
}
#endif
using System;
using AstraSyndicate.Campaign;
using UnityEditor;
using UnityEngine;

namespace AstraSyndicate.EditorTools
{
    /// <summary>Dropdown of built-in objective types; unknown values fall back to a text field for custom types.</summary>
    [CustomPropertyDrawer(typeof(ObjectiveTypeAttribute))]
    public sealed class ObjectiveTypeDrawer : PropertyDrawer
    {
        private const float CustomPopupWidth = 90f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            string[] builtIn = ObjectiveTypes.BuiltIn;
            var options = new GUIContent[builtIn.Length + 1];
            for (int i = 0; i < builtIn.Length; i++) options[i] = new GUIContent(builtIn[i]);
            options[builtIn.Length] = new GUIContent("Custom...");

            EditorGUI.BeginProperty(position, label, property);
            int index = Array.IndexOf(builtIn, property.stringValue);
            if (index >= 0)
            {
                int selected = EditorGUI.Popup(position, label, index, options);
                if (selected != index) property.stringValue = selected < builtIn.Length ? builtIn[selected] : string.Empty;
            }
            else
            {
                var fieldRect = new Rect(position.x, position.y, position.width - CustomPopupWidth - 4f, position.height);
                var popupRect = new Rect(position.xMax - CustomPopupWidth, position.y, CustomPopupWidth, position.height);
                property.stringValue = EditorGUI.TextField(fieldRect, label, property.stringValue);

                int selected = EditorGUI.Popup(popupRect, builtIn.Length, options);
                if (selected < builtIn.Length) property.stringValue = builtIn[selected];
            }
            EditorGUI.EndProperty();
        }
    }
}

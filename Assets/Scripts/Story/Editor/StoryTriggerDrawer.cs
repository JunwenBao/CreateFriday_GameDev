using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Story
{
    /// <summary>Type dropdown for [SerializeReference] StoryTrigger fields, which Unity 2022 lacks.</summary>
    [CustomPropertyDrawer(typeof(StoryTrigger), true)]
    public class StoryTriggerDrawer : PropertyDrawer
    {
        private static Type[] _types;
        private static string[] _names;

        private static void EnsureTypes()
        {
            if (_types != null)
                return;

            _types = TypeCache.GetTypesDerivedFrom<StoryTrigger>()
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Name)
                .ToArray();
            _names = new[] { "None (manual)" }
                .Concat(_types.Select(t => ObjectNames.NicifyVariableName(t.Name)))
                .ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.managedReferenceValue == null || !property.isExpanded)
                return height;

            foreach (var child in GetChildren(property))
                height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureTypes();
            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var labelRect = new Rect(line.x, line.y, EditorGUIUtility.labelWidth, line.height);
            var popupRect = new Rect(labelRect.xMax + 2f, line.y, line.width - labelRect.width - 2f, line.height);

            var currentType = property.managedReferenceValue?.GetType();
            if (currentType != null)
                property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
            else
                EditorGUI.LabelField(labelRect, label);

            int currentIndex = currentType == null ? 0 : Array.IndexOf(_types, currentType) + 1;
            int newIndex = EditorGUI.Popup(popupRect, currentIndex, _names);

            if (newIndex != currentIndex)
            {
                property.managedReferenceValue = newIndex == 0 ? null : Activator.CreateInstance(_types[newIndex - 1]);
                property.isExpanded = true;
            }
            else if (currentType != null && property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
                foreach (var child in GetChildren(property))
                {
                    float h = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), child, true);
                    y += h + EditorGUIUtility.standardVerticalSpacing;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property)
        {
            var it = property.Copy();
            var end = it.GetEndProperty();
            if (!it.NextVisible(true))
                yield break;

            while (!SerializedProperty.EqualContents(it, end))
            {
                yield return it.Copy();
                if (!it.NextVisible(false))
                    yield break;
            }
        }
    }
}

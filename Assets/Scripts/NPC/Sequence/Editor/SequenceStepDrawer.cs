using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// NPCSequence 步骤列表的类型下拉框（Unity 2022 不自带 [SerializeReference] 的类型选择）
[CustomPropertyDrawer(typeof(SequenceStep), true)]
public class SequenceStepDrawer : PropertyDrawer
{
    private static Type[] types;
    private static string[] names;

    private static void EnsureTypes()
    {
        if (types != null) return;

        types = TypeCache.GetTypesDerivedFrom<SequenceStep>()
            .Where(t => !t.IsAbstract && !t.IsGenericType)
            .OrderBy(t => t.Name)
            .ToArray();
        names = new[] { "None" }
            .Concat(types.Select(t => ObjectNames.NicifyVariableName(t.Name)))
            .ToArray();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (property.managedReferenceValue == null || !property.isExpanded) return height;

        foreach (SerializedProperty child in GetChildren(property))
            height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureTypes();
        label = new GUIContent(StepLabel(property, label));
        EditorGUI.BeginProperty(position, label, property);

        var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var labelRect = new Rect(line.x, line.y, EditorGUIUtility.labelWidth, line.height);
        var popupRect = new Rect(labelRect.xMax + 2f, line.y, line.width - labelRect.width - 2f, line.height);

        Type currentType = property.managedReferenceValue?.GetType();
        if (currentType != null)
            property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
        else
            EditorGUI.LabelField(labelRect, label);

        int currentIndex = currentType == null ? 0 : Array.IndexOf(types, currentType) + 1;
        int newIndex = EditorGUI.Popup(popupRect, currentIndex, names);

        if (newIndex != currentIndex)
        {
            property.managedReferenceValue = newIndex == 0 ? null : Activator.CreateInstance(types[newIndex - 1]);
            property.isExpanded = true;
        }
        else if (currentType != null && property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
            foreach (SerializedProperty child in GetChildren(property))
            {
                float h = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), child, true);
                y += h + EditorGUIUtility.standardVerticalSpacing;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    // 列表元素显示为 "Step 1"、"Step 2"……
    private static string StepLabel(SerializedProperty property, GUIContent label)
    {
        string path = property.propertyPath;
        if (!path.EndsWith("]")) return label.text;

        int open = path.LastIndexOf('[');
        return int.TryParse(path.Substring(open + 1, path.Length - open - 2), out int index)
            ? $"Step {index + 1}"
            : label.text;
    }

    private static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property)
    {
        SerializedProperty it = property.Copy();
        SerializedProperty end = it.GetEndProperty();
        if (!it.NextVisible(true)) yield break;

        while (!SerializedProperty.EqualContents(it, end))
        {
            yield return it.Copy();
            if (!it.NextVisible(false)) yield break;
        }
    }
}

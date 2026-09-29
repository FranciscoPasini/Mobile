using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(WeaponClassAttribute))]
public class WeaponClassDrawer : PropertyDrawer
{
    private static readonly HashSet<string> HiddenBaseFields = new HashSet<string>
    {
        "weaponData",
        "weaponName",
        "weaponDescription",
        "weaponIcon",
        "weaponBaseDamage",
        "weaponBaseRange",
        "weaponBaseFireRate",
        "weaponBaseBulletSpeed",
        "weaponBaseAmmo",
        "weaponCurrentAmmo",
        "bulletData",
        "isDefaultWeapon",
    };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Type[] types = GetWeaponTypes();
        string[] names = new string[types.Length];
        for (int i = 0; i < types.Length; i++) names[i] = types[i].Name;

        Type currentType = property.managedReferenceValue != null
            ? property.managedReferenceValue.GetType()
            : typeof(Base_Weapon);

        int index = Array.FindIndex(types, type => type == currentType);
        if (index < 0) index = 0;

        Rect popupRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        int newIndex = EditorGUI.Popup(popupRect, label.text, index, names);
        Type selectedType = types[newIndex];

        if (property.managedReferenceValue == null || property.managedReferenceValue.GetType() != selectedType)
        {
            property.managedReferenceValue = Activator.CreateInstance(selectedType);
        }

        DrawExtraFields(position, property);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        ForEachExtraField(property, child =>
        {
            height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
        });
        return height;
    }

    private static void DrawExtraFields(Rect position, SerializedProperty property)
    {
        float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.indentLevel++;
        ForEachExtraField(property, child =>
        {
            float h = EditorGUI.GetPropertyHeight(child, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), child, true);
            y += h + EditorGUIUtility.standardVerticalSpacing;
        });
        EditorGUI.indentLevel--;
    }

    private static void ForEachExtraField(SerializedProperty property, Action<SerializedProperty> onField)
    {
        SerializedProperty end = property.GetEndProperty();
        SerializedProperty child = property.Copy();
        if (!child.NextVisible(true)) return;

        while (!SerializedProperty.EqualContents(child, end))
        {
            if (child.depth == property.depth + 1 && !HiddenBaseFields.Contains(child.name))
            {
                onField(child);
            }

            if (!child.NextVisible(false)) break;
        }
    }

    private static Type[] GetWeaponTypes()
    {
        var list = new List<Type> { typeof(Base_Weapon) };
        foreach (Type type in TypeCache.GetTypesDerivedFrom<Base_Weapon>())
        {
            if (!type.IsAbstract && !type.IsGenericType) list.Add(type);
        }
        if (list.Count > 1)
        {
            list.Sort(1, list.Count - 1, Comparer<Type>.Create((a, b) => string.CompareOrdinal(a.Name, b.Name)));
        }
        return list.ToArray();
    }
}

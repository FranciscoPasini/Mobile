using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(WeaponClassAttribute))]
public class WeaponClassDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Type[] types = GetWeaponTypes();
        string[] names = types.Select(type => type.Name).ToArray();

        Type currentType = property.managedReferenceValue != null
            ? property.managedReferenceValue.GetType()
            : typeof(Base_Weapon);

        int index = Array.FindIndex(types, type => type == currentType);
        if (index < 0)
        {
            index = 0;
        }

        int newIndex = EditorGUI.Popup(position, label.text, index, names);
        Type selectedType = types[newIndex];

        if (property.managedReferenceValue == null || property.managedReferenceValue.GetType() != selectedType)
        {
            property.managedReferenceValue = Activator.CreateInstance(selectedType);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    private static Type[] GetWeaponTypes()
    {
        return TypeCache.GetTypesDerivedFrom<Base_Weapon>()
            .Where(type => !type.IsAbstract && !type.IsGenericType)
            .OrderBy(type => type.Name)
            .Prepend(typeof(Base_Weapon))
            .ToArray();
    }
}

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

[CustomPropertyDrawer(typeof(RangeAndSetPropertyAttribute))]
public class RangeAndSetPropertyAttributeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
        EditorGUI.BeginChangeCheck();

        RangeAndSetPropertyAttribute range = attribute as RangeAndSetPropertyAttribute;
        if (property.propertyType == SerializedPropertyType.Float)
        {
            EditorGUI.Slider(position, property, range.min, range.max);
        }
        else if (property.propertyType == SerializedPropertyType.Integer)
        {
            EditorGUI.IntSlider(position, property, (int)range.min, (int)range.max);
        }

        RangeAndSetPropertyAttribute setProperty = attribute as RangeAndSetPropertyAttribute;
        if (EditorGUI.EndChangeCheck())
        {
            setProperty.IsDirty = true;
        }
        else if (setProperty.IsDirty)
        {
            object parent = GetParentObjectOfProperty(property.propertyPath, property.serializedObject.targetObject);
            Type type = parent.GetType();
            PropertyInfo pi = type.GetProperty(setProperty.Name);
            if (pi == null)
            {
                Debug.LogError("Invalid property name: " + setProperty.Name + "\nCheck your [SetProperty] attribute");
            }
            else
            {
                pi.SetValue(parent, fieldInfo.GetValue(parent), null);
            }
            setProperty.IsDirty = false;
        }
    }

    private object GetParentObjectOfProperty(string path, object obj)
	{
		string[] fields = path.Split('.');

		if (fields.Length == 1)
		{
			return obj;
		}

		FieldInfo fi = obj.GetType().GetField(fields[0], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		obj = fi.GetValue(obj);

		return GetParentObjectOfProperty(string.Join(".", fields, 1, fields.Length - 1), obj);
	}
}

using UnityEngine;
using System.Collections;
using System;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class RangeAndSetPropertyAttribute : PropertyAttribute
{
	public string Name { get; private set; }
	public bool IsDirty { get; set; }

    public readonly float min;
    public readonly float max;

    public RangeAndSetPropertyAttribute(string name, float min, float max)
    {
        this.Name = name;
        this.min = min;
        this.max = max;
    }
}
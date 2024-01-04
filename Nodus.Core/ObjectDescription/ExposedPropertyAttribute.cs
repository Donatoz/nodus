using System;

namespace Nodus.Core.ObjectDescription;

[AttributeUsage(AttributeTargets.Property)]
public class ExposedPropertyAttribute : Attribute
{
    public string? Description { get; }

    public ExposedPropertyAttribute(string? description = null)
    {
        Description = description;
    }
}
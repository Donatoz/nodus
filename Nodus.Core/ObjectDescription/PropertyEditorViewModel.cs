using System;

namespace Nodus.Core.ObjectDescription;

public class PropertyEditorViewModel
{
    public string PropertyName { get; }
    public Type PropertyType { get; }
    public string Description { get; }
    public PropertyEditorContentViewModel Content { get; }

    public PropertyEditorViewModel(string propertyName, Type propertyType, string description, IPropertyBinding propertyBinding)
    {
        PropertyName = propertyName;
        Description = description;
        PropertyType = propertyType;
        Content = new StringEditorContentViewModel(propertyBinding.SetValue, propertyBinding.GetValue);
    }
}
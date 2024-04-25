using System;
using System.Collections.Generic;
using System.Linq;

namespace Nodus.Core.ObjectDescription;

public interface IPropertyEditorContentFactory
{
    PropertyEditorContentViewModel CreateViewModel(Type propertyType, IPropertyBinding binding);

    public static IPropertyEditorContentFactory Default { get; } = new PropertyEditorContentFactory();
}

public class PropertyEditorContentFactory : IPropertyEditorContentFactory
{
    protected delegate PropertyEditorContentViewModel SubFactory(Type propType, Action<object?> setter, Func<object?> getter);
    
    protected IDictionary<Type, SubFactory> SubFactories { get; private set; }

    public PropertyEditorContentFactory()
    {
        SubFactories = new Dictionary<Type, SubFactory>();

        SubFactories[typeof(string)] = (t, s, g) => new StringEditorContentViewModel(t, s, g);
        SubFactories[typeof(Enum)] = (t, s, g) => new EnumEditorContentViewModel(t, s, g);
        
        // Fallback editor for any type
        SubFactories[typeof(object)] = (t, s, g) => new StringEditorContentViewModel(t, s, g);
    }
    
    public PropertyEditorContentViewModel CreateViewModel(Type propertyType, IPropertyBinding binding)
    {
        var subfactory = SubFactories
            .FirstOrDefault(x => x.Key.IsAssignableFrom(propertyType))
            .Value;

        if (subfactory == null)
        {
            throw new ArgumentException($"Failed to create content vm for property type: {propertyType}");
        }

        return subfactory.Invoke(propertyType, binding.SetValue, binding.GetValue);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using FlowEditor.Models.Primitives;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Meta;
using PropertyModels.Extensions;

namespace FlowEditor.Models.Contexts;

public abstract class CachedExposeContext : FlowContextBase
{
    private readonly IDictionary<string, ExposedValue> cachedExposures;

    public CachedExposeContext()
    {
        cachedExposures = new Dictionary<string, ExposedValue>();
    }

    protected void ExposeValue<T>(string name, string displayName, T? initialValue, bool serialize = true, params Attribute[] attributes)
    {
        cachedExposures.Add(name, new ExposedValue(name, displayName, initialValue, serialize, attributes));
    }

    protected object? GetExposedValue(string name)
    {
        if (cachedExposures.TryGetValue(name, out var v))
        {
            return v.Value;
        }
        
        throw new Exception($"Failed to retrieve exposed value: {name}");
    }

    protected T GetExposedValue<T>(string name)
    {
        var val = GetExposedValue(name);
        
        if (val is T t)
        {
            return t;
        }

        throw new Exception($"Failed to retrieve exposed value: {name} - type mismatch: expected: {typeof(T)}, actual: {val?.GetType()}");
    }

    protected void SetExposedValue<T>(string name, T? value)
    {
        if (cachedExposures.TryGetValue(name, out var v))
        {
            v.Value = value;
            v.Descriptor.Value = value;
        }
        else
        {
            throw new Exception($"Failed to change exposed value: {name}");
        }
    }

    protected override IEnumerable<ValueDescriptor> GetDescriptors()
    {
        return cachedExposures.Values.Select(v => v.Descriptor);
    }

    protected ValueDescriptor? TryGetDescriptor(string name)
    {
        return cachedExposures.TryGetValue(name, out var d) ? d.Descriptor : null;
    }

    public override NodeContextData Serialize()
    {
        return new CachedExposedContextData(cachedExposures
            .Where(x => x.Value.Serialize)
            .ToDictionary(x => x.Key, x => 
                new ExposedValueData(x.Value.Value, x.Value.Descriptor.Name, x.Value.Descriptor.DisplayName, x.Value.Descriptor.ValueType)));
    }

    public override void Deserialize(NodeContextData data)
    {
        if (data is not CachedExposedContextData d) return;
        
        d.ExposedValues.ForEach(x =>
        {
            var val = ProcessDeserializedValue(x.Value.Value, x.Value.ValueType);
            
            if (!cachedExposures.ContainsKey(x.Value.Name))
            {
                ExposeValue(x.Value.Name, x.Value.DisplayName, val);
            }
            else
            {
                SetExposedValue(x.Key, val);
            }
        });
    }

    // TODO: Move this to the service layer, as this context shouldn't know about deserialization specifics.
    private object? ProcessDeserializedValue(object? value, Type valueType)
    {
        if (value is null) return null;

        if (valueType.IsEnum && value.GetType().IsNumericType())
        {
            return Enum.ToObject(valueType, Convert.ToInt32(value));
        }
        if (value is long l)
        {
            return Convert.ToInt32(l);
        }

        return value;
    }

    private class ExposedValue
    {
        public object? Value { get; set; }
        public bool Serialize { get; }
        public ValueDescriptor Descriptor { get; }

        public ExposedValue(string name, string displayName, object? defaultValue, bool serialize, Attribute[] attributes)
        {
            Value = defaultValue;
            Serialize = serialize;

            Descriptor = new ValueDescriptor(x => Value = x, () => Value)
            {
                Name = name,
                DisplayName = displayName,
                Value = Value,
                ExtraAttributes = attributes
            };
        }
    }
}

internal record struct ExposedValueData(object? Value, string Name, string DisplayName, Type ValueType);
internal record CachedExposedContextData(IDictionary<string, ExposedValueData> ExposedValues) : NodeContextData;
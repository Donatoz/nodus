using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using PropertyModels.ComponentModel;
using PropertyModels.ComponentModel.DataAnnotations;
using PropertyModels.Extensions;

namespace FlowEditor.Models.Primitives;

public class ValueDescriptor : ReactiveObject
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public bool Require { get; set; }

    [DependsOnProperty(nameof(Value))]
    public Type ValueType { get; set; }

    private Action<object?> setter;
    private Func<object> getter;
    
    public object Value
    {
        get => getter.Invoke();
        set
        {
            if (value == null)
            {
                setter.Invoke(null);
                ValueType = null;
                RaisePropertyChanged(nameof(Value));
                return;
            }

            if (value is IList list)
            {
                if (list.Count != 0)
                {
                    Type type = null;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null)
                        {
                            type = list[i].GetType();
                            break;
                        }
                    }

                    if (type != null)
                    {
                        setter.Invoke(Activator.CreateInstance(typeof(BindingList<>).MakeGenericType(type)));

                        for (int i = 0; i < list.Count; ++i)
                        {
                            (getter.Invoke() as IList)?.Add(list[i]);
                        }

                        ValueType = getter.Invoke().GetType();

                        RaisePropertyChanged(nameof(Value));

                        return;
                    }
                }

                setter.Invoke(new BindingList<string>());
                ValueType = getter.Invoke().GetType();
            }
            else
            {
                setter.Invoke(value);
                ValueType = getter.Invoke().GetType();
            }


            RaisePropertyChanged(nameof(Value));
        }
    }

    public ValueDescriptor(Action<object?> setter, Func<object> getter)
    {
        this.setter = setter;
        this.getter = getter;
    }

    public Attribute[] ExtraAttributes { get; set; }

    
    public override string ToString()
    {
        return (DisplayName.IsNotNullOrEmpty() ? DisplayName : Name) + "=" + (Value?.ToString() ?? "None");
    }

    public Attribute[] GetAttributes()
    {
        DisplayNameAttribute displayNameAttribute = new DisplayNameAttribute(DisplayName.IsNotNullOrEmpty() ? DisplayName : Name);
        DescriptionAttribute descAttribute = new DescriptionAttribute(Description.IsNotNullOrEmpty() ? Description : displayNameAttribute.DisplayName);

        if(ExtraAttributes != null)
        {
            return ExtraAttributes.Concat(new Attribute[] { descAttribute, displayNameAttribute }).ToArray();
        }
        else
        {
            return new Attribute[] { descAttribute, displayNameAttribute };
        }
    }

    public void Invalidate()
    {
        RaisePropertyChanged(nameof(Value));
    }

    public void ChangeType(Type type) => ValueType = type;
    public void SetValue(object value) => Value = value;
}
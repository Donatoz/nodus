using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Nodus.ObjectDescriptor;

public interface IMemberDescriptor
{
    bool CanDescribe(MemberInfo info);
    IExposed? DescribeMember(MemberInfo info, DescriptionContext context);
}

public record PropertyFieldMemberDescriptor : IMemberDescriptor
{
    public bool CanDescribe(MemberInfo info)
    {
        return info is PropertyInfo or FieldInfo;
    }

    public IExposed? DescribeMember(MemberInfo info, DescriptionContext context)
    {
        return info is PropertyInfo p ? DescribeProperty(p, context) : DescribeField((info as FieldInfo)!, context);
    }

    protected virtual IExposed? DescribeProperty(PropertyInfo info, DescriptionContext context)
    {
        if (!info.CanWrite) return null;
        
        return new ExposedPrimitive(new ExposureHeader { MemberName = info.Name, MemberType = info.PropertyType }, 
            info.GetValue(context.DescribedObject), x => info.SetValue(context.DescribedObject, x));
    }
    
    protected virtual IExposed DescribeField(FieldInfo info, DescriptionContext context)
    {
        return new ExposedPrimitive(new ExposureHeader { MemberName = info.Name, MemberType = info.FieldType },
            info.GetValue(context.DescribedObject), x => info.SetValue(context.DescribedObject, x));
    }
}

public partial record MethodMemberDescriptor : IMemberDescriptor
{
    [GeneratedRegex("^(?!get_|set_|add_|remove_).*")]
    private static partial Regex CompilerGeneratedMethodsExclude();
    
    public bool CanDescribe(MemberInfo info)
    {
        return info is MethodInfo && CompilerGeneratedMethodsExclude().IsMatch(info.Name);
    }

    public IExposed? DescribeMember(MemberInfo info, DescriptionContext context)
    {
        if (info is not MethodInfo i) return null;

        return new ExposedAction(new ExposureHeader { MemberName = i.Name, MemberType = i.ReturnType },
            args => i.Invoke(context.DescribedObject, args));
    }
}
using System.Diagnostics;

namespace Nodus.ObjectDescriptor;

public interface IObjectDescriptor
{
    IEnumerable<IExposed> Describe(object target, params string[] targetMembers);
}

public class ModularObjectDescriptor : IObjectDescriptor
{
    public static ModularObjectDescriptor Default { get; } = new(
        new PropertyFieldMemberDescriptor(), 
        new MethodMemberDescriptor()
    );

    public ISet<IMemberDescriptor> MemberDescriptors { get; }

    public ModularObjectDescriptor(params IMemberDescriptor[] memberDescriptors)
    {
        MemberDescriptors = new HashSet<IMemberDescriptor>(memberDescriptors);
    }
    
    public IEnumerable<IExposed> Describe(object target, params string[] targetMembers)
    {
        // TODO: Pre-cache common types members.

        var members = targetMembers.Any()
            ? target.GetType().GetMembers().Where(x => targetMembers.Contains(x.Name))
            : target.GetType().GetMembers();
        var context = new DescriptionContext { DescribedObject = target };

        foreach (var member in members)
        {
            var exposed = MemberDescriptors.FirstOrDefault(x => x.CanDescribe(member))?.DescribeMember(member, context);
            
            if (exposed == null) continue;

            yield return exposed;
        }
    }
}
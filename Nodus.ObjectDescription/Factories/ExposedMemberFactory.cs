using Nodus.DI.Factories;
using Nodus.ObjectDescriptor.ViewModels;

namespace Nodus.ObjectDescriptor.Factories;

public interface IExposedMemberViewModelFactory : IFactory<IExposed, ExposedMemberViewModel>
{
    ExposedValueViewModel CreateExposedValueViewModel(IExposedValue value);
    ExposedMethodViewModel CreateExposedMethodViewModel(IExposedMethod method);
}

public class ExposedMemberViewModelFactory : IExposedMemberViewModelFactory
{
    public static ExposedMemberViewModelFactory Default { get; } = new();
    
    public ExposedMemberViewModel Create(IExposed exposed)
    {
        return exposed switch
        {
            IExposedValue v => CreateExposedValueViewModel(v),
            IExposedMethod m => CreateExposedMethodViewModel(m),
            _ => throw new Exception($"Failed to create viewmodel for {exposed}: no subfactory found.")
        };
    }
    
    public virtual ExposedValueViewModel CreateExposedValueViewModel(IExposedValue value)
    {
        return ObjectDescriptionUtility.IsPrimitive(value.Header.MemberType)
            ? new ExposedValueViewModel(value)
            : ObjectDescriptionUtility.IsCollection(value.Header.MemberType) 
                ? new ExposedCollectionViewModel(value)
                : new ExposedCompoundViewModel(value);
    }

    public virtual ExposedMethodViewModel CreateExposedMethodViewModel(IExposedMethod method)
    {
        return new ExposedMethodViewModel(method);
    }
}
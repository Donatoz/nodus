using System.Collections.ObjectModel;
using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.ObjectDescriptor.Factories;

namespace Nodus.ObjectDescriptor.ViewModels;

public class DescribedObjectViewModel : IDisposable
{
    public ObservableCollection<ExposedValueViewModel> ExposedValues { get; }
    public ObservableCollection<ExposedMethodViewModel> ExposedMethods { get; }
    
    private IExposed[] currentExposedModels;
    private readonly IObjectDescriptor descriptor;
    private readonly IExposedMemberViewModelFactory memberFactory;

    public DescribedObjectViewModel(object? target = null, IObjectDescriptor? descriptor = null, 
        IExposedMemberViewModelFactory? memberFactory = null)
    {
        ExposedValues = new ObservableCollection<ExposedValueViewModel>();
        ExposedMethods = new ObservableCollection<ExposedMethodViewModel>();
        currentExposedModels = Array.Empty<IExposed>();
        
        this.memberFactory = memberFactory ?? ExposedMemberViewModelFactory.Default;
        this.descriptor = descriptor ?? ModularObjectDescriptor.Default;
        
        ChangeTarget(target);
    }

    public void ChangeTarget(object? target)
    {
        currentExposedModels.OfType<IDisposable>().DisposeAll();
        ExposedValues.Clear();
        ExposedMethods.Clear();
        currentExposedModels = Array.Empty<IExposed>();
        
        if (target == null) return;

        currentExposedModels = descriptor.Describe(target).ToArray();

        currentExposedModels
            .OfType<IExposedValue>()
            .ForEach(x => ExposedValues.Add(memberFactory.CreateExposedValueViewModel(x)));
        currentExposedModels
            .OfType<IExposedMethod>()
            .ForEach(x => ExposedMethods.Add(memberFactory.CreateExposedMethodViewModel(x)));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        
        currentExposedModels.OfType<IDisposable>().DisposeAll();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
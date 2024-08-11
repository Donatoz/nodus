using System.Collections;
using System.Diagnostics;
using System.Reactive;
using Nodus.Core.Extensions;
using PropertyModels.ComponentModel;
using ReactiveUI;
using ReactiveCommand = ReactiveUI.ReactiveCommand;
using ReactiveObject = PropertyModels.ComponentModel.ReactiveObject;

namespace Nodus.ObjectDescriptor.ViewModels;

public class TestObj : ReactiveObject
{
    private bool val;
    private string val2;
    private AnotherObj val3;
    private List<AnotherObj> val4 = new() { new AnotherObj() };

    public bool Value
    {
        get => val;
        set => this.RaiseAndSetIfChanged(ref val, value);
    }
    
    public string ValueTwo
    {
        get => val2;
        set => this.RaiseAndSetIfChanged(ref val2, value);
    }
    
    public AnotherObj ValueThree
    {
        get => val3;
        set => this.RaiseAndSetIfChanged(ref val3, value);
    }
    
    public List<AnotherObj> ValueFour
    {
        get => val4;
        set => this.RaiseAndSetIfChanged(ref val4, value);
    }

    public void Test()
    {
        Trace.WriteLine($"------------- Test");
    }
}

public enum TestEnum
{
    One,
    Two,
    Three
}

public class AnotherObj
{
    public float Value { get; set; }
    public Strct Struct { get; set; }
    
    public AnotherObj Child { get; set; }
    public List<Strct> List { get; set; }
}

public struct Strct
{
    public float StValue1 { get; set; }
    public float StValue2 { get; set; }
    public float StValue3 { get; set; }
}

public class ExposureTestVm
{
    public TestObj DescribedObject { get; }
    public DescribedObjectViewModel Described { get; }
    
    public ReactiveCommand<Unit, Unit> Test { get; }

    public ExposureTestVm()
    {
        DescribedObject = new TestObj();
        
        Described = new DescribedObjectViewModel(DescribedObject);
        
        Test = ReactiveCommand.Create(() => { Trace.WriteLine($"-------- {DescribedObject.ValueFour[3]}"); });
    }
}
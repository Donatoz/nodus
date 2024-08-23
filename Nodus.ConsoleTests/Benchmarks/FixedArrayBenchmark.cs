using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Nodus.Common;

namespace Nodus.ConsoleTests.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
public class FixedArrayBenchmark
{
    private int[] commonArray = null!;
    private FixedArray<int> fixedArray = null!;

    [Params(100)]
    public int ElementsCount;

    [GlobalSetup]
    public void Setup()
    {
        commonArray = new int[ElementsCount];
        fixedArray = new FixedArray<int>(ElementsCount);

        var random = new Random(12525);

        for (var i = 0u; i < ElementsCount; i++)
        {
            var n = random.Next();
            commonArray[i] = n;
            fixedArray[i] = n;
        }
    }

    [Benchmark]
    public void CommonArrayWrite()
    {
        var number = 123;
        
        for (var i = 0; i < ElementsCount; i++)
        {
            commonArray[i] = number;
        }
    }
    
    [Benchmark]
    public void FixedArrayWrite()
    {
        var number = 123;
        
        for (var i = 0u; i < ElementsCount; i++)
        {
            fixedArray[i] = number;
        }
    }
    
    [Benchmark]
    public void CommonArrayRead()
    {
        for (var i = 0; i < ElementsCount; i++)
        {
            _ = commonArray[i];
        }
    }
    
    [Benchmark]
    public void FixedArrayRead()
    {
        for (var i = 0u; i < ElementsCount; i++)
        {
            _ = fixedArray[i];
        }
    }

    [GlobalCleanup]
    public void CleanUp()
    {
        fixedArray.Dispose();
    }
}
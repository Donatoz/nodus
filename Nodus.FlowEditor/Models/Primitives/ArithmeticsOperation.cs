namespace FlowEditor.Models.Primitives;

public enum ArithmeticsOperation
{
    Add, Subtract, Multiply, Divide
}

public static class ArithmeticsOperationExtensions
{
    public static float Resolve(this ArithmeticsOperation op, float a, float b)
    {
        return op switch
        {
            ArithmeticsOperation.Add => a + b,
            ArithmeticsOperation.Divide => a / b,
            ArithmeticsOperation.Multiply => a * b,
            ArithmeticsOperation.Subtract => a - b,
            _ => a + b
        };
    }
}
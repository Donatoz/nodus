using System;

namespace FlowEditor.Models.Primitives;

public enum CompareOperation
{
    Greater,
    Equal,
    Less,
    GreaterEqual,
    LessEqual,
    NotEqual
}

public enum LogicalOperation
{
    And,
    Or,
    Not
}

public static class LogicalOperationsExtensions
{
    private const float FloatTolerance = 0.00001f;

    public static bool Evaluate(this LogicalOperation operation, bool a, bool b = true)
    {
        return operation switch
        {
            LogicalOperation.And => a & b,
            LogicalOperation.Or => a || b,
            LogicalOperation.Not => !a,
            _ => false
        };
    }
    
    public static bool Evaluate(this CompareOperation operation, float a, float b)
    {
        return operation switch
        {
            CompareOperation.Equal => Math.Abs(a - b) <= FloatTolerance,
            CompareOperation.Greater => a > b,
            CompareOperation.Less => a < b,
            CompareOperation.GreaterEqual => a >= b,
            CompareOperation.LessEqual => a <= b,
            CompareOperation.NotEqual => Math.Abs(a - b) > FloatTolerance,
            _ => false
        };
    }
}
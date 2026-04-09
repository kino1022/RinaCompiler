namespace RinaCompiler.AST;

public enum BinaryOperator {
    Add,
    Sub,
    Mul,
    Div,
    
    Less,
    LessEqual,
    Greater,
    GreaterEqual,
    
    And,
    Or,
    
    Equal,
    NotEqual,
    
    Assign,
    AddAssign,
    SubAssign,
    MulAssign,
    DivAssign,
}

public static class BinaryOperatorExtension {
    public static string ToOpCode(this BinaryOperator op) {
        switch (op) {
            case BinaryOperator.Add: return "+";
            case BinaryOperator.Sub : return "-";
            case BinaryOperator.Mul : return "*";
            case BinaryOperator.Div : return "/";
            case BinaryOperator.Less : return "<";
            case BinaryOperator.LessEqual : return "<=";
            case BinaryOperator.GreaterEqual : return ">=";
            case BinaryOperator.Greater : return ">";
            case BinaryOperator.Equal : return "==";
            case BinaryOperator.NotEqual : return "!=";
            case BinaryOperator.And : return "&&";
            case BinaryOperator.Or : return "||";
            case BinaryOperator.Assign : return "=";
            case BinaryOperator.AddAssign : return "+=";
            case BinaryOperator.SubAssign : return "-=";
            case BinaryOperator.MulAssign : return "*=";
            case BinaryOperator.DivAssign : return "/=";
            default: return op.ToString();
        }
    }
}
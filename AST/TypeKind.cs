namespace RinaCompiler.AST;

public enum TypeKind {
    Int,
    Float,
    Bool,
    String,
    Void,
}

public static class TypeKindExtension {
    public static string ToString(this TypeKind type) {
        switch (type) {
            case TypeKind.Int: return "int";
            case TypeKind.Bool : return "bool";
            case TypeKind.Float : return "float";
            case TypeKind.String : return "string";
            case TypeKind.Void : return "void";
            default: return type.ToString();
        }
    }
}
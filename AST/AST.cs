namespace RinaCompiler.AST;

public enum Access {
    Public,
    Private,
    Protected,
}

public abstract record  ASTNode;

public sealed record ProgramNode (List<ClassNode> Classes) : ASTNode;

public sealed record ClassNode(string Name, List<IMemberNode> Members) : ASTNode;

public interface IMemberNode {
    Access Access { get; }
}

public sealed record FieldNode(
    Access Access,
    bool IsConst,
    string Name,
    TypeRef Type,
    IExprNode? DefaultValue
) : ASTNode, IMemberNode;

public sealed record MethodNode (
    Access Access,
    string Name,
    List<ParamNode> Params,
    TypeRef ReturnType,
    BlockNode Body
    ): ASTNode, IMemberNode;

public sealed record CtorNode(
    Access Access,
    List<ParamNode> Params,
    BlockNode Body
    ): ASTNode, IMemberNode;

public sealed record DtorNode (Access Access, BlockNode Body) : ASTNode, IMemberNode;

public sealed record TypeRef (string Name) : ASTNode;

public sealed record ParamNode(
    List<ParamModifier> Modifiers,
    string Name,
    TypeRef Type,
    IExprNode? DefaultValue
) : ASTNode;

public enum ParamModifier {
    Mut,
    Ref,
}

public interface IExprNode;
public interface IStmtNode;

public sealed record NameExpr (string Name) : ASTNode, IExprNode;

public sealed record ReturnStmtNode(IExprNode? Value) : ASTNode, IStmtNode;

public sealed record ExprStmtNode(IExprNode Expr) : ASTNode, IStmtNode;

public sealed record LocalDeclStmtNode(FieldNode Decl) : ASTNode, IStmtNode;

public sealed record BlockNode (List<IStmtNode> Statements) : ASTNode;

public sealed record InnerBlockNode (BlockNode Block) : ASTNode, IStmtNode;

public sealed record IntLiteralExpr(int Value) : ASTNode, IExprNode;

public sealed record FloatLiteralExpr(float Value) : ASTNode, IExprNode;

public sealed record StringLiteralExpr (string Value) : ASTNode, IExprNode;

public sealed record BinaryExpr(
    IExprNode Left,
    BinaryOperator Op, 
    IExprNode Right
    ) : ASTNode, IExprNode;

public sealed record CallExpr(
    IExprNode Callee,
    List<IExprNode> Args
    ) : ASTNode, IExprNode;

public sealed record MemberAccessExpr(
    IExprNode Target,
    string MemberName
    ) : ASTNode, IExprNode;
    
using System.Runtime.CompilerServices;
using Antlr4.Runtime.Misc;
using RinaCompiler.AST;

namespace RinaCompiler.FrontEnd;

public sealed class AstBuilderVisitor : RinaLangBaseVisitor<ASTNode> {

    public ProgramNode Build(RinaLangParser.CompilationUnitContext cu) {
        return (ProgramNode)VisitCompilationUnit(cu)!;
    }

    public override ASTNode VisitCompilationUnit([NotNull]RinaLangParser.CompilationUnitContext context) {
        var classes = new List<ClassNode>();
        foreach (var t in context.topLevelDecl()) {
            var c = (ClassNode)Visit(t)!;
            classes.Add(c);
        }
        return new ProgramNode(classes);
    }

    public override ASTNode VisitTopLevelDecl([NotNull]RinaLangParser.TopLevelDeclContext context) {
        return Visit(context.classDecl());
    }

    public override ASTNode VisitClassDecl(RinaLangParser.ClassDeclContext context) {
        var name = context.Identifier().GetText();
        var members = new List<IMemberNode>();
        foreach (var m in context.classBody().classMemberDecl()) {
            var node = Visit(m);
            if (node is IMemberNode mem) {
                members.Add(mem);
            }
            else if (node is IEnumerable<IMemberNode> mems) {
                members.AddRange(mems);
            }
            else if (node is null) {
                /* ignore */
            }
            else throw new InvalidOperationException($"unexpected member node : {node.GetType().Name}");
        }
        return new ClassNode(name, members);
    }

    public override ASTNode VisitClassMemberDecl(RinaLangParser.ClassMemberDeclContext context) {
        if (context.memberDecl() is { } md) {
            return Visit(md);
        }

        if (context.functionDecl() is { } fd) {
            return Visit(fd);
        }

        if (context.ctorDecl() is { } cd) {
            return Visit(cd);
        }

        if (context.dtorDecl() is { } dt) {
            return Visit(dt);
        }

        return null;
    }

    public override ASTNode VisitMemberDecl(RinaLangParser.MemberDeclContext context) {
        var access = ParseAccessOrDefault(context.accessModifier());
        if (context.varDecl() is { } v) {
            var f = (FieldNode)Visit(v)!;
            return f with { Access = access };
        }
        else {
            var f = (FieldNode)Visit(context.letDecl())!;
            return f with { Access = access };
        }
    }

    public override ASTNode VisitVarDecl(RinaLangParser.VarDeclContext context) {
        var name = context.Identifier().GetText();
        var type = (TypeRef)Visit(context.typeRef())!;
        var init = context.expr == null ? null : (IExprNode)Visit(context.expr())!;
        return new FieldNode(Access.Public, IsConst: false, name, type, init);
    }

    public override ASTNode VisitLetDecl(RinaLangParser.LetDeclContext context) {
        var name = context.Identifier().GetText();
        var type = (TypeRef)Visit(context.typeRef())!;
        var init = context.expr() is null ? null : (IExprNode)Visit(context.expr());
        return new FieldNode(Access.Public, IsConst: true, name, type, init);
    }

    public override ASTNode VisitFunctionDecl(RinaLangParser.FunctionDeclContext context) {
        var access = ParseAccessOrDefault(context.accessModifier());
        var name = context.Identifier().GetText();
        var ps = context.paramList() is null ? new List<ParamNode>() : ParseParams(context.paramList());
        var ret = context.typeRef() is null ? new TypeRef(TypeKind.Void.ToString()) : (TypeRef)Visit(context.typeRef());
        var body = (BlockNode)Visit(context.block())!;
        return new MethodNode(access, name, ps, ret, body);
    }

    public override ASTNode VisitCtorDecl(RinaLangParser.CtorDeclContext context) {
        var access = ParseAccessOrDefault(context.accessModifier());
        var ps = context.paramList() is null ? new List<ParamNode>() : ParseParams(context.paramList());
        var body = (BlockNode)Visit(context.block())!;
        return new CtorNode(access, ps, body);
    }

    public override ASTNode VisitDtorDecl(RinaLangParser.DtorDeclContext context) {
        var access = ParseAccessOrDefault(context.accessModifier());
        var body = (BlockNode)Visit(context.block())!;
        return new DtorNode(access, body);
    }

    public override ASTNode VisitTypeRef(RinaLangParser.TypeRefContext context) {
        foreach (var kind in TypeKind.GetValues<TypeKind>()) {
            if (context.GetText() == kind.ToString()) {
                return new TypeRef(kind.ToString());
            }
        }
        return new TypeRef(context.GetText());
    }

    public override ASTNode VisitBlock(RinaLangParser.BlockContext context) {
        var stmts = new List<IStmtNode>();
        foreach (var s in context.stmt()) {
            stmts.Add((IStmtNode)Visit(s)!);
        }
        return new BlockNode(stmts);
    }

    public override ASTNode VisitStmt(RinaLangParser.StmtContext context) {
        if (context.returnStmt() is {} rs) return Visit(rs);
        if (context.exprStmt() is {} es) return Visit(es);
        if (context.localDecl() is {} ls) return Visit(ls);
        if (context.block() is {} bs) return new InnerBlockNode((BlockNode)Visit(bs)!);
        throw new InvalidOperationException("unknown statements");
    }

    public override ASTNode VisitLocalDecl(RinaLangParser.LocalDeclContext context) {
        if (context.varDecl() is {} v) {
            return new LocalDeclStmtNode((FieldNode)Visit(v)!);
        }
        return new LocalDeclStmtNode((FieldNode)Visit(context.letDecl())!);
    }

    public override ASTNode VisitReturnStmt(RinaLangParser.ReturnStmtContext context) {
        var val = context.expr() is null ? null : (IExprNode)Visit(context.expr())!;
        return new ReturnStmtNode(val);
    }

    public override ASTNode VisitExprStmt(RinaLangParser.ExprStmtContext context) {
        return new ExprStmtNode((IExprNode)Visit(context.expr())!);
    }

    public override ASTNode VisitPrimaryExpr(RinaLangParser.PrimaryExprContext context) {
        if (context.IntegerLiteral() is { } lit) {
            return new IntLiteralExpr(int.Parse(lit.GetText()));
        }

        if (context.StringLiteral() is { } str) {
            return new StringLiteralExpr(str.GetText());
        }
        
        if (context.FloatLiteral() is { } f) {
            return new FloatLiteralExpr(float.Parse(f.GetText()));
        }
        
        if (context.Identifier() is { } id) {
            return new NameExpr(id.GetText());
        }

        if (context.expr() is { } e) {
            IExprNode expr = (IExprNode)Visit(e)!;
            return (ASTNode)expr!;
        }
        
        throw new InvalidOperationException("unknown primary expression");
    }

    private static Access ParseAccessOrDefault(RinaLangParser.AccessModifierContext? ctx) {
        if (ctx is null) {
            return Access.Public;
        }

        return ctx.GetText() switch {
            "pub" => Access.Public,
            "pro" => Access.Protected,
            "pri" => Access.Private,
            _ => throw new NotSupportedException($"unknown access modifier : {ctx.GetText()}")
        };
    }

    private List<ParamNode> ParseParams(RinaLangParser.ParamListContext context) =>
        context.param().Select(ParseParam).ToList();

    private ParamNode ParseParam(RinaLangParser.ParamContext context) {
        var mods = context.paramModifier()
            .Select(m => m.GetText() switch {
                "mut" => ParamModifier.Mut,
                "ref" => ParamModifier.Ref,
                _ => throw new NotSupportedException($"unknown parameter modifier : {m.GetText()}")
            })
            .ToList();
        var name = context.Identifier().GetText();
        var type = (TypeRef)Visit(context.typeRef())!;
        var def = context.expr() is null ? null : (IExprNode)Visit(context.expr());
        return new ParamNode(mods, name, type, def);
    }
}  
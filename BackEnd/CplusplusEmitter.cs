using System.Data;
using System.Text;
using RinaCompiler.AST;

namespace RinaCompiler.BackEnd;

public sealed class CplusplusEmitter {

    private readonly Dictionary<string, string> _typeMap = new() {
        ["int"] = "int32_t",
        ["float"] = "float32_t",
    };

    public string Emit(ProgramNode program) {
        var sb = new StringBuilder();
        sb.AppendLine("#include <cstdint>");
        sb.AppendLine();
        foreach (var c in program.Classes) {
            EmitClass(sb, c);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private void EmitClass(StringBuilder sb, ClassNode c) {
        sb.Append("class ").Append(c.Name).AppendLine(" {");
        EmitAccessGroup(sb, c, Access.Public, "public");
        EmitAccessGroup(sb, c, Access.Protected, "protected");
        EmitAccessGroup(sb, c, Access.Private, "private");
        
        sb.AppendLine("}");
    }

    private void EmitAccessGroup(StringBuilder sb, ClassNode c, Access access, string label) {
        var members = c.Members.Where(m => m.Access == access).ToList();
        if (members.Count == 0) {
            return;
        }
        sb.Append(label).AppendLine(":");
        foreach (var m in members) {
            switch (m) {
                case FieldNode field: EmitField(sb, field); break;
                case MethodNode fn: EmitMethod(sb, fn.Name, fn); break;
                case CtorNode ctor: EmitCtor(sb, c.Name, ctor); break;
                case DtorNode dtor: EmitDtor(sb, c.Name, dtor); break;
                default : throw new NotSupportedException($"unknown member : {m.GetType().Name}");
            }
        }
        EmitSemiColon(sb);
    }

    private void EmitField(StringBuilder sb, FieldNode f) {
        if (f.IsConst) {
            sb.Append("const ");
        }
        else {
            sb.Append(' ');
        }
        sb.Append(MapType(f.Type)).Append(' ').Append(f.Name);
        if (f.DefaultValue is not null) {
            sb.Append(" = ").AppendLine(EmitExpr(f.DefaultValue));
        }
        EmitSemiColon(sb);
    }

    private void EmitMethod(StringBuilder sb, string className, MethodNode m) {
        sb.Append(' ')
            .Append(MapType(m.ReturnType))
            .Append(' ')
            .Append(m.Name).Append('(')
            .Append(string.Join(", ", m.Params.Select(EmitParam)))
            .AppendLine(") {");
        EmitBlockBody(sb, m.Body);
        sb.AppendLine(" }");
    }

    private void EmitCtor(StringBuilder sb, string className, CtorNode c) {
        sb.Append(' ').Append(className).Append('(')
            .Append(string.Join(", ", c.Params.Select(EmitParam)))
            .AppendLine(") {");
        EmitBlockBody(sb, c.Body);
        sb.AppendLine(" }");
    }

    private void EmitDtor(StringBuilder sb, string className, DtorNode d) {
        sb.Append("  ~").Append(className).AppendLine("() {");
        EmitBlockBody(sb, d.Body);
        sb.AppendLine("  }");
    }
    
    private void EmitLocalDecl(StringBuilder sb, FieldNode f) {
        sb.Append("    ");
        if (f.IsConst) sb.Append("const ");
        sb.Append(MapType(f.Type)).Append(' ').Append(f.Name);
        if (f.DefaultValue is not null) sb.Append(" = ").Append(EmitExpr(f.DefaultValue));
        sb.AppendLine(";");
    }
    
    private void EmitBlockBody(StringBuilder sb, BlockNode block) {
        foreach (var st in block.Statements) {
            switch (st) {
                case ReturnStmtNode r:
                    sb.Append("    return");
                    if (r.Value is not null) sb.Append(' ').Append(EmitExpr(r.Value));
                    sb.AppendLine(";");
                    break;

                case ExprStmtNode e:
                    sb.Append("    ").Append(EmitExpr(e.Expr)).AppendLine(";");
                    break;

                case LocalDeclStmtNode ld:
                    // ローカルは Access 無視
                    EmitLocalDecl(sb, ld.Decl);
                    break;

                case InnerBlockNode inner:
                    sb.AppendLine("    {");
                    EmitBlockBody(sb, inner.Block);
                    sb.AppendLine("    }");
                    break;

                default:
                    throw new NotSupportedException($"Unknown stmt: {st.GetType().Name}");
            }
        }
    }

    private string EmitParam(ParamNode p) {
        // 最小：ref/mut/move を全部C++に完全対応するのは後回しでもOK
        // まずは ref を & にする程度から
        var type = MapType(p.Type);

        var byRef = p.Modifiers.Contains(ParamModifier.Ref);
        if (byRef) type += "&";

        // let/var の概念はパラメータにはないので const は今は扱わない
        return $"{type} {p.Name}";
    }

    private string EmitExpr(IExprNode e) {
        return e switch {
            IntLiteralExpr lit => lit.Value.ToString(),
            NameExpr n => n.Name,
            MemberAccessExpr m => $"{EmitExpr(m.Target)}.{m.MemberName}",
            CallExpr c => $"{EmitExpr(c.Callee)}({string.Join(", ", c.Args.Select(EmitExpr))})",
            BinaryExpr b => $"{EmitExpr(b.Left)} {(b.Op.ToOpCode())} {EmitExpr(b.Right)}",
            _ => throw new NotSupportedException($"Unknown expr: {e.GetType().Name}")
        };
    }
    
    private static string EmitBinaryOp(BinaryOperator op) => op switch {
        BinaryOperator.Assign => "=",
        BinaryOperator.AddAssign => "+=",
        BinaryOperator.SubAssign => "-=",
        BinaryOperator.MulAssign => "*=",
        BinaryOperator.DivAssign => "/=",

        BinaryOperator.Add => "+",
        BinaryOperator.Sub => "-",
        BinaryOperator.Mul => "*",
        BinaryOperator.Div => "/",

        BinaryOperator.Less => "<",
        BinaryOperator.LessEqual => "<=",
        BinaryOperator.Greater => ">",
        BinaryOperator.GreaterEqual => ">=",
        BinaryOperator.Equal => "==",
        BinaryOperator.NotEqual => "!=",
        BinaryOperator.And => "&&",
        BinaryOperator.Or => "||",
        _ => throw new NotSupportedException($"Unknown op: {op}")
    };
    
    private void EmitSemiColon(StringBuilder sb, bool isLine = true) {
        if (isLine) {
            sb.AppendLine(";");
            return;
        }
        sb.Append(';');
    }
    
    private string MapType (TypeRef t) => _typeMap.TryGetValue(t.Name, out var type) ? type : t.Name;
}
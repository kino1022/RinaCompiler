using System;
using Antlr4.Runtime;
using RinaCompiler.FrontEnd;
using RinaCompiler.BackEnd;

public static class Program {
    
    private static string inputPath = "/Users/fujitayuuta/Documents/RinaCompiler/test_code.rina";
    
    private static string outputPath = "/Users/fujitayuuta/Documents/RinaCompiler/result.cpp";
    
    public static void Main()
    {
        
        var input = File.ReadAllText(inputPath);

        var stream = new AntlrInputStream(input);
        var lexer = new RinaLangLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new RinaLangParser(tokens);

        var cu = parser.compilationUnit();

        var ast = new AstBuilderVisitor().Build(cu);

        var cpp = new CplusplusEmitter().Emit(ast);
        File.AppendAllText(outputPath, cpp);
    }
}
using Antlr4.Runtime;
using RinaCompiler.AST;
using RinaCompiler.BackEnd;

class Program {
    static void Main(string[] args) {
        string inputFilePath = "/Users/fujitayuuta/Documents/RinaCompiler/test_code.rina";
        string outputFilePath = "/Users/fujitayuuta/Documents/RinaCompiler/result.cpp";

        try {
            string rinaSource = File.ReadAllText(inputFilePath);
            var stream = new AntlrInputStream(rinaSource);
            var lexer = new RinaLangLexer(stream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new RinaLangParser(tokenStream);

            var visitor = new RinaLangBaseVisitor<ASTNode>();
            var emitter = new CplusplusEmitter();
            emitter.Emit()
        }
    }
}
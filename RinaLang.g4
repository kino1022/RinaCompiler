grammar RinaLang;

// --------------------
// Parser rules
// --------------------

compilationUnit
  : topLevelDecl* EOF
  ;

topLevelDecl
  : classDecl       // もしトップレベル var/fn を許すなら
  ;

classDecl
  : accessModifier? CLASS Identifier classBody
  ;

classBody
  : '{' classMemberDecl* '}'
  ;

classMemberDecl
  : memberDecl ';'                // フィールド
  | functionDecl            // メソッド（ブロック終端で閉じるので ; 不要）
  | ctorDecl
  | dtorDecl
  ;

memberDecl
  : accessModifier? (varDecl | letDecl)
  ;

// 変数/不変
varDecl
  : VAR Identifier (':' typeRef)? ('=' expr)?
  ;

letDecl
  : LET Identifier (':' typeRef)? ('=' expr)?
  ;

functionDecl
  : accessModifier? FN Identifier '(' paramList? ')' (':' typeRef)? block
  ;
  
ctorDecl
  : accessModifier? CTOR '(' paramList? ')' (':' typeRef)? block
  ;
  
dtorDecl
  : accessModifier? DTOR '~' Identifier '(' ')' block
  ;

paramList
  : param (',' param)*
  ;

param
  : paramModifier* Identifier ':' typeRef ('=' expr)?
  ;

paramModifier
  : MUT
  | REF
  ;

// 将来的に複数修飾子や attribute を増やすならここに集約すると便利
accessModifier
  : PRI
  | PRO
  | PUB
  ;

typeRef
  : qualifiedName typeSuffix*
  ;

qualifiedName
  : Identifier ('.' Identifier)*
  ;

// 例: int32*, Foo&, Foo[] など将来拡張用（今は未使用でもOK）
typeSuffix
  : '*'
  | '&'
  | '[' ']'
  ;

block
  : '{' stmt* '}'
  ;

stmt
  : returnStmt ';'
  | exprStmt ';'
  | localDecl ';'
  | block
  ;

localDecl
  : (varDecl | letDecl)
  ;

returnStmt
  : RETURN expr?
  ;

exprStmt
  : expr
  ;

// --------------------
// Expressions (C++風優先順位の一部 + 呼び出し対応)
// --------------------

expr
  : assignmentExpr
  ;

assignmentExpr
  : lhs=logicalOrExpr op=('='|'+='|'-='|'*='|'/=') rhs=assignmentExpr
  | logicalOrExpr
  ;

logicalOrExpr
  : logicalAndExpr ( '||' logicalAndExpr )*
  ;

logicalAndExpr
  : equalityExpr ( '&&' equalityExpr )*
  ;

equalityExpr
  : relationalExpr ( ('=='|'!=') relationalExpr )*
  ;

relationalExpr
  : additiveExpr ( ('<'|'<='|'>'|'>=') additiveExpr )*
  ;

additiveExpr
  : multiplicativeExpr ( ('+'|'-') multiplicativeExpr )*
  ;

multiplicativeExpr
  : unaryExpr ( ('*'|'/'|'%') unaryExpr )*
  ;

unaryExpr
  : ('+'|'-'|'!') unaryExpr
  | postfixExpr
  ;

// 関数呼び出し・メンバアクセスをここで扱う（優先順位が高い）
postfixExpr
  : primaryExpr postfixPart*
  ;

postfixPart
  : '(' argumentList? ')'         // call
  | '.' Identifier                // member access
  ;

argumentList
  : expr (',' expr)*
  ;

primaryExpr
  : IntegerLiteral
  | FloatLiteral
  | StringLiteral
  | Identifier
  | '(' expr ')'
  ;

// --------------------
// Lexer rules
// --------------------

CLASS   : 'class' ;

STRUCT : 'struct' ;

ENTITY  : 'entity' ;

COMPONENT : 'component' ;

VAR     : 'var' ;
LET     : 'let' ;
FN      : 'fn' ;
RETURN  : 'return' ;

PRI     : 'pri' ;
PRO     : 'pro' ;
PUB     : 'pub' ;

MUT     : 'mut' ;
REF     : 'ref' ;

Identifier
  : [a-zA-Z_][a-zA-Z0-9_]*
  ;

IntegerLiteral
  : [0-9]+
  ;
  
FloatLiteral
    : [0-9]+ '.' [0-9]+
    ;
    
StringLiteral
    : '"' ( ~["\\] | '\\' . )* '"'
    ;

// Comments
LINE_COMMENT
  : '//' ~[\r\n]* -> channel(HIDDEN)
  ;

BLOCK_COMMENT
  : '/*' .*? '*/' -> channel(HIDDEN)
  ;

// Whitespace
WS
  : [ \t\r\n]+ -> channel(HIDDEN)
  ;

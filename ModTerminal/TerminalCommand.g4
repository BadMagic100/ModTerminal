grammar TerminalCommand;


command: value (value | namedParameter)* EOF;

value
    : ID
    | LITERAL
    | STRICT_LITERAL
    ;

namedParameter
    : NAME_PREFIX ID (NAME_INFIX? value)?
    | ID NAME_INFIX value
    ;

ID
    : IDSTARTCHAR IDCHAR*
    ;

LITERAL
    : QUOTE (~["\\\r\n] | '\\' (QUOTE | '\\' | '\n'))* QUOTE
    ;
STRICT_LITERAL
    : (NONSPECIALCHAR | '-' NONSPECIALCHAR)+
    ;

NAME_PREFIX: '--';
NAME_INFIX: '=';

WS
    : WSCHAR+ -> skip
    ;

fragment QUOTE: '"';
fragment IDSTARTCHAR: [a-zA-Z_];
fragment NONSPECIALCHAR: ~[ \n\-="];
fragment IDCHAR: IDSTARTCHAR | [0-9];
fragment WSCHAR: [ \r\n\t];
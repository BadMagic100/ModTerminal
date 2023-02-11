grammar TerminalCommand;

command: ID value* namedParameter* EOF;

value
    : ID
    | LITERAL
    | STRICT_LITERAL
    ;

namedParameter
    : NAME_PREFIX ID value?
    | ID NAME_INFIX value
    ;

ID
    : IDSTARTCHAR IDCHAR*
    ;

LITERAL
    : QUOTE (~["\\\r\n] | '\\' (QUOTE | '\\' | '\n'))* QUOTE
    ;
STRICT_LITERAL
    : (IDCHAR | '-' IDCHAR)+
    ;

NAME_PREFIX: '--';
NAME_INFIX: '=';

WS
    : WSCHAR+ -> skip
    ;

fragment QUOTE: '"';
fragment IDSTARTCHAR: [a-zA-Z_];
fragment IDCHAR: IDSTARTCHAR | [0-9];
fragment WSCHAR: [ \r\n\t];
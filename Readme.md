# Coco/R Compiler Compiler with Token Inheritance

Based on the Coco/R Sources at
http://www.ssw.uni-linz.ac.at/Coco
that we call the "2011 version".

This code includes an enhancement called "token inheritance".
A typical usage scenario for the extension
would be to allow keywords as identifiers 
based on a parsing context that expects an identifier.


## Token Inheritance

We denote base types for tokens in the grammar file so that
a more specific token can be valid in a parsing
context that expects a more general token.

The base type of a token has to be declared explicitly
in the TOKENS section of the grammar file like that:

    TOKENS
      ident = letter { letter }.
      var : ident = "var". //  ": ident" is the new syntax 
      as = "as".

meaning that the keyword "var" is now valid in a 
production at a place, where an ident would be expected.
So, if you have a production like

    PRODUCTIONS
      Declaration = var ident as ident ";".

A text like

    var var as int; // valid

would be valid, whereas a text like

    var as as int; // invalid, because "as" is not an ident

would be invalid, just as the first text would 
with a parser generated with the 2011 version of Coco/R.


## Extended Syntax

see http://www.ssw.uni-linz.ac.at/Coco/Doc/UserManual.pdf 
with this modification of section "2.3.2 Tokens":

    TokenDecl = Symbol [':' Symbol] ['=' TokenExpr '.']. 

The Symbol behind the colon has to be a previously declared
token and is called the base token type. The generated parser
now accepts this declared token everywhere a token of its
base token type is expected. 

This compatibility is transitive.
However, it would be bad design to have complicated
inheritance trees in the grammar.


## Languages

* C# - OK, beta testing
* VB.Net - planned
* Java - planned


## Known Bugs

see readme.md in the respective language folder
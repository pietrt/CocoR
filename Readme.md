# Enhanced Coco/R Compiler Compiler

Based on the Coco/R Sources at
http://www.ssw.uni-linz.ac.at/Coco
that we call the "2011 version".

This repository includes these enhancements. More detailed
information can be found in the following sections.

* Token Inheritance -
A typical usage scenario for the extension
would be to allow keywords as identifiers 
based on a parsing context that expects an identifier.

* Symbol Tables -
Having autocomplete information is quite useless
for token classes, unless you can specify from where
you will take all valid identifiers. In this version
of Coco/R you can declare global symbol tables for
a parser and mark tokenclasses in productions to 
create a new symbol, error out if it was already
declared and mark a tokenclass to use such a symbol,
i.e. error out if it was not declared before. At such 
a symbol use point, when looking at alternatives, you
can ask the parser of which symbols it knows at this
point to provide autocomplete for token classes.

* Lexical Scopes for Symbol Tables -
The existance and no-redefinition checks can be scoped
lexically by marking a production as a scope for a symbol
table. By creating a new scope for a production, you can
create and redefine any symbol, as well as use any symbol
in this scope and all of it's parent scopes. This scope
as well as all newly created symbols is destroyed when 
you leave the generated production method. So if you 
need to preserve the scoped symbols you might want to
add a semantic action that stores the symbol 
table's current scope `currentScope` in your AST. 

* useonce and useall Scopes for Symbols -
You can declare at the production level, that any symbol
has to be used at most (useonce) or at least (useall) once
inside this lexical scope. This is usefull for metaprogramming
where you declare idents as keyword-like and state that each such
keyword-like ident has to be used or can appear only one time.

* Strict and Non Strict Symbol Tables -
Symbol tables can be strict, i.e. every symbol has to be
declared before its use (aka *one-pass*), or non strict,
meaning that a symbol can be used and declared anywhere 
acording to common scoping rules (aka *two-pass*). However,
there are actually no multiple passes, the generated 
parser always makes only one pass.


* Autocomplete Information -
If the switch -ac is set, the generator produces a
parser that records to any parsed terminal symbol
all alternative terminal symbols that would be valid
instead of the actually parsed token.

* Find Symbol Declaration -
The autocomplete information includes data to locate
the token that declares the currently parsed symbol.

* Sample Winforms Editor -
Proof of concept to show autocomplete information
interactively. Supports "Goto declaration" and "Find all
references" as well as alternatives for keywords and symbols.

* BOM Free UTF-8 Scanner -
The standard scanner has now a new constructor that tells it
to use a UTF8Buffer even if the leading BOM (byte order mark)
is missing. This is handy if you scan from a string wraped 
in a stream or from a file that is written by a tool that
writes UTF-8 but no byte order marks. The Coco executable
has a new command line option that forces UTF-8 processing.

* Declarative, language independent AST -
Forming an abstract syntax tree (AST) from the parse can be done
using semantic actions. However, this couples the AST generation
strictly to the target language. With this extension, some prototyping 
can be done to explore the desired object hierarchy.
Starting with the extraction of the token from a terminal or
nonterminal or a literal, you can give them names and form lists.
As these objects travel up the call tree, you can compose objects
and lists from named objects deeper in the call tree by reusing
the names of your productions. If all runs
well, you get at toplevel a structured object made up from 
other objects, lists and string as leaf datatype. 
There are functions to inspect the structure and export it to JSON. 


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


### Extended syntax for token inheritance

see http://www.ssw.uni-linz.ac.at/Coco/Doc/UserManual.pdf 
with this modification of section "2.3.2 Tokens":

    // Coco/R grammar
    TokenDecl = Symbol [':' Symbol] ['=' TokenExpr '.']. 

The Symbol behind the colon has to be a previously declared
token and is called the base token type. The generated parser
now accepts this declared token everywhere a token of its
base token type is expected. 

This compatibility is transitive.
However, it would be bad design to have complicated
inheritance trees in the grammar.


## Symbol Tables

There is a new section `SYMBOLTABLES` just before `PRODUCTIONS`
where symbol tables for the generated parser can be declared and
initialized.

    // Coco/R grammar
    STDecl = ident [ "STRICT" ] { string } .

Example

    SYMBOLTABLES
      variables.  // an empty symbol table named variables
      types STRICT "string" "int" "double". // a symbol table named types with three prefilled entries

In productions you can append to each terminal or weak terminal `t`
an angle bracket plus a declared symboltable symbol to declare a new
name in this symbol table or a colon plus a symboltable to use
a declared name in this table. Semantic errors are generated if the name
is declared twice or does not exist respectivly.

Example

    Decl = "var" ident>variables ':' ident:types.

this production creates a new name in `variables` for the first ident
and checks that the second ident is present in `types`.


### Strict and Non Strict Symbol Tables

We call a symbol table *strict*, if a declaration of a particular symbol
(via `ident>table`) has to be strictly before its use (via `ident:table`).
So the declaration of a symbol is checked when its use-token is parsed.

We call a symbol table *non strict*, if declaration and use of a symbol
can be anywhere acording to lexical scoping rules.

In other words, you have to declare a symbol to use it. If a symbol table 
is declared as `STRICT` the declaration has to be before the use, otherwise the 
declaration can be anywhere inside the current lexical scope or its parent scopes.


### Scoped Symbols

To introduce a new lexical scope to a production like `Block`
for a list of symbol tables,
you have to list them in the production definition:

    Block SCOPES(variables, types) = 
      '{' 
      { Decl } 
      { Statement } 
      '}' .

So the extended Coco/R syntax for productions is

    // Coco/R grammar
    Production = ident [FormalAttributes] [ScopesDecl] [LocalDecl] '=' Expression '.'.
    ScopesDecl = "SCOPES" '(' ident { ',' ident } ')' .

See http://www.ssw.uni-linz.ac.at/Coco/Doc/UserManual.pdf 
section "2.4 Parser Specification".

Note: Every symbol table has at least one scope, so you don't have
to declare any `SCOPES(...)` block at all. This root scope is the only scope,
that is available after the call to `Parse()`. So, if you need
to preseve the content of a lexically scoped symbol table, store it's
`currentScope`, which is a `List<Token>` (C#) / `List(Of Token)` (VB.Net)
inside a semantic action. If you need all symbols in all currently active 
scopes, take a look at `items` which is an `IEnumerable<Token>`.


### Useall and Useonce Scopes

The once more extended Coco/R syntax for productions is

    // Coco/R grammar
    Production = ident [FormalAttributes] [ScopesDecl] [ UseOnceDecl ] [ UseAllDecl ] [LocalDecl] '=' Expression '.'.
    ScopesDecl  = "SCOPES"  '(' ident { ',' ident } ')' .
    UseOnceDecl = "USEONCE" '(' ident { ',' ident } ')' .
    UseAllDecl  = "USEALL"  '(' ident { ',' ident } ')' .

`USEONCE` and `USEALL` both create a scope for the production method,
where each use of a symbol in the symbol tables denoted by the `ident, ...` list 
is counted. After leaving the scope `USEONCE` checks that each symbol is used zero
to one times and `USEALL` checks that each symbol is used at least one time. Undeclared
symbols are not counted. If a check fails, 1 (`USEALL`) or 2..N (`USEONCE`) semantic errors 
are logged.

Note: A *use* is logged if a suffixed Coco/R symbol like `ident:symboltable` is
parsed and the token is recognized as a member of `symboltable`.

For our example, we consider a dictionary for a list of target languages, where
this list of target languages is not known in advance. So we add the list
to our grammar and demand that each term has at least one translation in 
each declared language.

Example grammar:

    COMPILER Dictionary

    SYMBOLTABLES
      lang STRICT.
    
    PRODUCTIONS
      Dictionary = Languages { Term }.      
      Languages = "languages" { ident>lang }.
      Term USEALL(lang) = "term" string { Translation }.
      Translation = ident:lang string.

Example text:

    languages EN DE FR

    term "car" 
      DE "Auto" 
      DE "Fahrzeug" 
      EN "vehicle" 
      FR "..."
    term "cat" 
      DE "Katze" 
      RF "Mieze" // undeclared lang 'RF'
      // missing EN, missing FR
    term ...


### Accessing symbol tables form outside

The declared symbol tables are accessible via a declared public readonly field
of type `Symboltable` and via the generic method `symbol(string name)` that
returns the symbol table with the specified name or null if not found.


## Autocomplete Information (class Alternative)

After parsing, the parser has a list of all relevant
tokens `public List<Alternative> tokens` in text ordering,
i.e. no comments, no whitespace and no pragmas.

For each such element the property `t` holds the actually
parsed token. 

The property `alt` holds a BitArray of all
Terminal symbols that would be valid before the current 
token `t` was parsed. With the help of the `Parser.tName`
array and the index in the `alt` array, a client can resolve
the name of the token from the TOKENS section of the atg grammar
file or the literal of an inline declared keyword.

Note: Currently there is a flaw in the implementiation, because
the calculation of possible alternatives stops as soon as the 
actual token gets validly parsed. So this list can be truncated.
-> this has to be investigated.

Furthermore, if an alternative token class is associated with 
a symbol table in the current production like in `ident:variables`, 
its symbol table `variables` is accessible by 
the `st` array at its kind position. 

The property `declaredAt` calculates a `Token`, where `t` was declared at,
as long as `t` is  associated with a symbol table as a usage point.
Otherwise this property returns null.


### Sample Code listing Alternatives

This Sample Code lists all tokens and their respective
variants by name. If a variant has an associated symbol table,
a colon and the symbol table's name is appended to the 
token's symbol name. 

    // parser.Parse() ommited here

    foreach (Alternative a in parser.tokens)
    {
        // the parsed token, simply called "the token"
        Token t = a.t; 

        // either null or the token where the token t is declared at:
        Token declAt = a.declaration; // not used in sample

        // print information about the token
        Console.Write("({0,3},{1,3}) {2,3} {3,-20} {4, -20}", 
          t.line, t.col, t.kind, Parser.tName[t.kind], t.val);

        // print information about variants of the token:
        Console.Write(" alt: ");
        for (int k = 0; k <= Parser.maxT; k++)
        {
            if (a.alt[k]) {
                // token kind k is a variant of the token
                Console.Write("{1} ", k, Parser.tName[k]);
                if (a.st[k] != null) {                    
                    // symbol table associated with this k-th terminal
                    // in the current parsing context
                    Console.Write(":{0}", a.st[k].name);
                    // list only locally declared symbols:  
                    foreach (Token tok in a.st[k].currentScope)
                        Console.Write("{0}({1},{2})|", tok.val, tok.line, tok.col); 
                }
                Console.Write(' ');
            }
        }
        Console.WriteLine();
    }

See CSharp/Test/main.cs for a more elaborate example.


### Autocomplete Information plus Editor (hypothetical)

While comparing the current position inside a hypothetical editor
with the token sequence of the paresd full text, the editor could
provide coloring based on the actual token `t`'s information 
as well as autocompletion based on the `alt` (for keywords)
and `st` array for (available symbols).

For a sample implementation as a *proof of concept* of such an editor, see our
WinForms version in the CSharp/WinFormsEditor subfolder.
Please note, that it is compiled and linked against the 
sample grammar in CSharp/test and it always loads sample.txt
from this folder and doesn't save at all. 
Nevertheless it doesn't depend on the particular grammar design. 

It parses the source from scratch on any
change, so don't expect any proper usability, if the sources grow larger. 
However, for the current sample grammar and sample text in this repository, the
responsiveness can be called "instant" on my machine 
(2011 Windows PC box, Core i5 CPU, 16GB RAM, SSD).  



Planned: Build a language-server for Visual Studio Code and other IDEs. See
https://code.visualstudio.com/docs/extensions/example-language-server

Alternativly: Adopt RUST's way (via stdin/stdout and the RACER process) to
provide suggestions to Visual Studio Code. 
See https://github.com/saviorisdead/RustyCode/blob/master/src/services/suggestService.ts
and its dependencies.

Alternativly: Build a JavaScript/TypeScript version of Coco/R with this extensions
and implement the language service in Visual Studio Code's native 
language (node + ECMA Script 6). Maybe the C# sources can be transformed to JS 
by a tool to bootstrap the process.


## Declarative, Language Independent AST

*under development*

The main technical terms we like to introduce 
are *hatching a token*, *sending up* and *priming*. 
Denoted by the symbols `#` (hash - hatch),
 `^` (up) and `'` (prime). Operating in a list context is 
 marked by doubling the symbol.

* hatching a token with `#` - push `t` on the stack. Call this a hatch.

* hatching a token as a list with `##` - push `[t]` on the stack. Call 
  this a list hatch.

* priming it with `#'` or `##'` - instead of `t`, operate on `Prime(t)`, where
  `public void Prime(Token t)` has to be defined in the `COMPILER` section and
  modifies a copy of the token `t`. Priming is commonly used to strip typical
  string-like decorators `"` from a token based on it's kind. Priming can only
  be combind with hatching a token.

* give it a name with `##':name` - append `:name`. Without such a name, the hatched
  token has no name. You can give it a name afterwards with `^` (up).

* give it a value with `##':name=value` - append `=value`, or `="string"` if
  your value is not a valid Coco/R identifier. Commonly used after an optional
  literal terminal symbol.

Inside the scope of a production, unnamed and equally named hatches
combine as an unnamed list or a list with the same name. This is even the case
when any is a list, these lists are merged. Differently named hatches form 
an object with each hatch as a property under its name. An object hatch
plus a named token hatch is combined in the object hatch. 

Unnamed hatches can never be integrated in a hatch object. They stay on the
stack until they can be combined.

* send it up with `^` - give the topmost unnamed hatch the name of the preceeding
  Coco/R symbol lowercased. If there is no hatch, form a new empty object hatch `{}`. 

* send it up as a list with `^^` - give the topmost unnamed hatch the name of the preceeding
  Coco/R symbol lowercased and wrap it in a list. If it is an unnamed list, don't wrap it in a list again.
  If there is no hatch, form a new empty list hatch `[]`. 

* give it a different name with `^^:name` - append `:name`. Without such a name, the
  hatch is named by its preceeding Coco/R symbol.



Coco/R Syntax extension

    // Coco/R grammar
    TODO

Example grammar

    TODO

Example text

    TODO

Resulting AST in JSON notation

    TODO



## Extended command line arguments

* -ac - Turn on generation of autocomplete / intellisense information

* -is - ignore semantic actions. With this switch turned on,
you can generate and build a .Net parser library with autocomplete information 
from an attributed grammar file, that is written for a different language 
such as Java. Maybe only to use an autocomplete aware editor for the grammar,
that probably is only available as a .Net program.

* -utf8 - force UTF-8 processing, even if the input file doesn't have a 
byte order mark. This is the default e.g. if you use Visual Studio Code.


## Languages - Subfolders

* C# - CSharp -
  token inheritance *beta*, 
  autocomplete information *beta*, 
  symbol tables *beta*,
  non strict symbol tables *beta*,
  useonce and useall scopes *beta*, 
  -is switch *beta*,
  -utf8 switch *beta*,
  editor *beta*,
  BOM free scanner *beta*
  AST *alpha*

* VB.Net - VisualBasic - 
  token inheritance *beta*

* Java - (TBD)
  maybe, but not yet scheduled


* Utilities/Hexdump -
  a utility to inspect files, in particular to see, if byte 
  order marks are present.


Note: The generated code for .Net languages targets
plain vanilla .Net Framework 2.0 compilers and libraries. 


## Distributables

To generate parsers from attributed grammars, you only need
these files from the language directory you choose:

* coco.exe (built with build.bat if you don't have one yet)
* scanner.frame
* parser.frame
* copyright.frame (optional)

There are no dependencies to use the generated scanners and parsers.


## License

Coco/R, including this extension, is distributed under the terms
of the GNU General Public License (slightly extended).

This means that you have to open source any extension to
Coco/R itself but you are licensed to use generated
scanners and parsers in any software that you build, 
even in closed source projects.


## Collaboration

Contributrions are very welcome. Please feel free to fork/clone
this repository.


## Known Bugs

see readme.md in the respective language folder.


## Git Branches

* master - stable development

* autocomplete - experimental branch to record possible
  alternatives for an editor with autocomplete/intellisense.
  Plus integrated symbol tables for use with autocomplete.
  Plus interactive TextBox style editor.  

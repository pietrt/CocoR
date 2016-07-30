using System.IO;



using System;
using System.Collections;
using System.Collections.Generic;

namespace at.jku.ssw.Coco {



public class Parser {
	public const int _EOF = 0; // TOKEN EOF
	public const int _ident = 1; // TOKEN ident
	public const int _number = 2; // TOKEN number
	public const int _string = 3; // TOKEN string
	public const int _badString = 4; // TOKEN badString
	public const int _char = 5; // TOKEN char
	public const int maxT = 45;
	public const int _ddtSym = 46;
	public const int _optionSym = 47;

	const bool _T = true;
	const bool _x = false;
	const string _DuplicateSymbol = "{0} '{1}' declared twice in '{2}'";
	const string _MissingSymbol ="{0} '{1}' not declared in '{2}'";
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;
	public List<Alternative> tokens = new List<Alternative>();
	
	BitArray alt;
	Symboltable[] altst;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

	public Symboltable symbols(string name) {
		return null;
	}

const int id = 0;
	const int str = 1;
	
	public TextWriter trace;    // other Coco objects referenced in this ATG
	public Tab tab;
	public DFA dfa;
	public ParserGen pgen;

	bool   genScanner;
	string tokenString;         // used in declarations of literal tokens
	string noString = "-none-"; // used in declarations of literal tokens

/*-------------------------------------------------------------------------*/



	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}
	

	void Get () {
		for (;;) {
			t = la;
			if (t.kind != _EOF) {
				tokens.Add(new Alternative(t, alt, altst));
				_newAlt();
			}
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }
				if (la.kind == 46) {
				tab.SetDDT(la.val); 
				}
				if (la.kind == 47) {
				tab.SetOption(la.val); 
				}

			la = t;
		}
	}

	void _newAlt() {
		alt = new BitArray(maxT+1);
		altst = new Symboltable[maxT+1];
	}

	void addAlt(int kind) {
		alt[kind] = true;
	}

	// a terminal tokenclass of kind kind is restricted to this symbol table 
	void addAlt(int kind, Symboltable st) {
		altst[kind] = st;
	}

	void addAlt(int[] range) {
		foreach(int kind in range)
			addAlt(kind);
	}

	void addAlt(bool[,] pred, int line) {
		for(int kind = 0; kind < maxT; kind++)
			if (pred[line, kind])
				addAlt(kind);
	}

	bool isKind(Token t, int n) {
		int k = t.kind;
		while(k >= 0) {
			if (k == n) return true;
			k = tBase[k];
		}
		return false;
	}
	
	void Expect (int n) {
		if (isKind(la, n)) Get(); else { SynErr(n); }
	}
	
	// is the lookahead token la a start of the production s?
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (isKind(la, n)) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (isKind(la, n)) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void Coco() {
		Symbol sym; Graph g, g1, g2; string gramName; CharSet s; int beg, line; 
		if (StartOf(1)) {
			Get();
			beg = t.pos; line = t.line; 
			while (StartOf(1)) {
				Get();
							}
			pgen.usingPos = new Position(beg, la.pos, 0, line); 
		}
		Expect(6); // "COMPILER"
		genScanner = true; 
		tab.ignored = new CharSet(); 
		Expect(1); // ident
		gramName = t.val;
		beg = la.pos; line = la.line;
		
		while (StartOf(2)) {
			Get();
					}
		tab.semDeclPos = new Position(beg, la.pos, 0, line); 
		if (isKind(la, 7)) {
			Get();
			dfa.ignoreCase = true; 
		}
		if (isKind(la, 8)) {
			Get();
			while (isKind(la, 1)) {
				SetDecl();
							}
		}
		if (isKind(la, 9)) {
			Get();
			while (isKind(la, 1) || isKind(la, 3) || isKind(la, 5)) {
				TokenDecl(Node.t);
							}
		}
		if (isKind(la, 10)) {
			Get();
			while (isKind(la, 1) || isKind(la, 3) || isKind(la, 5)) {
				TokenDecl(Node.pr);
							}
		}
		while (isKind(la, 11)) {
			Get();
			bool nested = false; 
			Expect(12); // "FROM"
			TokenExpr(out g1);
			Expect(13); // "TO"
			TokenExpr(out g2);
			if (isKind(la, 14)) {
				Get();
				nested = true; 
			}
			dfa.NewComment(g1.l, g2.l, nested); 
					}
		while (isKind(la, 15)) {
			Get();
			Set(out s);
			tab.ignored.Or(s); 
					}
		if (isKind(la, 16)) {
			Get();
			while (isKind(la, 1)) {
				STDecl();
							}
		}
		while (!(isKind(la, 0) || isKind(la, 17))) {SynErr(46); Get();}
		Expect(17); // "PRODUCTIONS"
		if (genScanner) dfa.MakeDeterministic();
		tab.DeleteNodes();
		
		while (isKind(la, 1)) {
			Get();
			sym = tab.FindSym(t.val);
			bool undef = sym == null;
			if (undef) sym = tab.NewSym(Node.nt, t.val, t.line);
			else {
			 if (sym.typ == Node.nt) {
			   if (sym.graph != null) SemErr("name declared twice");
			 } else SemErr("this symbol kind not allowed on left side of production");
			 sym.line = t.line;
			}
			bool noAttrs = sym.attrPos == null;
			sym.attrPos = null;
			
			if (isKind(la, 30) || isKind(la, 32)) {
				AttrDecl(sym);
			}
			if (!undef)
			 if (noAttrs != (sym.attrPos == null))
			   SemErr("attribute mismatch between declaration and use of this symbol");
			
			if (isKind(la, 21)) {
				ScopesDecl(sym);
			}
			if (isKind(la, 43)) {
				SemText(out sym.semPos);
			}
			ExpectWeak(18, 3); // "=" followed by string
			Expression(out g);
			sym.graph = g.l;
			tab.Finish(g);
			
			ExpectWeak(19, 4); // "." followed by badString
					}
		Expect(20); // "END"
		Expect(1); // ident
		if (gramName != t.val)
		 SemErr("name does not match grammar name");
		tab.gramSy = tab.FindSym(gramName);
		if (tab.gramSy == null)
		 SemErr("missing production for grammar name");
		else {
		 sym = tab.gramSy;
		 if (sym.attrPos != null)
		   SemErr("grammar symbol must not have attributes");
		}
		tab.noSym = tab.NewSym(Node.t, "???", 0); // noSym gets highest number
		tab.SetupAnys();
		tab.RenumberPragmas();
		if (tab.ddt[2]) tab.PrintNodes();
		if (errors.count == 0) {
		 Console.WriteLine("checking");
		 tab.CompSymbolSets();
		 if (tab.ddt[7]) tab.XRef();
		 if (tab.GrammarOk()) {
		   Console.Write("parser");
		   pgen.WriteParser();
		   if (genScanner) {
		     Console.Write(" + scanner");
		     dfa.WriteScanner();
		     if (tab.ddt[0]) dfa.PrintStates();
		   }
		   Console.WriteLine(" generated");
		   if (tab.ddt[8]) pgen.WriteStatistics();
		 }
		}
		if (tab.ddt[6]) tab.PrintSymbolTable();
		
		Expect(19); // "."
	}

	void SetDecl() {
		CharSet s; 
		Expect(1); // ident
		string name = t.val;
		CharClass c = tab.FindCharClass(name);
		if (c != null) SemErr("name declared twice");
		
		Expect(18); // "="
		Set(out s);
		if (s.Elements() == 0) SemErr("character set must not be empty");
		tab.NewCharClass(name, s);
		
		Expect(19); // "."
	}

	void TokenDecl(int typ) {
		string name; int kind; Symbol sym; Graph g; 
		string inheritsName; int inheritsKind; Symbol inheritsSym; 
		
		Sym(out name, out kind);
		sym = tab.FindSym(name);
		if (sym != null) SemErr("name declared twice");
		else {
		 sym = tab.NewSym(typ, name, t.line);
		 sym.tokenKind = Symbol.fixedToken;
		}
		tokenString = null;
		
		if (isKind(la, 29)) {
			Get();
			Sym(out inheritsName, out inheritsKind);
			inheritsSym = tab.FindSym(inheritsName);
			if (inheritsSym == null) SemErr(string.Format("token '{0}' can't inherit from '{1}', name not declared", sym.name, inheritsName));
			else if (inheritsSym == sym) SemErr(string.Format("token '{0}' must not inherit from self", sym.name));
			else if (inheritsSym.typ != typ) SemErr(string.Format("token '{0}' can't inherit from '{1}'", sym.name, inheritsSym.name));
			else sym.inherits = inheritsSym;
			
		}
		while (!(StartOf(5))) {SynErr(47); Get();}
		if (isKind(la, 18)) {
			Get();
			TokenExpr(out g);
			Expect(19); // "."
			if (kind == str) SemErr("a literal must not be declared with a structure");
			if (g.str != null) sym.definedAs = g.str;
			tab.Finish(g);
			if (tokenString == null || tokenString.Equals(noString))
			 dfa.ConvertToStates(g.l, sym);
			else { // TokenExpr is a single string
			 if (tab.literals[tokenString] != null)
			   SemErr("token string declared twice");
			 tab.literals[tokenString] = sym;
			 dfa.MatchLiteral(tokenString, sym);
			}
			
		} else if (StartOf(6)) {
			if (kind == id) genScanner = false;
			else dfa.MatchLiteral(sym.name, sym);
			
		} else SynErr(48);
		if (isKind(la, 43)) {
			SemText(out sym.semPos);
			if (typ != Node.pr) SemErr("semantic action not allowed in a pragma context"); 
		}
	}

	void TokenExpr(out Graph g) {
		Graph g2; 
		TokenTerm(out g);
		bool first = true; 
		while (WeakSeparator(34,7,8) ) {
			TokenTerm(out g2);
			if (first) { tab.MakeFirstAlt(g); first = false; }
			tab.MakeAlternative(g, g2);
			
					}
	}

	void Set(out CharSet s) {
		CharSet s2; 
		SimSet(out s);
		while (isKind(la, 25) || isKind(la, 26)) {
			if (isKind(la, 25)) {
				Get();
				SimSet(out s2);
				s.Or(s2); 
			} else {
				Get();
				SimSet(out s2);
				s.Subtract(s2); 
			}
					}
	}

	void STDecl() {
		SymTab st; 
		Expect(1); // ident
		string name = t.val.ToLower();                                    
		if (tab.FindSymtab(name) != null) 
		 SemErr("symbol table name declared twice");
		st = new SymTab(name);
		tab.symtabs.Add(st);
		
		while (isKind(la, 3)) {
			Get();
			string predef = t.val;
			predef = tab.Unescape(predef.Substring(1, predef.Length-2));
			if (dfa.ignoreCase) predef = predef.ToLower();
			st.Add(predef);
			
					}
		Expect(19); // "."
	}

	void AttrDecl(Symbol sym) {
		if (isKind(la, 30)) {
			Get();
			int beg = la.pos; int col = la.col; int line = la.line; 
			while (StartOf(9)) {
				if (StartOf(10)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes"); 
				}
							}
			Expect(31); // ">"
			if (t.pos > beg)
			 sym.attrPos = new Position(beg, t.pos, col, line); 
		} else if (isKind(la, 32)) {
			Get();
			int beg = la.pos; int col = la.col; int line = la.line; 
			while (StartOf(11)) {
				if (StartOf(12)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes"); 
				}
							}
			Expect(33); // ".>"
			if (t.pos > beg)
			 sym.attrPos = new Position(beg, t.pos, col, line); 
		} else SynErr(49);
	}

	void ScopesDecl(Symbol sym) {
		sym.scopes = new List<SymTab>(); 
		Expect(21); // "SCOPES"
		Expect(22); // "("
		ScopeStart(sym);
		while (isKind(la, 23)) {
			Get();
			ScopeStart(sym);
					}
		Expect(24); // ")"
	}

	void SemText(out Position pos) {
		Expect(43); // "(."
		int beg = la.pos; int col = la.col; int line = la.line; 
		while (StartOf(13)) {
			if (StartOf(14)) {
				Get();
			} else if (isKind(la, 4)) {
				Get();
				SemErr("bad string in semantic action"); 
			} else {
				Get();
				SemErr("missing end of previous semantic action"); 
			}
					}
		Expect(44); // ".)"
		pos = new Position(beg, t.pos, col, line); 
	}

	void Expression(out Graph g) {
		Graph g2; 
		Term(out g);
		bool first = true; 
		while (WeakSeparator(34,15,16) ) {
			Term(out g2);
			if (first) { tab.MakeFirstAlt(g); first = false; }
			tab.MakeAlternative(g, g2);
			
					}
	}

	void ScopeStart(Symbol sym) {
		Expect(1); // ident
		string stname = t.val.ToLower();
		SymTab st = tab.FindSymtab(stname); 
		if (st == null) SemErr("undeclared symbol table " + t.val);
		else sym.scopes.Add(st);
		
	}

	void SimSet(out CharSet s) {
		int n1, n2; 
		s = new CharSet(); 
		if (isKind(la, 1)) {
			Get();
			CharClass c = tab.FindCharClass(t.val);
			if (c == null) SemErr("undefined name"); else s.Or(c.set);
			
		} else if (isKind(la, 3)) {
			Get();
			string name = t.val;
			name = tab.Unescape(name.Substring(1, name.Length-2));
			foreach (char ch in name)
			 if (dfa.ignoreCase) s.Set(char.ToLower(ch));
			 else s.Set(ch); 
		} else if (isKind(la, 5)) {
			Char(out n1);
			s.Set(n1); 
			if (isKind(la, 27)) {
				Get();
				Char(out n2);
				for (int i = n1; i <= n2; i++) s.Set(i); 
			}
		} else if (isKind(la, 28)) {
			Get();
			s = new CharSet(); s.Fill(); 
		} else SynErr(50);
	}

	void Char(out int n) {
		Expect(5); // char
		string name = t.val; n = 0;
		name = tab.Unescape(name.Substring(1, name.Length-2));
		if (name.Length == 1) n = name[0];
		else SemErr("unacceptable character value");
		if (dfa.ignoreCase && (char)n >= 'A' && (char)n <= 'Z') n += 32;
		
	}

	void Sym(out string name, out int kind) {
		name = "???"; kind = id; 
		if (isKind(la, 1)) {
			Get();
			kind = id; name = t.val; 
		} else if (isKind(la, 3) || isKind(la, 5)) {
			if (isKind(la, 3)) {
				Get();
				name = t.val; 
			} else {
				Get();
				name = "\"" + t.val.Substring(1, t.val.Length-2) + "\""; 
			}
			kind = str;
			if (dfa.ignoreCase) name = name.ToLower();
			if (name.IndexOf(' ') >= 0)
			 SemErr("literal tokens must not contain blanks"); 
		} else SynErr(51);
	}

	void Term(out Graph g) {
		Graph g2; Node rslv = null; g = null; 
		if (StartOf(17)) {
			if (isKind(la, 41)) {
				rslv = tab.NewNode(Node.rslv, null, la.line); 
				Resolver(out rslv.pos);
				g = new Graph(rslv); 
			}
			Factor(out g2);
			if (rslv != null) tab.MakeSequence(g, g2);
			else g = g2;
			
			while (StartOf(18)) {
				Factor(out g2);
				tab.MakeSequence(g, g2); 
							}
		} else if (StartOf(19)) {
			g = new Graph(tab.NewNode(Node.eps, null, 0)); 
		} else SynErr(52);
		if (g == null) // invalid start of Term
		 g = new Graph(tab.NewNode(Node.eps, null, 0));
		
	}

	void Resolver(out Position pos) {
		Expect(41); // "IF"
		Expect(22); // "("
		int beg = la.pos; int col = la.col; int line = la.line; 
		Condition();
		pos = new Position(beg, t.pos, col, line); 
	}

	void Factor(out Graph g) {
		string name; int kind; Position pos; bool weak = false; 
		g = null;
		
		switch (la.kind) {
		case 1: // ident
		case 3: // string
		case 5: // char
		case 35: // "WEAK"
		{
			if (isKind(la, 35)) {
				Get();
				weak = true; 
			}
			Sym(out name, out kind);
			Symbol sym = tab.FindSym(name);
			if (sym == null && kind == str)
			 sym = tab.literals[name] as Symbol;
			bool undef = sym == null;
			if (undef) {
			 if (kind == id)
			   sym = tab.NewSym(Node.nt, name, 0);  // forward nt
			 else if (genScanner) { 
			   sym = tab.NewSym(Node.t, name, t.line);
			   dfa.MatchLiteral(sym.name, sym);
			 } else {  // undefined string in production
			   SemErr("undefined string in production");
			   sym = tab.eofSy;  // dummy
			 }
			}
			int typ = sym.typ;
			if (typ != Node.t && typ != Node.nt)
			 SemErr("this symbol kind is not allowed in a production");
			if (weak)
			 if (typ == Node.t) typ = Node.wt;
			 else SemErr("only terminals may be weak");
			Node p = tab.NewNode(typ, sym, t.line);
			g = new Graph(p);
			
			if (StartOf(20)) {
				if (isKind(la, 30) || isKind(la, 32)) {
					Attribs(p);
					if (kind != id) SemErr("a literal must not have attributes"); 
				} else if (isKind(la, 31)) {
					Get();
					Expect(1); // ident
					if (typ != Node.t && typ != Node.wt) SemErr("only terminals or weak terminals can declare a name in a symbol table"); 
					p.declares = t.val.ToLower();
					if (null == tab.FindSymtab(p.declares)) SemErr(string.Format("undeclared symbol table '{0}'", p.declares));
					
				} else {
					Get();
					Expect(1); // ident
					if (typ != Node.t && typ != Node.wt) SemErr("only terminals or weak terminals can lookup a name in a symbol table"); 
					p.declared = t.val.ToLower(); 
					if (null == tab.FindSymtab(p.declared)) SemErr(string.Format("undeclared symbol table '{0}'", p.declared));
					
				}
			}
			if (undef)
			 sym.attrPos = p.pos;  // dummy
			else if ((p.pos == null) != (sym.attrPos == null))
			 SemErr("attribute mismatch between declaration and use of this symbol");
			
			break;
		}
		case 22: // "("
		{
			Get();
			Expression(out g);
			Expect(24); // ")"
			break;
		}
		case 36: // "["
		{
			Get();
			Expression(out g);
			Expect(37); // "]"
			tab.MakeOption(g); 
			break;
		}
		case 38: // "{"
		{
			Get();
			Expression(out g);
			Expect(39); // "}"
			tab.MakeIteration(g); 
			break;
		}
		case 43: // "(."
		{
			SemText(out pos);
			Node p = tab.NewNode(Node.sem, null, 0);
			p.pos = pos;
			g = new Graph(p);
			
			break;
		}
		case 28: // "ANY"
		{
			Get();
			Node p = tab.NewNode(Node.any, null, 0);  // p.set is set in tab.SetupAnys
			g = new Graph(p);
			
			break;
		}
		case 40: // "SYNC"
		{
			Get();
			Node p = tab.NewNode(Node.sync, null, 0);
			g = new Graph(p);
			
			break;
		}
		default: SynErr(53); break;
		}
		if (g == null) // invalid start of Factor
		 g = new Graph(tab.NewNode(Node.eps, null, 0));
		
	}

	void Attribs(Node p) {
		if (isKind(la, 30)) {
			Get();
			int beg = la.pos; int col = la.col; int line = la.line; 
			while (StartOf(9)) {
				if (StartOf(10)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes"); 
				}
							}
			Expect(31); // ">"
			if (t.pos > beg) p.pos = new Position(beg, t.pos, col, line); 
		} else if (isKind(la, 32)) {
			Get();
			int beg = la.pos; int col = la.col; int line = la.line; 
			while (StartOf(11)) {
				if (StartOf(12)) {
					Get();
				} else {
					Get();
					SemErr("bad string in attributes"); 
				}
							}
			Expect(33); // ".>"
			if (t.pos > beg) p.pos = new Position(beg, t.pos, col, line); 
		} else SynErr(54);
	}

	void Condition() {
		while (StartOf(21)) {
			if (isKind(la, 22)) {
				Get();
				Condition();
			} else {
				Get();
			}
					}
		Expect(24); // ")"
	}

	void TokenTerm(out Graph g) {
		Graph g2; 
		TokenFactor(out g);
		while (StartOf(7)) {
			TokenFactor(out g2);
			tab.MakeSequence(g, g2); 
					}
		if (isKind(la, 42)) {
			Get();
			Expect(22); // "("
			TokenExpr(out g2);
			tab.SetContextTrans(g2.l); dfa.hasCtxMoves = true;
			tab.MakeSequence(g, g2); 
			Expect(24); // ")"
		}
	}

	void TokenFactor(out Graph g) {
		string name; int kind; 
		g = null; 
		if (isKind(la, 1) || isKind(la, 3) || isKind(la, 5)) {
			Sym(out name, out kind);
			if (kind == id) {
			 CharClass c = tab.FindCharClass(name);
			 if (c == null) {
			   SemErr("undefined name");
			   c = tab.NewCharClass(name, new CharSet());
			 }
			 Node p = tab.NewNode(Node.clas, null, 0); p.val = c.n;
			 g = new Graph(p);
			 tokenString = noString;
			} else { // str
			 g = tab.StrToGraph(name);
			 if (tokenString == null) tokenString = name;
			 else tokenString = noString;
			}
			
		} else if (isKind(la, 22)) {
			Get();
			TokenExpr(out g);
			Expect(24); // ")"
		} else if (isKind(la, 36)) {
			Get();
			TokenExpr(out g);
			Expect(37); // "]"
			tab.MakeOption(g); tokenString = noString; 
		} else if (isKind(la, 38)) {
			Get();
			TokenExpr(out g);
			Expect(39); // "}"
			tab.MakeIteration(g); tokenString = noString; 
		} else SynErr(55);
		if (g == null) // invalid start of TokenFactor
		 g = new Graph(tab.NewNode(Node.eps, null, 0)); 
	}



	public void Parse() {
		la = new Token();
		la.val = "";
		_newAlt();		
		Get();
		Coco();
		Expect(0);

	}
	
	// a token's base type
	static readonly int[] tBase = {
		-1,-1,-1,-1, -1,-1,-1,-1, -1,-1,-1,-1, -1,-1,-1,-1, -1,-1,-1,-1,
		-1,-1,-1,-1, -1,-1,-1,-1, -1,-1,-1,-1, -1,-1,-1,-1, -1,-1,-1,-1,
		-1,-1,-1,-1, -1,-1
	};

	// a token's name
	public static readonly string[] tName = {
		"EOF","ident","number","\"\"\"", "\"\"\"","\"\\\'\"","\"COMPILER\"","\"IGNORECASE\"", "\"CHARACTERS\"","\"TOKENS\"","\"PRAGMAS\"","\"COMMENTS\"", "\"FROM\"","\"TO\"","\"NESTED\"","\"IGNORE\"", "\"SYMBOLTABLES\"","\"PRODUCTIONS\"","\"=\"","\".\"",
		"\"END\"","\"SCOPES\"","\"(\"","\",\"", "\")\"","\"+\"","\"-\"","\"..\"", "\"ANY\"","\":\"","\"<\"","\">\"", "\"<.\"","\".>\"","\"|\"","\"WEAK\"", "\"[\"","\"]\"","\"{\"","\"}\"",
		"\"SYNC\"","\"IF\"","\"CONTEXT\"","\"(.\"", "\".)\"","???"
	};

	// states that a particular production (1st index) can start with a particular token (2nd index)
	static readonly bool[,] set0 = {
		{_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_x,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_x, _T,_T,_T,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_T,_T, _x,_x,_T,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_T,_T, _T,_x,_T,_x, _T,_T,_x,_T, _x,_x,_x},
		{_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_T,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
		{_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
		{_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
		{_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_T,_T,_T, _T,_T,_x,_T, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_x,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_x,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_T,_x},
		{_x,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _x,_T,_x},
		{_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_T,_x, _T,_x,_x,_x, _T,_x,_x,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_T,_x,_T, _x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x},
		{_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_T, _T,_x,_T,_x, _T,_T,_x,_T, _x,_x,_x},
		{_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_T, _T,_x,_T,_x, _T,_x,_x,_T, _x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x}

	};

	// as set0 but with token inheritance taken into account
	static readonly bool[,] set = {
		{_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_x,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_x, _T,_T,_T,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_T,_T, _x,_x,_T,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_T,_T, _T,_x,_T,_x, _T,_T,_x,_T, _x,_x,_x},
		{_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_T,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
		{_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
		{_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
		{_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_T,_T,_T, _T,_T,_x,_T, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_x,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_x,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
		{_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_T,_x},
		{_x,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _x,_T,_x},
		{_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_T,_x, _T,_x,_x,_x, _T,_x,_x,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_T,_x,_T, _x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x},
		{_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_T, _T,_x,_T,_x, _T,_T,_x,_T, _x,_x,_x},
		{_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_T, _T,_x,_T,_x, _T,_x,_x,_T, _x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
	public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public virtual void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "number expected"; break;
			case 3: s = "string expected"; break;
			case 4: s = "badString expected"; break;
			case 5: s = "char expected"; break;
			case 6: s = "\"COMPILER\" expected"; break;
			case 7: s = "\"IGNORECASE\" expected"; break;
			case 8: s = "\"CHARACTERS\" expected"; break;
			case 9: s = "\"TOKENS\" expected"; break;
			case 10: s = "\"PRAGMAS\" expected"; break;
			case 11: s = "\"COMMENTS\" expected"; break;
			case 12: s = "\"FROM\" expected"; break;
			case 13: s = "\"TO\" expected"; break;
			case 14: s = "\"NESTED\" expected"; break;
			case 15: s = "\"IGNORE\" expected"; break;
			case 16: s = "\"SYMBOLTABLES\" expected"; break;
			case 17: s = "\"PRODUCTIONS\" expected"; break;
			case 18: s = "\"=\" expected"; break;
			case 19: s = "\".\" expected"; break;
			case 20: s = "\"END\" expected"; break;
			case 21: s = "\"SCOPES\" expected"; break;
			case 22: s = "\"(\" expected"; break;
			case 23: s = "\",\" expected"; break;
			case 24: s = "\")\" expected"; break;
			case 25: s = "\"+\" expected"; break;
			case 26: s = "\"-\" expected"; break;
			case 27: s = "\"..\" expected"; break;
			case 28: s = "\"ANY\" expected"; break;
			case 29: s = "\":\" expected"; break;
			case 30: s = "\"<\" expected"; break;
			case 31: s = "\">\" expected"; break;
			case 32: s = "\"<.\" expected"; break;
			case 33: s = "\".>\" expected"; break;
			case 34: s = "\"|\" expected"; break;
			case 35: s = "\"WEAK\" expected"; break;
			case 36: s = "\"[\" expected"; break;
			case 37: s = "\"]\" expected"; break;
			case 38: s = "\"{\" expected"; break;
			case 39: s = "\"}\" expected"; break;
			case 40: s = "\"SYNC\" expected"; break;
			case 41: s = "\"IF\" expected"; break;
			case 42: s = "\"CONTEXT\" expected"; break;
			case 43: s = "\"(.\" expected"; break;
			case 44: s = "\".)\" expected"; break;
			case 45: s = "??? expected"; break;
			case 46: s = "this symbol not expected in Coco"; break;
			case 47: s = "this symbol not expected in TokenDecl"; break;
			case 48: s = "invalid TokenDecl"; break;
			case 49: s = "invalid AttrDecl"; break;
			case 50: s = "invalid SimSet"; break;
			case 51: s = "invalid Sym"; break;
			case 52: s = "invalid Term"; break;
			case 53: s = "invalid Factor"; break;
			case 54: s = "invalid Attribs"; break;
			case 55: s = "invalid TokenFactor"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public virtual void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
	
	public virtual void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}
	
	public virtual void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}
	
	public virtual void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}

public class Alternative {
	public readonly Token t;
	public readonly BitArray alt;
	public readonly Symboltable[] st;

	public Alternative(Token t, BitArray alt, Symboltable[] st) {
		this.t = t;
		this.alt = alt;
		this.st = st;
	}
}

public class Symboltable {
	private Stack<List<string>> scopes;
	public readonly string name;
	public readonly bool ignoreCase;

	public Symboltable(string name, bool ignoreCase) {
		this.name = name;
		this.ignoreCase = ignoreCase;
		this.scopes = new Stack<List<string>>();
		pushNewScope();		
	}

	void pushNewScope() {
		scopes.Push(new List<string>());
	}

	void popScope() {
		scopes.Pop();
	}

	public IDisposable createScope() {
		return new Popper(this);
	} 

	public List<string> currentScope {
		get { return scopes.Peek(); } 
	}

	public bool Add(string s) {
		if (ignoreCase) s = s.ToLower();
		if (currentScope.Contains(s))
			return false;
		currentScope.Add(s);
		return true;
	}

	public bool Contains(string s) {
		if (ignoreCase) s = s.ToLower();
		foreach(List<string> list in scopes)
			if (list.Contains(s)) return true;
		return false;
	}

	public IEnumerable<string> items {
		get { 
			Symboltable all = new Symboltable(name, ignoreCase);
			foreach(List<string> list in scopes)
				foreach(string s in list)
					all.Add(s);

			return all.currentScope; 
		}
	}

	private class Popper : IDisposable {
		private readonly Symboltable st;

		public Popper(Symboltable st) {
			this.st = st;
		}

		public void Dispose() {
			st.popScope();
		}
	}
}
}
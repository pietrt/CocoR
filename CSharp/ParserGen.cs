/*-------------------------------------------------------------------------
ParserGen.cs -- Generation of the Recursive Descent Parser
Compiler Generator Coco/R,
Copyright (c) 1990, 2004 Hanspeter Moessenboeck, University of Linz
extended by M. Loeberbauer & A. Woess, Univ. of Linz
with improvements by Pat Terry, Rhodes University
with token inheritance by Martin Lercher, Singhammer dtSoftware Munich

This program is free software; you can redistribute it and/or modify it 
under the terms of the GNU General Public License as published by the 
Free Software Foundation; either version 2, or (at your option) any 
later version.

This program is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License 
for more details.

You should have received a copy of the GNU General Public License along 
with this program; if not, write to the Free Software Foundation, Inc., 
59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.

As an exception, it is allowed to write an extension of Coco/R that is
used as a plugin in non-free software.

If not otherwise stated, any source code generated by Coco/R (other than 
Coco/R itself) does not fall under the GNU General Public License.
-------------------------------------------------------------------------*/
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace at.jku.ssw.Coco {

public class ParserGen {

	const int maxTerm = 3;		// sets of size < maxTerm are enumerated
	const char CR  = '\r';
	const char LF  = '\n';
	const int EOF = -1;

	const int tErr = 0;			// error codes
	const int altErr = 1;
	const int syncErr = 2;
	
	public Position usingPos; // "using" definitions from the attributed grammar
	public bool GenerateAutocompleteInformation = false;  // generate addAlt() calls to fill the "alt" set with alternatives to the next to Get() token.
	public readonly bool ignoreCase; 

	int errorNr;      // highest parser error number
	Symbol curSy;     // symbol whose production is currently generated
	FileStream fram;  // parser frame file
	StreamWriter gen; // generated parser source file
	StringWriter err; // generated parser error messages
	ArrayList symSet = new ArrayList();
	
	Tab tab;          // other Coco objects
	TextWriter trace;
	Errors errors;
	Buffer buffer;
	
	void Indent (int n) {
		for (int i = 1; i <= n; i++) gen.Write('\t');
	}
	
	
	bool Overlaps(BitArray s1, BitArray s2) {
		int len = s1.Count;
		for (int i = 0; i < len; ++i) {
			if (s1[i] && s2[i]) {
				return true;
			}
		}
		return false;
	}
	
	// use a switch if 
	//   more than 5 alternatives 
	//   and none starts with a resolver
	//   and no LL1 warning
	bool UseSwitch (Node p) {
		BitArray s1, s2;
		if (p.typ != Node.alt) return false;
		int nAlts = 0;
		s1 = new BitArray(tab.terminals.Count);
		while (p != null) {
			s2 = tab.Expected0(p.sub, curSy);
			// must not optimize with switch statement, if there are ll1 warnings
			if (Overlaps(s1, s2)) { return false; }
			s1.Or(s2);
			++nAlts;
			// must not optimize with switch-statement, if alt uses a resolver expression
			if (p.sub.typ == Node.rslv) return false;
			p = p.down;
		}
		return nAlts > 5;
	}

	void CopySourcePart (Position pos, int indent) {
		// Copy text described by pos from atg to gen
		int ch, i;
		if (pos != null) {
			buffer.Pos = pos.beg; ch = buffer.Read();
			if (tab.emitLines) {
				gen.WriteLine();
				gen.WriteLine("#line {0} \"{1}\"", pos.line, tab.srcName);
			}
			Indent(indent);
			while (buffer.Pos <= pos.end) {
				while (ch == CR || ch == LF) {  // eol is either CR or CRLF or LF
					gen.WriteLine(); Indent(indent);
					if (ch == CR) ch = buffer.Read(); // skip CR
					if (ch == LF) ch = buffer.Read(); // skip LF
					for (i = 1; i <= pos.col && (ch == ' ' || ch == '\t'); i++) { 
						// skip blanks at beginning of line
						ch = buffer.Read();
					}
					if (buffer.Pos > pos.end) goto done;
				}
				gen.Write((char)ch);
				ch = buffer.Read();
			}
			done:
			if (indent > 0) gen.WriteLine();
		}
	}

	void GenErrorMsg (int errTyp, Symbol sym) {
		errorNr++;
		err.Write("\t\t\tcase " + errorNr + ": s = \"");
		switch (errTyp) {
			case tErr: 
				if (sym.name[0] == '"') err.Write(tab.Escape(sym.name) + " expected");
				else err.Write(sym.name + " expected"); 
				break;
			case altErr: err.Write("invalid " + sym.name); break;
			case syncErr: err.Write("this symbol not expected in " + sym.name); break;
		}
		err.WriteLine("\"; break;");
	}
	
	int NewCondSet (BitArray s) {
		for (int i = 1; i < symSet.Count; i++) // skip symSet[0] (reserved for union of SYNC sets)
			if (Sets.Equals(s, (BitArray)symSet[i])) return i;
		symSet.Add(s.Clone());
		return symSet.Count - 1;
	}

	// for autocomplete/intellisense
	// same as GenCond(), but we only notfiy the 'alt' list of alternatives of new members		
	void GenAutocomplete(BitArray s, Node p, int indent, string comment)
	{
		if (!GenerateAutocompleteInformation) return; // we don't want autocomplete information in the parser
		if (p.typ == Node.rslv) return; // if we have a resolver, we don't know what to do (yet), so we do nothing
		int c = Sets.Elements(s);
		if (c == 0) return;
		if (c > maxTerm) {
			gen.WriteLine("addAlt(set0, {0}); // {1}", NewCondSet(s), comment);
		} else {
			gen.Write("addAlt(");
			if (c > 1) gen.Write("new int[] {");
			int n = 0;
			foreach (Symbol sym in tab.terminals) {
				if (s[sym.n]) {
					n++;
					if (n > 1) gen.Write(", ");
					gen.Write(sym.n);
					// note: we don't need to take sym.inherits or isKind() into account here
					// because we only want to see alternatives as specified in the parser productions.
					// So a keyword:indent = "keyword". token spec will produce only an "ident" variant
					// and not a "keyword" as well as an "ident".
				}
			}
			if (c > 1) gen.Write("}");
			gen.WriteLine("); // {0}", comment);
		}
		Indent(indent);
	}

	void GenAutocomplete(int kind, int indent, string comment) {
		if (!GenerateAutocompleteInformation) return; // we don't want autocomplete information in the parser
		gen.WriteLine("addAlt({0}); // {1}", kind, comment);
		Indent(indent);
	}


	void GenCond (BitArray s, Node p) {
		if (p.typ == Node.rslv) 
			CopySourcePart(p.pos, 0);
		else {
			int n = Sets.Elements(s);
			if (n == 0) 
				gen.Write("false"); // happens if an ANY set matches no symbol
			else if (n <= maxTerm) {
				foreach (Symbol sym in tab.terminals) {
					if (s[sym.n]) {
						gen.Write("isKind(la, {0})", sym.n);
						--n;
						if (n > 0) gen.Write(" || ");
					}
				}
			} else {
				gen.Write("StartOf({0})", NewCondSet(s));
			}
		}
	}
		
	void PutCaseLabels (BitArray s0, int indent) {
		BitArray s = DerivationsOf(s0);
		foreach (Symbol sym in tab.terminals)
			if (s[sym.n]) {
				Indent(indent); 
				gen.WriteLine("case {0}: // {1}", sym.n, sym.name);
			}
		Indent(indent);
	}

	BitArray DerivationsOf(BitArray s0) {
		BitArray s = (BitArray) s0.Clone();
		bool done = false;
		while (!done) {
			done = true;
			foreach (Symbol sym in tab.terminals) {
				if (s[sym.n]) {
					foreach (Symbol baseSym in tab.terminals) {
						if (baseSym.inherits == sym && !s[baseSym.n]) {
							s[baseSym.n] = true;
							done = false;
						}
					}
				}
			}			
		}
		return s;
	}

	void GenSymboltableCheck(Node p, int indent) {
		if (!string.IsNullOrEmpty(p.declares)) {
			Indent(indent);
			gen.WriteLine("if (!{0}.Add(la.val)) SemErr(string.Format(\"{2} '{{0}}' declared twice in '{1}'\", la.val));", p.declares, tab.Escape(p.declares), tab.Escape(p.sym.name));
		} else if (!string.IsNullOrEmpty(p.declared)) {
			Indent(indent);
			gen.WriteLine("if (!{0}.Contains(la.val)) SemErr(string.Format(\"{2} '{{0}}' not declared in '{1}'\", la.val));", p.declared, tab.Escape(p.declared), tab.Escape(p.sym.name));
		} 
	}
	
	void GenCode (Node p, int indent, BitArray isChecked) {
		Node p2;
		BitArray s1, s2;
		while (p != null) {
			switch (p.typ) {
				case Node.nt: {
					Indent(indent);
					gen.Write(p.sym.name + "(");
					CopySourcePart(p.pos, 0);
					gen.WriteLine(");");
					break;
				}
				case Node.t: {
					GenSymboltableCheck(p, indent);
					Indent(indent);
					// assert: if isChecked[p.sym.n] is true, then isChecked contains only p.sym.n
					if (isChecked[p.sym.n]) gen.WriteLine("Get();");
					else {
						GenAutocomplete(p.sym.n, indent, "T");
						gen.WriteLine("Expect({0}); // {1}", p.sym.n, p.sym.name);
					}
					break;
				}
				case Node.wt: {
					GenSymboltableCheck(p, indent);
					Indent(indent);
					s1 = tab.Expected(p.next, curSy);
					s1.Or(tab.allSyncSets);
					int ncs1 = NewCondSet(s1);
					Symbol ncs1sym = (Symbol)tab.terminals[ncs1];
					GenAutocomplete(p.sym.n, indent, "weak T");
					gen.WriteLine("ExpectWeak({0}, {1}); // {2} followed by {3}", p.sym.n, ncs1, p.sym.name, ncs1sym.name);
					break;
				}
				case Node.any: {
					Indent(indent);
					int acc = Sets.Elements(p.set);
					if (tab.terminals.Count == (acc + 1) || (acc > 0 && Sets.Equals(p.set, isChecked))) {
						// either this ANY accepts any terminal (the + 1 = end of file), or exactly what's allowed here
						gen.WriteLine("Get();");
					} else {
						GenErrorMsg(altErr, curSy);
						if (acc > 0) {
							GenAutocomplete(p.set, p, indent, "ANY");
							gen.Write("if ("); GenCond(p.set, p); gen.WriteLine(") Get(); else SynErr({0});", errorNr);
						} else gen.WriteLine("SynErr({0}); // ANY node that matches no symbol", errorNr);
					}
					break;
				}
				case Node.eps: break; // nothing
				case Node.rslv: break; // nothing
				case Node.sem: {
					CopySourcePart(p.pos, indent);
					break;
				}
				case Node.sync: {
					Indent(indent);
					GenErrorMsg(syncErr, curSy);
					s1 = (BitArray)p.set.Clone();
					gen.Write("while (!("); GenCond(s1, p); gen.Write(")) {");
					gen.Write("SynErr({0}); Get();", errorNr); gen.WriteLine("}");
					break;
				}
				case Node.alt: {
					s1 = tab.First(p);
					bool equal = Sets.Equals(s1, isChecked);

					// intellisense
					p2 = p;
					Indent(indent);
					while (p2 != null)
					{
						s1 = tab.Expected(p2.sub, curSy);						
						GenAutocomplete(s1, p2.sub, indent, "ALT");
						p2 = p2.down;
					}
					// end intellisense

					bool useSwitch = UseSwitch(p);
					if (useSwitch) { 
						gen.WriteLine("switch (la.kind) {"); 
					}
					p2 = p;
					while (p2 != null) {
						s1 = tab.Expected(p2.sub, curSy);
						if (useSwitch) { 
							PutCaseLabels(s1, indent);
							gen.WriteLine("{");
						} else if (p2 == p) {
							gen.Write("if ("); GenCond(s1, p2.sub); gen.WriteLine(") {"); 
						} else if (p2.down == null && equal) {
							Indent(indent);  
							gen.WriteLine("} else {");
						} else { 
							Indent(indent); 
							gen.Write("} else if (");  GenCond(s1, p2.sub); gen.WriteLine(") {"); 
						}
						GenCode(p2.sub, indent + 1, s1);
						if (useSwitch) {
							Indent(indent); gen.WriteLine("\tbreak;");
							Indent(indent); gen.WriteLine("}");
						}
						p2 = p2.down;
					}
					Indent(indent);
					if (equal) {
						gen.WriteLine("}");
					} else {
						GenErrorMsg(altErr, curSy);
						if (useSwitch) {
							gen.WriteLine("default: SynErr({0}); break;", errorNr);
							Indent(indent); gen.WriteLine("}");
						} else {
							gen.Write("} "); gen.WriteLine("else SynErr({0});", errorNr);
						}
					}
					break;
				}
				case Node.iter: {
					Indent(indent);
					p2 = p.sub;
					Node pac = p2;
					BitArray sac = (BitArray) tab.First(pac);
					GenAutocomplete(sac, pac, indent, "ITER start");
					gen.Write("while (");
					if (p2.typ == Node.wt) {
						s1 = tab.Expected(p2.next, curSy);
						s2 = tab.Expected(p.next, curSy);
						gen.Write("WeakSeparator({0},{1},{2}) ", p2.sym.n, NewCondSet(s1), NewCondSet(s2));
						s1 = new BitArray(tab.terminals.Count);  // for inner structure
						if (p2.up || p2.next == null) 
							p2 = null; 
						else 
							p2 = p2.next;
					} else {
						s1 = tab.First(p2);
						GenCond(s1, p2);
					}
					gen.WriteLine(") {");
					GenCode(p2, indent + 1, s1);
					Indent(indent + 1);
					GenAutocomplete(sac, pac, 0, "ITER end");
					Indent(indent); gen.WriteLine("}");
					break;
				}
				case Node.opt:
					s1 = tab.First(p.sub);
					Indent(indent);
					GenAutocomplete(s1, p.sub, indent, "OPT");
					gen.Write("if ("); GenCond(s1, p.sub); gen.WriteLine(") {");
					GenCode(p.sub, indent + 1, s1);
					Indent(indent); gen.WriteLine("}");
					break;
			}
			if (p.typ != Node.eps && p.typ != Node.sem && p.typ != Node.sync) 
				isChecked.SetAll(false);  // = new BitArray(tab.terminals.Count);
			if (p.up) break;
			p = p.next;
		}
	}
	
	void GenTokens() {
		foreach (Symbol sym in tab.terminals) {
			if (Char.IsLetter(sym.name[0]))
				gen.WriteLine("\tpublic const int _{0} = {1}; // TOKEN {0}{2}", sym.name, sym.n, sym.inherits != null ? " INHERITS " + sym.inherits.name : "");
		}
	}

	void ForAllTerminals(Action<Symbol> write) {
		int n = 0;
		foreach (Symbol sym in tab.terminals) {
			if (n%20 == 0)
				gen.Write("\t\t");
			else if (n%4 == 0)
				gen.Write(" ");			
			n++;
			write.Invoke(sym);
			if (n < tab.terminals.Count) gen.Write(",");
			if (n%20 == 0) gen.WriteLine();
		}
	}

	void GenTokenBase() {
		ForAllTerminals(delegate(Symbol sym) {
			if (sym.inherits == null)
				gen.Write("{0,2}", -1); // not inherited
			else
				gen.Write("{0,2}", sym.inherits.n);
		});
	}

	void GenTokenNames() {
		ForAllTerminals(delegate(Symbol sym) {
			gen.Write("\"{0}\"", tab.Escape(sym.name));
		});
	}
	
	void GenPragmas() {
		foreach (Symbol sym in tab.pragmas) {
			gen.WriteLine("\tpublic const int _{0} = {1};", sym.name, sym.n);
		}
	}

	void GenCodePragmas() {
		foreach (Symbol sym in tab.pragmas) {
			gen.WriteLine("\t\t\t\tif (la.kind == {0}) {{", sym.n);
			CopySourcePart(sym.semPos, 4);
			gen.WriteLine("\t\t\t\t}");
		}
	}

	void GenProductions() {
		foreach (Symbol sym in tab.nonterminals) {
			curSy = sym;
			gen.Write("\tvoid {0}(", sym.name);
			CopySourcePart(sym.attrPos, 0);
			gen.WriteLine(") {");
			CopySourcePart(sym.semPos, 2);
			GenCode(sym.graph, 2, new BitArray(tab.terminals.Count));
			gen.WriteLine("\t}"); gen.WriteLine();
		}
	}

	void InitSets0() {
		for (int i = 0; i < symSet.Count; i++) {
			BitArray s = (BitArray)symSet[i];
			gen.Write("\t\t{");
			int j = 0;
			foreach (Symbol sym in tab.terminals) {
				if (s[sym.n]) gen.Write("_T,"); else gen.Write("_x,");
				++j;
				if (j%4 == 0) gen.Write(" ");
			}
			// now write an elephant at the last position to not fiddle with the commas:
			if (i == symSet.Count-1) gen.WriteLine("_x}"); else gen.WriteLine("_x},");
		}
	}

	void InitSets() {
		for (int i = 0; i < symSet.Count; i++) {
			BitArray s = DerivationsOf((BitArray)symSet[i]);
			gen.Write("\t\t{");
			int j = 0;
			foreach (Symbol sym in tab.terminals) {
				if (s[sym.n]) gen.Write("_T,"); else gen.Write("_x,");
				++j;
				if (j%4 == 0) gen.Write(" ");
			}
			if (i == symSet.Count-1) gen.WriteLine("_x}"); else gen.WriteLine("_x},");
		}
	}

	void GenSymbolTables(bool declare) {
		foreach (SymTab st in tab.symtabs)
		{
			if (declare)
				gen.WriteLine("\tpublic readonly Symboltable {0} = new Symboltable(\"{0}\", {1});", st.name, ignoreCase ? "true" : "false");
			else
				foreach(string s in st.predefined)
					gen.WriteLine("\t\t{0}.Add(\"{1}\");", st.name, tab.Escape(s));
		}
		if (declare) {
			gen.WriteLine("\tpublic Symboltable symbols(string name) {");
			foreach (SymTab st in tab.symtabs)
				gen.WriteLine("\t\tif (name == \"{1}\") return {0};", st.name, tab.Escape(st.name));
			gen.WriteLine("\t\treturn null;");
			gen.WriteLine("\t}\n");
		}

	} 

	public void WriteParser () {
		Generator g = new Generator(tab);
		int oldPos = buffer.Pos;  // Pos is modified by CopySourcePart
		symSet.Add(tab.allSyncSets);

		fram = g.OpenFrame("Parser.frame");
		gen = g.OpenGen("Parser.cs");
		err = new StringWriter();
		foreach (Symbol sym in tab.terminals) GenErrorMsg(tErr, sym);
		
		g.GenCopyright();
		g.SkipFramePart("-->begin");

		if (usingPos != null) { CopySourcePart(usingPos, 0); gen.WriteLine(); }
		g.CopyFramePart("-->namespace");
		/* AW open namespace, if it exists */
		if (tab.nsName != null && tab.nsName.Length > 0) {
			gen.WriteLine("namespace {0} {{", tab.nsName);
			gen.WriteLine();
		}
		g.CopyFramePart("-->constants");
		GenTokens(); /* ML 2002/09/07 write the token kinds */		
		gen.WriteLine("\tpublic const int maxT = {0};", tab.terminals.Count-1);
		GenPragmas(); /* ML 2005/09/23 write the pragma kinds */
		g.CopyFramePart("-->declarations");
		GenSymbolTables(true);
		CopySourcePart(tab.semDeclPos, 0);
		g.CopyFramePart("-->pragmas"); GenCodePragmas();
		g.CopyFramePart("-->productions"); GenProductions();
		g.CopyFramePart("-->parseRoot"); 
		GenSymbolTables(false);
		gen.WriteLine("\t\t{0}();", tab.gramSy.name); 
		if (tab.checkEOF) gen.WriteLine("\t\tExpect(0);");
		g.CopyFramePart("-->tbase"); GenTokenBase(); // write all tokens base types
		g.CopyFramePart("-->tname"); GenTokenNames(); // write all token names
		g.CopyFramePart("-->initialization0"); InitSets0();
		g.CopyFramePart("-->initialization"); InitSets();
		g.CopyFramePart("-->errors"); gen.Write(err.ToString());
		g.CopyFramePart(null);
		/* AW 2002-12-20 close namespace, if it exists */
		if (tab.nsName != null && tab.nsName.Length > 0) gen.Write("}");
		gen.Close();
		buffer.Pos = oldPos;
	}
	
	public void WriteStatistics () {
		trace.WriteLine();
		trace.WriteLine("{0} terminals", tab.terminals.Count);
		trace.WriteLine("{0} symbols", tab.terminals.Count + tab.pragmas.Count +
		                               tab.nonterminals.Count);
		trace.WriteLine("{0} nodes", tab.nodes.Count);
		trace.WriteLine("{0} sets", symSet.Count);
	}

	public ParserGen (Parser parser) {
		tab = parser.tab;
		errors = parser.errors;
		trace = parser.trace;
		buffer = parser.scanner.buffer;
		ignoreCase = parser.dfa.ignoreCase;
		errorNr = -1;
		usingPos = null;		
	}

} // end ParserGen

} // end namespace

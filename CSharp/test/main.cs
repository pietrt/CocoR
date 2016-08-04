using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;


// AST belongs to parser.frame

public abstract class AST {
    public abstract string val { get; }
    public abstract AST this[int i] { get; }
    public abstract AST this[string s] { get; }
    public abstract int count { get; }
    public static readonly AST empty = new ASTLiteral(string.Empty);
    protected abstract void add(E e);

    public E create(Token t) {
        E e = new E();
        e.ast = new ASTLiteral(t.val);
        return e;
    }

    private abstract class ASTThrows : AST {
        public override string val { get { throw new ApplicationException("not a literal"); } }
        public override AST this[int i] { get { throw new ApplicationException("not a list"); } }
        public override AST this[string s] { get { throw new ApplicationException("not an object"); } }
        protected override void add(E e) { throw new ApplicationException("add not allowed");}
    }

    private class ASTLiteral : ASTThrows {
        public ASTLiteral(string s) { _val = s; }
        private readonly string _val;
        public override string val { get { return _val; } }
        public override int count { get { return -1; } }
    }

    private class ASTList : ASTThrows {
        private readonly List<AST> list;        
        public override AST this[int i] { 
            get { 
                if (i < 0 || count <= i)
                    return AST.empty;
                return list[i];
            } 
        }
        public override int count { get { return list.Count; } }
        
        protected override void add(E e) { 
            list.Add(e.ast); 
        }
    }

    private class ASTObject : ASTThrows {
        private readonly Dictionary<string,AST> ht = new Dictionary<string,AST>();         
        public override AST this[string s] { 
            get { 
                if (!ht.ContainsKey(s))
                    return AST.empty;
                return ht[s];
            } 
        }
        public override int count { get { return ht.Keys.Count; } }
        
        protected override void add(E e) { 
            ht[e.name] = e.ast; 
        }
    }

    public class E {
        public string name;
        public bool islist = false;
        public AST ast;

        public static E createNamedList(string name) {
            E el = new E();
            el.ast = new ASTList();
            el.name = name;
            return el;
        }
    }

    public class Builder {
        private readonly Stack<E> stack = new Stack<E>(); // marker = null

        public Builder() {
            stack.Push(null); // null = TopLevel Object
        }

        public AST current { get { return stack.Peek().ast; } }   

        private E construct() {
            // reverse the stack order:
            Stack<E> list = new Stack<E>();            
            while(true) {
                E e = stack.Pop();
                if (e == null) break;
                list.Push(e);
            }            

            AST obj = null;
            foreach(E e in list)
            {
                if (e.islist) {
                    // list
                    if (!string.IsNullOrEmpty(e.name)) {
                        // list with name
                        if (obj == null) obj = new ASTObject();
                        if (obj[e.name] == AST.empty) obj.add(E.createNamedList(e.name));
                        obj[e.name].add(e);    
                    } else {
                        // list without a name
                        if (obj == null) obj = new ASTList(); 
                        obj.add(e);
                    }
                } else if (!string.IsNullOrEmpty(e.name)) {
                    // named and no list
                    if (obj == null) obj = new ASTObject();
                    obj.add(e);
                } else {
                    // not named and no list
                    if (obj == null) obj = new ASTList(); 
                    obj.add(e);
                }
            }
            E ret = new E();
            ret.ast = obj;
            stack.Push(ret);
            return ret;
        }
    }
}



public class Inheritance {

    static void printST(Symboltable st) {
        Console.WriteLine("--- symbol-table{2} ------------------------------------------------------------------- {0}({1})", st.name, st.CountScopes, st.ignoreCase ? " IGNORECASE" : "");
        int n = 0;
        foreach (Token t in st.currentScope) {
            n++;
            string s = string.Format("{0}({1},{2})", t.val, t.line, t.col);
            Console.Write("{0,-20}  ", s);
            if (n%4 == 0) Console.WriteLine(); 
        }
        if (n%4 != 0) Console.WriteLine();
        Console.WriteLine();
    }

	public static int Main (string[] arg) {
		Console.WriteLine("Inheritance parser");
        if (arg.Length >= 1)
        {
            Console.WriteLine("scanning {0} ...", arg[0]);
            Scanner scanner = new Scanner(arg[0], true); // is UTF8 source
			Parser parser = new Parser(scanner);
            parser.Parse();
            Console.WriteLine("{0} error(s) detected", parser.errors.count);

            // list all symbol table values
            printST(parser.types);
            printST(parser.variables);

            if (arg.Length > 1) {
                // list all alternatives
                int line = 0;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (Alternative a in parser.tokens)
                {
                    Token t = a.t;
                    if (line == 0) {                    
                        sb.Append(new string('-', 50));
                        sb.Append(new string(' ', t.col));
                        sb.Append("  ");
                        line = t.line;
                    }
                    if (line != t.line) {
                        line = t.line;
                        Console.WriteLine(sb.ToString());
                        sb.Length = 0;
                        sb.Append(new string('-', 50));
                        sb.Append(new string(' ', t.col));
                        sb.Append("  ");
                    }
                    sb.Append(t.val); sb.Append(' ');
                    string decl = a.declaration == null ? "" : string.Format(" declared({0},{1})", a.declaration.line, a.declaration.col);
                    Console.Write("({0,3},{1,3}) {2,3} {3,-30} {4, -20}", t.line, t.col, t.kind, Parser.tName[t.kind] + decl, t.val);
                    Console.Write("      alt: ");
                    for (int k = 0; k <= Parser.maxT; k++)
                    {
                        if (a.alt[k]) {
                            Console.Write("{1}", k, Parser.tName[k]);
                            if (a.st[k] != null) {
                                Console.Write(":{0}({1})|", a.st[k].name, a.st[k].CountScopes);
                                foreach (Token tok in a.st[k].currentScope)
                                    Console.Write("{0}({1},{2})|", tok.val, tok.line, tok.col);    
                            }
                            Console.Write(' ');
                        }
                    }
                    Console.WriteLine();
                }
                Console.WriteLine(sb.ToString());
            }
            if (parser.errors.count > 0)
                return 1;
        } else {
            Console.WriteLine("usage: Inheritance.exe file [-ac]");
            return 99;
        }
        return 0;
    }
}
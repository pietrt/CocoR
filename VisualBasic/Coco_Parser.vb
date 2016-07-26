'-------------------------------------------------------------------------------
'Compiler Generator Coco/R,
'Copyright (c) 1990, 2004 Hanspeter Moessenboeck, University of Linz
'extended by M. Loeberbauer & A. Woess, Univ. of Linz
'with improvements by Pat Terry, Rhodes University
'
'This program is free software; you can redistribute it and/or modify it
'under the terms of the GNU General Public License as published by the
'Free Software Foundation; either version 2, or (at your option) any
'later version.
'
'This program is distributed in the hope that it will be useful, but
'WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
'or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
'for more details.
'
'You should have received a copy of the GNU General Public License along
'with this program; if not, write to the Free Software Foundation, Inc.,
'59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
'
'As an exception, it is allowed to write an extension of Coco/R that is
'used as a plugin in non-free software.
'
'If not otherwise stated, any source code generated by Coco/R (other than
'Coco/R itself) does not fall under the GNU General Public License.
'-------------------------------------------------------------------------------
Option Compare Binary
Option Explicit On
Option Strict On

Imports System
Imports System.IO

Namespace at.jku.ssw.Coco

	Public Class Parser
		Public  Const   _EOF        As Integer =  0
		Public  Const   _ident      As Integer =  1
		Public  Const   _number     As Integer =  2
		Public  Const   _string     As Integer =  3
		Public  Const   _badString  As Integer =  4
		Public  Const   _char       As Integer =  5
		Public  Const   maxT        As Integer = 42
		Public  Const   _ddtSym     As Integer = 43
		Public  Const   _optionSym  As Integer = 44
		Private Const   blnT        As Boolean = True
		Private Const   blnX        As Boolean = False
		Private Const   minErrDist  As Integer =  2
		Public          scanner     As Scanner
		Public          errors      As Errors
		Public          t           As Token                ' last recognized token
		Public          la          As Token                ' lookahead token
		Private         errDist     As Integer = minErrDist
		Private Const   id          As Integer =  0
		Private Const   str         As Integer =  1
		' other Coco objects referenced in this ATG
		'Public          trace       As TextWriter
		'Public          tab         As Tab
		Public          dfa         As DFA
		Public          pgen        As ParserGen
		Private         genScanner  As Boolean
		Private         tokenString As String               ' used in declarations of literal tokens
		Private         noString    As String = "-none-"    ' used in declarations of literal tokens
		Private         mTrace      As TextWriter
		Private         mTab        As Tab
		Private         mName       As String
		Public Property trace() As TextWriter
			Get
				Return mTrace
			End Get
			Set(ByVal tw As TextWriter)
				mTrace = tw
			End Set
		End Property
		Public Property tab() As Tab
			Get
				Return mTab
			End Get
			Set(ByVal t As Tab)
				mTab = t
			End Set
		End Property
		Public Property name() As String
			Get
				Return mName
			End Get
			Set(ByVal s As String)
				mName = s
			End Set
		End Property
		Public Sub New(ByVal scanner As Scanner)
			Me.scanner = scanner
			errors = New Errors()
		End Sub
		Private Sub SynErr(ByVal n As Integer)
			If errDist >= minErrDist Then
				errors.SynErr(la.line, la.col, n)
			End If
			errDist = 0
		End Sub
		Public Sub SemErr(ByVal msg As String)
			If errDist >= minErrDist Then
				errors.SemErr(t.line, t.col, msg)
			End If
			errDist = 0
		End Sub
		Private Sub [Get]()
			While True
				t = la
				la = scanner.Scan()
				If la.kind <= maxT Then
					errDist += 1
					Exit While
				End If
				If la.kind = 43 Then
					tab.SetDDT(la.val)
				End If
				If la.kind = 44 Then
					tab.SetOption(la.val)
				End If
				la = t
			End While
		End Sub
		Private Sub Expect(ByVal n As Integer)
			If la.kind = n Then
				[Get]()
			Else
				SynErr(n)
			End If
		End Sub
		Private Function StartOf(ByVal s As Integer) As Boolean
			Return blnSet(s, la.kind)
		End Function
		Private Sub ExpectWeak(ByVal n As Integer, ByVal follow As Integer)
			If la.kind = n Then
				[Get]()
			Else
				SynErr(n)
				While Not StartOf(follow)
					[Get]()
				End While
			End If
		End Sub
		Private Function WeakSeparator(ByVal n As Integer, ByVal syFol As Integer, ByVal repFol As Integer) As Boolean
			Dim kind As Integer = la.kind
			If kind = n Then
				[Get]()
				Return True
			ElseIf StartOf(repFol) Then
				Return False
			Else
				SynErr(n)
				While Not (blnSet(syFol, kind) OrElse blnSet(repFol, kind) OrElse blnSet(0, kind))
					[Get]()
					kind = la.kind
				End While
				Return StartOf(syFol)
			End If
		End Function
		Private Sub Coco()
			Dim sym      As Symbol
			Dim g        As Graph   = Nothing
			Dim g1       As Graph   = Nothing
			Dim g2       As Graph   = Nothing
			Dim gramName As String
			Dim s        As CharSet = Nothing
			Dim beg      As Integer
			Dim line     As Integer
			If la.kind = 6 Then
				[Get]()
				beg  = t.pos
				line = t.line
				While StartOf(1)
					[Get]()
				End While
				pgen.importPos = New Position(beg, la.pos, 0, line)
			End If
			Expect(7)
			genScanner  = True
			tab.ignored = New CharSet()
			Expect(1)
			gramName = t.val
			beg      = la.pos
			line     = la.line
			While StartOf(2)
				[Get]()
			End While
			tab.semDeclPos = New Position(beg, la.pos, 0, line)
			If la.kind = 8 Then
				[Get]()
				dfa.ignoreCase = True ' pdt
			End If
			If la.kind = 9 Then
				[Get]()
				While la.kind = 1
					SetDecl()
				End While
			End If
			If la.kind = 10 Then
				[Get]()
				While la.kind = 1 OrElse la.kind = 3 OrElse la.kind = 5
					TokenDecl(Node.t)
				End While
			End If
			If la.kind = 11 Then
				[Get]()
				While la.kind = 1 OrElse la.kind = 3 OrElse la.kind = 5
					TokenDecl(Node.pr)
				End While
			End If
			While la.kind = 12
				[Get]()
				Dim nested As Boolean = False
				Expect(13)
				TokenExpr(g1)
				Expect(14)
				TokenExpr(g2)
				If la.kind = 15 Then
					[Get]()
					nested = True
				End If
				dfa.NewComment(g1.l, g2.l, nested)
			End While
			While la.kind = 16
				[Get]()
				[Set](s)
				tab.ignored.[Or](s)
			End While
			While Not (la.kind = 0 OrElse la.kind = 17)
				SynErr(43)
				[Get]()
			End While
			Expect(17)
			If genScanner Then
				dfa.MakeDeterministic()
			End If
			tab.DeleteNodes()
			While la.kind = 1
				[Get]()
				sym = tab.FindSym(t.val)
				Dim undef As Boolean = sym Is Nothing
				If undef Then
					sym = tab.NewSym(Node.nt, t.val, t.line)
				Else
					If sym.typ = Node.nt Then
						If sym.graph IsNot Nothing Then
							SemErr("name declared twice")
						End If
					Else
						SemErr("this symbol kind not allowed on left side of production")
					End If
					sym.line = t.line
				End If
				Dim noAttrs As Boolean = sym.attrPos Is Nothing
				sym.attrPos = Nothing
				If la.kind = 25 OrElse la.kind = 27 Then
					AttrDecl(sym)
				End If
				If Not undef Then
					If noAttrs <> (sym.attrPos Is Nothing) Then
						SemErr("attribute mismatch between declaration and use of this symbol")
					End If
				End If
				If la.kind = 40 Then
					SemText(sym.semPos)
				End If
				ExpectWeak(18, 3)
				Expression(g)
				sym.graph = g.l
				tab.Finish(g)
				ExpectWeak(19, 4)
			End While
			Expect(20)
			Expect(1)
			If gramName <> t.val Then
				SemErr("name does not match grammar name")
			End If
			tab.gramSy = tab.FindSym(gramName)
			If tab.gramSy Is Nothing Then
				SemErr("missing production for grammar name")
			Else
				sym = tab.gramSy
				If sym.attrPos IsNot Nothing Then
					SemErr("grammar symbol must not have attributes")
				End If
			End If
			tab.noSym = tab.NewSym(Node.t, "???", 0) ' noSym gets highest number
			tab.SetupAnys()
			tab.RenumberPragmas()
			If tab.ddt(2) Then
				tab.PrintNodes()
			End If
			If errors.count = 0 Then
				Console.WriteLine("checking")
				tab.CompSymbolSets()
				If tab.ddt(7) Then
					tab.XRef()
				End If
				If tab.GrammarOk() Then
					Console.Write("parser")
					pgen.WriteParser()
					If genScanner Then
						Console.Write(" + scanner")
						dfa.WriteScanner()
						If tab.ddt(0) Then
							dfa.PrintStates()
						End If
					End If
					Console.WriteLine(" generated")
					If tab.ddt(8) Then
						pgen.WriteStatistics()
					End If
				End If
			End If
			If tab.ddt(6) Then
				tab.PrintSymbolTable()
			End If
			Expect(19)
		End Sub
		Private Sub SetDecl()
			Dim s As CharSet = Nothing
			Expect(1)
			Dim name As String = t.val
			Dim c As CharClass = tab.FindCharClass(name)
			If c IsNot Nothing Then
				SemErr("name declared twice")
			End If
			Expect(18)
			[Set](s)
			If s.Elements() = 0 Then
				SemErr("character set must not be empty")
			End If
			tab.NewCharClass(name, s)
			Expect(19)
		End Sub
		Private Sub TokenDecl(ByVal typ As Integer)
			Dim name As String = Nothing
			Dim kind As Integer
			Dim _sym As Symbol
			Dim g    As Graph  = Nothing
			Sym(name, kind)
			_sym = tab.FindSym(name)
			If _sym IsNot Nothing Then
				SemErr("name declared twice")
			Else
				_sym = tab.NewSym(typ, name, t.line)
				_sym.tokenKind = Symbol.fixedToken
			End If
			tokenString = Nothing
			While Not (StartOf(5))
				SynErr(44)
				[Get]()
			End While
			If la.kind = 18 Then
				[Get]()
				TokenExpr(g)
				Expect(19)
				If kind = str Then
					SemErr("a literal must not be declared with a structure")
				End If
				tab.Finish(g)
				If tokenString Is Nothing OrElse tokenString.Equals(noString) Then
					dfa.ConvertToStates(g.l, _sym)
				Else
					' TokenExpr is a single string
					If tab.literals(tokenString) IsNot Nothing Then
						SemErr("token string declared twice")
					End If
					tab.literals(tokenString) = _sym
					dfa.MatchLiteral(tokenString, _sym)
				End If
			ElseIf StartOf(6) Then
				If kind = id Then
					genScanner = False
				Else
					dfa.MatchLiteral(_sym.name, _sym)
				End If
			Else
				SynErr(45)
			End If
			If la.kind = 40 Then
				SemText(_sym.semPos)
				If typ <> Node.pr Then
					SemErr("semantic action not allowed here")
				End If
			End If
		End Sub
		Private Sub TokenExpr(ByRef g As Graph)
			Dim g2 As Graph = Nothing
			TokenTerm(g)
			Dim first As Boolean = True
			While WeakSeparator(29, 7, 8)
				TokenTerm(g2)
				If first Then
					tab.MakeFirstAlt(g)
					first = False
				End If
				tab.MakeAlternative(g, g2)
			End While
		End Sub
		Private Sub [Set](ByRef s As CharSet)
			Dim s2 As CharSet = Nothing
			SimSet(s)
			While la.kind = 21 OrElse la.kind = 22
				If la.kind = 21 Then
					[Get]()
					SimSet(s2)
					s.[Or](s2)
				Else
					[Get]()
					SimSet(s2)
					s.Subtract(s2)
				End If
			End While
		End Sub
		Private Sub AttrDecl(ByVal sym As Symbol)
			If la.kind = 25 Then
				[Get]()
				Dim beg  As Integer = la.pos
				Dim col  As Integer = la.col
				Dim line As Integer = la.line
				While StartOf(9)
					If StartOf(10) Then
						[Get]()
					Else
						[Get]()
						SemErr("bad string in attributes")
					End If
				End While
				Expect(26)
				If t.pos > beg Then
					sym.attrPos = New Position(beg, t.pos, col, line)
				End If
			ElseIf la.kind = 27 Then
				[Get]()
				Dim beg  As Integer = la.pos
				Dim col  As Integer = la.col
				Dim line As Integer = la.line
				While StartOf(11)
					If StartOf(12) Then
						[Get]()
					Else
						[Get]()
						SemErr("bad string in attributes")
					End If
				End While
				Expect(28)
				If t.pos > beg Then
					sym.attrPos = New Position(beg, t.pos, col, line)
				End If
			Else
				SynErr(46)
			End If
		End Sub
		Private Sub SemText(ByRef pos As Position)
			Expect(40)
			Dim beg  As Integer = la.pos
			Dim col  As Integer = la.col
			Dim line As Integer = la.line
			While StartOf(13)
				If StartOf(14) Then
					[Get]()
				ElseIf la.kind = 4 Then
					[Get]()
					SemErr("bad string in semantic action")
				Else
					[Get]()
					SemErr("missing end of previous semantic action")
				End If
			End While
			Expect(41)
			pos = New Position(beg, t.pos, col, line)
		End Sub
		Private Sub Expression(ByRef g As Graph)
			Dim g2 As Graph = Nothing
			Term(g)
			Dim first As Boolean = True
			While WeakSeparator(29, 15, 16)
				Term(g2)
				If first Then
					tab.MakeFirstAlt(g)
					first = False
				End If
				tab.MakeAlternative(g, g2)
			End While
		End Sub
		Private Sub SimSet(ByRef s As CharSet)
			Dim n1 As Integer
			Dim n2 As Integer
			s = New CharSet()
			If la.kind = 1 Then
				[Get]()
				Dim c As CharClass = tab.FindCharClass(t.val)
				If c Is Nothing Then
					SemErr("undefined name")
				Else
					s.[Or](c.[set])
				End If
			ElseIf la.kind = 3 Then
				[Get]()
				Dim name As String = t.val
				name = tab.Unescape(name.Substring(1, name.Length - 2))
				For Each ch As Char In name
					If dfa.ignoreCase Then
						s.[Set](AscW(Char.ToLower(ch)))
					Else
						s.[Set](AscW(ch))
					End If
				Next
			ElseIf la.kind = 5 Then
				[Char](n1)
				s.[Set](n1)
				If la.kind = 23 Then
					[Get]()
					[Char](n2)
					For i As Integer = n1 + 1 To n2
						s.[Set](i)
					Next
				End If
			ElseIf la.kind = 24 Then
				[Get]()
				s = New CharSet()
				s.Fill()
			Else
				SynErr(47)
			End If
		End Sub
		Private Sub [Char](ByRef n As Integer)
			Expect(5)
			Dim name As String = t.val
			n = 0
			name = tab.Unescape(name.Substring(1, name.Length - 2))
			If name.Length = 1 Then
				n = AscW(name(0))
			Else
				SemErr("unacceptable character value")
			End If
			If dfa.ignoreCase AndAlso n >= AscW("A"C) AndAlso n <= AscW("Z"C) Then
				n += 32
			End If
		End Sub
		Private Sub Sym(ByRef name As String, ByRef kind As Integer)
			name = "???"
			kind = id
			If la.kind = 1 Then
				[Get]()
				kind = id
				name = t.val
			ElseIf la.kind = 3 OrElse la.kind = 5 Then
				If la.kind = 3 Then
					[Get]()
					name = t.val
				Else
					[Get]()
					name = """" & t.val.Substring(1, t.val.Length - 2) & """"
				End If
				kind = str
				If dfa.ignoreCase Then
					name = name.ToLower()
				End If
				If name.IndexOf(" "C) >= 0 Then
					SemErr("literal tokens must not contain blanks")
				End If
			Else
				SynErr(48)
			End If
		End Sub
		Private Sub Term(ByRef g As Graph)
			Dim g2 As Graph = Nothing
			Dim rslv As Node = Nothing
			g = Nothing
			If StartOf(17) Then
				If la.kind = 38 Then
					rslv = tab.NewNode(Node.rslv, DirectCast(Nothing, Symbol), la.line)
					Resolver(rslv.pos)
					g = New Graph(rslv)
				End If
				Factor(g2)
				If rslv IsNot Nothing Then
					tab.MakeSequence(g, g2)
				Else
					g = g2
				End If
				While StartOf(18)
					Factor(g2)
					tab.MakeSequence(g, g2)
				End While
			ElseIf StartOf(19) Then
				g = New Graph(tab.NewNode(Node.eps, DirectCast(Nothing, Symbol), 0))
			Else
				SynErr(49)
			End If
			If g Is Nothing Then
				g = New Graph(tab.NewNode(Node.eps, DirectCast(Nothing, Symbol), 0)) ' invalid start of Term
			End If
		End Sub
		Private Sub Resolver(ByRef pos As Position)
			Expect(38)
			Expect(31)
			Dim beg  As Integer = la.pos
			Dim col  As Integer = la.col
			Dim line As Integer = la.line
			Condition()
			pos = New Position(beg, t.pos, col, line)
		End Sub
		Private Sub Factor(ByRef g As Graph)
			Dim name As String = Nothing
			Dim kind As Integer
			Dim pos As Position = Nothing
			Dim weak As Boolean = False
			g = Nothing
			Select Case la.kind
				Case 1, 3, 5, 30
					If la.kind = 30 Then
						[Get]()
						weak = True
					End If
					Sym(name, kind)
					Dim _sym As Symbol = tab.FindSym(name)
					If _sym Is Nothing AndAlso kind = str Then
						_sym = TryCast(tab.literals(name), Symbol)
					End If
					Dim undef As Boolean = _sym Is Nothing
					If undef Then
						If kind = id Then
							_sym = tab.NewSym(Node.nt, name, 0)
						ElseIf genScanner Then
							' forward nt
							_sym = tab.NewSym(Node.t, name, t.line)
							dfa.MatchLiteral(_sym.name, _sym)
						Else
							' undefined string in production
							SemErr("undefined string in production") ' dummy
							_sym = tab.eofSy
						End If
					End If
					Dim typ As Integer = _sym.typ
					If typ <> Node.t AndAlso typ <> Node.nt Then
						SemErr("this symbol kind is not allowed in a production")
					End If
					If weak Then
						If typ = Node.t Then
							typ = Node.wt
						Else
							SemErr("only terminals may be weak")
						End If
					End If
					Dim p As Node = tab.NewNode(typ, _sym, t.line)
					g = New Graph(p)
					If la.kind = 25 OrElse la.kind = 27 Then
						Attribs(p)
						If kind <> id Then
							SemErr("a literal must not have attributes")
						End If
					End If
					If undef Then
						_sym.attrPos = p.pos
					ElseIf (p.pos Is Nothing) <> (_sym.attrPos Is Nothing) Then
						SemErr("attribute mismatch between declaration and use of this symbol") ' dummy
					End If
				Case 31
					[Get]()
					Expression(g)
					Expect(32)
				Case 33
					[Get]()
					Expression(g)
					Expect(34)
					tab.MakeOption(g)
				Case 35
					[Get]()
					Expression(g)
					Expect(36)
					tab.MakeIteration(g)
				Case 40
					SemText(pos)
					Dim p As Node = tab.NewNode(Node.sem, DirectCast(Nothing, Symbol), 0)
					p.pos = pos
					g = New Graph(p)
				Case 24
					[Get]()
					Dim p As Node = tab.NewNode(Node.any, DirectCast(Nothing, Symbol), 0) ' p.set is set in tab.SetupAnys
					g = New Graph(p)
				Case 37
					[Get]()
					Dim p As Node = tab.NewNode(Node.sync, DirectCast(Nothing, Symbol), 0)
					g = New Graph(p)
				Case Else
					SynErr(50)
			End Select
			If g Is Nothing Then
				g = New Graph(tab.NewNode(Node.eps, DirectCast(Nothing, Symbol), 0)) ' invalid start of Factor
			End If
		End Sub
		Private Sub Attribs(ByVal p As Node)
			If la.kind = 25 Then
				[Get]()
				Dim beg  As Integer = la.pos
				Dim col  As Integer = la.col
				Dim line As Integer = la.line
				While StartOf(9)
					If StartOf(10) Then
						[Get]()
					Else
						[Get]()
						SemErr("bad string in attributes")
					End If
				End While
				Expect(26)
				If t.pos > beg Then
					p.pos = New Position(beg, t.pos, col, line)
				End If
			ElseIf la.kind = 27 Then
				[Get]()
				Dim beg  As Integer = la.pos
				Dim col  As Integer = la.col
				Dim line As Integer = la.line
				While StartOf(11)
					If StartOf(12) Then
						[Get]()
					Else
						[Get]()
						SemErr("bad string in attributes")
					End If
				End While
				Expect(28)
				If t.pos > beg Then
					p.pos = New Position(beg, t.pos, col, line )
				End If
			Else
				SynErr(51)
			End If
		End Sub
		Private Sub Condition()
			While StartOf(20)
				If la.kind = 31 Then
					[Get]()
					Condition()
				Else
					[Get]()
				End If
			End While
			Expect(32)
		End Sub
		Private Sub TokenTerm(ByRef g As Graph)
			Dim g2 As Graph = Nothing
			TokenFactor(g)
			While StartOf(7)
				TokenFactor(g2)
				tab.MakeSequence(g, g2)
			End While
			If la.kind = 39 Then
				[Get]()
				Expect(31)
				TokenExpr(g2)
				tab.SetContextTrans(g2.l)
				dfa.hasCtxMoves = True
				tab.MakeSequence(g, g2)
				Expect(32)
			End If
		End Sub
		Private Sub TokenFactor(ByRef g As Graph)
			Dim name As String = Nothing
			Dim kind As Integer
			g = Nothing
			If la.kind = 1 OrElse la.kind = 3 OrElse la.kind = 5 Then
				Sym(name, kind)
				If kind = id Then
					Dim c As CharClass = tab.FindCharClass(name)
					If c Is Nothing Then
						SemErr("undefined name")
						c = tab.NewCharClass(name, New CharSet())
					End If
					Dim p As Node = tab.NewNode(Node.clas, DirectCast(Nothing, Symbol), 0)
					p.val = c.n
					g = New Graph(p)
					tokenString = noString
				Else
					' str
					g = tab.StrToGraph(name)
					If tokenString Is Nothing Then
						tokenString = name
					Else
						tokenString = noString
					End If
				End If
			ElseIf la.kind = 31 Then
				[Get]()
				TokenExpr(g)
				Expect(32)
			ElseIf la.kind = 33 Then
				[Get]()
				TokenExpr(g)
				Expect(34)
				tab.MakeOption(g)
				tokenString = noString
			ElseIf la.kind = 35 Then
				[Get]()
				TokenExpr(g)
				Expect(36)
				tab.MakeIteration(g)
				tokenString = noString
			Else
				SynErr(52)
			End If
			If g Is Nothing Then
				g = New Graph(tab.NewNode(Node.eps, DirectCast(Nothing, Symbol), 0)) ' invalid start of TokenFactor
			End If
		End Sub
		Public Sub Parse()
			la = New Token()
			la.val = ""
			[Get]()
			Coco()
		End Sub
		Private Shared ReadOnly blnSet(,) As Boolean = { _
			{blnT,blnT,blnX,blnT, blnX,blnT,blnX,blnX, blnX,blnX,blnX,blnT, blnT,blnX,blnX,blnX, blnT,blnT,blnT,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnT,blnX,blnX,blnX}, _
			{blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnX, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnX}, _
			{blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnX,blnX,blnX,blnX, blnX,blnT,blnT,blnT, blnX,blnX,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnX}, _
			{blnT,blnT,blnX,blnT, blnX,blnT,blnX,blnX, blnX,blnX,blnX,blnT, blnT,blnX,blnX,blnX, blnT,blnT,blnT,blnT, blnX,blnX,blnX,blnX, blnT,blnX,blnX,blnX, blnX,blnT,blnT,blnT, blnX,blnT,blnX,blnT, blnX,blnT,blnT,blnX, blnT,blnX,blnX,blnX}, _
			{blnT,blnT,blnX,blnT, blnX,blnT,blnX,blnX, blnX,blnX,blnX,blnT, blnT,blnX,blnX,blnX, blnT,blnT,blnT,blnX, blnT,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnT,blnX,blnX,blnX}, _
			{blnT,blnT,blnX,blnT, blnX,blnT,blnX,blnX, blnX,blnX,blnX,blnT, blnT,blnX,blnX,blnX, blnT,blnT,blnT,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnT,blnX,blnX,blnX}, _
			{blnX,blnT,blnX,blnT, blnX,blnT,blnX,blnX, blnX,blnX,blnX,blnT, blnT,blnX,blnX,blnX, blnT,blnT,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnT,blnX,blnX,blnX}, _
			{blnX,blnT,blnX,blnT, blnX,blnT,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnT, blnX,blnT,blnX,blnT, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX}, _
			{blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnT,blnX,blnT,blnT, blnT,blnT,blnX,blnT, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnT,blnX,blnT,blnX, blnT,blnX,blnX,blnX, blnX,blnX,blnX,blnX}, _
			{blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnX,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnX}, _
			{blnX,blnT,blnT,blnT, blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnX,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnX}, _
			{blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnX}, _
			{blnX,blnT,blnT,blnT, blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnX}, _
			{blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnX,blnT,blnX}, _
			{blnX,blnT,blnT,blnT, blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnX,blnX,blnT,blnX}, _
			{blnX,blnT,blnX,blnT, blnX,blnT,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnT, blnX,blnX,blnX,blnX, blnT,blnX,blnX,blnX, blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnX, blnT,blnX,blnX,blnX}, _
			{blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnT, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnT,blnX,blnT,blnX, blnT,blnX,blnX,blnX, blnX,blnX,blnX,blnX}, _
			{blnX,blnT,blnX,blnT, blnX,blnT,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnT,blnX,blnX,blnX, blnX,blnX,blnT,blnT, blnX,blnT,blnX,blnT, blnX,blnT,blnT,blnX, blnT,blnX,blnX,blnX}, _
			{blnX,blnT,blnX,blnT, blnX,blnT,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnT,blnX,blnX,blnX, blnX,blnX,blnT,blnT, blnX,blnT,blnX,blnT, blnX,blnT,blnX,blnX, blnT,blnX,blnX,blnX}, _
			{blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnT, blnX,blnX,blnX,blnX, blnX,blnX,blnX,blnX, blnX,blnT,blnX,blnX, blnT,blnX,blnT,blnX, blnT,blnX,blnX,blnX, blnX,blnX,blnX,blnX}, _
			{blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnX,blnT,blnT,blnT, blnT,blnT,blnT,blnT, blnT,blnT,blnT,blnX} _
		}
	End Class

	Public Class Errors
		Public count        As Integer                                 ' number of errors detected
		Public errorStream  As TextWriter = Console.Out                ' error messages go to this stream
		Public errMsgFormat As String     = "-- line {0} col {1}: {2}" ' 0=line, 1=column, 2=text
		Public Overridable Sub SynErr(ByVal line As Integer, ByVal col As Integer, ByVal n As Integer)
			Dim s As String
			Select Case n
				Case    0 : s = "EOF expected"
				Case    1 : s = "ident expected"
				Case    2 : s = "number expected"
				Case    3 : s = "string expected"
				Case    4 : s = "badString expected"
				Case    5 : s = "char expected"
				Case    6 : s = """Imports"" expected"
				Case    7 : s = """COMPILER"" expected"
				Case    8 : s = """IGNORECASE"" expected"
				Case    9 : s = """CHARACTERS"" expected"
				Case   10 : s = """TOKENS"" expected"
				Case   11 : s = """PRAGMAS"" expected"
				Case   12 : s = """COMMENTS"" expected"
				Case   13 : s = """FROM"" expected"
				Case   14 : s = """TO"" expected"
				Case   15 : s = """NESTED"" expected"
				Case   16 : s = """IGNORE"" expected"
				Case   17 : s = """PRODUCTIONS"" expected"
				Case   18 : s = """="" expected"
				Case   19 : s = """."" expected"
				Case   20 : s = """END"" expected"
				Case   21 : s = """+"" expected"
				Case   22 : s = """-"" expected"
				Case   23 : s = """.."" expected"
				Case   24 : s = """ANY"" expected"
				Case   25 : s = """<"" expected"
				Case   26 : s = """>"" expected"
				Case   27 : s = """<."" expected"
				Case   28 : s = """.>"" expected"
				Case   29 : s = """|"" expected"
				Case   30 : s = """WEAK"" expected"
				Case   31 : s = """("" expected"
				Case   32 : s = """)"" expected"
				Case   33 : s = """["" expected"
				Case   34 : s = """]"" expected"
				Case   35 : s = """{"" expected"
				Case   36 : s = """}"" expected"
				Case   37 : s = """SYNC"" expected"
				Case   38 : s = """IF"" expected"
				Case   39 : s = """CONTEXT"" expected"
				Case   40 : s = """(."" expected"
				Case   41 : s = """.)"" expected"
				Case   42 : s = "??? expected"
				Case   43 : s = "this symbol not expected in Coco"
				Case   44 : s = "this symbol not expected in TokenDecl"
				Case   45 : s = "invalid TokenDecl"
				Case   46 : s = "invalid AttrDecl"
				Case   47 : s = "invalid SimSet"
				Case   48 : s = "invalid Sym"
				Case   49 : s = "invalid Term"
				Case   50 : s = "invalid Factor"
				Case   51 : s = "invalid Attribs"
				Case   52 : s = "invalid TokenFactor"
				Case Else : s = "error " & n
			End Select
			errorStream.WriteLine(errMsgFormat, line, col, s)
			count += 1
		End Sub
		Public Overridable Sub SemErr(ByVal line As Integer, ByVal col As Integer, ByVal s As String)
			errorStream.WriteLine(errMsgFormat, line, col, s)
			count += 1
		End Sub
		Public Overridable Sub SemErr(ByVal s As String)
			errorStream.WriteLine(s)
			count += 1
		End Sub
		Public Overridable Sub Warning(ByVal line As Integer, ByVal col As Integer, ByVal s As String)
			errorStream.WriteLine(errMsgFormat, line, col, s)
		End Sub
		Public Overridable Sub Warning(ByVal s As String)
			errorStream.WriteLine(s)
		End Sub
	End Class

	Public Class FatalError
		Inherits Exception
		Public Sub New(ByVal m As String)
			MyBase.New(m)
		End Sub
	End Class

End Namespace

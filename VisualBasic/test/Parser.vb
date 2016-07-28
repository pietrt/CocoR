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


Public Class Parser
	Public  Const   _EOF        As Integer =  0 ' TOKEN EOF       
	Public  Const   _ident      As Integer =  1 ' TOKEN ident     
	Public  Const   _keyword    As Integer =  2 ' TOKEN keyword   
	Public  Const   _var        As Integer =  3 ' TOKEN var       INHERITS ident
	Public  Const   _var1       As Integer =  4 ' TOKEN var1      INHERITS ident
	Public  Const   _var2       As Integer =  5 ' TOKEN var2      INHERITS ident
	Public  Const   _var3       As Integer =  6 ' TOKEN var3      INHERITS ident
	Public  Const   _var4       As Integer =  7 ' TOKEN var4      INHERITS ident
	Public  Const   _var5       As Integer =  8 ' TOKEN var5      INHERITS ident
	Public  Const   _var6       As Integer =  9 ' TOKEN var6      INHERITS ident
	Public  Const   _as         As Integer = 10 ' TOKEN as        INHERITS ident
	Public  Const   _colon      As Integer = 11 ' TOKEN colon     
	Public  Const   maxT        As Integer = 26
	Private Const   _T        As Boolean = True
	Private Const   _x        As Boolean = False
	Private Const   minErrDist  As Integer =  2
	Public          scanner     As Scanner
	Public          errors      As Errors
	Public          t           As Token                ' last recognized token
	Public          la          As Token                ' lookahead token
	Private         errDist     As Integer = minErrDist
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
			la = t
		End While
	End Sub
	Private Function isKind(ByVal t as Token, ByVal n as Integer) as Boolean
		Dim k as Integer = t.kind
		Do While k >= 0
			If k = n Then Return True
			k = tBase(k)
		Loop
		Return False
	End Function
	Private Sub Expect(ByVal n As Integer)
		If isKind(la, n) Then
			[Get]()
		Else
			SynErr(n)
		End If
	End Sub
	' is the lookahead token la a start of the production s?
	Private Function StartOf(ByVal s As Integer) As Boolean
		Return blnSet(s, la.kind)
	End Function
	Private Sub ExpectWeak(ByVal n As Integer, ByVal follow As Integer)
		If isKind(la, n) Then
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
		If isKind(la, n) Then
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
	Private Sub Inheritance()
		While StartOf(1)
			Declaration()
		End While
		While isKind(la, 12)
			[Get]()
			NumberIdent()
			Console.WriteLine("NumberIdent {0}", t.val)
			Expect(13) ' ";"
		End While
		While StartOf(2)
			IdentOrNumber()
		End While
	End Sub
	Private Sub Declaration()
		Var()
		Ident()
		While isKind(la, 14) OrElse isKind(la, 15)
			Separator()
			Ident()
		End While
		While Not (isKind(la, 0) OrElse isKind(la, 13))
			SynErr(27)
			[Get]()
		End While
		Expect(13) ' ";"
	End Sub
	Private Sub NumberIdent()
		Select Case la.kind
			Case 16 ' "0"
				[Get]()
			Case 17 ' "1"
				[Get]()
			Case 18 ' "2"
				[Get]()
			Case 19 ' "3"
				[Get]()
			Case 20 ' "4"
				[Get]()
			Case 21 ' "5"
				[Get]()
			Case 22 ' "6"
				[Get]()
			Case 23 ' "7"
				[Get]()
			Case 24 ' "8"
				[Get]()
			Case 25 ' "9"
				[Get]()
			Case 1, 3, 4, 5, 6, 7, 8, 9, 10 ' 1:ident 3:var 4:var1 5:var2 6:var3 7:var4 8:var5 9:var6 10:as 
				[Get]()
			Case Else
				SynErr(28)
		End Select
	End Sub
	Private Sub IdentOrNumber()
		If isKind(la, 1) Then
			[Get]()
		ElseIf StartOf(3) Then
			NumberVar()
		Else
			SynErr(29)
		End If
	End Sub
	Private Sub Var()
		Select Case la.kind
			Case 3 ' var
				[Get]()
			Case 4 ' var1
				[Get]()
			Case 5 ' var2
				[Get]()
			Case 6 ' var3
				[Get]()
			Case 7 ' var4
				[Get]()
			Case 8 ' var5
				[Get]()
			Case 9 ' var6
				[Get]()
			Case Else
				SynErr(30)
		End Select
	End Sub
	Private Sub Ident()
		Expect(1) ' ident
		If isKind(la, 10) OrElse isKind(la, 11) Then
			If isKind(la, 10) Then
				[Get]()
			Else
				[Get]()
			End If
			Expect(1) ' ident
		End If
	End Sub
	Private Sub Separator()
		If isKind(la, 14) Then
			ExpectWeak(14, 4) ' "," followed by var1
		ElseIf isKind(la, 15) Then
			ExpectWeak(15, 4) ' "|" followed by var1
		Else
			SynErr(31)
		End If
	End Sub
	Private Sub NumberVar()
		Select Case la.kind
			Case 16 ' "0"
				[Get]()
			Case 17 ' "1"
				[Get]()
			Case 18 ' "2"
				[Get]()
			Case 19 ' "3"
				[Get]()
			Case 20 ' "4"
				[Get]()
			Case 21 ' "5"
				[Get]()
			Case 22 ' "6"
				[Get]()
			Case 23 ' "7"
				[Get]()
			Case 24 ' "8"
				[Get]()
			Case 25 ' "9"
				[Get]()
			Case 3 ' var
				[Get]()
			Case Else
				SynErr(32)
		End Select
	End Sub
	Public Sub Parse()
		la = New Token()
		la.val = ""
		[Get]()
		Inheritance()
	End Sub
	' a token's base type
	Private Shared ReadOnly tBase() as Integer = { _
		-1,-1,-1, 1,  1, 1, 1, 1,  1, 1, 1,-1, -1,-1,-1,-1, -1,-1,-1,-1, _
		-1,-1,-1,-1, -1,-1,-1 _
	}
	' states that a particular production (1st index) can start with a particular token (2nd index)
	Private Shared ReadOnly blnSet0(,) As Boolean = { _
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x}, _
		{_x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x}, _
		{_x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x,_x}, _
		{_x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x,_x}, _
		{_T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x} _
	}
	' as blnSet0 but with token inheritance taken into account
	Private Shared ReadOnly blnSet(,) As Boolean = { _
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x}, _
		{_x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x}, _
		{_x,_T,_x,_T, _T,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x,_x}, _
		{_x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x,_x}, _
		{_T,_T,_x,_T, _T,_T,_T,_T, _T,_T,_T,_x, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x} _
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
			Case    2 : s = "keyword expected"
			Case    3 : s = "var expected"
			Case    4 : s = "var1 expected"
			Case    5 : s = "var2 expected"
			Case    6 : s = "var3 expected"
			Case    7 : s = "var4 expected"
			Case    8 : s = "var5 expected"
			Case    9 : s = "var6 expected"
			Case   10 : s = "as expected"
			Case   11 : s = "colon expected"
			Case   12 : s = """NumberIdent"" expected"
			Case   13 : s = """;"" expected"
			Case   14 : s = ""","" expected"
			Case   15 : s = """|"" expected"
			Case   16 : s = """0"" expected"
			Case   17 : s = """1"" expected"
			Case   18 : s = """2"" expected"
			Case   19 : s = """3"" expected"
			Case   20 : s = """4"" expected"
			Case   21 : s = """5"" expected"
			Case   22 : s = """6"" expected"
			Case   23 : s = """7"" expected"
			Case   24 : s = """8"" expected"
			Case   25 : s = """9"" expected"
			Case   26 : s = "??? expected"
			Case   27 : s = "this symbol not expected in Declaration"
			Case   28 : s = "invalid NumberIdent"
			Case   29 : s = "invalid IdentOrNumber"
			Case   30 : s = "invalid Var"
			Case   31 : s = "invalid Separator"
			Case   32 : s = "invalid NumberVar"
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
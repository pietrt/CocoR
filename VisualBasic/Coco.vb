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
'-------------------------------------------------------------------------------
'  Trace output options
'  0 | A: prints the states of the scanner automaton
'  1 | F: prints the First and Follow sets of all nonterminals
'  2 | G: prints the syntax graph of the productions
'  3 | I: traces the computation of the First sets
'  4 | J: prints the sets associated with ANYs and synchronisation sets
'  6 | S: prints the symbol table (terminals, nonterminals, pragmas)
'  7 | X: prints a cross reference list of all syntax symbols
'  8 | P: prints statistics about the Coco run
'
'  Trace output can be switched on by the pragma
'    $ { digit | letter }
'  in the attributed grammar or as a command-line option
'-------------------------------------------------------------------------------
Option Compare Binary
Option Explicit On
Option Strict On

Imports System
Imports System.IO

Namespace at.jku.ssw.Coco

	Public Class Coco
		Public Shared Function Main(ByVal arg() As String) As Integer
			Console.WriteLine("Coco/R (Dec 22, 2014)")
			Dim srcName As String = Nothing
			Dim nsName As String = Nothing
			Dim frameDir As String = Nothing
			Dim ddtString As String = Nothing
			Dim traceFileName As String = Nothing
			Dim outDir As String = Nothing
			Dim emitLines As Boolean = False
			Dim retVal As Integer = 1
			Dim i As Integer = 0
			While True
				If arg.Length = 0 Then Exit While
				If i >= arg.Length Then Exit While
				If arg(i) = "-namespace" AndAlso i < arg.Length - 1 Then
					i += 1 : nsName = arg(i).Trim()
				ElseIf arg(i) = "-frames" AndAlso i < arg.Length - 1 Then
					i += 1 : frameDir = arg(i).Trim()
				ElseIf arg(i) = "-trace" AndAlso i < arg.Length - 1 Then
					i += 1 : ddtString = arg(i).Trim()
				ElseIf arg(i) = "-o" AndAlso i < arg.Length - 1 Then
					i += 1 : outDir = arg(i).Trim()
				ElseIf arg(i) = "-lines" Then
					i += 1 : emitLines = True
				Else
					srcName = arg(i)
				End If
				i += 1
			End While
			If arg.Length > 0 AndAlso srcName IsNot Nothing Then
				Try
					Dim srcDir As String = Path.GetDirectoryName(srcName)
					Dim scanner As New Scanner(srcName)
					Dim parser As New Parser(scanner)
					traceFileName = Path.Combine(srcDir, "Trace.txt")
					parser.errors.errMsgFormat = srcName & "({0},{1}): {2}"
					parser.trace = New StreamWriter(New FileStream(traceFileName, FileMode.Create))
					parser.tab = New Tab(parser)
					parser.dfa = New DFA(parser)
					parser.pgen = New ParserGen(parser)
					parser.tab.srcName = srcName
					parser.tab.srcDir = srcDir
					parser.tab.nsName = nsName
					parser.tab.frameDir = frameDir
					If (outDir IsNot Nothing) Then
						parser.tab.outDir = outDir
					Else
						parser.tab.outDir = srcDir
					End If
					parser.tab.emitLines = emitLines
					If ddtString IsNot Nothing Then
						parser.tab.SetDDT(ddtString)
					End If
					parser.Parse()
					parser.trace.Close()
					Dim f As New FileInfo(traceFileName)
					If f.Length = 0 Then
						f.Delete()
					Else
						Console.WriteLine("trace output is in " & traceFileName)
					End If
					Console.WriteLine("{0} errors detected", parser.errors.count)
					If (parser.errors.count = 0) Then
						retVal = 0
					End If
				Catch generatedExceptionName As IOException
					Console.WriteLine("-- could not open " & traceFileName)
				Catch e As FatalError
					Console.WriteLine("-- " & e.Message)
				End Try
			Else
				Console.WriteLine( _
					"Usage: Coco Grammar.ATG {{Option}}{0}" _
					& "Options:{0}" _
					& "  -namespace <namespaceName>{0}" _
					& "  -frames    <frameFilesDirectory>{0}" _
					& "  -trace     <traceString>{0}" _
					& "  -o         <outputDirectory>{0}" _
					& "  -lines{0}" _
					& "Valid characters in the trace string:{0}" _
					& "  A  trace automaton{0}" _
					& "  F  list first/follow sets{0}" _
					& "  G  print syntax graph{0}" _
					& "  I  trace computation of first sets{0}" _
					& "  J  list ANY and SYNC sets{0}" _
					& "  P  print statistics{0}" _
					& "  S  list symbol table{0}" _
					& "  X  list cross reference table{0}" _
					& "Scanner.frame and Parser.frame files needed in ATG directory{0}" _
					& "or in a directory specified in the -frames option.", _
					vbNewLine _
				)
			End If
			Return retVal
		End Function
	End Class

End Namespace

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using CocoRCore;

namespace CocoRCore.Samples.CodeLens
{

    public class Parser : ParserBase 
    {
        public const int _ident = 1; // TOKEN ident
        public const int _dottedident = 2; // TOKEN dottedident
        public const int _number = 3; // TOKEN number
        public const int _int = 4; // TOKEN int
        public const int _string = 5; // TOKEN string
        public const int _braced = 6; // TOKEN braced
        public const int _bracketed = 7; // TOKEN bracketed
        public const int _end = 8; // TOKEN end
        public const int _dot = 9; // TOKEN dot
        public const int _bar = 10; // TOKEN bar
        public const int _colon = 11; // TOKEN colon
        public const int _versionnumber = 12; // TOKEN versionnumber
        public const int _version = 13; // TOKEN version INHERITS ident
        public const int _search = 14; // TOKEN search INHERITS ident
        public const int _select = 15; // TOKEN select INHERITS ident
        public const int _details = 16; // TOKEN details INHERITS ident
        public const int _edit = 17; // TOKEN edit INHERITS ident
        public const int _clear = 18; // TOKEN clear INHERITS ident
        public const int _keys = 19; // TOKEN keys INHERITS ident
        public const int _displayname = 20; // TOKEN displayname INHERITS ident
        public const int _vbident = 21; // TOKEN vbident INHERITS ident
        private const int __maxT = 72;
        private const bool _T = true;
        private const bool _x = false;
        
        public readonly Symboltable types;
        public readonly Symboltable enumtypes;
        public Symboltable symbols(string name)
        {
            if (name == "types") return types;
            if (name == "enumtypes") return enumtypes;
            return null;
        }



        public Parser()
        {
            types = new Symboltable("types", true, false, this);
            enumtypes = new Symboltable("enumtypes", true, false, this);
        }

        public static Parser Create(string fileName) 
            => Create(s => s.Initialize(fileName));

        public static Parser Create() 
            => Create(s => { });

        public static Parser Create(Action<Scanner> init)
        {
            var p = new Parser();
            var scanner = new Scanner();
            p.Initialize(scanner);
            init(scanner);
            return p;
        }


        public override int maxT => __maxT;

        protected override void Get() 
        {
            lb = t;
            t = la;
            if (alternatives != null) 
            {
                AlternativeTokens.Add(new Alternative(t, alternatives));
            }
            _newAlt();
            for (;;) 
            {
                la = scanner.Scan();
                if (la.kind <= maxT) 
                { 
                    ++errDist; 
                    break; // it's not a pragma
                }
                // pragma code
            }
        }


        void CodeLens‿NT()
        {
            {
                Version‿NT();
                Namespace‿NT();
                addAlt(23); // OPT
                if (isKind(la, 23 /*ReaderWriterPrefix*/))
                {
                    ReaderWriterPrefix‿NT();
                }
                RootClass‿NT();
                addAlt(set0, 1); // ITER start
                while (StartOf(1))
                {
                    addAlt(26); // ALT
                    addAlt(62); // ALT
                    addAlt(70); // ALT
                    addAlt(69); // ALT
                    if (isKind(la, 26 /*Class*/))
                    {
                        Class‿NT();
                    }
                    else if (isKind(la, 62 /*SubSystem*/))
                    {
                        SubSystem‿NT();
                    }
                    else if (isKind(la, 70 /*Enum*/))
                    {
                        Enum‿NT();
                    }
                    else
                    {
                        Flags‿NT();
                    }
                    addAlt(set0, 1); // ITER end
                }
                EndNamespace‿NT();
            }
        }


        void Version‿NT()
        {
            {
                addAlt(13); // T version
                Expect(13 /*version*/);
                addAlt(12); // T versionnumber
                Expect(12 /*[versionnumber]*/);
            }
        }


        void Namespace‿NT()
        {
            {
                while (!(isKind(la, 0 /*[EOF]*/) || isKind(la, 22 /*Namespace*/)))
                {
                    SynErr(74);
                    Get();
                }
                addAlt(22); // T "namespace"
                Expect(22 /*Namespace*/);
                DottedIdent‿NT();
            }
        }


        void ReaderWriterPrefix‿NT()
        {
            {
                while (!(isKind(la, 0 /*[EOF]*/) || isKind(la, 23 /*ReaderWriterPrefix*/)))
                {
                    SynErr(75);
                    Get();
                }
                addAlt(23); // T "readerwriterprefix"
                Expect(23 /*ReaderWriterPrefix*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
            }
        }


        void RootClass‿NT()
        {
            {
                while (!(isKind(la, 0 /*[EOF]*/) || isKind(la, 24 /*RootClass*/)))
                {
                    SynErr(76);
                    Get();
                }
                addAlt(24); // T "rootclass"
                Expect(24 /*RootClass*/);
                addAlt(25); // T "data"
                Expect(25 /*Data*/);
                Properties‿NT();
                addAlt(8); // T end
                Expect(8 /*end*/);
                addAlt(26); // T "class"
                Expect(26 /*Class*/);
            }
        }


        void Class‿NT()
        {
            {
                while (!(isKind(la, 0 /*[EOF]*/) || isKind(la, 26 /*Class*/)))
                {
                    SynErr(77);
                    Get();
                }
                addAlt(26); // T "class"
                Expect(26 /*Class*/);
                if (!types.Add(la)) SemErr(71, string.Format(DuplicateSymbol, "ident", la.val, types.name), la);
                alternatives.stdeclares = types;
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                addAlt(6); // OPT
                if (isKind(la, 6 /*[braced]*/))
                {
                    Title‿NT();
                }
                addAlt(28); // OPT
                if (isKind(la, 28 /*Inherits*/))
                {
                    Inherits‿NT();
                }
                addAlt(27); // OPT
                if (isKind(la, 27 /*Via*/))
                {
                    Via‿NT();
                }
                Properties‿NT();
                addAlt(8); // T end
                Expect(8 /*end*/);
                addAlt(26); // T "class"
                Expect(26 /*Class*/);
            }
        }


        void SubSystem‿NT()
        {
            {
                while (!(isKind(la, 0 /*[EOF]*/) || isKind(la, 62 /*SubSystem*/)))
                {
                    SynErr(78);
                    Get();
                }
                addAlt(62); // T "subsystem"
                Expect(62 /*SubSystem*/);
                if (!types.Add(la)) SemErr(71, string.Format(DuplicateSymbol, "ident", la.val, types.name), la);
                alternatives.stdeclares = types;
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                addAlt(63); // T "ssname"
                Expect(63 /*SSName*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                addAlt(64); // T "ssconfig"
                Expect(64 /*SSConfig*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                addAlt(65); // T "sstyp"
                Expect(65 /*SSTyp*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                addAlt(66); // T "sscommands"
                Expect(66 /*SSCommands*/);
                SSCommands‿NT();
                addAlt(67); // OPT
                if (isKind(la, 67 /*SSKey*/))
                {
                    Get();
                    addAlt(5); // T string
                    Expect(5 /*[string]*/);
                }
                addAlt(68); // OPT
                if (isKind(la, 68 /*SSClear*/))
                {
                    Get();
                    addAlt(5); // T string
                    Expect(5 /*[string]*/);
                }
                addAlt(30); // ITER start
                while (isKind(la, 30 /*InfoProperty*/))
                {
                    InfoProperty‿NT();
                    addAlt(30); // ITER end
                }
                addAlt(8); // T end
                Expect(8 /*end*/);
                addAlt(62); // T "subsystem"
                Expect(62 /*SubSystem*/);
            }
        }


        void Enum‿NT()
        {
            {
                while (!(isKind(la, 0 /*[EOF]*/) || isKind(la, 70 /*Enum*/)))
                {
                    SynErr(79);
                    Get();
                }
                addAlt(70); // T "enum"
                Expect(70 /*Enum*/);
                if (!enumtypes.Add(la)) SemErr(71, string.Format(DuplicateSymbol, "ident", la.val, enumtypes.name), la);
                alternatives.stdeclares = enumtypes;
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                EnumValues‿NT();
                addAlt(8); // T end
                Expect(8 /*end*/);
                addAlt(70); // T "enum"
                Expect(70 /*Enum*/);
            }
        }


        void Flags‿NT()
        {
            {
                while (!(isKind(la, 0 /*[EOF]*/) || isKind(la, 69 /*Flags*/)))
                {
                    SynErr(80);
                    Get();
                }
                addAlt(69); // T "flags"
                Expect(69 /*Flags*/);
                if (!types.Add(la)) SemErr(71, string.Format(DuplicateSymbol, "ident", la.val, types.name), la);
                alternatives.stdeclares = types;
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                addAlt(1); // ITER start
                while (isKind(la, 1 /*[ident]*/))
                {
                    EnumValue‿NT();
                    addAlt(1); // ITER end
                }
                addAlt(8); // T end
                Expect(8 /*end*/);
                addAlt(69); // T "flags"
                Expect(69 /*Flags*/);
            }
        }


        void EndNamespace‿NT()
        {
            {
                addAlt(8); // T end
                Expect(8 /*end*/);
                addAlt(22); // T "namespace"
                Expect(22 /*Namespace*/);
            }
        }


        void DottedIdent‿NT()
        {
            {
                addAlt(2); // OPT
                if (isKind(la, 2 /*[dottedident]*/))
                {
                    Get();
                    addAlt(9); // T dot
                    Expect(9 /*.*/);
                    addAlt(2); // ITER start
                    while (isKind(la, 2 /*[dottedident]*/))
                    {
                        Get();
                        addAlt(9); // T dot
                        Expect(9 /*.*/);
                        addAlt(2); // ITER end
                    }
                }
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
            }
        }


        void Properties‿NT()
        {
            {
                addAlt(set0, 2); // ITER start
                while (StartOf(2))
                {
                    Prop‿NT();
                    addAlt(set0, 2); // ITER end
                }
            }
        }


        void Title‿NT()
        {
            {
                addAlt(6); // T braced
                Expect(6 /*[braced]*/);
            }
        }


        void Inherits‿NT()
        {
            {
                addAlt(28); // T "inherits"
                Expect(28 /*Inherits*/);
                DottedIdent‿NT();
            }
        }


        void Via‿NT()
        {
            {
                addAlt(27); // T "via"
                Expect(27 /*Via*/);
                DottedIdent‿NT();
            }
        }


        void Prop‿NT()
        {
            {
                while (!(StartOf(3)))
                {
                    SynErr(81);
                    Get();
                }
                addAlt(29); // ALT
                addAlt(30); // ALT
                addAlt(31); // ALT
                addAlt(32); // ALT
                addAlt(33); // ALT
                addAlt(34); // ALT
                addAlt(35); // ALT
                addAlt(36); // ALT
                switch (la.kind)
                {
                    case 29: /*Property*/
                        { // scoping
                            Property‿NT();
                        }
                        break;
                    case 30: /*InfoProperty*/
                        { // scoping
                            InfoProperty‿NT();
                        }
                        break;
                    case 31: /*APProperty*/
                        { // scoping
                            APProperty‿NT();
                        }
                        break;
                    case 32: /*List*/
                        { // scoping
                            List‿NT();
                        }
                        break;
                    case 33: /*SelectList*/
                        { // scoping
                            SelectList‿NT();
                        }
                        break;
                    case 34: /*FlagsList*/
                        { // scoping
                            FlagsList‿NT();
                        }
                        break;
                    case 35: /*LongProperty*/
                        { // scoping
                            LongProperty‿NT();
                        }
                        break;
                    case 36: /*InfoLongProperty*/
                        { // scoping
                            InfoLongProperty‿NT();
                        }
                        break;
                    default:
                        SynErr(82);
                        break;
                } // end switch
            }
        }


        void Property‿NT()
        {
            {
                addAlt(29); // T "property"
                Expect(29 /*Property*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                Type‿NT();
            }
        }


        void InfoProperty‿NT()
        {
            {
                addAlt(30); // T "infoproperty"
                Expect(30 /*InfoProperty*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                Type‿NT();
            }
        }


        void APProperty‿NT()
        {
            {
                addAlt(31); // T "approperty"
                Expect(31 /*APProperty*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                Type‿NT();
            }
        }


        void List‿NT()
        {
            {
                addAlt(32); // T "list"
                Expect(32 /*List*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                addAlt(41); // OPT
                if (isKind(la, 41 /*As*/))
                {
                    As‿NT();
                }
            }
        }


        void SelectList‿NT()
        {
            {
                addAlt(33); // T "selectlist"
                Expect(33 /*SelectList*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                As‿NT();
            }
        }


        void FlagsList‿NT()
        {
            {
                addAlt(34); // T "flagslist"
                Expect(34 /*FlagsList*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                Mimics‿NT();
            }
        }


        void LongProperty‿NT()
        {
            {
                addAlt(35); // T "longproperty"
                Expect(35 /*LongProperty*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
            }
        }


        void InfoLongProperty‿NT()
        {
            {
                addAlt(36); // T "infolongproperty"
                Expect(36 /*InfoLongProperty*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
            }
        }


        void Type‿NT()
        {
            {
                addAlt(41); // ALT
                addAlt(57); // ALT
                addAlt(set0, 4); // ALT
                if (isKind(la, 41 /*As*/))
                {
                    As‿NT();
                }
                else if (isKind(la, 57 /*Mimics*/))
                {
                    Mimics‿NT();
                }
                else if (StartOf(4))
                {
                } // end if
                else
                    SynErr(83);
                addAlt(37); // OPT
                if (isKind(la, 37 /*=*/))
                {
                    Get();
                    InitValue‿NT();
                }
                addAlt(6); // OPT
                if (isKind(la, 6 /*[braced]*/))
                {
                    SampleValue‿NT();
                }
            }
        }


        void As‿NT()
        {
            {
                addAlt(41); // T "as"
                Expect(41 /*As*/);
                addAlt(set0, 5); // ALT
                addAlt(1); // ALT
                addAlt(1, types); // ALT ident uses symbol table 'types'
                addAlt(new int[] {1, 2}); // ALT
                if (StartOf(5))
                {
                    BaseType‿NT();
                }
                else if (isKind(la, 1 /*[ident]*/))
                {
                    if (!types.Use(la, alternatives)) SemErr(72, string.Format(MissingSymbol, "ident", la.val, types.name), la);
                    Get();
                }
                else if (isKind(la, 1 /*[ident]*/) || isKind(la, 2 /*[dottedident]*/))
                {
                    DottedIdent‿NT();
                } // end if
                else
                    SynErr(84);
            }
        }


        void Mimics‿NT()
        {
            {
                addAlt(57); // T "mimics"
                Expect(57 /*Mimics*/);
                addAlt(set0, 6); // ALT
                addAlt(1); // ALT
                addAlt(1, enumtypes); // ALT ident uses symbol table 'enumtypes'
                if (StartOf(6))
                {
                    MimicsSpec‿NT();
                }
                else if (isKind(la, 1 /*[ident]*/))
                {
                    if (!enumtypes.Use(la, alternatives)) SemErr(72, string.Format(MissingSymbol, "ident", la.val, enumtypes.name), la);
                    Get();
                } // end if
                else
                    SynErr(85);
            }
        }


        void InitValue‿NT()
        {
            {
                addAlt(3); // ALT
                addAlt(4); // ALT
                addAlt(5); // ALT
                addAlt(38); // ALT
                addAlt(39); // ALT
                addAlt(40); // ALT
                addAlt(new int[] {1, 2}); // ALT
                switch (la.kind)
                {
                    case 3: /*[number]*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 4: /*[int]*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 5: /*[string]*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 38: /*true*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 39: /*false*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 40: /*#*/
                        { // scoping
                            Get();
                            addAlt(set0, 7); // ITER start
                            while (StartOf(7))
                            {
                                Get();
                                addAlt(set0, 7); // ITER end
                            }
                            addAlt(40); // T "#"
                            Expect(40 /*#*/);
                        }
                        break;
                    case 1: /*[ident]*/
                    case 2: /*[dottedident]*/
                    case 13: /*version*/
                    case 14: /*search*/
                    case 15: /*select*/
                    case 16: /*details*/
                    case 17: /*edit*/
                    case 18: /*clear*/
                    case 19: /*keys*/
                    case 20: /*displayname*/
                    case 21: /*[vbident]*/
                        { // scoping
                            FunctionCall‿NT();
                        }
                        break;
                    default:
                        SynErr(86);
                        break;
                } // end switch
            }
        }


        void SampleValue‿NT()
        {
            {
                addAlt(6); // T braced
                Expect(6 /*[braced]*/);
            }
        }


        void FunctionCall‿NT()
        {
            {
                DottedIdent‿NT();
                addAlt(7); // T bracketed
                Expect(7 /*[bracketed]*/);
            }
        }


        void BaseType‿NT()
        {
            {
                addAlt(42); // ALT
                addAlt(43); // ALT
                addAlt(44); // ALT
                addAlt(45); // ALT
                addAlt(46); // ALT
                addAlt(47); // ALT
                addAlt(48); // ALT
                addAlt(49); // ALT
                addAlt(50); // ALT
                addAlt(51); // ALT
                addAlt(52); // ALT
                addAlt(53); // ALT
                addAlt(54); // ALT
                addAlt(55); // ALT
                addAlt(56); // ALT
                switch (la.kind)
                {
                    case 42: /*double*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 43: /*date*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 44: /*datetime*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 45: /*integer*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 46: /*percent*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 47: /*percentwithdefault*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 48: /*doublewithdefault*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 49: /*integerwithdefault*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 50: /*n2*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 51: /*n0*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 52: /*String*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 53: /*boolean*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 54: /*Guid*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 55: /*String()*/
                        { // scoping
                            Get();
                        }
                        break;
                    case 56: /*XML*/
                        { // scoping
                            Get();
                        }
                        break;
                    default:
                        SynErr(87);
                        break;
                } // end switch
            }
        }


        void MimicsSpec‿NT()
        {
            {
                addAlt(58); // ALT
                addAlt(59); // ALT
                addAlt(60); // ALT
                addAlt(61); // ALT
                if (isKind(la, 58 /*query*/))
                {
                    Query‿NT();
                }
                else if (isKind(la, 59 /*txt*/))
                {
                    Txt‿NT();
                }
                else if (isKind(la, 60 /*xl*/))
                {
                    XL‿NT();
                }
                else if (isKind(la, 61 /*ref*/))
                {
                    Ref‿NT();
                } // end if
                else
                    SynErr(88);
            }
        }


        void Query‿NT()
        {
            {
                addAlt(58); // T "query"
                Expect(58 /*query*/);
                addAlt(11); // T colon
                Expect(11 /*:*/);
                addAlt(2); // T dottedident
                Expect(2 /*[dottedident]*/);
                addAlt(9); // T dot
                Expect(9 /*.*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                addAlt(11); // T colon
                Expect(11 /*:*/);
                StringOrIdent‿NT();
            }
        }


        void Txt‿NT()
        {
            {
                addAlt(59); // T "txt"
                Expect(59 /*txt*/);
                addAlt(11); // T colon
                Expect(11 /*:*/);
                addAlt(2); // T dottedident
                Expect(2 /*[dottedident]*/);
                addAlt(9); // T dot
                Expect(9 /*.*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                addAlt(11); // T colon
                Expect(11 /*:*/);
                StringOrIdent‿NT();
            }
        }


        void XL‿NT()
        {
            {
                addAlt(60); // T "xl"
                Expect(60 /*xl*/);
                addAlt(11); // T colon
                Expect(11 /*:*/);
                addAlt(2); // T dottedident
                Expect(2 /*[dottedident]*/);
                addAlt(9); // T dot
                Expect(9 /*.*/);
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                addAlt(11); // T colon
                Expect(11 /*:*/);
                StringOrIdent‿NT();
            }
        }


        void Ref‿NT()
        {
            {
                addAlt(61); // T "ref"
                Expect(61 /*ref*/);
                addAlt(11); // T colon
                Expect(11 /*:*/);
                addAlt(19); // ALT
                addAlt(20); // ALT
                if (isKind(la, 19 /*keys*/))
                {
                    Get();
                }
                else if (isKind(la, 20 /*displayname*/))
                {
                    Get();
                } // end if
                else
                    SynErr(89);
                addAlt(11); // T colon
                Expect(11 /*:*/);
                StringOrIdent‿NT();
            }
        }


        void StringOrIdent‿NT()
        {
            {
                addAlt(5); // ALT
                addAlt(new int[] {1, 2}); // ALT
                if (isKind(la, 5 /*[string]*/))
                {
                    Get();
                }
                else if (isKind(la, 1 /*[ident]*/) || isKind(la, 2 /*[dottedident]*/))
                {
                    DottedIdent‿NT();
                } // end if
                else
                    SynErr(90);
            }
        }


        void SSCommands‿NT()
        {
            {
                SSCommand‿NT();
                addAlt(10); // ITER start
                while (isKind(la, 10 /*|*/))
                {
                    Get();
                    SSCommand‿NT();
                    addAlt(10); // ITER end
                }
            }
        }


        void SSCommand‿NT()
        {
            {
                addAlt(14); // ALT
                addAlt(15); // ALT
                addAlt(16); // ALT
                addAlt(17); // ALT
                addAlt(18); // ALT
                if (isKind(la, 14 /*search*/))
                {
                    Get();
                }
                else if (isKind(la, 15 /*select*/))
                {
                    Get();
                }
                else if (isKind(la, 16 /*details*/))
                {
                    Get();
                }
                else if (isKind(la, 17 /*edit*/))
                {
                    Get();
                }
                else if (isKind(la, 18 /*clear*/))
                {
                    Get();
                } // end if
                else
                    SynErr(91);
            }
        }


        void EnumValue‿NT()
        {
            {
                addAlt(1); // T ident
                Expect(1 /*[ident]*/);
                addAlt(37); // OPT
                if (isKind(la, 37 /*=*/))
                {
                    EnumIntValue‿NT();
                }
            }
        }


        void EnumValues‿NT()
        {
            {
                addAlt(1); // ITER start
                while (isKind(la, 1 /*[ident]*/))
                {
                    EnumValue‿NT();
                    addAlt(1); // ITER end
                }
                addAlt(71); // T "default"
                Expect(71 /*DEFAULT*/);
                EnumValue‿NT();
                addAlt(1); // ITER start
                while (isKind(la, 1 /*[ident]*/))
                {
                    EnumValue‿NT();
                    addAlt(1); // ITER end
                }
            }
        }


        void EnumIntValue‿NT()
        {
            {
                addAlt(37); // T "="
                Expect(37 /*=*/);
                addAlt(4); // T int
                Expect(4 /*[int]*/);
            }
        }



        public override void Parse() 
        {
            if (scanner == null) throw new FatalError("This parser is not Initialize()-ed.");
            lb = Token.Zero;
            la = Token.Zero;
            Get();
            CodeLens‿NT();
            Expect(0);
            types.CheckDeclared();
            enumtypes.CheckDeclared();
        }
    
        // a token's base type
        public static readonly int[] tBase = {
            -1,-1,-1,-1,  -1,-1,-1,-1,  -1,-1,-1,-1,  -1, 1, 1, 1,   1, 1, 1, 1,
             1, 1,-1,-1,  -1,-1,-1,-1,  -1,-1,-1,-1,  -1,-1,-1,-1,  -1,-1,-1,-1,
            -1,-1,-1,-1,  -1,-1,-1,-1,  -1,-1,-1,-1,  -1,-1,-1,-1,  -1,-1,-1,-1,
            -1,-1,-1,-1,  -1,-1,-1,-1,  -1,-1,-1,-1,  -1
        };
		protected override int BaseKindOf(int kind) => tBase[kind];

        // a token's name
        public static readonly string[] varTName = {
            "[EOF]",
            "[ident]",
            "[dottedident]",
            "[number]",
            "[int]",
            "[string]",
            "[braced]",
            "[bracketed]",
            "end",
            ".",
            "|",
            ":",
            "[versionnumber]",
            "version",
            "search",
            "select",
            "details",
            "edit",
            "clear",
            "keys",
            "displayname",
            "[vbident]",
            "Namespace",
            "ReaderWriterPrefix",
            "RootClass",
            "Data",
            "Class",
            "Via",
            "Inherits",
            "Property",
            "InfoProperty",
            "APProperty",
            "List",
            "SelectList",
            "FlagsList",
            "LongProperty",
            "InfoLongProperty",
            "=",
            "true",
            "false",
            "#",
            "As",
            "double",
            "date",
            "datetime",
            "integer",
            "percent",
            "percentwithdefault",
            "doublewithdefault",
            "integerwithdefault",
            "n2",
            "n0",
            "String",
            "boolean",
            "Guid",
            "String()",
            "XML",
            "Mimics",
            "query",
            "txt",
            "xl",
            "ref",
            "SubSystem",
            "SSName",
            "SSConfig",
            "SSTyp",
            "SSCommands",
            "SSKey",
            "SSClear",
            "Flags",
            "Enum",
            "DEFAULT",
            "[???]"
        };
        public override string NameOfTokenKind(int tokenKind) => varTName[tokenKind];

		// states that a particular production (1st index) can start with a particular token (2nd index). Needed by addAlt().
		static readonly bool[,] set0 = {
            {_T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_T,  _T,_x,_T,_x,  _x,_T,_T,_T,  _T,_T,_T,_T,  _T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_x,  _x,_x,_x,_x,  _x,_T,_T,_x,  _x,_x},
            {_x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_x,  _x,_x,_x,_x,  _x,_T,_T,_x,  _x,_x},
            {_x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_T,_T,_T,  _T,_T,_T,_T,  _T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x},
            {_T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_T,_T,_T,  _T,_T,_T,_T,  _T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x},
            {_x,_x,_x,_x,  _x,_x,_T,_x,  _T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x},
            {_x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x},
            {_x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_T,  _T,_T,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x},
            {_x,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _x,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_x}
		};

        // as set0 but with token inheritance taken into account
        static readonly bool[,] set = {
            {_T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_T,  _T,_x,_T,_x,  _x,_T,_T,_T,  _T,_T,_T,_T,  _T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_x,  _x,_x,_x,_x,  _x,_T,_T,_x,  _x,_x},
            {_x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_x,  _x,_x,_x,_x,  _x,_T,_T,_x,  _x,_x},
            {_x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_T,_T,_T,  _T,_T,_T,_T,  _T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x},
            {_T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_T,_T,_T,  _T,_T,_T,_T,  _T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x},
            {_x,_x,_x,_x,  _x,_x,_T,_x,  _T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x},
            {_x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x},
            {_x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x,_T,_T,  _T,_T,_x,_x,  _x,_x,_x,_x,  _x,_x,_x,_x,  _x,_x},
            {_x,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _x,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_T,_T,_T,  _T,_x}
        };

        protected override bool StartOf(int s, int kind) => set[s, kind];



        public override string Syntaxerror(int n) 
        {
            switch (n) 
            {
                case 1: return "[EOF] expected";
                case 2: return "[ident] expected";
                case 3: return "[dottedident] expected";
                case 4: return "[number] expected";
                case 5: return "[int] expected";
                case 6: return "[string] expected";
                case 7: return "[braced] expected";
                case 8: return "[bracketed] expected";
                case 9: return "end expected";
                case 10: return ". expected";
                case 11: return "| expected";
                case 12: return ": expected";
                case 13: return "[versionnumber] expected";
                case 14: return "version expected";
                case 15: return "search expected";
                case 16: return "select expected";
                case 17: return "details expected";
                case 18: return "edit expected";
                case 19: return "clear expected";
                case 20: return "keys expected";
                case 21: return "displayname expected";
                case 22: return "[vbident] expected";
                case 23: return "Namespace expected";
                case 24: return "ReaderWriterPrefix expected";
                case 25: return "RootClass expected";
                case 26: return "Data expected";
                case 27: return "Class expected";
                case 28: return "Via expected";
                case 29: return "Inherits expected";
                case 30: return "Property expected";
                case 31: return "InfoProperty expected";
                case 32: return "APProperty expected";
                case 33: return "List expected";
                case 34: return "SelectList expected";
                case 35: return "FlagsList expected";
                case 36: return "LongProperty expected";
                case 37: return "InfoLongProperty expected";
                case 38: return "= expected";
                case 39: return "true expected";
                case 40: return "false expected";
                case 41: return "# expected";
                case 42: return "As expected";
                case 43: return "double expected";
                case 44: return "date expected";
                case 45: return "datetime expected";
                case 46: return "integer expected";
                case 47: return "percent expected";
                case 48: return "percentwithdefault expected";
                case 49: return "doublewithdefault expected";
                case 50: return "integerwithdefault expected";
                case 51: return "n2 expected";
                case 52: return "n0 expected";
                case 53: return "String expected";
                case 54: return "boolean expected";
                case 55: return "Guid expected";
                case 56: return "String() expected";
                case 57: return "XML expected";
                case 58: return "Mimics expected";
                case 59: return "query expected";
                case 60: return "txt expected";
                case 61: return "xl expected";
                case 62: return "ref expected";
                case 63: return "SubSystem expected";
                case 64: return "SSName expected";
                case 65: return "SSConfig expected";
                case 66: return "SSTyp expected";
                case 67: return "SSCommands expected";
                case 68: return "SSKey expected";
                case 69: return "SSClear expected";
                case 70: return "Flags expected";
                case 71: return "Enum expected";
                case 72: return "DEFAULT expected";
                case 73: return "[???] expected";
                case 74: return "symbol not expected in Namespace (SYNC error)";
                case 75: return "symbol not expected in ReaderWriterPrefix (SYNC error)";
                case 76: return "symbol not expected in RootClass (SYNC error)";
                case 77: return "symbol not expected in Class (SYNC error)";
                case 78: return "symbol not expected in SubSystem (SYNC error)";
                case 79: return "symbol not expected in Enum (SYNC error)";
                case 80: return "symbol not expected in Flags (SYNC error)";
                case 81: return "symbol not expected in Prop (SYNC error)";
                case 82: return "invalid Prop, expected Property InfoProperty APProperty List SelectList FlagsList LongProperty InfoLongProperty";
                case 83: return "invalid Type, expected As Mimics [braced] end Property InfoProperty APProperty List SelectList FlagsList LongProperty InfoLongProperty =";
                case 84: return "invalid As, expected double date datetime integer percent percentwithdefault doublewithdefault integerwithdefault n2 n0 String boolean Guid String() XML [ident] [dottedident]";
                case 85: return "invalid Mimics, expected query txt xl ref [ident]";
                case 86: return "invalid InitValue, expected [number] [int] [string] true false # [ident] [dottedident]";
                case 87: return "invalid BaseType, expected double date datetime integer percent percentwithdefault doublewithdefault integerwithdefault n2 n0 String boolean Guid String() XML";
                case 88: return "invalid MimicsSpec, expected query txt xl ref";
                case 89: return "invalid Ref, expected keys displayname";
                case 90: return "invalid StringOrIdent, expected [string] [ident] [dottedident]";
                case 91: return "invalid SSCommand, expected search select details edit clear";
                default: return $"error {n}";
            }
        }

    } // end Parser

// end namespace implicit
}
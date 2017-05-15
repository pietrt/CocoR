/*-------------------------------------------------------------------------
DFA.cs -- Generation of the Scanner Automaton
Compiler Generator Coco/R,
Copyright (c) 1990, 2004 Hanspeter Moessenboeck, University of Linz
extended by M. Loeberbauer & A. Woess, Univ. of Linz
with improvements by Pat Terry, Rhodes University

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

namespace CocoRCore.CSharp // was at.jku.ssw.Coco for .Net V2
{
    //-----------------------------------------------------------------------------
    //  State
    //-----------------------------------------------------------------------------

    public class State
    {               // state of finite automaton
        public int nr;                      // state number
        public Action firstAction;// to first action of this state
        public Symbol endOf;            // recognized token if state is final
        public bool ctx;                    // true if state is reached via contextTrans
        public State next;

        public void AddAction(Action act)
        {
            Action lasta = null, a = firstAction;
            while (a != null && act.typ >= a.typ) { lasta = a; a = a.next; }
            // collecting classes at the beginning gives better performance
            act.next = a;
            if (a == firstAction) firstAction = act; else lasta.next = act;
        }

        public void DetachAction(Action act)
        {
            Action lasta = null, a = firstAction;
            while (a != null && a != act) { lasta = a; a = a.next; }
            if (a != null)
                if (a == firstAction) firstAction = a.next; else lasta.next = a.next;
        }

        public void MeltWith(State s)
        { // copy actions of s to state
            for (Action action = s.firstAction; action != null; action = action.next)
            {
                Action a = new Action(action.typ, action.sym, action.tc);
                a.AddTargets(action);
                AddAction(a);
            }
        }

    }

} // end namespace
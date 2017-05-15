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
using System;
using System.IO;

namespace CocoRCore.CSharp // was at.jku.ssw.Coco for .Net V2
{
    //-----------------------------------------------------------------------------
    //  Generator
    //-----------------------------------------------------------------------------
    public class Generator
    {
        private const int EOF = -1;

        private FileStream fram;
        private StreamWriter gen;
        private readonly Tab tab;
        private string frameFile;

        public Generator(Tab tab)
        {
            this.tab = tab;
        }

        public FileStream OpenFrame(String frame)
        {
            if (tab.frameDir != null) frameFile = Path.Combine(tab.frameDir, frame);
            if (frameFile == null || !File.Exists(frameFile)) frameFile = Path.Combine(tab.srcDir, frame);
            if (frameFile == null || !File.Exists(frameFile)) throw new FatalError("Cannot find : " + frame);

            try
            {
                fram = new FileStream(frameFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (FileNotFoundException)
            {
                throw new FatalError("Cannot open frame file: " + frameFile);
            }
            return fram;
        }



        public StreamWriter OpenGen(string target)
        {
            string fn = Path.Combine(tab.outDir, target);
            try
            {
                if (File.Exists(fn)) File.Copy(fn, fn + ".old", true);
                gen = new StreamWriter(new FileStream(fn, FileMode.Create)); /* pdt */
            }
            catch (IOException)
            {
                throw new FatalError("Cannot generate file: " + fn);
            }
            return gen;
        }


        public void GenCopyright()
        {
            string copyFr = null;
            if (tab.frameDir != null) copyFr = Path.Combine(tab.frameDir, "Copyright.frame");
            if (copyFr == null || !File.Exists(copyFr)) copyFr = Path.Combine(tab.srcDir, "Copyright.frame");
            if (copyFr == null || !File.Exists(copyFr)) return;

            try
            {
                FileStream scannerFram = fram;
                fram = new FileStream(copyFr, FileMode.Open, FileAccess.Read, FileShare.Read);
                CopyFramePart(null);
                fram = scannerFram;
            }
            catch (FileNotFoundException)
            {
                throw new FatalError("Cannot open Copyright.frame");
            }
        }

        public void SkipFramePart(String stop)
        {
            CopyFramePart(stop, false);
        }


        public void CopyFramePart(String stop)
        {
            CopyFramePart(stop, true);
        }

        // if stop == null, copies until end of file
        public void CopyFramePart(string stop, bool generateOutput)
        {
            char startCh = (char)0;
            int endOfStopString = 0;

            if (stop != null)
            {
                startCh = stop[0];
                endOfStopString = stop.Length - 1;
            }

            int ch = framRead();
            while (ch != EOF)
            {
                if (stop != null && ch == startCh)
                {
                    int i = 0;
                    do
                    {
                        if (i == endOfStopString) return; // stop[0..i] found
                        ch = framRead(); i++;
                    } while (ch == stop[i]);
                    // stop[0..i-1] found; continue with last read character
                    if (generateOutput) gen.Write(stop.Substring(0, i));
                }
                else
                {
                    if (generateOutput) gen.Write((char)ch);
                    ch = framRead();
                }
            }

            if (stop != null) throw new FatalError("Incomplete or corrupt frame file: " + frameFile);
        }

        private int framRead()
        {
            try
            {
                return fram.ReadByte();
            }
            catch (Exception)
            {
                throw new FatalError("Error reading frame file: " + frameFile);
            }
        }
    }

} // end namespace

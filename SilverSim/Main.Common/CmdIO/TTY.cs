﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Main.Common.CmdIO
{
    public abstract class TTY 
    {
        public TTY()
        {

        }

        public UUID SelectedScene = UUID.Zero;

        public abstract void Write(string text);
        
        public void WriteFormatted(string format, params object[] parms)
        {
            Write(String.Format(format, parms));
        }

        public virtual bool HasPrompt 
        { 
            get
            {
                return false;
            }
        }

        public virtual void LockOutput()
        {

        }

        public virtual void UnlockOutput()
        {

        }

        public string CmdPrompt { get; set; }

        public virtual string ReadLine(string p, bool echoInput)
        {
            return string.Empty;
        }

        public string GetInput(string prompt)
        {
            return ReadLine(String.Format("{0}: ", prompt), true);
        }

        public string GetInput(string prompt, string defaultvalue)
        {
            string res = ReadLine(String.Format("{0} [{1}]: ", prompt, defaultvalue), true);
            if(res == string.Empty)
            {
                res = defaultvalue;
            }

            return res;
        }

        public string GetPass(string prompt)
        {
            return ReadLine(String.Format("{0}: ", prompt), false);
        }

        public List<string> GetCmdLine(string cmdline)
        {
            List<string> cmdargs = new List<string>();
            cmdline = cmdline.Trim();
            if (cmdline == string.Empty)
            {
                return cmdargs;
            }

            bool indoublequotes = false;
            bool insinglequotes = false;
            bool inargument = false;
            bool hasescape = false;
            string argument = string.Empty;

            foreach (char c in cmdline)
            {
                if (indoublequotes)
                {
                    if (hasescape)
                    {
                        hasescape = false;
                        argument += c;
                    }
                    else
                    {
                        switch (c)
                        {
                            case '\\':
                                hasescape = true;
                                break;

                            case '\"':
                                indoublequotes = false;
                                cmdargs.Add(argument);
                                argument = string.Empty;
                                break;

                            default:
                                argument += c;
                                break;
                        }
                    }
                }
                else if (insinglequotes)
                {
                    if (hasescape)
                    {
                        hasescape = false;
                        argument += c;
                    }
                    else
                    {
                        switch (c)
                        {
                            case '\\':
                                hasescape = true;
                                break;

                            case '\"':
                                insinglequotes = false;
                                cmdargs.Add(argument);
                                argument = string.Empty;
                                break;

                            default:
                                argument += c;
                                break;
                        }
                    }
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (inargument)
                    {
                        cmdargs.Add(argument);
                        argument = "";
                    }
                }
                else
                {
                    argument += c;
                    inargument = true;
                }
            }

            if (indoublequotes || insinglequotes || inargument)
            {
                cmdargs.Add(argument);
            }

            return cmdargs;
        }
    }
}

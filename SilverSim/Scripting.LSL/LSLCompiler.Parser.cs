﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {
        private CompilerException parserException(Parser p, string message)
        {
            string fname;
            int lineno;
            p.getfileinfo(out fname, out lineno);
            return new CompilerException(lineno, message);
        }

        private void checkValidName(Parser p, string type, string name)
        {
            if (name.Length == 0)
            {
                throw parserException(p, string.Format("{1} name '{0}' is not valid.", name, type));
            }
            if (name[0] != '_' && !(name[0] >= 'a' && name[0] <= 'z') && !(name[0] >= 'A' && name[0] <= 'Z') && name[0] != '_' && name[0] != '$')
            {
                throw parserException(p, string.Format("{1} name '{0}' is not valid.", name, type));
            }
            foreach (char c in name.Substring(1))
            {
                if (!(name[0] >= 'a' && name[0] <= 'z') && !(name[0] >= 'A' && name[0] <= 'Z') && name[0] != '_' && name[0] != '$')
                {
                    throw parserException(p, string.Format("{1} name '{0}' is not valid.", name, type));
                }
            }
        }

        private void checkUsedName(CompileState cs, Parser p, string type, string name)
        {
            checkValidName(p, type, name);
            if (m_ReservedWords.Contains(name))
            {
                throw parserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is a reserved word.", name, type));
            }
            else if (m_MethodNames.Contains(name))
            {
                throw parserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined function name.", name, type));
            }
            else if (m_Constants.ContainsKey(name) && (m_Constants[name] & cs.AcceptedFlags) != 0)
            {
                throw parserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined constant.", name, type));
            }
            else if (m_EventDelegates.ContainsKey(name))
            {
                throw parserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined constant.", name, type));
            }
            else if (cs.m_VariableDeclarations.ContainsKey(name))
            {
                throw parserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined as user variable.", name, type));
            }
            else if (cs.m_Functions.ContainsKey(name))
            {
                throw parserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined as user function.", name, type));
            }
            if(cs.m_LocalVariables.Count == 0)
            {

            }
            else if (cs.m_LocalVariables[cs.m_LocalVariables.Count - 1].Contains(name))
            {
                throw parserException(p, string.Format("{1} cannot be declared as '{0}'. '{0}' is an already defined as local variable in the same block.", name, type));
            }
        }

        struct FuncParamInfo
        {
            public Type Type;
            public string Name;
        }

        List<FuncParamInfo> checkFunctionParameters(CompileState cs, Parser p, List<string> arguments)
        {
            List<FuncParamInfo> funcParams = new List<FuncParamInfo>();
            if (cs.m_LocalVariables.Count != 0)
            {
                throw parserException(p, "Internal parser error");
            }
            cs.m_LocalVariables.Add(new List<string>());
            if (arguments.Count == 1 && arguments[0] == ")")
            {
                return funcParams;
            }
            for (int i = 0; i < arguments.Count; i += 3)
            {
                FuncParamInfo fp = new FuncParamInfo();
                switch (arguments[i])
                {
                    case "integer":
                        fp.Type = typeof(int);
                        break;

                    case "vector":
                        fp.Type = typeof(Vector3);
                        break;

                    case "list":
                        fp.Type = typeof(AnArray);
                        break;

                    case "float":
                        fp.Type = typeof(double);
                        break;

                    case "string":
                        fp.Type = typeof(string);
                        break;

                    case "key":
                        fp.Type = typeof(LSLKey);
                        break;

                    case "rotation":
                    case "quaternion":
                        fp.Type = typeof(Quaternion);
                        break;

                    default:
                        throw parserException(p, string.Format("Invalid type for parameter {0}", i / 3));
                }

                checkUsedName(cs, p, "Parameter", arguments[i + 1]);
                cs.m_LocalVariables[0].Add(arguments[i + 1]);
                fp.Name = arguments[i + 1];
                funcParams.Add(fp);

                if (arguments[i + 2] == ",")
                {
                }
                else if (arguments[i + 2] == ")")
                {
                    if (i + 3 != arguments.Count)
                    {
                        throw parserException(p, string.Format("Missing ')' at the end of function declaration"));
                    }
                    return funcParams;
                }
            }
            throw parserException(p, string.Format("Missing ')' at the end of function declaration"));
        }

        int findEndOfControlFlow(List<string> line, int lineNumber)
        {
            int i;
            List<string> parenstack = new List<string>();

            if(line[1] != "(")
            {
                throw new CompilerException(lineNumber, string.Format("'{0}' is not followed by '('", line[0]));
            }

            for (i = 1; i < line.Count; ++i)
            {
                switch(line[i])
                {
                    case "(":
                    case "[":
                        parenstack.Insert(0, line[i]);
                        break;

                    case ")":
                        if(parenstack[0] != "(")
                        {
                            throw new CompilerException(lineNumber, string.Format("Mismatching '{0}' for '{1}'", line[i], parenstack[parenstack.Count - 1]));
                        }
                        parenstack.RemoveAt(0);
                        if(parenstack.Count == 0)
                        {
                            return i;
                        }
                        break;

                    case "]":
                        if (parenstack[0] != "[")
                        {
                            throw new CompilerException(lineNumber, string.Format("Mismatching '{0}' for '{1}'", line[i], parenstack[parenstack.Count - 1]));
                        }
                        parenstack.RemoveAt(0);
                        break;

                    case "if":
                    case "for":
                    case "else":
                    case "while":
                    case "do":
                    case "return":
                    case "state":
                        if(m_ReservedWords.Contains(line[i]))
                        {
                            throw new CompilerException(lineNumber, string.Format("'{0}' not allowed in '{1}'", line[i], line[0]));
                        }
                        break;

                    default:
                        break;
                }
            }
            throw new CompilerException(lineNumber, string.Format("Could not find end of '{0}'", line[0]));
        }

        void parseBlockLine(CompileState compileState, Parser p, List<LineInfo> block, List<string> args, int lineNumber, bool inState)
        {
            for (; ; )
            {
                if(args[0] == "else" && args[1] == "if")
                {
                    int eocf = findEndOfControlFlow(args.GetRange(1, args.Count - 1), lineNumber) + 1;
                    /* make it a block */
                    if (args[eocf + 1] == "{")
                    {
                        block.Add(new LineInfo(args, lineNumber));
                        parseBlock(compileState, p, block, inState, true);
                        return;
                    }
                    else
                    {
                        List<string> controlflow = args.GetRange(0, eocf + 1);
                        block.Add(new LineInfo(controlflow, lineNumber));
                        args = args.GetRange(eocf + 1, args.Count - eocf - 1);
                    }
                }
                else if (args[0] == "else")
                {
                    if (args[1] == "{")
                    {
                        block.Add(new LineInfo(args, lineNumber));
                        parseBlock(compileState, p, block, inState, true);
                        return;
                    }
                    else
                    {
                        List<string> controlflow = args.GetRange(0, 1);
                        block.Add(new LineInfo(controlflow, lineNumber));
                        args = args.GetRange(1, args.Count - 1);
                    }
                }
                else if (args[0] == "do")
                {
                    if (args[1] == "{")
                    {
                        parseBlock(compileState, p, block, inState, true);
                        return;
                    }
                    else
                    {
                        List<string> controlflow = args.GetRange(0, 1);
                        block.Add(new LineInfo(controlflow, lineNumber));

                        args = args.GetRange(1, args.Count - 1);
                    }
                }
                else if (args[0] == "if")
                {
                    int eocf = findEndOfControlFlow(args, lineNumber);
                    /* make it a block */
                    if (args[eocf + 1] == "{")
                    {
                        block.Add(new LineInfo(args, lineNumber));
                        parseBlock(compileState, p, block, inState, true);
                        return;
                    }
                    else
                    {
                        List<string> controlflow = args.GetRange(0, eocf + 1);
                        block.Add(new LineInfo(controlflow, lineNumber));
                        args = args.GetRange(eocf + 1, args.Count - eocf - 1);
                    }
                }
                else if (args[0] == "for" || args[0] == "while")
                {
                    int eocf = findEndOfControlFlow(args, lineNumber);
                    /* make it a block */
                    if(args[eocf + 1] == "{")
                    {
                        parseBlock(compileState, p, block, inState, true);
                        return;
                    }
                    else
                    {
                        List<string> controlflow = args.GetRange(0, eocf + 1);
                        block.Add(new LineInfo(controlflow, lineNumber));
                        args = args.GetRange(eocf + 1, args.Count - eocf - 1);
                    }
                }
                else if (args[0] == "{")
                {
                    block.Add(new LineInfo(args, lineNumber));
                    parseBlock(compileState, p, block, inState, true);
                    return;
                }
                else if (args[0] == ";")
                {
                    block.Add(new LineInfo(new List<string>(new string[] { "}" }), lineNumber));
                    return;
                }
                else if(args[args.Count - 1] == "{")
                {
                    throw parserException(p, "'{' not allowed at end of line without control flow instruction");
                }
                else
                {
                    switch (args[0])
                    {
                        case "integer":
                        case "vector":
                        case "list":
                        case "float":
                        case "string":
                        case "key":
                        case "rotation":
                        case "quaternion":
                            checkUsedName(compileState, p, "Local Variable", args[1]);
                            compileState.m_LocalVariables[compileState.m_LocalVariables.Count - 1].Add(args[1]);
                            if (args[2] != ";" && args[2] != "=")
                            {
                                throw parserException(p, string.Format("Expecting '=' or ';' after variable name {0}", args[1]));
                            }
                            break;

                        case "state":
                            if (!inState)
                            {
                                throw parserException(p, "state change not allowed in global function");
                            }
                            break;

                        default:
                            break;
                    }
                    block.Add(new LineInfo(args, lineNumber));
                    return;
                }
            }
        }

        void parseBlock(CompileState compileState, Parser p, List<LineInfo> block, bool inState, bool addNewLocals = false)
        {
            if (addNewLocals)
            {
                compileState.m_LocalVariables.Add(new List<string>());
            }
            for (; ; )
            {
                int lineNumber;
                List<string> args = new List<string>();
                try
                {
                    p.read(args);
                }
                catch (ParserBase.EndOfStringException)
                {
                    throw parserException(p, "Missing '\"' at the end of string");
                }
                catch (ParserBase.EndOfFileException)
                {
                    throw parserException(p, "Premature end of script");
                }
                lineNumber = p.CurrentLineNumber;

                if (args.Count == 0)
                {
                    /* should not happen but better be safe here */
                }
                else if (args[0] == "}")
                {
                    compileState.m_LocalVariables.RemoveAt(compileState.m_LocalVariables.Count - 1);
                    block.Add(new LineInfo(args, lineNumber));
                    return;
                }
                else
                {
                    parseBlockLine(compileState, p, block, args, lineNumber, inState);
                }
            }
        }

        void parseState(CompileState compileState, Parser p, string stateName)
        {
            compileState.m_States.Add(stateName, new Dictionary<string, List<LineInfo>>());
            for (; ; )
            {
                int lineNumber;
                List<string> args = new List<string>();
                try
                {
                    p.read(args);
                }
                catch (ParserBase.EndOfStringException)
                {
                    throw parserException(p, "Missing '\"' at the end of string");
                }
                catch (ParserBase.EndOfFileException)
                {
                    throw parserException(p, "Missing '}' at end of script");
                }
                lineNumber = p.CurrentLineNumber;

                if (args.Count == 0)
                {
                    /* should not happen but better be safe here */
                }
                else if (args[args.Count - 1] == ";")
                {
                    throw parserException(p, string.Format("Neither variable declarations nor statements allowed outside of event functions. Offending state {0}.", stateName));
                }
                else if (args[args.Count - 1] == "{")
                {
                    if (!m_EventDelegates.ContainsKey(args[0]))
                    {
                        throw parserException(p, string.Format("'{0}' is not a valid event.", args[0]));
                    }
                    List<FuncParamInfo> fp = checkFunctionParameters(compileState, p, args.GetRange(2, args.Count - 3));
                    MethodInfo m = m_EventDelegates[args[0]];
                    ParameterInfo[] pi = m.GetParameters();
                    if (fp.Count != pi.Length)
                    {
                        throw parserException(p, string.Format("'{0}' does not have the correct parameters.", args[0]));
                    }
                    int i;
                    for (i = 0; i < fp.Count; ++i)
                    {
                        if (!fp[i].Type.Equals(pi[i].ParameterType))
                        {
                            throw parserException(p, string.Format("'{0}' does not match in parameter types", args[0]));
                        }
                    }
                    if (compileState.m_States[stateName].ContainsKey(args[0]))
                    {
                        throw parserException(p, string.Format("Event '{0}' already defined", args[0]));
                    }
                    List<LineInfo> stateList = new List<LineInfo>();
                    compileState.m_States[stateName].Add(args[0], stateList);
                    stateList.Add(new LineInfo(args, lineNumber));
                    parseBlock(compileState, p, stateList, true);
                }
                else if (args[0] == "}")
                {
                    if(compileState.m_States[stateName].Count == 0)
                    {
                        throw parserException(p, string.Format("state '{0}' does not have any events.", stateName));
                    }
                    return;
                }
            }
        }

        CompileState Preprocess(UUI user, Dictionary<int, string> shbangs, TextReader reader, int lineNumber = 1)
        {
            CompileState compileState = new CompileState();
            compileState.AcceptedFlags = APIFlags.OSSL | APIFlags.LSL | APIFlags.LightShare;
            APIFlags extraflags = APIFlags.None;
            foreach (KeyValuePair<int, string> shbang in shbangs)
            {
                if (shbang.Value.StartsWith("//#!Mode:"))
                {
                    /* we got a sh-bang here, it is a lot safer than what OpenSimulator uses */
                    string mode = shbang.Value.Substring(9).Trim().ToLower();
                    if (mode == "lsl")
                    {
                        compileState.AcceptedFlags = APIFlags.LSL;
                    }
                    else if (mode == "assl")
                    {
                        compileState.AcceptedFlags = APIFlags.ASSL | APIFlags.OSSL | APIFlags.LightShare | APIFlags.LSL;
                    }
                    else if (mode == "aurora" || mode == "whitecore")
                    {
                        compileState.AcceptedFlags = APIFlags.OSSL | APIFlags.WindLight_Aurora | APIFlags.LSL;
                    }
                }
                else if (shbang.Value.StartsWith("//#!Enable:"))
                {
                    string api = shbang.Value.Substring(11).Trim().ToLower();
                    if (api == "admin")
                    {
                        extraflags |= APIFlags.ASSL_Admin;
                    }
                }
                compileState.AcceptedFlags |= extraflags;
            }

            Parser p = new Parser();
            p.push(reader, "", lineNumber);

            for (; ; )
            {
                List<string> args = new List<string>();
                try
                {
                    p.read(args);
                }
                catch (ParserBase.EndOfStringException)
                {
                    throw parserException(p, "Missing '\"' at the end of string");
                }
                catch (ParserBase.EndOfFileException)
                {
                    break;
                }
                lineNumber = p.CurrentLineNumber;
                if (args.Count == 0)
                {
                    /* should not happen but better be safe here */
                }
                else if (args[args.Count - 1] == ";")
                {
                    /* variable definition */
                    if (args[2] != "=" && args[2] != ";")
                    {
                        throw parserException(p, "Invalid variable definition. Either ';' or an expression preceeded by '='");
                    }
                    switch (args[0])
                    {
                        case "integer":
                            checkUsedName(compileState, p, "Variable", args[1]);
                            compileState.m_VariableDeclarations[args[1]] = typeof(int);
                            if (args[2] == "=")
                            {
                                compileState.m_VariableInitValues[args[1]] = new LineInfo(args.GetRange(3, args.Count - 4), lineNumber);
                            }
                            break;

                        case "vector":
                            checkUsedName(compileState, p, "Variable", args[1]);
                            compileState.m_VariableDeclarations[args[1]] = typeof(Vector3);
                            if (args[2] == "=")
                            {
                                compileState.m_VariableInitValues[args[1]] = new LineInfo(args.GetRange(3, args.Count - 4), lineNumber);
                            }
                            break;

                        case "list":
                            checkUsedName(compileState, p, "Variable", args[1]);
                            compileState.m_VariableDeclarations[args[1]] = typeof(AnArray);
                            if (args[2] == "=")
                            {
                                compileState.m_VariableInitValues[args[1]] = new LineInfo(args.GetRange(3, args.Count - 4), lineNumber);
                            }
                            break;

                        case "float":
                            checkUsedName(compileState, p, "Variable", args[1]);
                            compileState.m_VariableDeclarations[args[1]] = typeof(double);
                            if (args[2] == "=")
                            {
                                compileState.m_VariableInitValues[args[1]] = new LineInfo(args.GetRange(3, args.Count - 4), lineNumber);
                            }
                            break;

                        case "string":
                            checkUsedName(compileState, p, "Variable", args[1]);
                            compileState.m_VariableDeclarations[args[1]] = typeof(string);
                            if (args[2] == "=")
                            {
                                compileState.m_VariableInitValues[args[1]] = new LineInfo(args.GetRange(3, args.Count - 4), lineNumber);
                            }
                            break;

                        case "key":
                            checkUsedName(compileState, p, "Variable", args[1]);
                            compileState.m_VariableDeclarations[args[1]] = typeof(string);
                            if (args[2] == "=")
                            {
                                compileState.m_VariableInitValues[args[1]] = new LineInfo(args.GetRange(3, args.Count - 4), lineNumber);
                            }
                            break;

                        case "rotation":
                        case "quaternion":
                            checkUsedName(compileState, p, "Variable", args[1]);
                            compileState.m_VariableDeclarations[args[1]] = typeof(Quaternion);
                            if(args[2] == "=")
                            {
                                compileState.m_VariableInitValues[args[1]] = new LineInfo(args.GetRange(3, args.Count - 4), lineNumber);
                            }
                            break;

                        default:
                            throw parserException(p, string.Format("Invalid variable definition. Wrong type {0}.", args[0]));
                    }
                }
                else if (args[args.Count - 1] == "{")
                {
                    if (args[0] == "default")
                    {
                        /* default state begin */
                        if (args[1] != "{")
                        {
                            throw parserException(p, "Invalid default state declaration");
                        }
                        parseState(compileState, p, "default");
                    }
                    else if (args[0] == "state")
                    {
                        /* state begin */
                        if (args[1] == "default")
                        {
                            throw parserException(p, "default state cannot be declared with state");
                        }
                        else if (compileState.m_States.Count == 0)
                        {
                            throw parserException(p, "default state must be first declared state in script");
                        }
                        checkValidName(p, "State", args[1]);
                        if (compileState.m_States.ContainsKey(args[1]))
                        {
                            throw parserException(p, "state definition cannot be declared twice");
                        }

                        if (args[2] != "{")
                        {
                            throw parserException(p, "Invalid state declaration");
                        }
                        parseState(compileState, p, args[1]);
                    }
                    else
                    {
                        List<FuncParamInfo> fp;
                        List<LineInfo> funcList = new List<LineInfo>();
                        /* either type or function name */
                        switch (args[0])
                        {
                            case "integer":
                            case "vector":
                            case "list":
                            case "float":
                            case "string":
                            case "key":
                            case "rotation":
                            case "quaternion":
                            case "void":
                                checkUsedName(compileState, p, "Function", args[1]);
                                fp = checkFunctionParameters(compileState, p, args.GetRange(3, args.Count - 4));
                                funcList.Add(new LineInfo(args, lineNumber));
                                parseBlock(compileState, p, funcList, false);
                                compileState.m_Functions[args[1]] = funcList;
                                break;

                            default:
                                fp = checkFunctionParameters(compileState, p, args.GetRange(2, args.Count - 3));
                                args.Insert(0, "void");
                                funcList.Add(new LineInfo(args, lineNumber));
                                parseBlock(compileState, p, funcList, false);
                                compileState.m_Functions[args[1]] = funcList;
                                break;
                        }
                    }
                }
                else if (args[0] == "}")
                {
                    throw parserException(p, "'}' found without matching '{'");
                }
            }
            return compileState;
        }
    }
}

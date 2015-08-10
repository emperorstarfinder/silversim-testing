﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.LSL.Expression
{
    public class Resolver
    {
        public class ResolverException : Exception
        {
            public ResolverException(string msg)
                : base(msg)
            {

            }
        }

        class ListTreeEnumState
        {
            public int Position = -1;
            public Tree Tree;

            public ListTreeEnumState(Tree tree)
            {
                Tree = tree;
            }

            public bool MoveNext()
            {
                if (Position >= Tree.SubTree.Count)
                {
                    return false;
                }
                return (++Position < Tree.SubTree.Count);
            }

            public Tree Current
            {
                get
                {
                    return Tree.SubTree[Position];
                }
            }
        }

        class ListTreeEnumReverseState
        {
            public int Position;
            public Tree Tree;

            public ListTreeEnumReverseState(Tree tree)
            {
                Tree = tree;
                Position = tree.SubTree.Count;
            }

            public bool MoveNext()
            {
                if (Position < 0)
                {
                    return false;
                }
                return (--Position >= 0);
            }

            public Tree Current
            {
                get
                {
                    return Tree.SubTree[Position];
                }
            }
        }

        public enum OperatorType
        {
            Unknown,
            RightUnary,
            LeftUnary,
            Binary
        }

        List<string> m_ReservedWords;
        List<Dictionary<string, OperatorType>> m_Operators;
        Dictionary<string, string> m_BlockOps;

        public Resolver(List<string> reservedWords, List<Dictionary<string, OperatorType>> operators, Dictionary<string, string> blockOps)
        {
            m_ReservedWords = reservedWords;
            m_Operators = operators;
            m_BlockOps = blockOps;
        }

        void resolveValues(Tree nt)
        {
            List<ListTreeEnumState> enumeratorStack = new List<ListTreeEnumState>();
            enumeratorStack.Insert(0, new ListTreeEnumState(nt));
            while (enumeratorStack.Count != 0)
            {
                if(!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    nt = enumeratorStack[0].Current;
                    if (nt.Type == Tree.EntryType.StringValue || nt.Type == Tree.EntryType.Value)
                    {
                        // delay the 2147483648
                        if (nt.Entry != "2147483648")
                        {
                            nt.Process();
                        }
                    }
                    enumeratorStack.Insert(0, new ListTreeEnumState(nt));
                }
            }
        }

        void resolveSeparators(Tree nt)
        {
            List<Tree> enumTree = new List<Tree>();
            enumTree.Add(nt);
            while (enumTree.Count != 0)
            {
                nt = enumTree[0];
                enumTree.RemoveAt(0);
                enumTree.AddRange(nt.SubTree);
                if (nt.Entry == "," && nt.Type != Tree.EntryType.StringValue)
                {
                    nt.Type = Tree.EntryType.Separator;
                }
            }
        }

        void resolveBlockOps(Tree nt)
        {
            List<Tree> enumTree = new List<Tree>();
            enumTree.Add(nt);
            while (enumTree.Count != 0)
            {
                nt = enumTree[0];
                enumTree.RemoveAt(0);
                enumTree.AddRange(nt.SubTree);
                if (nt.Type == Tree.EntryType.OperatorUnknown || nt.Type == Tree.EntryType.Unknown)
                {
                    if (m_BlockOps.ContainsKey(nt.Entry))
                    {
                        nt.Type = Tree.EntryType.LevelBegin;
                    }
                    if (m_BlockOps.ContainsValue(nt.Entry))
                    {
                        nt.Type = Tree.EntryType.LevelEnd;
                    }
                }
            }
        }

        void identifyReservedWords(Tree tree)
        {
            List<Tree> processNodes = new List<Tree>();
            List<ListTreeEnumState> enumeratorStack = new List<ListTreeEnumState>();
            enumeratorStack.Insert(0, new ListTreeEnumState(tree));
            processNodes.Add(tree);
            while(enumeratorStack.Count != 0)
            {
                if(!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    tree = enumeratorStack[0].Current;
                    processNodes.Add(tree);
                    enumeratorStack.Add(new ListTreeEnumState(tree));
                }
            }

            foreach (Tree nt in processNodes)
            {
                if (nt.Type == Tree.EntryType.Unknown)
                {
                    if (m_ReservedWords.Contains(nt.Entry))
                    {
                        nt.Type = Tree.EntryType.ReservedWord;
                    }
                }
            }
        }

        void sortBlockOps(Tree nt, int i = 0)
        {
            List<Tree> blockSort = new List<Tree>();

            for (i = 0; i < nt.SubTree.Count; )
            {
                if (nt.SubTree[i].Type == Tree.EntryType.LevelBegin)
                {
                    Tree sub = nt.SubTree[i];
                    nt.SubTree[i].Type = Tree.EntryType.Level;
                    if (blockSort.Count != 0)
                    {
                        nt.SubTree.RemoveAt(i);
                        blockSort[blockSort.Count - 1].SubTree.Add(sub);
                    }
                    else
                    {
                        ++i;
                    }
                    blockSort.Add(sub);
                }
                else if (nt.SubTree[i].Type == Tree.EntryType.LevelEnd)
                {
                    if (m_BlockOps[blockSort[blockSort.Count - 1].Entry] != nt.SubTree[i].Entry)
                    {
                        throw new ResolverException(string.Format("'{1}' does not match '{0}'", blockSort[blockSort.Count - 1].Entry, nt.SubTree[i].Entry));
                    }
                    nt.SubTree.RemoveAt(i);
                    blockSort.RemoveAt(blockSort.Count - 1);
                }
                else
                {
                    if (blockSort.Count > 0)
                    {
                        blockSort[blockSort.Count - 1].SubTree.Add(nt.SubTree[i]);
                        nt.SubTree.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }
            if(blockSort.Count != 0)
            {
                throw new ResolverException(string.Format("'{0}' missing a matching '{1}'", blockSort[blockSort.Count - 1].Entry, m_BlockOps[blockSort[blockSort.Count - 1].Entry]));
            }
        }

        void identifyFunctions(Tree tree)
        {
            List<Tree> processNodes = new List<Tree>();
            List<ListTreeEnumReverseState> enumeratorStack = new List<ListTreeEnumReverseState>();
            enumeratorStack.Insert(0, new ListTreeEnumReverseState(tree));
            processNodes.Add(tree);
            while(enumeratorStack.Count != 0)
            {
                if(!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    tree = enumeratorStack[0].Current;
                    processNodes.Insert(0, tree);
                    enumeratorStack.Add(new ListTreeEnumReverseState(tree));
                }
            }

            foreach (Tree nt in processNodes)
            {
                for (int i = 0; i < nt.SubTree.Count - 1; ++i)
                {
                    if (nt.SubTree[i].Type == Tree.EntryType.Unknown &&
                        nt.SubTree[i + 1].Type == Tree.EntryType.Level &&
                        nt.SubTree[i + 1].Entry == "(")
                    {
                        Tree st = nt.SubTree[i + 1];
                        nt.SubTree.RemoveAt(i + 1);
                        nt.SubTree[i].Type = Tree.EntryType.Function;

                        /* rebuild arguments */
                        List<Tree> argumentsList = new List<Tree>();
                        Tree argument = null;
                        bool lastIsSeparator = false;
                        for (int j = 0; j < st.SubTree.Count; ++j)
                        {
                            if (st.SubTree[j].Type != Tree.EntryType.Separator)
                            {
                                if (argument == null)
                                {
                                    argument = new Tree();
                                    argumentsList.Add(argument);
                                    argument.Type = Tree.EntryType.FunctionArgument;
                                }
                                argument.SubTree.Add(st.SubTree[j]);
                                lastIsSeparator = false;
                            }
                            else
                            {
                                if (argument == null)
                                {
                                    throw new ResolverException(string.Format("Missing parameter to function '{0}'", nt.SubTree[i].Entry));
                                }
                                argument = null;
                                lastIsSeparator = true;
                            }
                        }

                        if (lastIsSeparator)
                        {
                            throw new ResolverException(string.Format("Missing parameter to function '{0}'", nt.SubTree[i].Entry));
                        }
                        nt.SubTree[i].SubTree = argumentsList;
                    }
                }
            }
        }

        void reorderDeclarationArguments(Tree nt, int start, int end)
        {
            List<Tree> argumentsInput = nt.SubTree.GetRange(start + 1, end - start - 1);
            nt.SubTree.RemoveRange(start + 1, end - start);
            nt.SubTree[start].Type = Tree.EntryType.Declaration;

            /* rebuild arguments */
            List<Tree> argumentsList = new List<Tree>();
            Tree argument = null;
            bool lastIsSeparator = false;
            for (int j = 0; j < argumentsInput.Count; ++j)
            {
                if (argumentsInput[j].Type != Tree.EntryType.Separator)
                {
                    if (argument == null)
                    {
                        argument = new Tree();
                        argumentsList.Add(argument);
                        argument.Type = Tree.EntryType.DeclarationArgument;
                    }
                    argument.SubTree.Add(argumentsInput[j]);
                    lastIsSeparator = false;
                }
                else
                {
                    if (argument == null)
                    {
                        throw new ResolverException("Missing parameter to declaration by '<' and '>'");
                    }
                    argument = null;
                    lastIsSeparator = true;
                }
            }

            if (lastIsSeparator)
            {
                throw new ResolverException("Missing parameter to declaration by '<' and '>'");
            }
            nt.SubTree[start].SubTree = argumentsList;
        }

        void identifySquareBracketDeclarations(Tree tree)
        {
            int start ;
            List<Tree> processNodes = new List<Tree>();
            List<ListTreeEnumReverseState> enumeratorStack = new List<ListTreeEnumReverseState>();
            enumeratorStack.Insert(0, new ListTreeEnumReverseState(tree));
            processNodes.Add(tree);
            while(enumeratorStack.Count != 0)
            {
                if(!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    tree = enumeratorStack[0].Current;
                    processNodes.Insert(0, tree);
                    enumeratorStack.Add(new ListTreeEnumReverseState(tree));
                }
            }

            foreach (Tree nt in processNodes)
            {
                for (start = 0; start < nt.SubTree.Count; ++start)
                {
                    Tree startnode = nt.SubTree[start];
                    if (startnode.Entry == "<")
                    {
                        if (start == 0)
                        {
                            int end;
                            /* definitely a declaration, search for end */
                            for (end = start + 1; end < nt.SubTree.Count; ++end)
                            {
                                if (nt.SubTree[end].Entry == ">")
                                {
                                    if (end == nt.SubTree.Count - 1)
                                    {
                                        /* has to be a declaration */
                                        break;
                                    }
                                    else if (nt.SubTree[end + 1].Type == Tree.EntryType.OperatorUnknown ||
                                        nt.SubTree[end + 1].Type == Tree.EntryType.OperatorBinary ||
                                        nt.SubTree[end + 1].Type == Tree.EntryType.Separator)
                                    {
                                        /* has to be a declaration */
                                        break;
                                    }
                                }
                            }
                            if (end == nt.SubTree.Count)
                            {
                                throw new ResolverException("Missing end of declaration begun by '<'");
                            }
                            else
                            {
                                reorderDeclarationArguments(nt, start, end);
                            }
                        }
                        else if (nt.SubTree[start - 1].Type == Tree.EntryType.OperatorUnknown ||
                            nt.SubTree[start - 1].Type == Tree.EntryType.OperatorBinary ||
                            nt.SubTree[start - 1].Type == Tree.EntryType.Separator ||
                            nt.SubTree[start - 1].Type == Tree.EntryType.Level)
                        {
                            int end;
                            /* a declaration too */
                            for (end = start + 1; end < nt.SubTree.Count; ++end)
                            {
                                if (nt.SubTree[end].Entry == ">")
                                {
                                    if (end == nt.SubTree.Count - 1)
                                    {
                                        /* has to be a declaration */
                                        break;
                                    }
                                    else if (nt.SubTree[end + 1].Type == Tree.EntryType.OperatorUnknown ||
                                        nt.SubTree[end + 1].Type == Tree.EntryType.OperatorBinary ||
                                        nt.SubTree[end + 1].Type == Tree.EntryType.Separator)
                                    {
                                        /* has to be a declaration */
                                        break;
                                    }
                                }
                            }
                            if (end == nt.SubTree.Count)
                            {
                                throw new ResolverException("Missing end of declaration begun by '<'");
                            }
                            else
                            {
                                reorderDeclarationArguments(nt, start, end);
                            }
                        }
                    }
                }
            }
        }

        bool isValidLeftHand(Tree nt)
        {
            switch(nt.Type)
            {
                case Tree.EntryType.OperatorBinary:
                case Tree.EntryType.OperatorLeftUnary:
                case Tree.EntryType.OperatorRightUnary:
                case Tree.EntryType.StringValue:
                case Tree.EntryType.Variable:
                case Tree.EntryType.Value:
                case Tree.EntryType.Function:
                case Tree.EntryType.Declaration:
                case Tree.EntryType.Unknown:
                case Tree.EntryType.Level:
                case Tree.EntryType.Vector:
                case Tree.EntryType.Rotation:
                    return true;

                default:
                    return false;
            }
        }

        bool isValidRightHand(Tree nt)
        {
            switch(nt.Type)
            {
                case Tree.EntryType.OperatorLeftUnary:
                case Tree.EntryType.OperatorRightUnary:
                case Tree.EntryType.StringValue:
                case Tree.EntryType.Value:
                case Tree.EntryType.Variable:
                case Tree.EntryType.Function:
                case Tree.EntryType.Declaration:
                case Tree.EntryType.Unknown:
                case Tree.EntryType.Level:
                case Tree.EntryType.Vector:
                case Tree.EntryType.Rotation:
                    return true;

                default:
                    return false;
            }
        }

        bool isValidUnaryLeft(Tree nt)
        {
            switch(nt.Type)
            {
                case Tree.EntryType.Unknown:
                case Tree.EntryType.Variable:
                case Tree.EntryType.Value:
                case Tree.EntryType.StringValue:
                case Tree.EntryType.OperatorLeftUnary:
                case Tree.EntryType.Vector:
                case Tree.EntryType.Rotation:
                case Tree.EntryType.Declaration:
                case Tree.EntryType.Level:
                case Tree.EntryType.Function:
                    return true;


                default:
                    return false;
            }
        }

        bool isValidUnaryRight(Tree nt)
        {
            switch(nt.Type)
            {
                case Tree.EntryType.Unknown:
                case Tree.EntryType.Value:
                case Tree.EntryType.Variable:
                case Tree.EntryType.StringValue:
                case Tree.EntryType.Function:
                case Tree.EntryType.OperatorLeftUnary:
                    return true;

                default:
                    return false;
            }
        }

        void identifyUnaryOps(Tree tree)
        {
            int pos;
            List<Tree> processNodes = new List<Tree>();
            List<ListTreeEnumState> enumeratorStack = new List<ListTreeEnumState>();
            enumeratorStack.Insert(0, new ListTreeEnumState(tree));
            processNodes.Add(tree);
            while(enumeratorStack.Count != 0)
            {
                if(!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    tree = enumeratorStack[0].Current;
                    processNodes.Insert(0, tree);
                    enumeratorStack.Add(new ListTreeEnumState(tree));
                }
            }

            foreach (Tree nt in processNodes)
            {
                for (pos = nt.SubTree.Count; pos-- > 0; )
                {
                    Tree st = nt.SubTree[pos];
                    if (st.Type == Tree.EntryType.OperatorUnknown)
                    {
                        OperatorType optype;
                        foreach (Dictionary<string, OperatorType> oilist in m_Operators)
                        {
                            if (oilist.TryGetValue(st.Entry, out optype))
                            {
                                if (optype == OperatorType.LeftUnary)
                                {
                                    if (pos == 0 && pos < nt.SubTree.Count - 1)
                                    {
                                        if (isValidUnaryLeft(nt.SubTree[pos + 1]))
                                        {
                                            st.Type = Tree.EntryType.OperatorLeftUnary;
                                            break;
                                        }
                                    }
                                    else if (pos > 0 && pos < nt.SubTree.Count - 1 &&
                                        (nt.SubTree[pos - 1].Type == Tree.EntryType.OperatorUnknown ||
                                        nt.SubTree[pos - 1].Type == Tree.EntryType.OperatorBinary) &&
                                        isValidUnaryLeft(nt.SubTree[pos + 1]))
                                    {
                                        st.Type = Tree.EntryType.OperatorLeftUnary;
                                        break;
                                    }
                                }
                                else if (optype == OperatorType.RightUnary)
                                {
                                    if (pos > 0)
                                    {
                                        if (isValidUnaryRight(nt.SubTree[pos - 1]))
                                        {
                                            st.Type = Tree.EntryType.OperatorRightUnary;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void identifyBinaryOps(Tree tree)
        {
            int pos;
            List<Tree> processNodes = new List<Tree>();
            List<ListTreeEnumReverseState> enumeratorStack = new List<ListTreeEnumReverseState>();
            enumeratorStack.Insert(0, new ListTreeEnumReverseState(tree));
            processNodes.Add(tree);
            while(enumeratorStack.Count != 0)
            {
                if(!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    tree = enumeratorStack[0].Current;
                    processNodes.Insert(0, tree);
                    enumeratorStack.Add(new ListTreeEnumReverseState(tree));
                }
            }

            foreach (Tree nt in processNodes)
            {
                for (pos = 0; pos < nt.SubTree.Count; ++pos)
                {
                    Tree st = nt.SubTree[pos];
                    if (st.Type == Tree.EntryType.OperatorUnknown)
                    {
                        OperatorType? optype = null;
                        foreach (Dictionary<string, OperatorType> oilist in m_Operators)
                        {
                            OperatorType _optype;
                            if (oilist.TryGetValue(st.Entry, out _optype))
                            {
                                if (_optype == OperatorType.Binary)
                                {
                                    optype = _optype;
                                    break;
                                }
                            }
                        }
                        if (null != optype)
                        {
                            switch (optype.Value)
                            {
                                case OperatorType.Binary:
                                    if (pos > 0 && pos < nt.SubTree.Count - 1)
                                    {
                                        if (isValidLeftHand(nt.SubTree[pos - 1]) &&
                                            isValidRightHand(nt.SubTree[pos + 1]))
                                        {
                                            st.Type = Tree.EntryType.OperatorBinary;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        void sortUnaryOps(Tree tree)
        {
            List<ListTreeEnumReverseState> enumeratorStack = new List<ListTreeEnumReverseState>();
            enumeratorStack.Insert(0, new ListTreeEnumReverseState(tree));
            while(enumeratorStack.Count != 0)
            {
                if(!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    tree = enumeratorStack[0].Current;
                    int pos = enumeratorStack[0].Position;
                    switch (tree.Type)
                    {
                        case Tree.EntryType.OperatorLeftUnary:
                            if (!tree.ProcessedOpSort)
                            {
                                tree.ProcessedOpSort = true;
                                tree.SubTree.Add(enumeratorStack[0].Tree.SubTree[pos + 1]);
                                enumeratorStack[0].Tree.SubTree.RemoveAt(pos + 1);
                            }
                            break;

                        case Tree.EntryType.OperatorRightUnary:
                            if (!tree.ProcessedOpSort)
                            {
                                tree.ProcessedOpSort = true;
                                tree.SubTree.Add(enumeratorStack[0].Tree.SubTree[pos - 1]);
                                enumeratorStack[0].Tree.SubTree.RemoveAt(pos - 1);
                            }
                            break;

                        default:
                            break;
                    }
                    enumeratorStack.Insert(0, new ListTreeEnumReverseState(tree));
                }
            }
        }

        void sortBinaryOps(Tree tree)
        {
            List<ListTreeEnumState> enumeratorStack = new List<ListTreeEnumState>();
            enumeratorStack.Insert(0, new ListTreeEnumState(tree));
            int pos;
            foreach (Dictionary<string, OperatorType> plist in m_Operators)
            {
                for (pos = 0; pos < tree.SubTree.Count; )
                {
                    switch (tree.SubTree[pos].Type)
                    {
                        case Tree.EntryType.OperatorBinary:
                            if (plist.ContainsKey(tree.SubTree[pos].Entry) &&
                                !tree.SubTree[pos].ProcessedOpSort)
                            {
                                tree.SubTree[pos].SubTree.Add(tree.SubTree[pos - 1]);
                                tree.SubTree[pos].SubTree.Add(tree.SubTree[pos + 1]);
                                tree.SubTree[pos].ProcessedOpSort = true;
                                tree.SubTree.RemoveAt(pos + 1);
                                tree.SubTree.RemoveAt(pos - 1);
                            }
                            else
                            {
                                ++pos;
                            }
                            break;

                        default:
                            ++pos;
                            break;
                    }
                }
            }

            while (enumeratorStack.Count != 0)
            {
                if (!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    tree = enumeratorStack[0].Current;

                    foreach (Dictionary<string, OperatorType> plist in m_Operators)
                    {
                        for (pos = 0; pos < tree.SubTree.Count; )
                        {
                            switch (tree.SubTree[pos].Type)
                            {
                                case Tree.EntryType.OperatorBinary:
                                    if (plist.ContainsKey(tree.SubTree[pos].Entry) &&
                                        !tree.SubTree[pos].ProcessedOpSort)
                                    {
                                        tree.SubTree[pos].SubTree.Add(tree.SubTree[pos - 1]);
                                        tree.SubTree[pos].SubTree.Add(tree.SubTree[pos + 1]);
                                        tree.SubTree[pos].ProcessedOpSort = true;
                                        tree.SubTree.RemoveAt(pos + 1);
                                        tree.SubTree.RemoveAt(pos - 1);
                                    }
                                    else
                                    {
                                        ++pos;
                                    }
                                    break;

                                default:
                                    ++pos;
                                    break;
                            }
                        }
                    }

                    enumeratorStack.Insert(0, new ListTreeEnumState(tree));
                }
            }
        }

        void identifyVariables(Tree tree, ICollection<string> variables)
        {
            List<ListTreeEnumState> enumeratorStack = new List<ListTreeEnumState>();
            enumeratorStack.Insert(0, new ListTreeEnumState(tree));

            while(enumeratorStack.Count != 0)
            {
                if(!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    tree = enumeratorStack[0].Current;
                    if (tree.Type == Tree.EntryType.Unknown && variables.Contains(tree.Entry))
                    {
                        tree.Type = Tree.EntryType.Variable;
                    }
                    enumeratorStack.Insert(0, new ListTreeEnumState(tree));
                }
            }
        }

        public void solveDotOperator(Tree tree)
        {
            List<ListTreeEnumState> enumeratorStack = new List<ListTreeEnumState>();
            int pos;
            for (pos = 0; pos < tree.SubTree.Count; ++pos)
            {
                if (tree.SubTree[pos].Type == Tree.EntryType.OperatorUnknown && tree.SubTree[pos].Entry == "." && pos > 0 && pos + 1 < tree.SubTree.Count)
                {
                    if (isValidLeftHand(tree.SubTree[pos - 1]) &&
                        (tree.SubTree[pos + 1].Type == Tree.EntryType.Variable ||
                        tree.SubTree[pos + 1].Type == Tree.EntryType.Unknown))
                    {
                        tree.SubTree[pos].Type = Tree.EntryType.OperatorBinary;
                        tree.SubTree[pos].SubTree.Add(tree.SubTree[pos - 1]);
                        tree.SubTree[pos].SubTree.Add(tree.SubTree[pos + 1]);
                        tree.SubTree[pos].ProcessedOpSort = true;
                        tree.SubTree.RemoveAt(pos + 1);
                        tree.SubTree.RemoveAt(pos - 1);
                    }
                    else
                    {
                        enumeratorStack.Insert(0, new ListTreeEnumState(tree.SubTree[pos]));
                    }
                }
                else
                {
                    enumeratorStack.Insert(0, new ListTreeEnumState(tree.SubTree[pos]));
                }
            }

            while(enumeratorStack.Count != 0)
            {
                if(!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    tree = enumeratorStack[0].Current;
                    for (pos = 0; pos < tree.SubTree.Count; ++pos)
                    {
                        if (tree.SubTree[pos].Type == Tree.EntryType.OperatorUnknown && tree.SubTree[pos].Entry == "." && pos > 0 && pos + 1 < tree.SubTree.Count)
                        {
                            if (isValidLeftHand(tree.SubTree[pos - 1]) &&
                                (tree.SubTree[pos + 1].Type == Tree.EntryType.Variable ||
                                tree.SubTree[pos + 1].Type == Tree.EntryType.Unknown))
                            {
                                tree.SubTree[pos].Type = Tree.EntryType.OperatorBinary;
                                tree.SubTree[pos].SubTree.Add(tree.SubTree[pos - 1]);
                                tree.SubTree[pos].SubTree.Add(tree.SubTree[pos + 1]);
                                tree.SubTree[pos].ProcessedOpSort = true;
                                tree.SubTree.RemoveAt(pos + 1);
                                tree.SubTree.RemoveAt(pos - 1);
                            }
                            else
                            {
                                enumeratorStack.Insert(0, new ListTreeEnumState(tree.SubTree[pos]));
                            }
                        }
                        else
                        {
                            enumeratorStack.Insert(0, new ListTreeEnumState(tree.SubTree[pos]));
                        }
                    }
                }
            }

        }

        public void Process(Tree tree, ICollection<string> variables)
        {
            identifyReservedWords(tree);
            identifyVariables(tree, variables);
            resolveValues(tree);
            resolveSeparators(tree);
            resolveBlockOps(tree);
            sortBlockOps(tree);
            identifySquareBracketDeclarations(tree);
            identifyFunctions(tree);
            solveDotOperator(tree);
            identifyUnaryOps(tree);
            sortUnaryOps(tree);
            identifyBinaryOps(tree);
            sortBinaryOps(tree);
            if(tree.SubTree.Count != 1)
            {
                throw new ResolverException("Internal Error! Expression tree not solved");
            }
        }
    }
}

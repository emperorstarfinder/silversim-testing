﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scripting.LSL.Expression;
using SilverSim.Types;
using System.Collections.Generic;
using System.Globalization;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {
        void solveDeclarations(Tree tree)
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
                    if (tree.Type == Tree.EntryType.Declaration)
                    {
                        if (tree.SubTree.Count == 3)
                        {
                            tree.Type = Tree.EntryType.Vector;
                        }
                        else if (tree.SubTree.Count == 4)
                        {
                            tree.Type = Tree.EntryType.Rotation;
                        }
                        else
                        {
                            throw new Resolver.ResolverException("argument list for <> has neither 3 nor 4 arguments");
                        }
                    }
                    enumeratorStack.Insert(0, new ListTreeEnumState(tree));
                }
            }
        }

        class ConstantValueVector : Tree.ConstantValue
        {
            public Vector3 Value;

            public ConstantValueVector(Vector3 v)
            {
                Value = v;
            }

            public new string ToString()
            {
                return Value.ToString();
            }

            public override Tree.ValueBase Negate()
            {
                return new ConstantValueVector(-Value);
            }
        }

        class ConstantValueRotation : Tree.ConstantValue
        {
            public Quaternion Value;

            public ConstantValueRotation(Quaternion v)
            {
                Value = v;
            }

            public new string ToString()
            {
                return Value.ToString();
            }

            public override Tree.ValueBase Negate()
            {
                return new ConstantValueRotation(-Value);
            }
        }

        void solveConstantOperations(Tree tree)
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
                    processNodes.Insert(0, tree);
                    enumeratorStack.Add(new ListTreeEnumState(tree));
                }
            }

            foreach (Tree st in processNodes)
            {
                if (st.Entry != "<")
                {

                }
                else if (st.Type == Tree.EntryType.Vector)
                {
                    if (st.SubTree[0].SubTree[0].Value != null &&
                        st.SubTree[1].SubTree[0].Value != null &&
                        st.SubTree[2].SubTree[0].Value != null)
                    {
                        double[] v = new double[3];
                        for (int idx = 0; idx < 3; ++idx)
                        {
                            if (st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueFloat)
                            {
                                v[idx] = ((Tree.ConstantValueFloat)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else if (st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueInt)
                            {
                                v[idx] = ((Tree.ConstantValueInt)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else
                            {
                                throw new Resolver.ResolverException("constant vector cannot contain other values than float or int");
                            }
                        }

                        st.Value = new ConstantValueVector(new Vector3(v[0], v[1], v[2]));
                    }
                }
                else if (st.Type == Tree.EntryType.Rotation)
                {
                    if (st.SubTree[0].SubTree[0].Value != null &&
                        st.SubTree[1].SubTree[0].Value != null &&
                        st.SubTree[2].SubTree[0].Value != null &&
                        st.SubTree[3].SubTree[0].Value != null)
                    {
                        double[] v = new double[4];
                        for (int idx = 0; idx < 4; ++idx)
                        {
                            if (st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueFloat)
                            {
                                v[idx] = ((Tree.ConstantValueFloat)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else if (st.SubTree[idx].SubTree[0].Value is Tree.ConstantValueInt)
                            {
                                v[idx] = ((Tree.ConstantValueInt)st.SubTree[idx].SubTree[0].Value).Value;
                            }
                            else
                            {
                                throw new Resolver.ResolverException("constant rotation cannot contain other values than float or int");
                            }
                        }

                        st.Value = new ConstantValueRotation(new Quaternion(v[0], v[1], v[2], v[3]));

                    }
                }

                #region Binary operators
                if (st.Type == Tree.EntryType.OperatorBinary)
                {
                    foreach (Tree ot in st.SubTree)
                    {
                        if (ot.Type == Tree.EntryType.Value && null == ot.Value)
                        {
                            ot.Process();
                        }
                    }
                }

                if (st.Type == Tree.EntryType.OperatorBinary && st.SubTree[0].Value != null && st.SubTree[1].Value != null)
                {
                    switch (st.Entry)
                    {
                        case ".":
                            if(st.SubTree[0].Value is ConstantValueRotation)
                            {
                                switch(st.SubTree[1].Entry)
                                {
                                    case "x":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueRotation)st.SubTree[0].Value).Value.X);
                                        break;
                                    case "y":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueRotation)st.SubTree[0].Value).Value.Y);
                                        break;
                                    case "z":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueRotation)st.SubTree[0].Value).Value.Z);
                                        break;
                                    case "s":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueRotation)st.SubTree[0].Value).Value.W);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else if(st.SubTree[0].Value is ConstantValueVector)
                            {
                                switch (st.SubTree[1].Entry)
                                {
                                    case "x":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueVector)st.SubTree[0].Value).Value.X);
                                        break;
                                    case "y":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueVector)st.SubTree[0].Value).Value.Y);
                                        break;
                                    case "z":
                                        st.Value = new Tree.ConstantValueFloat(((ConstantValueVector)st.SubTree[0].Value).Value.Z);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            break;

                        case "+":
                            if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value +
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueString && st.SubTree[1].Value is Tree.ConstantValueString)
                            {
                                st.Value = new Tree.ConstantValueString(
                                    ((Tree.ConstantValueString)(st.SubTree[0].Value)).Value +
                                    ((Tree.ConstantValueString)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new ConstantValueRotation(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value +
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "-":
                            if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value -
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value -
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new ConstantValueRotation(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value -
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "*":
                            if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(LSLCompiler.LSL_IntegerMultiply(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value,
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value));
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value.Dot(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value));
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value *
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new ConstantValueRotation(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value *
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value *
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "/":
                            if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(LSLCompiler.LSL_IntegerDivision(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value,
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value));
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value /
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new ConstantValueRotation(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value /
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value /
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "%":
                            if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueFloat(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value %
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(LSLCompiler.LSL_IntegerModulus(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value,
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value));
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new ConstantValueVector(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value %
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "^":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ^
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "<<":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <<
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case ">>":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >>
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case ">":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "<":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case ">=":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value >=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "<=":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value <=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "!=":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value !=
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value !=
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value !=
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                        case "==":
                            if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueInt)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueInt)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueInt && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueInt)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is Tree.ConstantValueFloat && st.SubTree[1].Value is Tree.ConstantValueFloat)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((Tree.ConstantValueFloat)(st.SubTree[0].Value)).Value ==
                                    ((Tree.ConstantValueFloat)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueVector && st.SubTree[1].Value is ConstantValueVector)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueVector)(st.SubTree[0].Value)).Value ==
                                    ((ConstantValueVector)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else if (st.SubTree[0].Value is ConstantValueRotation && st.SubTree[1].Value is ConstantValueRotation)
                            {
                                st.Value = new Tree.ConstantValueInt(
                                    ((ConstantValueRotation)(st.SubTree[0].Value)).Value ==
                                    ((ConstantValueRotation)(st.SubTree[1].Value)).Value ? 1 : 0);
                            }
                            else
                            {
                                throw new Resolver.ResolverException(string.Format("Cannot process '{0}' on parameters of mismatching type", st.Entry));
                            }
                            break;

                    }
                }
                #endregion
                #region Left unary operators
                else if (st.Type == Tree.EntryType.OperatorLeftUnary && (st.SubTree[0].Value != null || st.SubTree[0].Type == Tree.EntryType.Value))
                {
                    if(st.Entry != "-" && st.SubTree[0].Type == Tree.EntryType.Value)
                    {
                        st.Process();
                    }
                    if (st.Entry == "+")
                    {
                        st.Value = st.SubTree[0].Value;
                    }
                    else if (st.Entry == "-")
                    {
                        if(st.SubTree[0].Value == null)
                        {
                            st.SubTree[0].Process();
                        }
                        if (st.Value == null)
                        {
                            st.Value = st.SubTree[0].Value.Negate();
                        }
                    }
                    else if (st.Entry == "~")
                    {
                        if (st.Value is Tree.ConstantValueFloat)
                        {
                            throw new Resolver.ResolverException("float cannot be binary-negated");
                        }
                        else if (st.Value is Tree.ConstantValueInt)
                        {
                            st.Value = new Tree.ConstantValueInt(~((Tree.ConstantValueInt)(st.Value)).Value);
                        }
                        else if (st.Value is Tree.ConstantValueString)
                        {
                            throw new Resolver.ResolverException("string cannot be binary negated");
                        }
                    }
                    else if (st.Entry == "!")
                    {
                        if (st.Value is Tree.ConstantValueFloat)
                        {
                            throw new Resolver.ResolverException("float cannot be logically negated");
                        }
                        else if (st.Value is Tree.ConstantValueInt)
                        {
                            st.Value = new Tree.ConstantValueInt(((Tree.ConstantValueInt)(st.Value)).Value == 0 ? 1 : 0);
                        }
                        else if (st.Value is Tree.ConstantValueString)
                        {
                            throw new Resolver.ResolverException("string cannot be logically negated");
                        }
                    }
                    else if(st.SubTree[0].Value != null)
                    {
                        switch (st.Entry)
                        {
                            case "(string)":
                                if (st.SubTree[0].Value is ConstantValueRotation)
                                {
                                    st.Value = new Tree.ConstantValueString(((ConstantValueRotation)st.SubTree[0].Value).ToString());
                                }
                                else if (st.SubTree[0].Value is ConstantValueVector)
                                {
                                    st.Value = new Tree.ConstantValueString(((ConstantValueVector)st.SubTree[0].Value).ToString());
                                }
                                else if (st.SubTree[0].Value is Tree.ConstantValueFloat)
                                {
                                    st.Value = new Tree.ConstantValueString(((Tree.ConstantValueFloat)st.SubTree[0].Value).ToString());
                                }
                                else if (st.SubTree[0].Value is Tree.ConstantValueInt)
                                {
                                    st.Value = new Tree.ConstantValueString(((Tree.ConstantValueInt)st.SubTree[0].Value).ToString());
                                }
                                else if (st.SubTree[0].Value is Tree.ConstantValueString)
                                {
                                    st.Value = st.SubTree[0].Value;
                                }
                                else if (st.SubTree[0].Type == Tree.EntryType.Level && st.SubTree[0].Entry == "[")
                                {
                                    /* check if all parts are constants */
                                    bool isConstant = true;
                                    foreach (Tree sst in st.SubTree[0].SubTree)
                                    {
                                        if (sst.Value == null)
                                        {
                                            isConstant = false;
                                        }
                                    }

                                    if (isConstant)
                                    {
                                        string o = string.Empty;
                                        foreach (Tree sst in st.SubTree[0].SubTree)
                                        {
                                            o += sst.Value.ToString();
                                        }
                                        st.Value = new Tree.ConstantValueString(o);
                                    }
                                }
                                break;

                            case "(rotation)":
                            case "(quaternion)":
                                if (st.SubTree[0].Value is ConstantValueRotation)
                                {
                                    st.Value = st.SubTree[0].Value;
                                }
                                else if (st.SubTree[0].Value is Tree.ConstantValueString)
                                {
                                    Quaternion q;
                                    if (Quaternion.TryParse(((Tree.ConstantValueString)st.SubTree[0].Value).Value, out q))
                                    {
                                        st.Value = new ConstantValueRotation(q);
                                    }
                                    else
                                    {
                                        st.Value = new ConstantValueRotation(Quaternion.Identity);
                                    }
                                }
                                break;

                            case "(integer)":
                                if (st.SubTree[0].Value is Tree.ConstantValueInt)
                                {
                                    st.Value = st.SubTree[0].Value;
                                }
                                else if (st.SubTree[0].Value is Tree.ConstantValueFloat)
                                {
                                    st.Value = new Tree.ConstantValueInt(LSLCompiler.ConvToInt(((Tree.ConstantValueFloat)st.SubTree[0].Value).Value));
                                }
                                else if (st.SubTree[0].Value is Tree.ConstantValueString)
                                {
                                    try
                                    {
                                        st.Value = new Tree.ConstantValueInt(LSLCompiler.ConvToInt(((Tree.ConstantValueString)st.SubTree[0].Value).Value));
                                    }
                                    catch
                                    {
                                        st.Value = new Tree.ConstantValueInt(0);
                                    }
                                }
                                break;

                            case "(float)":
                                if (st.SubTree[0].Value is Tree.ConstantValueFloat)
                                {
                                    st.Value = st.SubTree[0].Value;
                                }
                                else if (st.SubTree[0].Value is Tree.ConstantValueInt)
                                {
                                    st.Value = new Tree.ConstantValueFloat((int)((Tree.ConstantValueInt)st.SubTree[0].Value).Value);
                                }
                                else if (st.SubTree[0].Value is Tree.ConstantValueString)
                                {
                                    try
                                    {
                                        st.Value = new Tree.ConstantValueFloat(double.Parse(((Tree.ConstantValueString)st.SubTree[0].Value).Value, NumberStyles.Float, CultureInfo.InvariantCulture));
                                    }
                                    catch
                                    {
                                        st.Value = new Tree.ConstantValueFloat(0);
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
                #endregion
                #region Parenthesis
                else if(st.Type == Tree.EntryType.Level && st.Entry == "(" && st.SubTree.Count == 1)
                {
                    st.Value = st.SubTree[0].Value;
                }
                #endregion
            }
        }

        void combineTypecasts(CompileState cs, Tree resolvetree)
        {
            int i;

            for(i = 0; i < resolvetree.SubTree.Count; ++i)
            {
                if(resolvetree.SubTree[i].SubTree.Count != 0)
                {
                    throw new Resolver.ResolverException("invalid state for combineTypecasts");
                }
            }

            for(i = 0; i < resolvetree.SubTree.Count; ++i)
            {
                if(i + 2 < resolvetree.SubTree.Count)
                {
                    if(resolvetree.SubTree[i].Entry == "(" && resolvetree.SubTree[i + 2].Entry == ")" &&
                        m_ReservedWords.Contains(resolvetree.SubTree[i + 1].Entry))
                    {
                        switch(resolvetree.SubTree[i + 1].Entry)
                        {
                            case "integer":
                            case "float":
                            case "string":
                            case "list":
                            case "rotation":
                            case "quaternion":
                            case "vector":
                            case "key":
                                resolvetree.SubTree[i].Entry = "(" + resolvetree.SubTree[i + 1].Entry + ")";
                                resolvetree.SubTree.RemoveAt(i + 1);
                                resolvetree.SubTree.RemoveAt(i + 1);
                                break;

                            default:
                                throw new Resolver.ResolverException(string.Format("invalid typecast {0}", resolvetree.SubTree[i + 1].Entry));
                        }
                    }
                }
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

        void solveMaxNegValues(CompileState cs, Tree resolvetree)
        {
            List<ListTreeEnumState> enumeratorStack = new List<ListTreeEnumState>();
            enumeratorStack.Insert(0, new ListTreeEnumState(resolvetree));
            while(enumeratorStack.Count != 0)
            {
                if (!enumeratorStack[0].MoveNext())
                {
                    enumeratorStack.RemoveAt(0);
                }
                else
                {
                    resolvetree = enumeratorStack[0].Current;
                    if (resolvetree.Type == Tree.EntryType.OperatorLeftUnary && resolvetree.Entry == "-" &&
                        resolvetree.SubTree.Count == 1 && resolvetree.SubTree[0].Entry == "2147483648" && resolvetree.SubTree[0].Type == Tree.EntryType.Value)
                    {
                        resolvetree.Value = new Tree.ConstantValueInt(-2147483648);
                    }
                    else if (resolvetree.Entry == "2147483648" && resolvetree.Type == Tree.EntryType.Value)
                    {
                        resolvetree.Value = new Tree.ConstantValueFloat(2147483648f);
                    }
                    else
                    {
                        enumeratorStack.Insert(0, new ListTreeEnumState(resolvetree));
                    }
                }
            }
        }

        void solveTree(CompileState cs, Tree resolvetree, ICollection<string> varNames)
        {
            combineTypecasts(cs, resolvetree);
            m_Resolver.Process(resolvetree, varNames);
            solveDeclarations(resolvetree);
            solveMaxNegValues(cs, resolvetree);
            solveConstantOperations(resolvetree);
        }

    }
}

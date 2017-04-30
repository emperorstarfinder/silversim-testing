﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;

namespace SilverSim.Types
{
    [SuppressMessage("Gendarme.Rules.Design", "EnsureSymmetryForOverloadedOperatorsRule")]
    public sealed class AnArray : List<IValue>, IValue
    {
        #region Constructors
        public AnArray()
        {

        }

        public AnArray(AnArray v)
            : base(v)
        {

        }

        public AnArray(List<IValue> ival)
        {
            foreach(IValue v in ival)
            {
                Add(v);
            }
        }
        #endregion Constructors

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.Array;
            }
        }

        public LSLValueType LSL_Type 
        { 
            get
            {
                return LSLValueType.Invalid;
            }
        }

        #endregion Properties

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            bool first = true;
            foreach(IValue iv in this)
            {
                if(!first)
                {
                    s.Append(",");
                }
                s.Append(iv.ToString());
                first = false;
            }
            return s.ToString();
        }

        #region Add methods
        public void Add(bool val)
        {
            base.Add(new ABoolean(val));
        }

        public void Add(double val)
        {
            base.Add(new Real(val));
        }

        public void Add(string val)
        {
            base.Add(new AString(val));
        }

        public void Add(Uri val)
        {
            base.Add(new URI(val.ToString()));
        }

        public void Add(Int32 val)
        {
            base.Add(new Integer(val));
        }

        public void AddLongInt(long val)
        {
            base.Add(new LongInteger(val));
        }

        public static AnArray operator +(AnArray a, AnArray b)
        {
            AnArray n = new AnArray(a);
            n.AddRange(b);
            return n;
        }
        public static AnArray operator +(AnArray a, int b)
        {
            AnArray n = new AnArray(a);
            n.Add(b);
            return n;
        }
        public static AnArray operator +(AnArray a, double b)
        {
            AnArray n = new AnArray(a);
            n.Add(b);
            return n;
        }
        public static AnArray operator +(AnArray a, string b)
        {
            AnArray n = new AnArray(a);
            n.Add(b);
            return n;
        }
        public static AnArray operator +(AnArray a, Vector3 b)
        {
            AnArray n = new AnArray(a);
            n.Add(b);
            return n;
        }
        public static AnArray operator +(AnArray a, Quaternion b)
        {
            AnArray n = new AnArray(a);
            n.Add(b);
            return n;
        }
        #endregion Add methods

        public bool TryGetValue<T>(int key, out T val)
        {
            IValue iv;
            if(key < 0 || key >= Count)
            {
                val = default(T);
                return false;
            }
            iv = this[key];
            if (!(iv is T))
            {
                val = default(T);
                return false;
            }
            val = (T)iv;
            return true;
        }

        public IEnumerable<T> GetValues<T>()
        {
            return from val in this where val is T select (T)val;
        }

        #region Helper
        public ABoolean AsBoolean { get { return new ABoolean(); } }
        public Integer AsInteger { get { return new Integer(); } }
        public Quaternion AsQuaternion { get { return new Quaternion(); } }
        public Real AsReal { get { return new Real(); } }
        public AString AsString { get { return new AString(); } }
        public UUID AsUUID { get { return new UUID(); } }
        public Vector3 AsVector3 { get { return new Vector3(); } }
        public uint AsUInt { get { return 0; } }
        public int AsInt { get { return 0; } }
        public ulong AsULong { get { return 0; } }
        public long AsLong { get { return 0; } }
        #endregion 

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public Vector4 ElementsToVector4
        {
            get
            {
                return new Vector4(this[0].AsReal, this[1].AsReal, this[2].AsReal, this[3].AsReal);
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public Vector3 ElementsToVector3
        {
            get
            {
                return new Vector3(this[0].AsReal, this[1].AsReal, this[2].AsReal);
            }
        }

        #region Stateful Enumerator
        public class MarkEnumerator : IEnumerator<IValue>, IEnumerator, IDisposable
        {
            private int m_CurrentIndex;
            private int m_MarkIndex;
            private AnArray m_Array;

            public MarkEnumerator(AnArray array)
            {
                m_Array = array;
                m_CurrentIndex = -1;
            }
            public IValue Current 
            { 
                get
                {
                    return m_Array[m_CurrentIndex];
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public void Dispose()
            {
                m_Array = null;
            }

            public void Reset()
            {
                m_CurrentIndex = -1;
            }

            public bool MoveNext()
            {
                return ++m_CurrentIndex < m_Array.Count;
            }

            public void MarkPosition()
            {
                m_MarkIndex = m_CurrentIndex;
            }

            public void GoToMarkPosition()
            {
                m_CurrentIndex = m_MarkIndex;
            }
        }

        public MarkEnumerator GetMarkEnumerator()
        {
            return new MarkEnumerator(this);
        }
        #endregion
    }
}

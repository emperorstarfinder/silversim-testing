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

#pragma warning disable RCS1123

using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SilverSim.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3 : IEquatable<Vector3>, IValue
    {
        public double X;
        public double Y;
        public double Z;

        #region Properties
        public ValueType Type => ValueType.Vector;

        public LSLValueType LSL_Type => LSLValueType.Vector;
        #endregion Properties

        #region Constructors
        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(double value)
        {
            X = value;
            Y = value;
            Z = value;
        }

        public Vector3(Vector3 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public Vector3(byte[] data, int offset)
        {
            X = 0;
            Y = 0;
            Z = 0;
            FromBytes(data, offset);
        }

        #endregion Constructors

        #region Properties
        /** <summary>Delivers X and Y length only. No Z component</summary> */
        public double HorizontalLength => Math.Sqrt(X * X + Y * Y);

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public double LengthSquared => X * X + Y * Y + Z * Z;
        #endregion Properties

        /// <summary>
        /// Test if this vector is equal to another vector, within a given
        /// tolerance range
        /// </summary>
        /// <param name="vec">Vector to test against</param>
        /// <param name="tolerance">The acceptable magnitude of difference
        /// between the two vectors</param>
        /// <returns>True if the magnitude of difference between the two vectors
        /// is less than the given tolerance, otherwise false</returns>
        public bool ApproxEquals(Vector3 vec, double tolerance) => (this - vec).LengthSquared <= tolerance * tolerance;

        /// <summary>
        /// IComparable.CompareTo implementation
        /// </summary>
        public int CompareTo(Vector3 vector) => Length.CompareTo(vector.Length);

        /// <summary>
        /// Builds a vector from a byte array
        /// </summary>
        /// <param name="byteArray">Byte array containing a 12 byte vector</param>
        /// <param name="pos">Beginning position in the byte array</param>
        public void FromBytes(byte[] byteArray, int pos)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // Big endian architecture
                var conversionBuffer = new byte[12];

                Buffer.BlockCopy(byteArray, pos, conversionBuffer, 0, 12);

                Array.Reverse(conversionBuffer, 0, 4);
                Array.Reverse(conversionBuffer, 4, 4);
                Array.Reverse(conversionBuffer, 8, 4);

                X = BitConverter.ToSingle(conversionBuffer, 0);
                Y = BitConverter.ToSingle(conversionBuffer, 4);
                Z = BitConverter.ToSingle(conversionBuffer, 8);
            }
            else
            {
                // Little endian architecture
                X = BitConverter.ToSingle(byteArray, pos);
                Y = BitConverter.ToSingle(byteArray, pos + 4);
                Z = BitConverter.ToSingle(byteArray, pos + 8);
            }
        }

        /// <summary>
        /// Writes the raw bytes for this vector to a byte array
        /// </summary>
        /// <param name="dest">Destination byte array</param>
        /// <param name="pos">Position in the destination array to start
        /// writing. Must be at least 12 bytes before the end of the array</param>
        public void ToBytes(byte[] dest, int pos)
        {
            Buffer.BlockCopy(BitConverter.GetBytes((float)X), 0, dest, pos + 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((float)Y), 0, dest, pos + 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((float)Z), 0, dest, pos + 8, 4);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(dest, pos + 0, 4);
                Array.Reverse(dest, pos + 4, 4);
                Array.Reverse(dest, pos + 8, 4);
            }
        }

        public double Dot(Vector3 value2) => X * value2.X + Y * value2.Y + Z * value2.Z;

        public static Vector3 Normalize(Vector3 value) => new Vector3(value).Normalize();

        public Vector3 Normalize()
        {
            double factor = Length;
            if (factor > Double.Epsilon)
            {
                factor = 1f / factor;
                X *= factor;
                Y *= factor;
                Z *= factor;
            }
            else
            {
                X = 0f;
                Y = 0f;
                Z = 0f;
            }
            return this;
        }

        /// <summary>
        /// Parse a vector from a string
        /// </summary>
        /// <param name="val">A string representation of a 3D vector, enclosed 
        /// in arrow brackets and separated by commas</param>
        public static Vector3 Parse(string val)
        {
            Vector3 v;
            if(!TryParse(val, out v))
            {
                throw new ArgumentException("Invalid Vector3 string specified");
            }
            return v;
        }

        public static bool TryParse(string val, out Vector3 result)
        {
            result = default(Vector3);
            char[] splitChar = { ',' };
            var split = val.Replace("<", System.String.Empty).Replace(">", System.String.Empty).Split(splitChar);
            if(split.Length != 3)
            {
                return false;
            }

            double x;
            double y;
            double z;

            if(!Double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                !Double.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                !Double.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            {
                return false;
            }
            result = new Vector3(x, y, z);
            return true;
        }

        /// <summary>
        /// Calculate the rotation between two vectors
        /// </summary>
        /// <param name="a">Normalized directional vector (such as 1,0,0 for forward facing)</param>
        /// <param name="b">Normalized target vector</param>
        public static Quaternion RotationBetween(Vector3 a, Vector3 b)
        {
            double dotProduct = a.Dot(b);
            var crossProduct = a.Cross(b);
            double magProduct = a.Length * b.Length;
            double angle = Math.Acos(dotProduct / magProduct);
            var axis = Normalize(crossProduct);
            double s = Math.Sin(angle / 2d);

            return new Quaternion(
                axis.X * s,
                axis.Y * s,
                axis.Z * s,
                Math.Cos(angle / 2d));
        }

        public static Vector3 Transform(Vector3 position, Matrix4 matrix) => new Vector3(
                (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41,
                (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42,
                (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43);

        public static Vector3 TransformNormal(Vector3 position, Matrix4 matrix) => new Vector3(
                (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31),
                (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32),
                (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33));

        public override bool Equals(object obj) => (obj is Vector3) && this == (Vector3)obj;

        public bool Equals(Vector3 other) => this == other;

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

        public Vector3 ComponentMin(Vector3 b) => new Vector3(
                Math.Min(X, b.X),
                Math.Min(Y, b.Y),
                Math.Min(Z, b.Z));

        public Vector3 ComponentMin(double b) => new Vector3(
                Math.Min(X, b),
                Math.Min(Y, b),
                Math.Min(Z, b));

        public Vector3 ComponentMax(Vector3 b) => new Vector3(
                Math.Max(X, b.X),
                Math.Max(Y, b.Y),
                Math.Max(Z, b.Z));

        public Vector3 ComponentMax(double b) => new Vector3(
                Math.Max(X, b),
                Math.Max(Y, b),
                Math.Max(Z, b));

        public Vector3 ComponentClamp(double min, double max) => new Vector3(
            X.Clamp(min, max),
            Y.Clamp(min, max),
            Z.Clamp(min, max));

        public Vector3 ComponentClamp(Vector3 min, Vector3 max) => new Vector3(
            X.Clamp(min.X, max.X),
            Y.Clamp(min.Y, max.Y),
            Z.Clamp(min.Z, max.Z));

        /// <summary>
        /// Get a formatted string representation of the vector
        /// </summary>
        /// <returns>A string representation of the vector</returns>
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "<{0},{1},{2}>", X, Y, Z);

        public string X_String
        {
            get { return string.Format(CultureInfo.InvariantCulture, "{0}", X); }

            set { X = double.Parse(value, CultureInfo.InvariantCulture); }
        }

        public string Y_String
        {
            get { return string.Format(CultureInfo.InvariantCulture, "{0}", Y); }

            set { Y = double.Parse(value, CultureInfo.InvariantCulture); }
        }

        public string Z_String
        {
            get { return string.Format(CultureInfo.InvariantCulture, "{0}", Z); }

            set { Z = double.Parse(value, CultureInfo.InvariantCulture); }
        }

        public static Vector3 Lerp(Vector3 lhs, Vector3 rhs, double c) => lhs + (rhs - lhs) * c;

        public Vector3 ElementDivide(Vector3 b) => new Vector3(X / b.X, Y / b.Y, Z / b.Z);

        public Vector3 ElementMultiply(Vector3 b) => new Vector3(X * b.X, Y * b.Y, Z * b.Z);

        #region Operators
        public static bool operator ==(Vector3 value1, Vector3 value2) => Math.Abs(value1.X - value2.X) < Double.Epsilon
                && Math.Abs(value1.Y - value2.Y) < Double.Epsilon
                && Math.Abs(value1.Z - value2.Z) < Double.Epsilon;

        public static bool operator !=(Vector3 value1, Vector3 value2) => !(value1 == value2);

        public static Vector3 operator +(Vector3 value1, Vector3 value2) => new Vector3(value1.X + value2.X, value1.Y + value2.Y, value1.Z + value2.Z);

        public static AnArray operator +(Vector3 v, AnArray a)
        {
            var b = new AnArray
            {
                v
            };
            b.AddRange(a);
            return b;
        }

        public static Vector3 operator -(Vector3 value) => new Vector3(-value.X, -value.Y, -value.Z);

        public static Vector3 operator -(Vector3 value1, Vector3 value2) => new Vector3(value1.X - value2.X, value1.Y - value2.Y, value1.Z - value2.Z);

        public static Vector3 operator *(Vector3 value, double scaleFactor) => new Vector3(value.X * scaleFactor, value.Y * scaleFactor, value.Z * scaleFactor);

        public static Vector3 operator *(double scaleFactor, Vector3 value) => new Vector3(value.X * scaleFactor, value.Y * scaleFactor, value.Z * scaleFactor);

        public static Vector3 operator *(Vector3 vec, Quaternion rot)
        {
            double rw = -rot.X * vec.X - rot.Y * vec.Y - rot.Z * vec.Z;
            double rx = rot.W * vec.X + rot.Y * vec.Z - rot.Z * vec.Y;
            double ry = rot.W * vec.Y + rot.Z * vec.X - rot.X * vec.Z;
            double rz = rot.W * vec.Z + rot.X * vec.Y - rot.Y * vec.X;

            vec.X = -rw * rot.X + rx * rot.W - ry * rot.Z + rz * rot.Y;
            vec.Y = -rw * rot.Y + ry * rot.W - rz * rot.X + rx * rot.Z;
            vec.Z = -rw * rot.Z + rz * rot.W - rx * rot.Y + ry * rot.X;

            return vec;
        }

        public static Vector3 operator /(Vector3 vec, Quaternion rot) => vec * rot.Conjugate();

        public static Vector3 operator *(Vector3 vector, Matrix4 matrix) => Transform(vector, matrix);

        public static double operator *(Vector3 v1, Vector3 v2) => v1.Dot(v2);

        public static Vector3 operator %(Vector3 v1, Vector3 v2) => v1.Cross(v2);

        public static Vector3 operator /(Vector3 value, double divider) =>
            new Vector3(value.X / divider, value.Y / divider, value.Z / divider);

        /// <summary>
        /// Cross product between two vectors
        /// </summary>
        public Vector3 Cross(Vector3 value2) => new Vector3(
                Y * value2.Z - value2.Y * Z,
                Z * value2.X - value2.Z * X,
                X * value2.Y - value2.X * Y);

        public static explicit operator Vector3(string val) => Parse(val);

        public static explicit operator string(Vector3 val) => val.ToString();

        public static explicit operator GridVector(Vector3 v) => new GridVector((uint)(v.X * 256f), (uint)(v.Y * 256f));
        #endregion Operators

        #region Helpers
        public ABoolean AsBoolean => new ABoolean(Length >= Single.Epsilon);
        public Integer AsInteger => new Integer((int)Length);
        public Quaternion AsQuaternion => new Quaternion(X, Y, Z, 1);
        public Real AsReal => new Real(Length);
        public AString AsString => new AString(ToString());
        public UUID AsUUID => new UUID();
        public Vector3 AsVector3 => new Vector3(X, Y, Z);
        public uint AsUInt => (uint)Length;
        public int AsInt => (int)Length;
        public ulong AsULong => (ulong)Length;
        public long AsLong => (long)Length;
        public bool IsNaN => double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(Z);
        #endregion

        /// <summary>A vector with a value of 0,0,0</summary>
        public readonly static Vector3 Zero = new Vector3();
        /// <summary>A vector with a value of 1,1,1</summary>
        public readonly static Vector3 One = new Vector3(1f, 1f, 1f);
        /// <summary>A unit vector facing forward (X axis), value 1,0,0</summary>
        public readonly static Vector3 UnitX = new Vector3(1f, 0f, 0f);
        /// <summary>A unit vector facing left (Y axis), value 0,1,0</summary>
        public readonly static Vector3 UnitY = new Vector3(0f, 1f, 0f);
        /// <summary>A unit vector facing up (Z axis), value 0,0,1</summary>
        public readonly static Vector3 UnitZ = new Vector3(0f, 0f, 1f);
    }
}
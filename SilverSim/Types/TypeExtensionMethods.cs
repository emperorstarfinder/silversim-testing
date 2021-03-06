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

using SilverSim.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace SilverSim.Types
{
    public static class TypeExtensionMethods
    {
        public static int PosIfNotNeg(this double a) => a < 0 ? -1 : 1;

        public static double Mix(this double a, double b, double m) => a * (1 - m) + b * m;

        public static double Lerp(this double a, double b, double u) => a + ((b - a) * u);

        public static Vector3 ClampElements(this Vector3 val, double min, double max) => new Vector3(val.X.Clamp(min, max), val.Y.Clamp(min, max), val.Z.Clamp(min, max));

        public static double Clamp(this double val, double min, double max)
        {
            if(val < min)
            {
                return min;
            }
            else if(val > max)
            {
                return max;
            }
            else
            {
                return val;
            }
        }

        public static double DegToRad(this int val) => val * Math.PI / 180.0;

        public static double DegToRad(this double val) => val * Math.PI / 180.0;

        public static double RadToDeg(this double val) => val * 180.0 / Math.PI;

        public static Vector3 DegToRad(this Vector3 val) => val * Math.PI / 180.0;

        public static Vector3 RadToDeg(this Vector3 val) => val * 180.0 / Math.PI;

        public static bool IsInRange(this double val, double min, double max) => val >= min && val <= max;

        public static bool IsFinite(this double value) => !(double.IsNaN(value) || double.IsInfinity(value));

        public static int Clamp(this int val, int min, int max)
        {
            if (val < min)
            {
                return min;
            }
            else if(val > max)
            {
                return max;
            }
            else
            {
                return val;
            }
        }

        public static double Clamp(this Real val, double min, double max)
        {
            if (val < min)
            {
                return min;
            }
            else if (val > max)
            {
                return max;
            }
            else
            {
                return val;
            }
        }

        public static Vector3 AgentLookAt(this Quaternion quat)
        {
            double roll;
            double pitch;
            double yaw;
            quat.GetEulerAngles(out roll, out pitch, out yaw);
            return new Vector3(Math.Cos(yaw), Math.Sin(yaw), 0);
        }

        public static Quaternion AgentLookAtToQuaternion(this Vector3 lookat)
        {
            double yaw = Math.Atan2(lookat.Y, lookat.X);
            return Quaternion.CreateFromEulers(0, 0, yaw);
        }

        private static readonly UTF8Encoding m_UTF8NoBOM = new UTF8Encoding(false);

        public static byte[] ToUTF8Bytes(this string s) => m_UTF8NoBOM.GetBytes(s);

        public static int ToUTF8ByteCount(this string s) => m_UTF8NoBOM.GetByteCount(s);

        public static string FromUTF8Bytes(this byte[] data)
        {
            string s = m_UTF8NoBOM.GetString(data);
            int pos = s.IndexOf('\0');
            return (pos >= 0) ? s.Substring(0, pos) : s;
        }

        public static string FromUTF8Bytes(this byte[] data, int index, int count)
        {
            if(count <= 0)
            {
                return string.Empty;
            }
            string s = m_UTF8NoBOM.GetString(data, index, count);
            int pos = s.IndexOf('\0');
            return (pos >= 0) ? s.Substring(0, pos) : s;
        }

        public static XmlTextWriter UTF8XmlTextWriter(this Stream s) => new XmlTextWriter(s, m_UTF8NoBOM);

        public static StreamReader UTF8StreamReader(this Stream s) => new StreamReader(s, m_UTF8NoBOM);

        public static StreamWriter UTF8StreamWriter(this Stream s) => new StreamWriter(s, m_UTF8NoBOM);

        public static StreamWriter UTF8StreamWriterLeaveOpen(this Stream s) => new StreamWriter(s, m_UTF8NoBOM, 16384, true);

        public static byte[] FromHexStringToByteArray(this string hex) => Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();

        public static string ToHexString(this byte[] data) => BitConverter.ToString(data).Replace("-", string.Empty);

        public static string ComputeMD5(this string input)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(m_UTF8NoBOM.GetBytes(input)).ToHexString().ToLower();
            }
        }

        public static string TrimToMaxLength(this string s, int length) => length < s.Length ?
                (
                    length < 0 ?
                    string.Empty :
                    s.Substring(0, length)
                ) :
                s;

        public static bool EndsWith(this StringBuilder s, string m)
        {
            int n = m.Length;
            int ofs;
            if(s.Length < n)
            {
                return false;
            }
            ofs = s.Length - m.Length;
            for(int i = 0; i < n; ++i)
            {
                if(s[ofs + i] != m[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool StartsWith(this StringBuilder s, string m)
        {
            int n = m.Length;
            if (s.Length < n)
            {
                return false;
            }

            for(int i = 0; i < n; ++i)
            {
                if(s[i] != m[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static byte[] ReadToStreamEnd(this Stream s)
        {
            var segments = new List<byte[]>();
            int dataSize;
            int totalSize = 0;
            byte[] buffer;
            do
            {
                buffer = new byte[10240];
                dataSize = s.Read(buffer, 0, buffer.Length);
                totalSize += dataSize;
                if (dataSize < buffer.Length)
                {
                    var actData = new byte[dataSize];
                    Buffer.BlockCopy(buffer, 0, actData, 0, dataSize);
                    segments.Add(actData);
                }
                else
                {
                    segments.Add(buffer);
                }
            } while (dataSize != 0);

            int offset = 0;
            var finalData = new byte[totalSize];
            foreach(var seg in segments)
            {
                Buffer.BlockCopy(seg, 0, finalData, offset, seg.Length);
                offset += seg.Length;
            }
            return finalData;
        }

        #region ReferenceBoxed<Vector3>
        public static Vector3 ElementDivide(this ReferenceBoxed<Vector3> boxed, Vector3 value)
        {
            return ((Vector3)boxed).ElementDivide(value);
        }
        public static Vector3 ElementMultiply(this ReferenceBoxed<Vector3> boxed, Vector3 value)
        {
            return ((Vector3)boxed).ElementMultiply(value);
        }
        #endregion

        #region Intrinsic Tait-Bryan-Angles aka. Nautical Angles
        public static Vector3 GetNauticalAngles(this Quaternion q) => new Vector3(
                Math.Atan2(2 * (q.W * q.X + q.Y * q.Z), 1 - 2 * (q.X * q.X + q.Y * q.Y)),
                Math.Asin(2 * (q.W * q.Y - q.Z * q.X)),
                Math.Atan2(2 * (q.W * q.Z + q.X * q.Y), 1 - 2 * (q.Y * q.Y + q.Z * q.Z))
                );

        public static Quaternion FromNauticalAngles(this Vector3 v) => Quaternion.CreateFromEulers(v.X, 0, 0) * Quaternion.CreateFromEulers(0, v.Y, 0) * Quaternion.CreateFromEulers(0, 0, v.Z);
        #endregion
    }
}

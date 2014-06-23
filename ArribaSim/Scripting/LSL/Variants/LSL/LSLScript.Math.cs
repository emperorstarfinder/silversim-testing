﻿/*

ArribaSim is distributed under the terms of the
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public int llAbs(int v)
        {
            if(v < 0)
            {
                return -v;
            }
            else
            {
                return v;
            }
        }

        public double llAcos(double v)
        {
            return Math.Acos(v);
        }

        public double llAsin(double v)
        {
            return Math.Asin(v);
        }

        public double llAtan2(double y, double x)
        {
            return Math.Atan2(y, x);
        }

        public double llCos(double v)
        {
            return Math.Cos(v);
        }

        public double llFabs(double v)
        {
            return Math.Abs(v);
        }

        public double llLog(double v)
        {
            return Math.Log(v);
        }

        public double llLog10(double v)
        {
            return Math.Log10(v);
        }

        public double llPow(double bas, double exponent)
        {
            return Math.Pow(bas, exponent);
        }

        public double llSin(double v)
        {
            return Math.Sin(v);
        }

        public double llSqrt(double v)
        {
            return Math.Sqrt(v);
        }

        public double llTan(double v)
        {
            return Math.Tan(v);
        }

        public double llVecDist(Vector3 a, Vector3 b)
        {
            return (a - b).Length;
        }

        public double llVecMag(Vector3 v)
        {
            return v.Length;
        }

        public Vector3 llVecNorm(Vector3 v)
        {
            return v / v.Length;
        }

        public int llModPow(int a, int b, int c)
        {
            return ((int)Math.Pow(a, b)) % c;
        }

        public Vector3 llRot2Euler(Quaternion q)
        {
            double roll, pitch, yaw;

            q.GetEulerAngles(out roll, out pitch, out yaw);
            return new Vector3(roll, pitch, yaw);
        }

        public double llRot2Angle(Quaternion r)
        {
            /* based on http://wiki.secondlife.com/wiki/LlRot2Angle */
            double s2 = r.Z * r.Z; // square of the s-element
            double v2 = r.X * r.X + r.Y * r.Y + r.Z * r.Z; // sum of the squares of the v-elements

            if (s2 < v2) // compare the s-component to the v-component
                return 2.0 * Math.Acos(Math.Sqrt(s2 / (s2 + v2))); // use arccos if the v-component is dominant
            if (v2 != 0) // make sure the v-component is non-zero
                return 2.0 * Math.Asin(Math.Sqrt(v2 / (s2 + v2))); // use arcsin if the s-component is dominant

            return 0.0; // argument is scaled too small to be meaningful, or it is a zero rotation, so return zer
        }

        public Vector3 llRot2Axis(Quaternion q)
        {
            return llVecNorm(new Vector3(q.X, q.Y, q.Z)) * Math.Sign(q.W);
        }

        public Quaternion llAxisAngle2Rot(Vector3 axis, double angle)
        {
            return Quaternion.CreateFromAxisAngle(axis, angle);
        }

        public Quaternion llEuler2Rot(Vector3 v)
        {
            return Quaternion.CreateFromEulers(v);
        }

        public double llAngleBetween(Quaternion a, Quaternion b)
        {   /* based on http://wiki.secondlife.com/wiki/LlAngleBetween */
            Quaternion r = b / a;
            double s2 = r.W * r.W;
            double v2 = r.X * r.X + r.Y * r.Y + r.Z * r.Z;
            if (s2 < v2)
            {
                return 2.0 * Math.Acos(Math.Sqrt(s2 / (s2 + v2)));
            }
            else if (v2 > Double.Epsilon)
            {
                return 2.0 * Math.Asin(Math.Sqrt(v2 / (s2 + v2)));
            }
            return 0f;
        }

        public Quaternion llAxes2Rot(Vector3 fwd, Vector3 left, Vector3 up)
        {
            double s;
            double t = fwd.X + left.Y + up.Z + 1.0;

            if(t >= 1.0)
            {
                s = 0.5 / Math.Sqrt(t);
                return new Quaternion((left.Z - up.Y) * s, (up.X - fwd.Z) * s, (fwd.Y - left.X) * s, 0.25 / s);
            }
            else
            {
                double m = (left.Y > up.Z) ? left.Y : up.Z;

                if(m < fwd.X)
                {
                    s = Math.Sqrt(fwd.X - (left.Y + up.Z) + 1.0);
                    return new Quaternion(
                        s * 0.5,
                        (fwd.Y + left.X) * (0.5 / s),
                        (up.X + fwd.Z) * (0.5 / s),
                        (left.Z - up.Y) * (0.5 / s));
                }
                else if(m == left.Y)
                {
                    s = Math.Sqrt(left.Y - (up.Z + fwd.X) + 1.0);
                    return new Quaternion(
                        (fwd.Y + left.X) * (0.5 / s),
                        s * 0.5,
                        (left.Z + up.Y) * (0.5 / s),
                        (up.X - fwd.Z) * (0.5 / s));
                }
                else
                {
                    s = Math.Sqrt(up.Z - (fwd.X + left.Y) + 1.0);
                    return new Quaternion(
                        (up.X + fwd.Z) * (0.5 / s),
                        (left.Z + up.Y) * (0.5 / s),
                        s * 0.5,
                        (fwd.Y - left.X) * (0.5 / s));
                }
            }
        }

        public Vector3 llRot2Fwd(Quaternion r)
        {
            double x, y, z, sq;
            sq = r.LengthSquared;
            if(Math.Abs(1.0 -sq)>0.000001)
            {
                sq = 1.0 / Math.Sqrt(sq);
                r.X *= sq;
                r.Y *= sq;
                r.Z *= sq;
                r.W *= sq;
            }

            x = r.X * r.X - r.Y * r.Y - r.Z * r.Z + r.W * r.W;
            y = 2 * (r.X * r.Y + r.Z * r.W);
            z = 2 * (r.X * r.Z - r.Y * r.W);
            return new Vector3(x, y, z);
        }

        public Vector3 llRot2Left(Quaternion r)
        {
            double x, y, z, sq;

            sq = r.LengthSquared;
            if (Math.Abs(1.0 - sq) > 0.000001)
            {
                sq = 1.0 / Math.Sqrt(sq);
                r.X *= sq;
                r.Y *= sq;
                r.Z *= sq;
                r.W *= sq;
            }

            x = 2 * (r.X * r.Y - r.Z * r.W);
            y = -r.X * r.X + r.Y * r.Y - r.Z * r.Z + r.W * r.W;
            z = 2 * (r.X * r.W + r.Y * r.Z);
            return new Vector3(x, y, z);
        }

        public Vector3 llRot2Up(Quaternion r)
        {
            double x, y, z, sq;

            sq = r.LengthSquared;
            if (Math.Abs(1.0 - sq) > 0.000001)
            {
                sq = 1.0 / Math.Sqrt(sq);
                r.X *= sq;
                r.Y *= sq;
                r.Z *= sq;
                r.W *= sq;
            }

            x = 2 * (r.X * r.Z + r.Y * r.W);
            y = 2 * (-r.X * r.W + r.Y * r.Z);
            z = -r.X * r.X - r.Y * r.Y + r.Z * r.Z + r.W * r.W;
            return new Vector3(x, y, z);
        }

        public Quaternion llRotBetween(Vector3 a, Vector3 b)
        {
            return Quaternion.RotBetween(a, b);
        }

        public int llFloor(double f)
        {
            return (int)Math.Floor(f);
        }

        public int llCeil(double f)
        {
            return (int)Math.Ceiling(f);
        }

        public int llRound(double f)
        {
            return (int)Math.Round(f, MidpointRounding.AwayFromZero);
        }

        private static Random random = new Random();
        public double llFrand(double mag)
        {
            return random.NextDouble() * mag;
        }
    }
}

﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;
namespace SilverSim.Types
{
    public class Color
    {
        public double R;
        public double G;
        public double B;

        #region Constructors
        public Color()
        {

        }

        public Color(double r, double g, double b)
        {
            if (r < 0f) R = 0f;
            else if (r > 1f) R = 1f;
            else R = r;

            if (g < 0f) G = 0f;
            else if (g > 1f) G = 1f;
            else G = g;

            if (b < 0f) B = 0f;
            else if (b > 1f) B = 1f;
            else B = b;
        }

        public Color(Vector3 v)
        {
            if (v.X < 0f) R = 0f;
            else if (v.X > 1f) R = 1f;
            else R = v.X;

            if (v.Y < 0f) G = 0f;
            else if (v.Y > 1f) G = 1f;
            else G = v.Y;

            if (v.Z < 0f) B = 0f;
            else if (v.Z > 1f) B = 1f;
            else B = v.Z;
        }

        public Color(Color v)
        {
            R = v.R;
            G = v.G;
            B = v.B;
        }
        #endregion

        #region Operators
        public static implicit operator Vector3(Color v)
        {
            return new Vector3(v.R, v.G, v.B);
        }
        #endregion

        #region Properties
        public double GetR
        {
            get
            {
                return R;
            }
        }
        public double GetG
        {
            get
            {
                return G;
            }
        }
        public double GetB
        {
            get
            {
                return B;
            }
        }
        public Vector3 AsVector3
        {
            get
            {
                return new Vector3(R, G, B);
            }
        }

        public byte R_AsByte
        {
            get
            {
                if(R > 1)
                {
                    return 255;
                }
                else if(R < 0)
                {
                    return 0;
                }
                else
                {
                    return (byte)(R * 255);
                }
            }
            set
            {
                R = value / 255f;
            }
        }

        public byte G_AsByte
        {
            get
            {
                if (G > 1)
                {
                    return 255;
                }
                else if (G < 0)
                {
                    return 0;
                }
                else
                {
                    return (byte)(G * 255);
                }
            }
            set
            {
                G = value / 255f;
            }
        }

        public byte B_AsByte
        {
            get
            {
                if (B > 1)
                {
                    return 255;
                }
                else if (B < 0)
                {
                    return 0;
                }
                else
                {
                    return (byte)(B * 255);
                }
            }
            set
            {
                B = value / 255f;
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
        public virtual byte[] AsByte
        {
            get
            {
                return new byte[] { R_AsByte, G_AsByte, B_AsByte };
            }
        }
        #endregion
    }

    public class ColorAlpha : Color
    {
        public double A = 1f;

        #region Constructors
        public ColorAlpha()
        {
            
        }

        public ColorAlpha(double r, double g, double b, double alpha)
            : base(r, g, b)
        {
            if (alpha < 0f) A = 0f;
            else if (alpha > 1f) A = 1f;
            else A = alpha;
        }

        public ColorAlpha(Vector3 v, double alpha)
            : base(v)
        {
            if (alpha < 0f) A = 0f;
            else if (alpha > 1f) A = 1f;
            else A = alpha;
        }

        public ColorAlpha(Color v, double alpha)
            : base(v)
        {
            if (alpha < 0f) A = 0f;
            else if (alpha > 1f) A = 1f;
            else A = alpha;
        }

        public ColorAlpha(ColorAlpha v)
        {
            R = v.R;
            G = v.G;
            B = v.B;
            A = v.A;
        }

        public ColorAlpha(byte[] b)
        {
            R = b[0] / 255f;
            G = b[1] / 255f;
            B = b[2] / 255f;
            A = b[3] / 255f;
        }

        public ColorAlpha(byte[] b, int pos)
        {
            R = b[pos + 0] / 255f;
            G = b[pos + 1] / 255f;
            B = b[pos + 2] / 255f;
            A = b[pos + 3] / 255f;
        }
        #endregion

        #region Properties

        public byte A_AsByte
        {
            get
            {
                if (A > 1)
                {
                    return 255;
                }
                else if (A < 0)
                {
                    return 0;
                }
                else
                {
                    return (byte)(A * 255);
                }
            }
            set
            {
                A = value / 255f;
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
        public override byte[] AsByte
        {
            get
            {
                return new byte[] { R_AsByte, G_AsByte, B_AsByte, A_AsByte };
            }
        }
        #endregion

        /// <summary>A Color4 with zero RGB values and fully opaque (alpha 1.0)</summary>
        public readonly static ColorAlpha Black = new ColorAlpha(0f, 0f, 0f, 1f);

        /// <summary>A Color4 with full RGB values (1.0) and fully opaque (alpha 1.0)</summary>
        public readonly static ColorAlpha White = new ColorAlpha(1f, 1f, 1f, 1f);

    }
}

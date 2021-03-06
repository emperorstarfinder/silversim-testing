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
using SilverSim.Types.Primitive;
using System;
using System.Threading;

namespace SilverSim.Scene.Types.Object.Localization
{
    public sealed partial class ObjectPartLocalizedInfo
    {
        private TextureEntry m_TextureEntry;
        private byte[] m_TextureEntryBytes;
        private byte[] m_TextureEntryBytes_LimitsEnabled;
        private readonly ReaderWriterLock m_TextureEntryLock = new ReaderWriterLock();

        private byte[] m_TextureAnimationBytes;

        private string m_MediaURL;

        public string MediaURL
        {
            get
            {
                return m_MediaURL ?? m_ParentInfo.MediaURL;
            }
            set
            {
                if (value != null)
                {
                    m_MediaURL = value;
                }
                else
                {
                    if(m_ParentInfo == null)
                    {
                        throw new InvalidOperationException();
                    }
                    m_MediaURL = value;
                }
                UpdateData(UpdateDataFlags.AllObjectUpdate);
                if (m_ParentInfo == null)
                {
                    foreach (ObjectPartLocalizedInfo localization in m_Part.NamedLocalizations)
                    {
                        if (!localization.HasMediaURL)
                        {
                            localization.UpdateData(UpdateDataFlags.AllObjectUpdate);
                        }
                    }
                }
                m_Part.TriggerOnUpdate(0);
            }
        }

        public bool HasMediaURL => m_MediaURL != null;

        private UpdateChangedFlags ChangedTexParams(TextureEntryFace oldTexFace, TextureEntryFace newTexFace)
        {
            UpdateChangedFlags flags = 0;
            if (oldTexFace.Glow != newTexFace.Glow ||
                oldTexFace.Bump != newTexFace.Bump ||
                oldTexFace.FullBright != newTexFace.FullBright ||
                oldTexFace.MaterialID != newTexFace.MaterialID ||
                oldTexFace.OffsetU != newTexFace.OffsetU ||
                oldTexFace.OffsetV != newTexFace.OffsetV ||
                oldTexFace.RepeatU != newTexFace.RepeatU ||
                oldTexFace.RepeatV != newTexFace.RepeatV ||
                oldTexFace.Rotation != newTexFace.Rotation ||
                oldTexFace.Shiny != newTexFace.Shiny ||
                oldTexFace.TexMapType != newTexFace.TexMapType ||
                oldTexFace.TextureID != newTexFace.TextureID)
            {
                flags |= UpdateChangedFlags.Texture;
            }

            if (oldTexFace.TextureColor.R != newTexFace.TextureColor.R ||
                oldTexFace.TextureColor.G != newTexFace.TextureColor.G ||
                oldTexFace.TextureColor.B != newTexFace.TextureColor.B ||
                oldTexFace.TextureColor.A != newTexFace.TextureColor.A)
            {
                flags |= UpdateChangedFlags.Color;
            }

            if (oldTexFace.MediaFlags != newTexFace.MediaFlags)
            {
                flags |= UpdateChangedFlags.Media;
            }
            return flags;
        }

        private UpdateChangedFlags ChangedTexParams(TextureEntry oldTex, TextureEntry newTex)
        {
            UpdateChangedFlags flags = ChangedTexParams(oldTex.DefaultTexture, newTex.DefaultTexture);
            uint index;
            for (index = 0; index < 32; ++index)
            {
                flags |= ChangedTexParams(oldTex[index], newTex[index]);
            }
            return flags;
        }

        public TextureEntry TextureEntry
        {
            get
            {
                TextureEntry t = m_TextureEntryLock.AcquireReaderLock(() => m_TextureEntry != null ? new TextureEntry(m_TextureEntry) : null);
                if(t == null && m_ParentInfo != null)
                {
                    t = m_ParentInfo.TextureEntry;
                }
                return t;
            }
            set
            {
                UpdateChangedFlags flags = 0;
                var copy = new TextureEntry(value);
                m_TextureEntryLock.AcquireWriterLock(() =>
                {
                    flags = ChangedTexParams(m_TextureEntry, copy);
                    m_TextureEntry = copy;
                    m_TextureEntryBytes = value.GetBytes();
                    ObjectPart part = m_Part;
                    if (part != null)
                    {
                        m_TextureEntryBytes_LimitsEnabled = value.GetBytes(part.IsFullbrightDisabled, (float)part.GlowLimitIntensity);
                    }
                    else
                    {
                        m_TextureEntryBytes_LimitsEnabled = m_TextureEntryBytes;
                    }
                });
                UpdateData(UpdateDataFlags.AllObjectUpdate);
                if (m_ParentInfo == null)
                {
                    foreach (ObjectPartLocalizedInfo localization in m_Part.NamedLocalizations)
                    {
                        if (!localization.HasTextureEntry)
                        {
                            localization.UpdateData(UpdateDataFlags.AllObjectUpdate);
                        }
                    }
                }
                m_Part.TriggerOnUpdate(flags);
            }
        }

        public bool HasTextureEntry => m_TextureEntry != null;

        public byte[] TextureEntryBytesLimitedLight
        {
            get
            {
                byte[] t = m_TextureEntryBytes_LimitsEnabled;
                if(t == null && m_ParentInfo != null)
                {
                    t = m_ParentInfo.m_TextureEntryBytes_LimitsEnabled;
                }
                return t;
            }
        }

        public byte[] TextureEntryBytes
        {
            get
            {
                byte[] t = m_TextureEntryLock.AcquireReaderLock(() =>
                {
                    if (m_TextureEntryBytes != null)
                    {
                        byte[] b = new byte[m_TextureEntryBytes.Length];
                        Buffer.BlockCopy(m_TextureEntryBytes, 0, b, 0, m_TextureEntryBytes.Length);
                        return b;
                    }
                    else
                    {
                        return null;
                    }
                });

                if(t == null && m_ParentInfo != null)
                {
                    t = m_ParentInfo.TextureEntryBytes;
                }
                return t;
            }
            set
            {
                if (value == null)
                {
                    if(m_ParentInfo == null)
                    {
                        throw new InvalidOperationException();
                    }
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        m_TextureEntryBytes = null;
                        m_TextureEntryBytes_LimitsEnabled = null;
                        m_TextureEntry = null;
                    });
                    UpdateData(UpdateDataFlags.AllObjectUpdate);
                    if (m_ParentInfo == null)
                    {
                        foreach (ObjectPartLocalizedInfo localization in m_Part.NamedLocalizations)
                        {
                            if (!localization.HasTextureEntry)
                            {
                                localization.UpdateData(UpdateDataFlags.AllObjectUpdate);
                            }
                        }
                    }
                    m_Part.TriggerOnUpdate(UpdateChangedFlags.Texture | UpdateChangedFlags.Color);
                }
                else
                {
                    UpdateChangedFlags flags = m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        TextureEntry newTex;
                        m_TextureEntryBytes = value;
                        newTex = new TextureEntry(value);
                        UpdateChangedFlags flag = ChangedTexParams(m_TextureEntry, newTex);
                        m_TextureEntry = newTex;
                        ObjectPart part = m_Part;
                        if (part != null)
                        {
                            m_TextureEntryBytes_LimitsEnabled = newTex.GetBytes(part.IsFullbrightDisabled, (float)part.GlowLimitIntensity);
                        }
                        else
                        {
                            m_TextureEntryBytes_LimitsEnabled = newTex.GetBytes();
                        }
                        return flag;
                    });
                    UpdateData(UpdateDataFlags.AllObjectUpdate);
                    if (m_ParentInfo == null)
                    {
                        foreach (ObjectPartLocalizedInfo localization in m_Part.NamedLocalizations)
                        {
                            if (!localization.HasTextureEntry)
                            {
                                localization.UpdateData(UpdateDataFlags.AllObjectUpdate);
                            }
                        }
                    }
                    m_Part.TriggerOnUpdate(flags);
                }
            }
        }

        public TextureAnimationEntry TextureAnimation
        {
            get
            {
                byte[] tab = m_TextureAnimationBytes;
                return tab == null ? m_ParentInfo.TextureAnimation : new TextureAnimationEntry(tab, 0);
            }
            set
            {
                if(value == null && m_ParentInfo != null)
                {
                    m_TextureAnimationBytes = null;
                }
                else if (value == null || (value.Flags & TextureAnimationEntry.TextureAnimMode.ANIM_ON) == 0)
                {
                    m_TextureAnimationBytes = new byte[0];
                }
                else
                {
                    m_TextureAnimationBytes = value.GetBytes();
                }
                UpdateData(UpdateDataFlags.AllObjectUpdate);
                if (m_ParentInfo == null)
                {
                    foreach (ObjectPartLocalizedInfo localization in m_Part.NamedLocalizations)
                    {
                        if (!localization.HasTextureAnimation)
                        {
                            localization.UpdateData(UpdateDataFlags.AllObjectUpdate);
                        }
                    }
                }
                m_Part.TriggerOnUpdate(0);
            }
        }

        public bool HasTextureAnimation => m_TextureAnimationBytes != null;

        public byte[] TextureAnimationBytes
        {
            get
            {
                byte[] b = m_TextureAnimationBytes;
                if(b == null)
                {
                    return m_ParentInfo.TextureAnimationBytes;
                }
                else
                {
                    var res = new byte[b.Length];
                    Buffer.BlockCopy(b, 0, res, 0, b.Length);
                    return res;
                }
            }
            set
            {
                if(value == null)
                {
                    m_TextureAnimationBytes = m_ParentInfo != null ? null : new byte[0];
                }
                else
                {
                    m_TextureAnimationBytes = value;
                }
                UpdateData(UpdateDataFlags.AllObjectUpdate);
                if (m_ParentInfo == null)
                {
                    foreach (ObjectPartLocalizedInfo localization in m_Part.NamedLocalizations)
                    {
                        if (!localization.HasTextureAnimation)
                        {
                            localization.UpdateData(UpdateDataFlags.AllObjectUpdate);
                        }
                    }
                }
                m_Part.TriggerOnUpdate(0);
            }
        }
    }
}

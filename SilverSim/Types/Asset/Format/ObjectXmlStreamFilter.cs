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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SilverSim.Types.Asset.Format
{
    public class ObjectXmlStreamFilter : Stream
    {
        byte[] m_Buffer = new byte[10240];
        int m_BufFill = 0;
        int m_BufUsed = 0;
        Stream m_BufInput;

        public ObjectXmlStreamFilter(Stream input)
        {
            m_BufInput = input;
        }

        public override bool CanRead
        {
            get 
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get 
            {
                return false;

            }
        }

        public override bool CanWrite
        {
            get 
            {
                return false;
            }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get 
            { 
                throw new NotImplementedException(); 
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int rescount = 0;
            while(count > 0)
            {
                if(m_BufFill == m_BufUsed)
                {
                    m_BufUsed = 0;
                    m_BufFill = m_BufInput.Read(m_Buffer, 0, m_Buffer.Length);
                    if(m_BufFill < 0)
                    {
                        return -1;
                    }
                    if(0 == m_BufFill)
                    {
                        return rescount;
                    }
                }

                if(m_Buffer[m_BufUsed] == (byte)'<')
                {
                    if (m_BufUsed != 0)
                    {
                        Buffer.BlockCopy(m_Buffer, m_BufUsed, m_Buffer, 0, m_BufFill - m_BufUsed);
                    }
                    m_BufFill -= m_BufUsed;
                    m_BufUsed = 0;
                    int addbytes = m_BufInput.Read(m_Buffer, m_BufFill, m_Buffer.Length - m_BufFill);
                    if(addbytes < 0)
                    {
                        return -1;
                    }
                    m_BufFill += addbytes;

                    int tagend;
                    for (tagend = 0; tagend < m_BufFill; ++tagend)
                    {
                        if(m_Buffer[tagend] == '>')
                        {
                            break;
                        }
                    }
                    string test = UTF8NoBOM.GetString(m_Buffer, 0, tagend + 1);
                    if(test.StartsWith("<SceneObjectPart"))
                    {
                        if(test.Contains("xmlns:xmlns"))
                        {
                            test = test.Replace("xmlns:xmlns:", "xmlns:");
                            byte[] newbuf = new byte[m_BufFill - tagend - 1];
                            Buffer.BlockCopy(m_Buffer, tagend + 1, newbuf, 0, m_BufFill - tagend - 1);
                            byte[] newstr = UTF8NoBOM.GetBytes(test);
                            Buffer.BlockCopy(newstr, 0, m_Buffer, 0, newstr.Length);
                            Buffer.BlockCopy(newbuf, 0, m_Buffer, newstr.Length, newbuf.Length);
                            m_BufFill = newstr.Length + newbuf.Length;
                        }
                    }
                    else if(test.StartsWith("<?xml"))
                    {
                        /* OpenSim guys messed up xml declarations, so we have to ignore it */
                        /* filter every other tag opensim does not use anything else than UTF-8
                         * but falsely declared some as UTF-16 
                         */
                        test = "<?xml version=\"1.0\"?>";
                        byte[] newbuf = new byte[m_BufFill - tagend - 1];
                        Buffer.BlockCopy(m_Buffer, tagend + 1, newbuf, 0, m_BufFill - tagend - 1);
                        byte[] newstr = UTF8NoBOM.GetBytes(test);
                        Buffer.BlockCopy(newstr, 0, m_Buffer, 0, newstr.Length);
                        Buffer.BlockCopy(newbuf, 0, m_Buffer, newstr.Length, newbuf.Length);
                        m_BufFill = newstr.Length + newbuf.Length;
                    }
                }

                buffer[offset++] = m_Buffer[m_BufUsed++];
                --count;
                ++rescount;
            }
            return rescount;
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public new void Dispose()
        {
            m_BufInput.Dispose();
        }
    }
}

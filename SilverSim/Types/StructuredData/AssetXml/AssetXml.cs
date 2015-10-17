﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SilverSim.StructuredData.AssetXml
{
    public static class AssetXml
    {

        #region Asset Deserialization
        [Serializable]
        public class InvalidAssetSerializationException : Exception
        {
            public InvalidAssetSerializationException()
            {
            }
        }

        private static void parseAssetFullID(XmlTextReader reader)
        {
            while (true)
            {
                if (!reader.Read())
                {
                    throw new InvalidAssetSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {

                            case "Guid":
                                reader.ReadElementValueAsString();
                                break;

                            default:
                                throw new InvalidAssetSerializationException();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "FullID")
                        {
                            throw new InvalidAssetSerializationException();
                        }

                        return;
                }
            }
        }

        private static AssetData parseAssetDataInternal(XmlTextReader reader)
        {
            AssetData asset = new AssetData();
            while (true)
            {
                if (!reader.Read())
                {
                    throw new InvalidAssetSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "Data":
                                if(reader.IsEmptyElement)
                                {
                                    asset.Data = new byte[0];
                                }
                                else
                                {
                                    asset.Data = reader.ReadContentAsBase64();
                                }
                                break;

                            case "FullID":
                                parseAssetFullID(reader);
                                break;

                            case "ID":
                                asset.ID = reader.ReadElementValueAsString();
                                break;

                            case "Name":
                                if (reader.IsEmptyElement)
                                {
                                    asset.Name = "";
                                }
                                else
                                {
                                    asset.Name = reader.ReadElementValueAsString();
                                }
                                break;

                            case "Description":
                                reader.ReadToEndElement();
                                break;

                            case "Type":
                                asset.Type = (AssetType)reader.ReadElementValueAsInt();
                                break;

                            case "Local":
                                if (!reader.IsEmptyElement)
                                {
                                    asset.Local = reader.ReadElementValueAsBoolean();
                                }
                                break;

                            case "Temporary":
                                if (!reader.IsEmptyElement)
                                {
                                    asset.Temporary = reader.ReadElementValueAsBoolean();
                                }
                                break;

                            case "CreatorID":
                                if (reader.IsEmptyElement)
                                {
                                    asset.Creator = UUI.Unknown;
                                }
                                else
                                {
                                    string creatorID = reader.ReadElementValueAsString();
                                    try
                                    {
                                        asset.Creator = new UUI(creatorID);
                                    }
                                    catch
                                    {
                                        asset.Creator = UUI.Unknown;
                                    }
                                }
                                break;

                            case "Flags":
                                asset.Flags = AssetFlags.Normal;
                                string flags = reader.ReadElementValueAsString();
                                if (flags != "Normal")
                                {
                                    string[] flaglist = flags.Split(',');
                                    foreach (string flag in flaglist)
                                    {
                                        if (flag == "Maptile")
                                        {
                                            asset.Flags |= AssetFlags.Maptile;
                                        }
                                        if (flag == "Rewritable")
                                        {
                                            asset.Flags |= AssetFlags.Rewritable;
                                        }
                                        if (flag == "Collectable")
                                        {
                                            asset.Flags |= AssetFlags.Collectable;
                                        }
                                    }
                                }
                                break;

                            default:
                                throw new InvalidAssetSerializationException();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "AssetBase")
                        {
                            throw new InvalidAssetSerializationException();
                        }

                        return asset;
                }
            }
        }

        public static AssetData parseAssetData(Stream input)
        {
            using (XmlTextReader reader = new XmlTextReader(input))
            {
                while (true)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidAssetSerializationException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name != "AssetBase")
                            {
                                throw new InvalidAssetSerializationException();
                            }

                            return parseAssetDataInternal(reader);

                        case XmlNodeType.EndElement:
                            throw new InvalidAssetSerializationException();
                    }
                }
            }
        }
        #endregion

        #region Asset Metadata Deserialization
        [Serializable]
        public class InvalidAssetMetadataSerializationException : Exception
        {
            public InvalidAssetMetadataSerializationException()
            {
            }
        }

        private static AssetMetadata parseAssetMetadataInternal(XmlTextReader reader)
        {
            AssetMetadata asset = new AssetMetadata();
            while (true)
            {
                if (!reader.Read())
                {
                    throw new InvalidAssetMetadataSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "FullID":
                                parseAssetFullID(reader);
                                break;

                            case "ID":
                                asset.ID = reader.ReadElementValueAsString();
                                break;

                            case "Name":
                                asset.Name = reader.ReadElementValueAsString();
                                break;

                            case "Description":
                                reader.ReadElementValueAsString();
                                break;

                            case "Type":
                                asset.Type = (AssetType)reader.ReadElementValueAsInt();
                                break;

                            case "Local":
                                asset.Local = reader.ReadElementValueAsBoolean();
                                break;

                            case "Temporary":
                                asset.Temporary = reader.ReadElementValueAsBoolean();
                                break;

                            case "CreatorID":
                                asset.Creator = new UUI(reader.ReadElementValueAsString());
                                break;

                            case "Flags":
                                asset.Flags = AssetFlags.Normal;
                                string flags = reader.ReadElementValueAsString();
                                if (flags != "Normal")
                                {
                                    string[] flaglist = flags.Split(',');
                                    foreach (string flag in flaglist)
                                    {
                                        if (flag == "Maptile")
                                        {
                                            asset.Flags |= AssetFlags.Maptile;
                                        }
                                        if (flag == "Rewritable")
                                        {
                                            asset.Flags |= AssetFlags.Rewritable;
                                        }
                                        if (flag == "Collectable")
                                        {
                                            asset.Flags |= AssetFlags.Collectable;
                                        }
                                    }
                                }
                                break;

                            default:
                                throw new InvalidAssetMetadataSerializationException();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "AssetMetadata")
                        {
                            throw new InvalidAssetMetadataSerializationException();
                        }

                        return asset;
                }
            }
        }

        public static AssetMetadata parseAssetMetadata(Stream input)
        {
            using (XmlTextReader reader = new XmlTextReader(input))
            {
                while (true)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidAssetMetadataSerializationException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name != "AssetMetadata")
                            {
                                throw new InvalidAssetMetadataSerializationException();
                            }

                            return parseAssetMetadataInternal(reader);

                        case XmlNodeType.EndElement:
                            throw new InvalidAssetMetadataSerializationException();
                    }
                }
            }
        }

        #endregion

    }
}

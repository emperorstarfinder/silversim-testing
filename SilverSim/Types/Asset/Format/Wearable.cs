﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SilverSim.Types.Asset.Format
{
    public enum WearableType : byte
    {
        Shape = 0,
        Skin = 1,
        Hair = 2,
        Eyes = 3,
        Shirt = 4,
        Pants = 5,
        Shoes = 6,
        Socks = 7,
        Jacket = 8,
        Gloves = 9,
        Undershirt = 10,
        Underpants = 11,
        Skirt = 12,
        Alpha = 13,
        Tattoo = 14,
        Physics = 15,
        NumWearables = 16,
        Invalid = 255
    }

    public class Wearable : IReferencesAccessor
    {
        public string Name = string.Empty;
        public string Description = string.Empty;
        public WearableType Type = WearableType.Invalid;
        public Dictionary<uint, double> Params = new Dictionary<uint,double>();
        public Dictionary<uint, UUID> Textures = new Dictionary<uint, UUID>();
        public UUI Creator = new UUI();
        public UUI LastOwner = new UUI();
        public UUI Owner = new UUI();
        public UGI Group = new UGI();
        InventoryPermissionsData Permissions;
        InventoryItem.SaleInfoData SaleInfo;

        #region Constructors
        public Wearable()
        {
            SaleInfo.PermMask = (InventoryPermissionsMask)0x7FFFFFFF;
        }

        public Wearable(AssetData asset)
        {
            string[] lines = Encoding.UTF8.GetString(asset.Data).Split('\n');

            string[] versioninfo = lines[0].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if(versioninfo[0] != "LLWearable")
            {
                throw new NotAWearableFormatException();
            }

            SaleInfo.PermMask = (InventoryPermissionsMask)0x7FFFFFFF;

            Name = lines[1].Trim();
            Description = lines[2].Trim();

            for(int idx = 3; idx < lines.Length; ++idx)
            {
                string line = lines[idx].Trim();
                string[] para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                if(para.Length == 2 && para[0] == "type")
                {
                    Type = (WearableType)int.Parse(para[1]);
                }
                else if(para.Length == 2 && para[0] == "parameters")
                {
                    /* we got a parameter block */
                    uint parametercount = uint.Parse(para[1]);
                    for(uint paranum = 0; paranum < parametercount; ++paranum)
                    {
                        line = lines[++idx].Trim();
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length == 2)
                        {
                            Params[uint.Parse(para[0])] = double.Parse(para[1]);
                        }
                    }
                }
                else if(para.Length == 2 && para[0] == "textures")
                {
                    /* we got a textures block */
                    uint texturecount = uint.Parse(para[1]);
                    for(uint paranum = 0; paranum < texturecount; ++paranum)
                    {
                        line = lines[++idx].Trim();
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length == 2)
                        {
                            Textures[uint.Parse(para[0])] = para[1];
                        }
                    }
                }
                else if(para.Length == 2 && para[0] == "permissions")
                {
                    if(lines[++idx].Trim() != "{")
                    {
                        throw new NotAWearableFormatException();
                    }
                    if (idx == lines.Length)
                    {
                        throw new NotAWearableFormatException();
                    }
                    while ((line = lines[idx].Trim()) != "}")
                    {
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length < 2)
                        {
                        }
                        else if(para[0] == "base_mask")
                        {
                            Permissions.Base = (InventoryPermissionsMask)uint.Parse(para[1], NumberStyles.HexNumber);
                        }
                        else if(para[0] == "owner_mask")
                        {
                            Permissions.Current = (InventoryPermissionsMask)uint.Parse(para[1], NumberStyles.HexNumber);
                        }
                        else if(para[0] == "group_mask")
                        {
                            Permissions.Group = (InventoryPermissionsMask)uint.Parse(para[1], NumberStyles.HexNumber);
                        }
                        else if(para[0] == "everyone_mask")
                        {
                            Permissions.EveryOne = (InventoryPermissionsMask)uint.Parse(para[1], NumberStyles.HexNumber);
                        }
                        else if(para[0] == "next_owner_mask")
                        {
                            Permissions.NextOwner = (InventoryPermissionsMask)uint.Parse(para[1], NumberStyles.HexNumber);
                        }
                        else if(para[0] == "creator_id")
                        {
                            Creator.ID = para[1];
                        }
                        else if(para[0] == "owner_id")
                        {
                            Owner.ID = para[1];
                        }
                        else if(para[0] == "last_owner_id")
                        {
                            LastOwner.ID = para[1];
                        }
                        else if(para[0] == "group_id")
                        {
                            Group.ID = para[1];
                        }

                        if(++idx == lines.Length)
                        {
                            throw new NotAWearableFormatException();
                        }
                    }
                }
                else if (para.Length == 2 && para[0] == "sale_info")
                {
                    if (lines[++idx].Trim() != "{")
                    {
                        throw new NotAWearableFormatException();
                    }
                    if (idx == lines.Length)
                    {
                        throw new NotAWearableFormatException();
                    }
                    while ((line = lines[idx].Trim()) != "}")
                    {
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length < 2)
                        {
                        }
                        else if(para[0] == "sale_type")
                        {
                            SaleInfo.TypeName = para[1];
                        }
                        else if(para[0] == "sale_price")
                        {
                            SaleInfo.Price = int.Parse(para[1]);
                        }
                        else if (para[0] == "perm_mask")
                        {
                            SaleInfo.PermMask = (InventoryPermissionsMask)uint.Parse(para[1], NumberStyles.HexNumber);
                        }

                        if (++idx == lines.Length)
                        {
                            throw new NotAWearableFormatException();
                        }
                    }
                }
            }
        }
        #endregion

        #region References
        public List<UUID> References
        {
            get
            {
                List<UUID> refs = new List<UUID>();
                foreach(UUID tex in Textures.Values)
                {
                    if(!refs.Contains(tex))
                    {
                        refs.Add(tex);
                    }
                }
                return refs;
            }
        }
        #endregion

        #region Operators
        private static readonly string WearableFormat =
                "permissions 0\n" +
                "{{\n" +
                "\tbase_mask\t{0:x8}\n" +
                "\towner_mask\t{1:x8}\n" +
                "\tgroup_mask\t{2:x8}\n" +
                "\teveryone_mask\t{3:x8}\n" +
                "\tnext_owner_mask\t{4:x8}\n" +
                "\tcreator_id\t{5}\n" +
                "\towner_id\t{6}\n" +
                "\tlast_owner_id\t{7}\n" +
                "\tgroup_id\t{8}\n" +
                "}}\n" +
                "sale_info 0\n"+
                "{{\n" +
                "\tsale_type\t{9}\n" +
                "\tsale_price\t{10}\n" +
                "}}\n" +
                "type\t{11}\n";

        public byte[] WearableData
        {
            get
            {
                string fmt = "LLWearable version 22\n";
                fmt += Name + "\n";
                fmt += Description + "\n";
                fmt += String.Format(WearableFormat,
                    (uint)Permissions.Base,
                    (uint)Permissions.Current,
                    (uint)Permissions.Group,
                    (uint)Permissions.EveryOne,
                    (uint)Permissions.NextOwner,
                    Creator.ID,
                    Owner.ID,
                    LastOwner.ID,
                    Group.ID,
                    SaleInfo.TypeName,
                    SaleInfo.Price,
                    (uint)Type);
                fmt += String.Format("parameters {0}\n", Params.Count);
                foreach(KeyValuePair<uint, double> kvp in Params)
                {
                    fmt += String.Format("{0}\t{1}\n", kvp.Key, kvp.Value);
                }
                fmt += String.Format("textures {0}\n", Textures.Count);
                foreach(KeyValuePair<uint, UUID> kvp in Textures)
                {
                    fmt += String.Format("{0}\t{1}\n", kvp.Key, kvp.Value);
                }

                return Encoding.UTF8.GetBytes(fmt);
            }
        }

        public AssetData Asset()
        {
            return (AssetData)this;
        }

        public static implicit operator AssetData(Wearable v)
        {
            AssetData asset = new AssetData();

            switch(v.Type)
            {
                case WearableType.Hair:
                case WearableType.Skin:
                case WearableType.Shape:
                case WearableType.Eyes:
                    asset.Type = AssetType.Bodypart;
                    break;

                default:
                    asset.Type = AssetType.Clothing;
                    break;
            }

            asset.Name = "Wearable";
            asset.Data = v.WearableData;

            return asset;
        }
        #endregion
    }
}

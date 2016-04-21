﻿using log4net;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private static readonly ILog m_BakeLog = LogManager.GetLogger("AVATAR BAKING");

        UUID m_CurrentOutfitFolder = UUID.Zero;

        enum BakeType
        {
            Head,
            UpperBody,
            LowerBody,
            Eyes,
            Skirt,
            Hair
        }

        class OutfitItem
        {
            public InventoryItem ActualItem;
            public Wearable WearableData;

            public OutfitItem(InventoryItem linkItem)
            {
                ActualItem = linkItem;
            }
        }

        class BakeStatus : IDisposable
        {
            public readonly Dictionary<UUID, OutfitItem> OutfitItems = new Dictionary<UUID, OutfitItem>();
            public readonly Dictionary<UUID, Image> Textures = new Dictionary<UUID, Image>();
            public readonly Dictionary<UUID, Image> TexturesResized128 = new Dictionary<UUID, Image>();
            public readonly Dictionary<UUID, Image> TexturesResized512 = new Dictionary<UUID, Image>();
            public UUID Layer0TextureID = UUID.Zero;

            public BakeStatus()
            {

            }

            public bool TryGetTexture(BakeType bakeType, UUID textureID, out Image img)
            {
                int targetDimension;
                Dictionary<UUID, Image> resizeCache;
                if(bakeType == BakeType.Eyes)
                {
                    resizeCache = TexturesResized128;
                    targetDimension = 128;
                }
                else
                {
                    resizeCache = TexturesResized512;
                    targetDimension = 512;
                }

                /* do not redo the hard work of rescaling images unnecessarily */
                if (resizeCache.TryGetValue(textureID, out img))
                {
                    return true;
                }

                if (Textures.TryGetValue(textureID, out img))
                {
                    if(img.Width != targetDimension || img.Height != targetDimension)
                    {
                        img = new Bitmap(img, targetDimension, targetDimension);
                        resizeCache.Add(textureID, img);
                    }
                    return true;
                }

                img = null;
                return false;
            }

            public void Dispose()
            {
                foreach(Image img in Textures.Values)
                {
                    img.Dispose();
                }
                foreach (Image img in TexturesResized128.Values)
                {
                    img.Dispose();
                }
                foreach (Image img in TexturesResized512.Values)
                {
                    img.Dispose();
                }
            }
        }

        public class BakingErrorException : Exception
        {
            public BakingErrorException()
            {

            }

            public BakingErrorException(string message)
            : base(message)
            {

            }

            protected BakingErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
            {
            }

            public BakingErrorException(string message, Exception innerException)
            : base(message, innerException)
            {

            }
        }

        #region Get Current Outfit
        public void BakeAppearanceFromCurrentOutfit(bool rebake = false)
        {
            if (m_CurrentOutfitFolder == UUID.Zero)
            {
                InventoryFolder currentOutfitFolder = InventoryService.Folder[Owner.ID, AssetType.CurrentOutfitFolder];
                m_CurrentOutfitFolder = currentOutfitFolder.ID;
            }

            InventoryFolderContent currentOutfit = InventoryService.Folder.Content[Owner.ID, m_CurrentOutfitFolder];
            if (currentOutfit.Version == Appearance.Serial || rebake)
            {
                return;
            }

#if PREMATURE
            foreach (InventoryItem item in currentOutfit.Items)
            {
                if (item.AssetType == AssetType.Link)
                {
                    bakeStatus.OutfitItems.Add(item.AssetID, new OutfitItem(item));
                }
            }
#endif
        }
        #endregion

        #region Actual Baking Code
        const int MAX_WEARABLES_PER_TYPE = 5;

        public void BakeAppearanceFromWearablesInfo()
        {
            Dictionary<WearableType, List<AgentWearables.WearableInfo>> wearables = Wearables.All;
            using (BakeStatus bakeStatus = new BakeStatus())
            {
                List<UUID> wearablesItemIds = new List<UUID>();
                for (int wearableIndex = 0; wearableIndex < MAX_WEARABLES_PER_TYPE; ++wearableIndex)
                {
                    for(int wearableType = 0; wearableType < (int)WearableType.NumWearables; ++wearableType)
                    {
                        List<AgentWearables.WearableInfo> wearablesList;
                        if(wearables.TryGetValue((WearableType)wearableType, out wearablesList))
                        {
                            if (wearablesList.Count > wearableIndex)
                            {
                                wearablesItemIds.Add(wearablesList[wearableIndex].ItemID);
                            }
                        }
                    }
                }

                List<InventoryItem> actualItems = InventoryService.Item[Owner.ID, new List<UUID>(wearablesItemIds)];
                Dictionary<UUID, InventoryItem> actualItemsInDict = new Dictionary<UUID, InventoryItem>();
                foreach(InventoryItem item in actualItems)
                {
                    actualItemsInDict.Add(item.ID, item);
                }

                foreach(UUID itemId in wearablesItemIds)
                {
                    OutfitItem outfitItem;
                    AssetData outfitData;
                    InventoryItem inventoryItem;
                    if(actualItemsInDict.TryGetValue(itemId, out inventoryItem))
                    {
                        outfitItem = new OutfitItem(inventoryItem);
                        outfitItem.ActualItem = inventoryItem;
                        switch (inventoryItem.AssetType)
                        {
                            case AssetType.Bodypart:
                            case AssetType.Clothing:
                                if (AssetService.TryGetValue(inventoryItem.AssetID, out outfitData))
                                {
                                    try
                                    {
                                        outfitItem.WearableData = new Wearable(outfitData);
                                    }
                                    catch (Exception e)
                                    {
                                        string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as wearable", inventoryItem.AssetID, Owner.FullName, Owner.ID);
                                        m_BakeLog.ErrorFormat(info, e);
                                        throw new BakingErrorException(info, e);
                                    }

                                    /* load textures beforehand and do not load unnecessarily */
                                    foreach (UUID textureID in outfitItem.WearableData.Textures.Values)
                                    {
                                        if (bakeStatus.Textures.ContainsKey(textureID))
                                        {
                                            /* skip we already got that one */
                                            continue;
                                        }
                                        AssetData textureData;
                                        if (!AssetService.TryGetValue(textureID, out textureData))
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) failed to be retrieved", inventoryItem.AssetID, Owner.FullName, Owner.ID);
                                            m_BakeLog.ErrorFormat(info);
                                            throw new BakingErrorException(info);
                                        }

                                        if (textureData.Type != AssetType.Texture)
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) is not a texture (got {3})", inventoryItem.AssetID, Owner.FullName, Owner.ID, textureData.Type.ToString());
                                            m_BakeLog.ErrorFormat(info);
                                            throw new BakingErrorException(info);
                                        }

                                        try
                                        {
                                            bakeStatus.Textures.Add(textureData.ID, CSJ2K.J2kImage.FromStream(textureData.InputStream));
                                        }
                                        catch (Exception e)
                                        {
                                            string info = string.Format("Asset {0} for agent {1} ({2}) failed to decode as a texture", inventoryItem.AssetID, Owner.FullName, Owner.ID);
                                            m_BakeLog.ErrorFormat(info, e);
                                            throw new BakingErrorException(info, e);
                                        }
                                    }

                                    if(bakeStatus.Layer0TextureID == UUID.Zero &&
                                        !outfitItem.WearableData.Textures.TryGetValue(AvatarTextureIndex.HeadBodypaint, out bakeStatus.Layer0TextureID))
                                    {
                                        bakeStatus.Layer0TextureID = UUID.Zero;
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }

                CoreBakeLogic(bakeStatus);
            }
        }

        void AddAlpha(Bitmap bmp, Image inp)
        {
            Bitmap bmpin = null;
            try
            {
                if (inp.Width != bmp.Width || inp.Height != bmp.Height)
                {
                    bmpin = new Bitmap(inp, bmp.Size);
                }
                else
                {
                    bmpin = new Bitmap(inp);
                }

                int x;
                int y;

                for (y = 0; y < bmp.Height; ++y)
                {
                    for (x = 0; x < bmp.Width; ++x)
                    {
                        System.Drawing.Color dst = bmp.GetPixel(x, y);
                        System.Drawing.Color src = bmpin.GetPixel(x, y);
                        bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(
                            dst.A > src.A ? src.A : dst.A,
                            dst.R,
                            dst.G,
                            dst.B));
                    }
                }
            }
            finally
            {
                if(null != bmpin)
                {
                    bmpin.Dispose();
                }
            }
        }

        void MultiplyLayerFromAlpha(Bitmap bmp, Image inp)
        {
            Bitmap bmpin = null;
            try
            {
                if (inp.Width != bmp.Width || inp.Height != bmp.Height)
                {
                    bmpin = new Bitmap(inp, bmp.Size);
                }
                else
                {
                    bmpin = new Bitmap(inp);
                }

                int x;
                int y;

                for (y = 0; y < bmp.Height; ++y)
                {
                    for (x = 0; x < bmp.Width; ++x)
                    {
                        System.Drawing.Color dst = bmp.GetPixel(x, y);
                        System.Drawing.Color src = bmpin.GetPixel(x, y);
                        bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(
                            dst.A,
                            (byte)((dst.R * src.A) / 255),
                            (byte)((dst.G * src.A) / 255),
                            (byte)((dst.B * src.A) / 255)));
                    }
                }
            }
            finally
            {
                if (null != bmpin)
                {
                    bmpin.Dispose();
                }
            }
        }

        System.Drawing.Color GetTint(Wearable w, BakeType bType)
        {
            Types.Color wColor = new Types.Color(1, 1, 1);
            double val;
            switch (w.Type)
            {
                case WearableType.Tattoo:
                    if (w.Params.TryGetValue(1071, out val))
                    {
                        wColor.R = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(1072, out val))
                    {
                        wColor.G = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(1073, out val))
                    {
                        wColor.B = val.Clamp(0, 1);
                    }
                    switch (bType)
                    {
                        case BakeType.Head:
                            if(w.Params.TryGetValue(1062, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1063, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1064, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.UpperBody:
                            if (w.Params.TryGetValue(1065, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1066, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1067, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.LowerBody:
                            if (w.Params.TryGetValue(1068, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1069, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1070, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                case WearableType.Jacket:
                    if (w.Params.TryGetValue(834, out val))
                    {
                        wColor.R = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(835, out val))
                    {
                        wColor.G = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(836, out val))
                    {
                        wColor.B = val.Clamp(0, 1);
                    }
                    switch (bType)
                    {
                        case BakeType.UpperBody:
                            if (w.Params.TryGetValue(831, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(832, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(833, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.LowerBody:
                            if (w.Params.TryGetValue(809, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(810, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(811, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                default:
                    wColor = w.GetTint();
                    break;
            }

            return System.Drawing.Color.FromArgb(wColor.R_AsByte, wColor.G_AsByte, wColor.B_AsByte);
        }

        void ApplyTint(Bitmap bmp, Types.Color col)
        {
            int x;
            int y;
            for(y = 0; y < bmp.Height; ++y)
            {
                for (x = 0; x < bmp.Width; ++x)
                {
                    System.Drawing.Color inp = bmp.GetPixel(x, y);
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(
                        inp.A,
                        (byte)(inp.R * col.R).Clamp(0, 255),
                        (byte)(inp.G * col.G).Clamp(0, 255),
                        (byte)(inp.B * col.B).Clamp(0, 255)));
                }
            }
        }

        AssetData BakeTexture(BakeStatus status, BakeType bake)
        {
            int bakeDimensions = (bake == BakeType.Eyes) ? 128 : 512;
            Image srcimg;
            AssetData data = new AssetData();
            data.Type = AssetType.Texture;
            data.Local = true;
            data.Temporary = true;
            data.Flags = AssetFlags.Collectable | AssetFlags.Rewritable;
            AvatarTextureIndex[] bakeProcessTable;
            switch(bake)
            {
                case BakeType.Head:
                    bakeProcessTable = IndexesForBakeHead;
                    data.Name = "Baked Head Texture";
                    break;

                case BakeType.Eyes:
                    bakeProcessTable = IndexesForBakeEyes;
                    data.Name = "Baked Eyes Texture";
                    break;

                case BakeType.Hair:
                    bakeProcessTable = IndexesForBakeHair;
                    data.Name = "Baked Hair Texture";
                    break;

                case BakeType.LowerBody:
                    bakeProcessTable = IndexesForBakeLowerBody;
                    data.Name = "Baked Lower Body Texture";
                    break;

                case BakeType.UpperBody:
                    bakeProcessTable = IndexesForBakeUpperBody;
                    data.Name = "Baked Upper Body Texture";
                    break;

                case BakeType.Skirt:
                    bakeProcessTable = IndexesForBakeSkirt;
                    data.Name = "Baked Skirt Texture";
                    break;

                default:
                    throw new ArgumentOutOfRangeException("bake");
            }

            using (Bitmap bitmap = new Bitmap(bakeDimensions, bakeDimensions, PixelFormat.Format32bppArgb))
            {
                using (Graphics gfx = Graphics.FromImage(bitmap))
                {
                    if (bake == BakeType.Eyes)
                    {
                        /* eyes have white base texture */
                        using (SolidBrush brush = new SolidBrush(System.Drawing.Color.White))
                        {
                            gfx.FillRectangle(brush, new Rectangle(0, 0, 128, 128));
                        }
                    }
                    else if(status.Layer0TextureID != UUID.Zero &&
                        status.TryGetTexture(bake, status.Layer0TextureID, out srcimg))
                    {
                        /* all others are inited from layer 0 */
                        gfx.DrawImage(srcimg, 0, 0, 512, 512);
                    }
                    else
                    {
                        switch(bake)
                        {
                            case BakeType.Head:
                                gfx.DrawImage(BaseBakes.HeadColor, 0, 0, 512, 512);
                                AddAlpha(bitmap, BaseBakes.HeadAlpha);
                                MultiplyLayerFromAlpha(bitmap, BaseBakes.HeadSkinGrain);
                                break;

                            case BakeType.UpperBody:
                                gfx.DrawImage(BaseBakes.UpperBodyColor, 0, 0, 512, 512);
                                break;

                            case BakeType.LowerBody:
                                gfx.DrawImage(BaseBakes.LowerBodyColor, 0, 0, 512, 512);
                                break;

                            default:
                                break;
                        }
                    }

                    /* alpha blending is enabled by changing the compositing mode of the graphics object */
                    gfx.CompositingMode = CompositingMode.SourceOver;

                    foreach (AvatarTextureIndex texIndex in bakeProcessTable)
                    {
                        foreach (OutfitItem item in status.OutfitItems.Values)
                        {
                            UUID texture;
                            Image img;
                            if (null != item.WearableData && item.WearableData.Textures.TryGetValue(texIndex, out texture))
                            {
                                if (status.TryGetTexture(bake, texture, out img))
                                {
                                    /* duplicate texture */
                                    using (Bitmap bmp = new Bitmap(img))
                                    {
                                        switch (texIndex)
                                        {
                                            case AvatarTextureIndex.HeadBodypaint:
                                            case AvatarTextureIndex.UpperBodypaint:
                                            case AvatarTextureIndex.LowerBodypaint:
                                                /* no tinting here */
                                                break;

                                            default:
                                                ApplyTint(bmp, item.WearableData.GetTint());
                                                break;
                                        }

                                        gfx.DrawImage(bmp, 0, 0, bakeDimensions, bakeDimensions);
                                        AddAlpha(bitmap, bmp);
                                    }
                                }
                            }
                        }
                    }
                }

                data.Data = CSJ2K.J2KEncoder.EncodeJPEG(bitmap);
            }

            return data;
        }

        static readonly AvatarTextureIndex[] IndexesForBakeHead = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.HeadAlpha,
            AvatarTextureIndex.HeadBodypaint,
            AvatarTextureIndex.HeadTattoo
        };

        static readonly AvatarTextureIndex[] IndexesForBakeUpperBody = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.UpperBodypaint,
            AvatarTextureIndex.UpperGloves,
            AvatarTextureIndex.UpperUndershirt,
            AvatarTextureIndex.UpperShirt,
            AvatarTextureIndex.UpperJacket,
            AvatarTextureIndex.UpperAlpha
        };

        static readonly AvatarTextureIndex[] IndexesForBakeLowerBody = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.LowerBodypaint,
            AvatarTextureIndex.LowerUnderpants,
            AvatarTextureIndex.LowerSocks,
            AvatarTextureIndex.LowerShoes,
            AvatarTextureIndex.LowerPants,
            AvatarTextureIndex.LowerJacket,
            AvatarTextureIndex.LowerAlpha
        };

        static readonly AvatarTextureIndex[] IndexesForBakeEyes = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.EyesIris,
            AvatarTextureIndex.EyesAlpha
        };

        static readonly AvatarTextureIndex[] IndexesForBakeHair = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.Hair,
            AvatarTextureIndex.HairAlpha
        };

        static readonly AvatarTextureIndex[] IndexesForBakeSkirt = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.Skirt
        };

        void CoreBakeLogic(BakeStatus bakeStatus)
        {
            AssetData bakeHead = BakeTexture(bakeStatus, BakeType.Head);
            AssetData bakeUpperBody = BakeTexture(bakeStatus, BakeType.UpperBody);
            AssetData bakeLowerBody = BakeTexture(bakeStatus, BakeType.LowerBody);
            AssetData bakeEyes = BakeTexture(bakeStatus, BakeType.Eyes);
            AssetData bakeHair = BakeTexture(bakeStatus, BakeType.Hair);
            AssetData bakeSkirt = null;

            bool haveSkirt = false;
            foreach(OutfitItem item in bakeStatus.OutfitItems.Values)
            {
                if(item.WearableData != null && item.WearableData.Type == WearableType.Skirt)
                {
                    haveSkirt = true;
                    break;
                }
            }

            if (haveSkirt)
            {
                bakeSkirt = BakeTexture(bakeStatus, BakeType.Skirt);
            }

            AgentCircuit circuit = Circuits[m_CurrentSceneID];
            SceneInterface scene = circuit.Scene;
            AssetServiceInterface assetService = scene.AssetService;

            assetService.Store(bakeEyes);
            assetService.Store(bakeHead);
            assetService.Store(bakeUpperBody);
            assetService.Store(bakeLowerBody);
            assetService.Store(bakeHair);
            if(null != bakeSkirt)
            {
                assetService.Store(bakeSkirt);
            }

            Textures[(int)AvatarTextureIndex.EyesBaked] = bakeEyes.ID;
            Textures[(int)AvatarTextureIndex.HeadBaked] = bakeHead.ID;
            Textures[(int)AvatarTextureIndex.UpperBaked] = bakeUpperBody.ID;
            Textures[(int)AvatarTextureIndex.LowerBaked] = bakeLowerBody.ID;
            Textures[(int)AvatarTextureIndex.HairBaked] = bakeHair.ID;
            Textures[(int)AvatarTextureIndex.Skirt] = bakeSkirt != null ? bakeSkirt.ID : UUID.Zero;
        }

        #endregion

        #region Base Bake textures
        static class BaseBakes
        {
            public static readonly Image HeadAlpha;
            public static readonly Image HeadColor;
            public static readonly Image HeadHair;
            public static readonly Image HeadSkinGrain;
            public static readonly Image LowerBodyColor;
            public static readonly Image UpperBodyColor;

            static BaseBakes()
            {
                HeadAlpha = LoadResourceImage("head_alpha.tga.gz");
                HeadColor = LoadResourceImage("head_color.tga.gz");
                HeadHair = LoadResourceImage("head_hair.tga.gz");
                HeadSkinGrain = LoadResourceImage("head_skingrain.tga.gz");
                LowerBodyColor = LoadResourceImage("lowerbody_color.tga.gz");
                UpperBodyColor = LoadResourceImage("upperbody_color.tga.gz");
            }

            static Image LoadResourceImage(string name)
            {
                Assembly assembly = typeof(BaseBakes).Assembly;
                using (Stream resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources." + name))
                {
                    using (GZipStream gz = new GZipStream(resource, CompressionMode.Decompress))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            byte[] buf = new byte[10240];
                            int bytesRead;
                            for (bytesRead = gz.Read(buf, 0, buf.Length);
                                bytesRead > 0;
                                bytesRead = gz.Read(buf, 0, buf.Length))
                            {
                                ms.Write(buf, 0, bytesRead);
                            }
                            ms.Seek(0, SeekOrigin.Begin);
                            return Paloma.TargaImage.LoadTargaImage(ms);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.GameData.Crops;
using StardewValley.GameData.Objects;

namespace QualitySmash
{
    internal class CropBaseColors
    {
        private struct ColorRec
        {
            public ColorRec(string itemId, Color itemColor)
            {
                id = itemId;
                color = itemColor;
            }

            public string id;
            public Color color;
        }

        private List<ColorRec> baseColorList;
        private bool cropTableLoaded;

        public CropBaseColors()
        {
            baseColorList = new();
            cropTableLoaded = false;
        }

        private void LoadCropTable()
        {
            try
            {
                if (Game1.content != null)
                {
                    var objectData = DataLoader.Objects(Game1.content);
                    var cropData = DataLoader.Crops(Game1.content);

                    if ((objectData != null) && (cropData != null))
                    {
                        cropTableLoaded = true;
                        baseColorList.Clear();

                        // for items that have multiple possible color tints, we use the first one for our smashed color value.
                        // it might be better(?) to have the ColoredObject changed to a non-Colored object.
                        // that could be risky if I miss a data field in the conversion.
                        // this is safer.
                        // previously 'default' was used for the color. which seemed to be a non color. transparent and black.
                        // this only affected tailoring. inventory/cooking was not affected by the 'default' color.

                        foreach (var obj in objectData)
                        {
                            if (obj.Value.Category == StardewValley.Object.flowersCategory)
                            {
                                foreach (var crop in cropData)
                                {
                                    ParsedItemData harvestItemData = ItemRegistry.GetDataOrErrorItem(crop.Value.HarvestItemId);
                                    if (obj.Key.Equals(harvestItemData.ItemId))
                                    {
                                        if (crop.Value.TintColors.Count > 0)
                                        {
                                            Color? clr = Utility.StringToColor(crop.Value.TintColors[0]);
                                            if (clr.HasValue)
                                            {
                                                baseColorList.Add(new ColorRec(harvestItemData.ItemId, clr.Value));
                                                //ModEntry.Instance.Monitor.Log($"Color match. item={harvestItemData.ItemId}, tint={clr.Value}", LogLevel.Debug);
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        ModEntry.Instance.Monitor.Log($"QualitySmash: objectData or cropData is null. objectData={objectData == null}, cropdata={cropData == null}", LogLevel.Error);
                    }
                }
            }
            catch (Exception e)
            {
                ModEntry.Instance.Monitor.Log($"QualitySmash: exception in LoadCropTable. e={e}", LogLevel.Error);
            }
        }

        public Color FindBaseColor(string objectId)
        {
            // this handles a situation that if for some reason the class contructor fails to load crop data
            // we try again at first attempt to use color smash.
            if (!cropTableLoaded)
                LoadCropTable();

            if (baseColorList.Count > 0)
            {
                foreach (var item in baseColorList)
                {
                    if (objectId.Equals(item.id))
                        return item.color;
                }
            }
            return Color.White;// default if I cannot find a crop tint.
        }

        public void ClearList()
        {
            baseColorList.Clear();
            cropTableLoaded = false;
        } 
    }
}

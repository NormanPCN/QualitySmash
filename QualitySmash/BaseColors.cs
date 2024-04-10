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

namespace QualitySmash
{
    internal class GetBaseColors
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

        public GetBaseColors()
        {
            baseColorList = new();

            var objectData = DataLoader.Objects(Game1.content);
            var cropData = DataLoader.Crops(Game1.content);

            // for items that have multiple possible color tints, we use the first one for our smashed color value.
            // it might be better(?) to have the ColoredObject changed to a non-Colored object.
            // that could be risky if I miss a data field in the conversion.
            // this is safer.
            // previously 'default' was used for the color. which is a non color. transparent and black.
            // I now default to White if I cannot find a tint.

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

        public Color FindBaseColor(string objectId)
        {
            if (this.baseColorList.Count > 0)
            {
                foreach (var item in this.baseColorList)
                {
                    if (objectId.Equals(item.id))
                        return item.color;
                }
            }
            return Color.White;
        }

        public void ClearList()
        {
            this.baseColorList.Clear();
        } 
    }
}

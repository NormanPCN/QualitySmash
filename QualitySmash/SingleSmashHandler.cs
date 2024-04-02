using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
//using System.Web.DynamicData;
//using Microsoft.Build.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace QualitySmash
{
    internal class SingleSmashHandler
    {
        private string hoverTextColor;
        private string hoverTextQuality;
        private bool drawHoverTextC;
        private bool drawHoverTextQ;
        private readonly Texture2D cursorColor;
        private readonly Texture2D cursorQuality;
        private readonly ModEntry modEntry;
        private readonly ModConfig config;

        private IList<Item> actualItems;
        private int itemSlotNumber;
        private ItemGrabMenu chestMenu;// != null, if the item was not in the backpack inventory

        public SingleSmashHandler(ModEntry modEntry, ModConfig config, Texture2D cursorColor, Texture2D cursorQuality)
        {
            this.modEntry = modEntry;
            this.config = config;
            this.cursorColor = cursorColor;
            this.cursorQuality = cursorQuality;
            this.drawHoverTextC = false;
            this.drawHoverTextQ = false;
            this.hoverTextColor = modEntry.helper.Translation.Get("hoverTextColor");
            this.hoverTextQuality = modEntry.helper.Translation.Get("hoverTextQuality");
        }

        internal void HandleClick(ButtonPressedEventArgs e)
        {
            var menu = ModEntry.GetValidKeybindSmashMenu();

            if (menu == null || !config.EnableSingleItemSmashKeybinds)
                return;

            ModEntry.SmashType smashType;
            if (modEntry.helper.Input.IsDown(config.ColorSmashKeybind))
                smashType = ModEntry.SmashType.Color;
            else if (modEntry.helper.Input.IsDown(config.QualitySmashKeybind))
                smashType = ModEntry.SmashType.Quality;
            else
                return;

            bool oldUiMode = Game1.uiMode;
            Game1.uiMode = true;
            var cursorPos = e.Cursor.GetScaledScreenPixels();
            Game1.uiMode = oldUiMode;

            // If the cursor was over a valid item when left clicked, initiate smash
            var cursorHoverItem = CheckInventoriesForCursorHoverItem(menu, cursorPos);

            if (cursorHoverItem == null)
                return;

            var itemToSmash = GetActualItem(cursorHoverItem);

            if (itemToSmash != null)
                DoSmash(itemToSmash, smashType);
        }

        private Item GetActualItem(ClickableComponent clickableItem)
        {
            itemSlotNumber = Convert.ToInt32(clickableItem.name);

            if (actualItems == null)
                return null;

            if (itemSlotNumber < actualItems.Count && actualItems[itemSlotNumber] != null)
                return actualItems[itemSlotNumber];
            return null;
        }

        private ClickableComponent CheckInventoriesForCursorHoverItem(IClickableMenu menu, Vector2 cursorPos)
        {
            ClickableComponent itemToSmash;
            chestMenu = null;

            if (menu is ItemGrabMenu grabMenu)
            {
                itemToSmash = ScanForHoveredItem(grabMenu.inventory.inventory, cursorPos);
                if (itemToSmash != null)
                {
                    this.actualItems = grabMenu.inventory.actualInventory;
                    return itemToSmash;
                }

                itemToSmash = ScanForHoveredItem(grabMenu.ItemsToGrabMenu.inventory, cursorPos);
                if (itemToSmash != null)
                {
                    chestMenu = grabMenu;
                    this.actualItems = grabMenu.ItemsToGrabMenu.actualInventory;
                    return itemToSmash;
                }
            }
            if (menu is GameMenu gameMenu)
            {
                if (!(gameMenu.GetCurrentPage() is InventoryPage inventoryPage))
                    return null;

                itemToSmash = ScanForHoveredItem(inventoryPage.inventory.inventory, cursorPos);
                if (itemToSmash != null)
                {
                    actualItems = inventoryPage.inventory.actualInventory;
                    return itemToSmash;
                }
            }

            return null;
        }

        private static ClickableComponent ScanForHoveredItem(List<ClickableComponent> clickableItems, Vector2 cursorPos)
        {
            foreach (var clickableItem in clickableItems)
            {
                if (clickableItem.containsPoint((int)cursorPos.X, (int)cursorPos.Y))
                    return clickableItem;
            }
            return null;
        }

        private void DoSmash(Item item, ModEntry.SmashType smashType)
        {
            if (item.maximumStackSize() <= 1)
                return;

            Game1.playSound("clubhit");

            if (smashType == ModEntry.SmashType.Color)
            {
                if (item.Category == -5)
                {
                    if (item.ParentSheetIndex == 180)
                        item.ParentSheetIndex = 176;

                    if (item.ParentSheetIndex == 182)
                        item.ParentSheetIndex = 174;
                }

                if ((item.Category == StardewValley.Object.flowersCategory) && (item is ColoredObject c))
                    c.color.Value = modEntry.colorTable.FindBaseColor(item.ItemId);

            }

            if (smashType == ModEntry.SmashType.Quality)
            {
                if ((item is StardewValley.Object o) && (o.Quality != 0))
                {
                    o.Quality /= 2;
                    if (config.EnableSingleSmashToBaseQuality)
                        o.Quality = 0;
                }
            }

            //now look for another stack of the same item (quality+color) and stack this to that

            int slot = itemSlotNumber;
            int idx = item.ParentSheetIndex;
            for (int j = 0; j < actualItems.Count; j++)
            {
                if (
                    (j != slot) &&
                    (actualItems[j] != null) &&
                    (actualItems[j].ParentSheetIndex == idx) &&
                    actualItems[j].canStackWith(item)
                   )
                {
                    // stack onto the item with the lowest index for chests
                    // stack onto the clicked item for backpack/inventory
                    if (chestMenu != null)
                    {
                        if (j < slot)
                        {
                            int temp = j;
                            j = slot;
                            slot = temp;
                        }
                    }

                    if (actualItems[slot].getRemainingStackSpace() > 0)
                    {
                        int remain = actualItems[slot].addToStack(actualItems[j]);
                        if (chestMenu != null)
                            chestMenu.ItemsToGrabMenu.ShakeItem(actualItems[slot]);

                        if (remain == 0)
                        {
                            if (chestMenu == null)
                                actualItems[j] = default; // backpack, leave a hole
                            else
                                actualItems.RemoveAt(j);
                        }
                        else
                        {
                            actualItems[j].Stack = remain;
                        }
                    }

                    return;
                }
            }
        }

        private static bool IsSmashable(Item item, ModEntry.SmashType smashType)
        {
            if (item == null)
                return false;

            if (smashType == ModEntry.SmashType.Color)
            {
                if ((item.Category == StardewValley.Object.flowersCategory) && (item is ColoredObject))
                    return true;
                if (item.ParentSheetIndex == 180 || item.ParentSheetIndex == 182)
                    return true;
                //modEntry.Monitor.Log($"Not smashable. category={item.Category}, isColored={item is ColoredObject}", LogLevel.Debug);
            }

            if (smashType == ModEntry.SmashType.Quality)
            {
                if (item is StardewValley.Object o && o.Quality != 0)
                    return true;
            }
            return false;
        }

        // Should be reworked to hover over any item in any inventory
        internal bool TryHover(IClickableMenu menu, float x, float y)
        {
            if (menu == null || !config.EnableSingleItemSmashKeybinds)
                return false;

            //this.hoverTextColor = "";
            //this.hoverTextQuality = "";
            this.drawHoverTextC = false;
            this.drawHoverTextQ = false;

            var cursorPos = new Vector2(x, y);

            var item = CheckInventoriesForCursorHoverItem(menu, cursorPos);

            if (item != null)
            {
                if (modEntry.helper.Input.IsDown(config.ColorSmashKeybind))
                {
                    if (item.containsPoint((int) x, (int) y) && IsSmashable(GetActualItem(item), ModEntry.SmashType.Color))
                    {
                        //this.hoverTextColor = modEntry.helper.Translation.Get("hoverTextColor");
                        this.drawHoverTextC = true;
                        return true;
                    }
                }
                else if (modEntry.helper.Input.IsDown(config.QualitySmashKeybind))
                {
                    if (item.containsPoint((int) x, (int) y) && IsSmashable(GetActualItem(item), ModEntry.SmashType.Quality))
                    {
                        //this.hoverTextQuality = modEntry.helper.Translation.Get("hoverTextQuality");
                        this.drawHoverTextQ = true;
                        return true;
                    }
                }
            }
            return false;
        }

        public void DrawHoverText(SpriteBatch b)
        {
            Texture2D cursor;
            int yOffset;
            int xOffset;

            if (this.drawHoverTextC)
            {
                IClickableMenu.drawHoverText(b, hoverTextColor, Game1.smallFont, 57, -87);
                cursor = this.cursorColor;
                yOffset = -50;
                xOffset = 32;
            }

            else if (this.drawHoverTextQ)
            {
                IClickableMenu.drawHoverText(b, hoverTextQuality, Game1.smallFont, 57, -87);
                cursor = this.cursorQuality;
                yOffset = -50;
                xOffset = 32;
            }
            else
            {
                return;
                //cursor = Game1.mouseCursors;
                //yOffset = 0;
                //xOffset = 0;
            }

            // draw our button icon beside the hover text.
            b.Draw(cursor,
                   new Vector2(Game1.getOldMouseX() + xOffset, Game1.getOldMouseY() + yOffset),
                   Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16),
                   Color.White,
                   0f,
                   Vector2.Zero,
                   4f + Game1.dialogueButtonScale / 150f,
                   SpriteEffects.None,
                   0);
        }
    }
}

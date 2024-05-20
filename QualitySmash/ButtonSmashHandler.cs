//#define OriginalButtonSmash

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Events;
using System.Runtime.CompilerServices;

namespace QualitySmash
{
    internal class ButtonSmashHandler
    {
        private readonly ModEntry modEntry;
        private readonly Texture2D buttonColor;
        private readonly Texture2D buttonQuality;
        private readonly UiButtonHandler buttonHandler;
        private readonly ModConfig config;
        private List<string> autoSmashItems;

        // 1.6 has these fields in the InventoryPage public now
        //IReflectedField<Item> Reflect_hoveredItem;
        //IReflectedField<string> Reflect_hoverText;
        //IReflectedField<int> Reflect_hoverAmount;

        /// <summary>
        /// Initializes stuff for the mod.
        /// </summary>
        /// <param name="modEntry">The ModEntry</param>
        /// <param name="config">The mods config</param>
        /// <param name="imageColor">Button texture for the color smash button</param>
        /// <param name="imageQuality">Button texture for the quality smash button</param>
        public ButtonSmashHandler(ModEntry modEntry, ModConfig config, Texture2D buttonColor, Texture2D buttonQuality)
        {
            this.modEntry = modEntry;
            this.config = config;
            this.buttonColor = buttonColor;
            this.buttonQuality = buttonQuality;
            this.autoSmashItems = new List<string>();

            this.buttonHandler = new UiButtonHandler(modEntry);

            //Reflect_hoveredItem = null;
            //Reflect_hoverText = null;
            //Reflect_hoverAmount = null;
        }

        public void NewMenuActive(IClickableMenu menu)
        {
            //Reflect_hoveredItem = null;
            //Reflect_hoverText = null;
            //Reflect_hoverAmount = null;

            //if ((menu is GameMenu gMenu) && (gMenu.GetCurrentPage() is InventoryPage iPage))
            //{
            //    IReflectionHelper reflect = modEntry.helper.Reflection;
            //    Reflect_hoveredItem = reflect.GetField<Item>(iPage, "hoveredItem");
            //    Reflect_hoverText = reflect.GetField<string>(iPage, "hoverText");
            //    Reflect_hoverAmount = reflect.GetField<int>(iPage, "hoverAmount");
            //}

            if (modEntry.IsValidSmashMenuAny(menu))
            {
                if (modEntry.Config.EnableUIQualitySmashButton)
                    buttonHandler.AddButton(ModEntry.SmashType.Quality, buttonQuality, new Rectangle(0, 0, 16, 16));
                else
                    buttonHandler.RemoveButton(ModEntry.SmashType.Quality);

                if (modEntry.Config.EnableUIColorSmashButton)
                    buttonHandler.AddButton(ModEntry.SmashType.Color, buttonColor, new Rectangle(0, 0, 16, 16));
                else
                    buttonHandler.RemoveButton(ModEntry.SmashType.Color);

                buttonHandler.UpdateBounds(menu, true);
            }
        }

        public void MenuDeactivate(IClickableMenu menu)
        {
            if (modEntry.IsValidSmashMenuAny(menu))
            {
                buttonHandler.UpdateBounds(menu, false);
            }
        }

        //public void AddButton(ModEntry.SmashType smashType, Texture2D image, Rectangle clickableArea)
        //{
        //    this.buttonHandler.AddButton(smashType, image, clickableArea);
        //}

        //public void RemoveButton(ModEntry.SmashType smashType)
        //{
        //    this.buttonHandler.RemoveButton(smashType);
        //}

        public void DrawButtons(IClickableMenu menu, SpriteBatch b)
        {
            buttonHandler.DrawButtons(b);

#if !UseHarmony
            // redraw any foreground hovertext our buttons may have drawn over

            Item hoveredItem = null;
            string hoverText = null;
            int hoverAmount = 0;
            InventoryMenu iMenu = null;
            bool heldItem = false;

            if (menu is ItemGrabMenu grabMenu)
            {
                hoveredItem = grabMenu.hoveredItem;
                hoverAmount = grabMenu.hoverAmount;
                hoverText = grabMenu.hoverText;
                iMenu = grabMenu.ItemsToGrabMenu;
                heldItem = grabMenu.heldItem != null;
            }
            else if ((menu is GameMenu gMenu) && (gMenu.GetCurrentPage() is InventoryPage iPage))
            {
                hoveredItem = iPage.hoveredItem;
                hoverText = iPage.hoverText;
                hoverAmount = iPage.hoverAmount;
                iMenu = iPage.inventory;
                heldItem = Game1.player.CursorSlotItem != null;//looking at InventoryPage.checkHeldItem code
            }

            // code logic for hover items redraw provided by furyx639.
            // which in turn is taken from ItemGrabMenu.draw (bottom of method)
            // might have an issue on iPage but seems to be fine thus far. iPage has a slightly different hover draw.

            if ((hoverText is not null) && (hoveredItem == null))
            {
                if (hoverAmount > 0)
                    IClickableMenu.drawToolTip(b, hoverText, hoverTitle: string.Empty, hoveredItem: null, heldItem: true, moneyAmountToShowAtBottom: hoverAmount);
                else
                    IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);
            }

            if (iMenu != null)
            {
                if ((iMenu.hoverText is not null) && (hoveredItem != null))
                    IClickableMenu.drawToolTip(b, hoveredItem.getDescription(), hoveredItem.DisplayName, hoveredItem, heldItem);
                else if (hoveredItem != null)
                    IClickableMenu.drawToolTip(b, iMenu.descriptionText, iMenu.descriptionTitle, hoveredItem, heldItem);
            }

            menu.drawMouse(b);
#endif
        }

        internal void TryHover(IClickableMenu menu, float x, float y)
        {
            if (menu != null)
                buttonHandler.TryHover(x, y);
        }

        internal void HandleClick(ButtonPressedEventArgs e)
        {

            var oldUiMode = Game1.uiMode;
            Game1.uiMode = true;
            var cursorPos = e.Cursor.GetScaledScreenPixels();
            Game1.uiMode = oldUiMode;

            IList<Item> actualItems;
            ItemGrabMenu chestMenu = null;
            IClickableMenu menu = ModEntry.GetValidButtonSmashMenu();
            if (menu is ItemGrabMenu)
            {
                chestMenu = menu as ItemGrabMenu;
                actualItems = chestMenu.ItemsToGrabMenu.actualInventory;
            }
            else if ((menu is GameMenu gameMenu) && (gameMenu.GetCurrentPage() is InventoryPage iPage))
            {
                actualItems = iPage.inventory.actualInventory;
            }
            else
                return;

            var buttonClicked = buttonHandler.GetButtonClicked((int)cursorPos.X, (int)cursorPos.Y);
            if (buttonClicked == ModEntry.SmashType.None)
                return;

            Game1.playSound("Ship");

            DoSmash(chestMenu, actualItems, buttonClicked);
        }

        private bool IsFilteredQuality(Item item, int itemIdx)
        {
            int quality = item.Quality;
            if (quality > 0)
            {
                if (quality == 2)
                    return config.IgnoreGold;
                else if (quality == 1)
                    return config.IgnoreSilver;
                else if ((quality == 4) && config.IgnoreIridium)
                {
                    if (!config.IgnoreIridiumItemExceptions.Contains(itemIdx) && !config.IgnoreIridiumCategoryExceptions.Contains(item.Category))
                        return true;
                };
            }
            return false;
        }

        private bool IsFiltered(Item item, ModEntry.SmashType smashType)
        {
            if ((item == null) || (item is not StardewValley.Object) || (item.maximumStackSize() <= 1))
                return true;

            if (config.IgnoreItemsCategory.Contains(item.Category))
                return true;

            // vanilla objects, the string is just a number in Stardew 1.6. it was an actual number in <=1.5
            int itemIdx;
            if (!int.TryParse(item.ItemId, out itemIdx))
                itemIdx = 0;

            if (smashType != ModEntry.SmashType.Color)
            {
                if (config.IgnoreItemsQuality.Contains(itemIdx))
                    return true;

                int quality = item.Quality;
                if (quality > 0)
                {
                    if (quality == 2)
                    {
                        if (config.IgnoreGold)
                            return true;
                    }
                    else if (quality == 1)
                    {
                        if (config.IgnoreSilver)
                            return true;
                    }
                    else if ((quality == 4) && config.IgnoreIridium)
                    {
                        if (!config.IgnoreIridiumItemExceptions.Contains(itemIdx) && !config.IgnoreIridiumCategoryExceptions.Contains(item.Category))
                            return true;
                    };
                }

            };

            if (smashType != ModEntry.SmashType.Quality)
            {
                if (config.IgnoreItemsColor.Contains(itemIdx))
                    return true;
            }

            if (autoSmashItems.Count > 0)
            {
                for (int i = 0; i < autoSmashItems.Count; i++)
                {
                    if (item.ItemId == autoSmashItems[i])
                        return false;
                }
                return true;
            }

            return false;
        }

        private void DoSmash(ItemGrabMenu chestMenu, IList<Item> actualItems, ModEntry.SmashType smashType)
        {
            // smash in place.
            // then stack/combine the results where possible.

            bool changed = false;

            if (smashType != ModEntry.SmashType.Color)
            {
                for (int i = 0; i < actualItems.Count; i++)
                {
                    if (
                        (actualItems[i] != null) &&
                        (actualItems[i].maximumStackSize() > 1) &&
                        !IsFiltered(actualItems[i], smashType)
                       )
                    {
                        // find the min quality for this item type (id)

                        int itemIdx;
                        if (!int.TryParse(actualItems[i].ItemId, out itemIdx))
                            itemIdx = 0;

                        StardewValley.Object oi = actualItems[i] as StardewValley.Object;
                        int minQuality = oi.Quality;
                        for (int j = i + 1; j < actualItems.Count; j++)
                        {
                            if (
                                (actualItems[j] != null) &&
                                (actualItems[j].ItemId == oi.ItemId) &&
                                (actualItems[j].Quality < minQuality) &&
                                !IsFilteredQuality(actualItems[j], itemIdx)//account for ignore gold/silver lists
                               )
                            {
                                minQuality = actualItems[j].Quality;
                            }
                        }
                        changed = changed || (oi.Quality != minQuality);
                        oi.Quality = minQuality;

                        // change the quality for all items of this type to the min quality
                        for (int j = i + 1; j < actualItems.Count; j++)
                        {
                            if (
                                (actualItems[j] != null) &&
                                (actualItems[j].ItemId == oi.ItemId) &&
                                (actualItems[j].Quality != minQuality) &&
                                !IsFilteredQuality(actualItems[j], itemIdx)
                               )
                            {
                                changed = true;
                                actualItems[j].Quality = minQuality;
                            }
                        }
                    }
                }
            };

            if (smashType != ModEntry.SmashType.Quality)
            {
                for (int i = 0; i < actualItems.Count; i++)
                {
                    if ((actualItems[i] != null) && (actualItems[i].maximumStackSize() > 1) && !IsFiltered(actualItems[i], smashType))
                    {
                        if ((actualItems[i] is ColoredObject c) && (c.Category == StardewValley.Object.flowersCategory))
                        {
                            Color baseColor = modEntry.colorTable.FindBaseColor(c.ItemId);
                            changed = changed || (c.color.Value != baseColor);
                            c.color.Value = baseColor;
                        }
                        else if ((actualItems[i].Category == StardewValley.Object.EggCategory) && config.EnableEggColorSmashing)
                        {
                            if (actualItems[i].ItemId == "182")//large brown egg
                            {
                                changed = true;
                                actualItems[i].ItemId = "174";
                                actualItems[i].ParentSheetIndex = 174;
                            }
                            else if (actualItems[i].ItemId == "180")//brown egg
                            {
                                changed = true;
                                actualItems[i].ItemId = "176";
                                actualItems[i].ParentSheetIndex = 176;
                            }
                        };
                        //if (actualItems[i].Category == StardewValley.Object.flowersCategory && (actualItems[i] is not ColoredObject))
                        //{
                        //    modEntry.Monitor.Log($"Flower is Not a colored item. item={actualItems[i].ItemId}", LogLevel.Debug);
                        //}
                    }
                }
            }

            // now look for another stackable item and stack that(j) onto this(i)
            // later stuff is stacked onto earlier stuff in the list

            if (changed)
            {
                for (int i = 0; i < actualItems.Count; i++)
                {
                    if (!IsFiltered(actualItems[i], smashType))
                    {
                        bool shaked = false;
                        for (int j = i + 1; j < actualItems.Count; j++)
                        {
                            if (
                                (actualItems[j] != null) &&
                                actualItems[i].canStackWith(actualItems[j]) &&
                                (actualItems[i].getRemainingStackSpace() > 0)
                               )
                            {
                                int remain = actualItems[i].addToStack(actualItems[j]);
                                if (remain == 0)
                                {
                                    if (chestMenu != null)
                                    {
                                        actualItems.RemoveAt(j);
                                        j--;// ehh, generally not happy about messing with a for loop control var
                                    }
                                    else
                                        actualItems[j] = default;// leave a hole, backpack inventory
                                }
                                else
                                {
                                    // why the hell does addToStack add to one stack without also removing the added items from the other?
                                    actualItems[j].Stack = remain;
                                }

                                if (!shaked && (chestMenu != null))
                                {
                                    shaked = true;
                                    chestMenu.ItemsToGrabMenu.ShakeItem(actualItems[i]);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Called when the player inventory has changed.
        /// This method implements our auto smash
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public static void Player_InventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            if (
                ModEntry.Instance.AutoSmashEnabled &&
                (e.Added != null) &&
                e.IsLocalPlayer &&
                (Game1.activeClickableMenu == null)
               )
            {
                ModEntry.Instance.buttonSmashHandler.autoSmashItems.Clear();
                bool smash = false;
                foreach (Item item in e.Added)
                {
                    if (!ModEntry.Instance.buttonSmashHandler.IsFiltered(item, ModEntry.SmashType.AutoSmash))
                    {
                        smash = true;
                        ModEntry.Instance.buttonSmashHandler.autoSmashItems.Add(item.ItemId);
                    }
                }

                if (smash)
                {
                    ModEntry.Instance.buttonSmashHandler.DoSmash(null, Game1.player.Items, ModEntry.SmashType.AutoSmash);
                    ModEntry.Instance.buttonSmashHandler.autoSmashItems.Clear();
                }
            }
        }

    }
}


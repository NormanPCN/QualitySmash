using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using xTile;

namespace QualitySmash
{
    internal class UiButtonHandler
    {
        private const int Length = 64;
        private const int PositionFromBottom = 2;
        private const int GapSize = 16;

        private readonly List<QSButton> qsButtons;
        private readonly ModEntry modEntry;

        private static int ButtonComparer(QSButton a, QSButton b)
        {
            if (a.smashType == b.smashType)
                return 0;
            if (a.smashType > b.smashType)
                return 1;
            return -1;
        }

        public UiButtonHandler(ModEntry modEntry)
        {
            qsButtons = new List<QSButton>();

            this.modEntry = modEntry;
        }

        public void AddButton(ModEntry.SmashType smashType, Texture2D texture, Rectangle clickableArea)
        {
            // Make sure button doesn't already exist
            foreach (QSButton button in qsButtons)
            {
                if (button.smashType == smashType)
                    return;
            }

            QSButton newButton = new QSButton(smashType, texture, modEntry.helper.Translation.Get(ModEntry.TranslationMapping[smashType]), clickableArea);

            qsButtons.Add(newButton);

            qsButtons.Sort(ButtonComparer);
        }

        public void RemoveButton(ModEntry.SmashType smashType)
        {
            for (int i = 0; i < qsButtons.Count; i++)
            {
                if (qsButtons[i].smashType == smashType)
                {
                    qsButtons.RemoveAt(i);
                    return;
                }
            }
        }

        private void SetButtonNeighbors(IClickableMenu menu)
        {
            if (qsButtons.Count > 0)
            {
                ClickableTextureComponent clickableLeft0 = null;
                ClickableTextureComponent clickableLeft1 = null;
                ClickableTextureComponent clickable0;
                ClickableTextureComponent clickable1;
                List<ClickableComponent> allClickableComponents = null;

                if (menu is ItemGrabMenu grabMenu)
                {
                    clickableLeft0 = grabMenu.fillStacksButton;
                    clickableLeft1 = grabMenu.organizeButton;
                    allClickableComponents = menu.allClickableComponents;
                }
                else if ((menu is GameMenu gameMenu) && (gameMenu.GetCurrentPage() is InventoryPage iPage))
                {
                    clickableLeft0 = iPage.organizeButton;
                    allClickableComponents = iPage.allClickableComponents;
                }

                clickable0 = qsButtons[0].GetClickable();
                int leftId = -1;
                allClickableComponents?.Add(clickable0);

                if (clickableLeft0 != null)
                {
                    leftId = clickableLeft0.myID;
                    clickable0.leftNeighborID = leftId;
                    clickableLeft0.rightNeighborID = clickable0.myID;
                }

                if (qsButtons.Count > 1)
                {
                    clickable1 = qsButtons[1].GetClickable();
                    clickable0.downNeighborID = clickable1.myID;
                    allClickableComponents?.Add(clickable1);

                    if (clickableLeft1 != null)
                    {
                        leftId = clickableLeft1.myID;
                        clickableLeft1.rightNeighborID = clickable1.myID;
                    }
                    clickable1.leftNeighborID = leftId;
                    clickable1.upNeighborID = clickable0.myID;
                }
            }
        }

        public void UpdateBounds(IClickableMenu menu, bool activate)
        {
            if (!activate)
            {
                List<ClickableComponent> allClickableComponents = null;
                if (menu is ItemGrabMenu grabMenu)
                {
                    allClickableComponents = grabMenu.allClickableComponents;
                }
                else if ((menu is GameMenu gameMenu) && (gameMenu.GetCurrentPage() is InventoryPage iPage))
                {
                    allClickableComponents = iPage.allClickableComponents;
                }

                for (int i = 0; i < qsButtons.Count; i++)
                {
                    var clickable = qsButtons[i].GetClickable();
                    clickable.visible = false;
                    allClickableComponents?.Remove(clickable);
                }
                return;
            }

            if (activate)
            {
                int screenX = menu.xPositionOnScreen + menu.width + GapSize + Length;
                int screenY;

                // there is actually a different gap in between vanilla buttons (organize, fillstacks) with CC button active.
                int gap = GapSize;

                if (menu is ItemGrabMenu grabMenu)
                {
                    // if >= 4 buttons the gap is smaller.
                    if (
                        (grabMenu.fillStacksButton != null) &&
                        (grabMenu.organizeButton != null) &&
                        (grabMenu.colorPickerToggleButton != null) &&
                        (
                         (grabMenu.junimoNoteIcon != null) || (grabMenu.specialButton != null)
                        )
                       )
                    {
                        gap /= 2;
                    }

                    screenY = menu.yPositionOnScreen + (menu.height / 3) - Length - Length - GapSize;// code from ItemGrabMenu, for fillStacksButton.
                    if (grabMenu.fillStacksButton != null)
                    {
                        screenY = grabMenu.fillStacksButton.bounds.Y;
                        screenX = grabMenu.fillStacksButton.bounds.X + Length + GapSize;//need this with big chests. menu.width seems to not give what we need.
                    }
#if ButtonOffsets
                screenX += modEntry.Config.SmashButtonXOffset_Chest;
#endif
                }
                else
                {
                    screenY = menu.yPositionOnScreen + (menu.height / 3) - Length + 8;// code from InventoryPage, for organizeButton.
                    if ((menu is GameMenu gameMenu) && (gameMenu.GetCurrentPage() is InventoryPage iPage) && (iPage.organizeButton != null))
                    {
                        screenY = iPage.organizeButton.bounds.Y;
                    }
#if ButtonOffsets
                screenX += modEntry.Config.SmashButtonXOffset_Inventory;
#endif
                }

                for (int i = 0; i < qsButtons.Count; i++)
                    qsButtons[i].SetBounds(screenX, screenY + (i * (Length + gap)), Length);

                SetButtonNeighbors(menu);
            }
        }

        public void TryHover(float x, float y)
        {
            foreach (QSButton button in qsButtons)
            {
                button.TryHover((int)x, (int)y);
            }
        }

        public void DrawButtons(SpriteBatch b)
        {
            // draw the hover text after all buttons. lower button can obscure upper button hover text.
            // drawing hover after buttons means we do not care about button draw order.
            QSButton hover = null;
            foreach (QSButton button in qsButtons)
            {
                if (button.DrawButton(b))
                    hover = button;

            }
            if (hover != null)
                hover.DrawHoverText(b);
        }

        public ModEntry.SmashType GetButtonClicked(int x, int y)
        {
            foreach (var button in qsButtons)
            {
                if (button.ContainsPoint(x, y))
                    return button.smashType;
            }

            return ModEntry.SmashType.None;
        }
    }
}

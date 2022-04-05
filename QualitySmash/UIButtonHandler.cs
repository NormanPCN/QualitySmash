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
            if (qsButtons.Any(button => button.smashType == smashType))
                return;

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

        public void UpdateBounds(IClickableMenu menu)
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
                    screenY = grabMenu.fillStacksButton.bounds.Y;
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

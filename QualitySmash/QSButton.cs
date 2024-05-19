using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Util;

namespace QualitySmash
{
    internal class QSButton
    {
        public ModEntry.SmashType smashType { get; private set; }

        private ClickableTextureComponent clickable;
        private bool boundsSet;
        private readonly String hoverText;
        private bool drawHoverText;
        //private Texture2D texture;

        public QSButton(ModEntry.SmashType smashType, Texture2D texture, String hoverText, Rectangle buttonClickableArea)
        {
            this.smashType = smashType;
            this.boundsSet = false;
            this.hoverText = hoverText;
            this.drawHoverText = false;
            //this.texture = texture;

            clickable = new ClickableTextureComponent(Rectangle.Empty, texture, buttonClickableArea, 4f);

            clickable.myID = 150 + (int)smashType;
        }

        public void SetBounds(int screenX, int screenY, int size)
        {
            boundsSet = true;
            clickable.bounds = new Rectangle(screenX, screenY, size, size);
            clickable.visible = true;
        }

        public ClickableTextureComponent GetClickable()
        {
            return clickable;
        }

        public bool DrawButton(SpriteBatch b)
        {
            if (boundsSet)
            {
                clickable.draw(b, Color.White, layerDepth: 0f, frameOffset: 0);
                return this.drawHoverText;
                //if (drawHoverText)
                //    IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont);
            }
            else
                throw new Exception("QSButton: SetBounds not called. Cannot draw button");
        }

        public void DrawHoverText(SpriteBatch b)
        {
            if (this.drawHoverText)
                IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont);
        }

        //Ensure passing scaled pixels
        public bool ContainsPoint(int x, int y)
        {
            return clickable.containsPoint(x, y);
        }

        //public void EnableHoverText(bool enable)
        //{
        //    DrawHoverText = enable;
        //}

        /// <summary>
        /// Scale the button if hovered
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public bool TryHover(int x, int y)
        {
            drawHoverText = this.clickable.containsPoint((int)x, (int)y);
            clickable.tryHover(x, y, 0.4f);
            return drawHoverText;
        }
        
    }
}

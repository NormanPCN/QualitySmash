using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
#if UseHarmony
using HarmonyLib;
#endif
using System;
using StardewModdingAPI;

namespace QualitySmash
{
#if UseHarmony
	public class MenuWithInventoryPatches
	{
		public static void Draw_Postfix(MenuWithInventory __instance, SpriteBatch b)
		{
			try
			{
                IClickableMenu vmenu = ModEntry.GetValidButtonSmashMenu();
                if ((vmenu != null) && ModEntry.Instance.Config.EnableUISmashButtons)
                    ModEntry.Instance.buttonSmashHandler.DrawButtons(vmenu, b);
            }
            catch (Exception e)
			{
				ModEntry.Instance.Monitor.Log($"Failed in {nameof(Draw_Postfix)}:\n{e}", LogLevel.Error);
			}
		}
	}

	public class InventoryMenuPatches
	{
		public static void Draw_Postfix(InventoryMenu __instance, SpriteBatch b)
		{
			try
			{
                IClickableMenu vmenu = ModEntry.GetValidButtonSmashMenu();
                if ((vmenu != null) && ModEntry.Instance.Config.EnableUISmashButtons)
                    ModEntry.Instance.buttonSmashHandler.DrawButtons(vmenu, b);
            }
            catch (Exception e)
			{
				ModEntry.Instance.Monitor.Log($"Failed in {nameof(Draw_Postfix)}:\n{e}", LogLevel.Error);
			}
		}
	}
	//public static class DoPatchClass
	//{
	//	public static void DoPostMenuDraw<T>(T menu, SpriteBatch b) where T : IClickableMenu
	//	{
	//		IClickableMenu vmenu = ModEntry.GetValidButtonSmashMenu();
	//		if ((vmenu != null) && ModEntry.Instance.Config.EnableUISmashButtons)
	//			ModEntry.Instance.buttonSmashHandler.DrawButtons(vmenu, b);
	//	}

	//}
#endif
}

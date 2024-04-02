using System;
using System.Collections.Generic;
using System.IO;
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
#if UseHarmony
using HarmonyLib;
#endif

namespace QualitySmash
{
    public class ModEntry : Mod
    {
        internal enum SmashType
        {
            Color,
            Quality,
            Undo,
            None
        }

        internal static Dictionary<SmashType, string> TranslationMapping = new Dictionary<SmashType, string>()
        {
            { SmashType.Color, "hoverTextColor" },
            { SmashType.Quality, "hoverTextQuality" },
            { SmashType.Undo, "hoverTextUndo"}
        };

        private string assetsPath;

        internal ButtonSmashHandler buttonSmashHandler;
        internal SingleSmashHandler singleSmashHandler;
        private UndoHandler undoHandler;
        internal ModConfig Config;
        internal GetBaseColors colorTable;

        // For GenericModConfigMenu
        private Dictionary<int, string> itemDictionary;
        private Dictionary<int, string> coloredItemDictionary;
        private Dictionary<int, string> categoryDictionary;

        internal IModHelper helper;
        public static ModEntry Instance { get; private set; }
        private bool EventsHooked;

        public override void Entry(IModHelper helper)
        {
            this.assetsPath = Path.Combine(this.Helper.DirectoryPath, "assets");
            
            this.Config = helper.ReadConfig<ModConfig>();
            this.helper = helper;
            Instance = this;

            var buttonColor = helper.ModContent.Load<Texture2D>("assets/buttonColor.png");
            var buttonQuality = helper.ModContent.Load<Texture2D>("assets/buttonQuality.png");
            var buttonUndo = helper.ModContent.Load<Texture2D>("assets/buttonUndo.png");

            PopulateIdReferences();

            this.buttonSmashHandler = new ButtonSmashHandler(this, this.Config);

            if (Config.EnableUIColorSmashButton)
                this.buttonSmashHandler.AddButton(ModEntry.SmashType.Color, buttonColor, new Rectangle(0, 0, 16, 16));

            if (Config.EnableUIQualitySmashButton)
                this.buttonSmashHandler.AddButton(ModEntry.SmashType.Quality, buttonQuality, new Rectangle(0, 0, 16, 16));

            // Config for enable undo?

            this.singleSmashHandler = new SingleSmashHandler(this, this.Config, buttonColor, buttonQuality);

            EventsHooked = false;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

#if UseHarmony
            var harmony = new Harmony(this.ModManifest.UniqueID);

            harmony.PatchAll();
#endif
        }

        /// <summary>
        /// Gets the ItemGrabMenu if it's from a fridge or chest
        /// </summary>
        /// <returns>The ItemGrabMenu</returns>
        internal static IClickableMenu GetValidButtonSmashMenu()
        {
            IClickableMenu menu = Game1.activeClickableMenu;
            if (menu != null)
            {
                if (menu is ItemGrabMenu grabMenu)
                {
                    // Exclude shipping bin, fishing chests, CJB Item spawner, etc
                    // some mods can change the capacity of mini shipping bins
                    // chests anywhere makes the shipping bin look like a 36 chest, but with infinite cap, 36 visible
                    // .shippingBin excludes the main shipping bin.
                    // can't acces the private ItemGrabMenu.sourceItem field to access the chest object and the SpecialChestType field.
                    // how to identify the mini-shipping bin or other SpecialChestType?
                    //    hack. check if the organizeButton exists. if null it will be a (mini)shipping bin
                    if (
                        (grabMenu.source == ItemGrabMenu.source_chest) &&
                        (!grabMenu.shippingBin) &&
                        (grabMenu.organizeButton != null) &&
                        (grabMenu.ItemsToGrabMenu.capacity > 9)
                       )
                    {
                        return grabMenu;
                    }
                }
                else if ((menu is GameMenu gameMenu) && (gameMenu.GetCurrentPage() is InventoryPage))
                {
                    return menu;
                }
            }
            return null;
        }

        internal static IClickableMenu GetValidKeybindSmashMenu()
        {
            IClickableMenu menu = Game1.activeClickableMenu;
            if (
                (menu != null) &&
                (
                 // exclude non chest grab menus. e.g. fishing, CJB Item spawner
                 ((menu is ItemGrabMenu grabMenu) && (grabMenu.source == ItemGrabMenu.source_chest)) ||
                 ((menu is GameMenu gameMenu) && (gameMenu.GetCurrentPage() is InventoryPage))
                )
               )
            {
                return menu;
            }
            return null;
        }

        // valid for either button or keybind smash
        internal bool IsValidSmashMenuAny(IClickableMenu menu)
        {
            if (
                (menu != null) &&
                (
                 // exclude non chest grab menus. e.g. fishing, CJB Item spawner
                 ((menu is ItemGrabMenu grabMenu) && (grabMenu.source == ItemGrabMenu.source_chest)) ||
                 ((menu is GameMenu gameMenu) && (gameMenu.GetCurrentPage() is InventoryPage))
                )
               )
            {
                return true;
            }
            return false;
        }

        // For use with generic mod config menu to display item/category names, but still be compatible with original config structure
        private void PopulateIdReferences()
        {
            itemDictionary = new Dictionary<int, string>();

            using (StreamReader fileStream = new StreamReader(Path.Combine(assetsPath, "ItemIDs.txt")))
            {
                string line = fileStream.ReadLine();

                while (line != null)
                {
                    string[] set = line.Split(',');
                    set[0] = set[0].Trim();
                    set[1] = set[1].Trim();

                    itemDictionary.Add(int.Parse(set[1]), set[0]);

                    line = fileStream.ReadLine();
                }
            }

            coloredItemDictionary = new Dictionary<int, string>();

            using (StreamReader fileStream = new StreamReader(Path.Combine(assetsPath, "ColoredItemIDs.txt")))
            {
                string line = fileStream.ReadLine();

                while (line != null)
                {
                    string[] set = line.Split(',');
                    set[0] = set[0].Trim();
                    set[1] = set[1].Trim();

                    coloredItemDictionary.Add(int.Parse(set[1]), set[0]);

                    line = fileStream.ReadLine();
                }
            }

            categoryDictionary = new Dictionary<int, string>();

            using (StreamReader fileStream = new StreamReader(Path.Combine(assetsPath, "CategoryIDs.txt")))
            {
                string line = fileStream.ReadLine();

                while (line != null)
                {
                    string[] set = line.Split(',');
                    set[0] = set[0].Trim();
                    set[1] = set[1].Trim();

                    categoryDictionary.Add(int.Parse(set[1]), set[0]);

                    line = fileStream.ReadLine();
                }
            }
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu API (if it's installed)
            //var api = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            var api = this.Helper.ModRegistry.GetGenericModConfigMenuApi(this.Monitor);
            if (api is null)
                return;

            // register mod configuration
            api.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // add some Config options
            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Show UI Smash Buttons",
                tooltip: () => "Show the color and quality smash buttons in the user interface",
                getValue: () => this.Config.EnableUISmashButtons,
                setValue: value => this.Config.EnableUISmashButtons = value
            );

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable Color Smash UI Button",
                tooltip: () => "Show the Color Smash button in the user interface. Requires \"Enable UI Buttons\" be enabled.",
                getValue: () => this.Config.EnableUIColorSmashButton,
                setValue: value =>
                {
                    this.Config.EnableUIColorSmashButton = value;
                    if (!value)
                        this.buttonSmashHandler.RemoveButton(ModEntry.SmashType.Color);
                    else
                        this.buttonSmashHandler.AddButton(ModEntry.SmashType.Color, helper.ModContent.Load<Texture2D>("assets/buttonColor.png"), new Rectangle(0, 0, 16, 16));
                });

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable Quality Smash UI Button",
                tooltip: () => "Show the Quality Smash button in the user interface. Requires \"Enable UI Buttons\" be enabled.",
                getValue: () => this.Config.EnableUIQualitySmashButton,
                setValue: value =>
                {
                    this.Config.EnableUIQualitySmashButton = value;
                    if (!value)
                        this.buttonSmashHandler.RemoveButton(ModEntry.SmashType.Quality);
                    else
                        this.buttonSmashHandler.AddButton(ModEntry.SmashType.Quality, helper.ModContent.Load<Texture2D>("assets/buttonQuality.png"), new Rectangle(0, 0, 16, 16));
                });

#if ButtonOffsets
            api.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Chest Button Offset",
                tooltip: () => "If Quality Smash buttons conflict with another Mod, you can offset the button location to compensate. This offsets the button on the chest menu.",
                getValue: () => this.Config.SmashButtonXOffset_Chest,
                setValue: (int value) => this.Config.SmashButtonXOffset_Chest = value,
                min: 0,
                max: 128
                );

            api.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Inventory Button Offset",
                tooltip: () => "If Quality Smash buttons conflict with another Mod, you can offset the button location to compensate. This offsets the button on the inventory menu.",
                getValue: () => this.Config.SmashButtonXOffset_Inventory,
                setValue: (int value) => this.Config.SmashButtonXOffset_Inventory = value,
                min: 0,
                max: 128
                );
#endif

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable Egg Color Smash",
                tooltip: () => "Enable egg colors to be smashed when Color Smashing with UI buttons or using keybinds",
                getValue: () => this.Config.EnableEggColorSmashing,
                setValue: value => this.Config.EnableEggColorSmashing = value
            );

            api.AddPageLink(this.ModManifest, "Smash Filters", () => "Smash Filters", () => "Basic filters to exclude sets of items from Quality Smash");
            api.AddPageLink(this.ModManifest, "Exceptions: Ignore Iridium by Category", () => "Exceptions: Ignore Iridium by Category", () => "Exceptions by category to the \"Ignore Iridium\" smash filter");
            api.AddPageLink(this.ModManifest, "Exceptions: Ignore Iridium by Item", () => "Exceptions: Ignore Iridium", () => "Exceptions to the \"Ignore Iridium\" smash filter");
            api.AddPageLink(this.ModManifest, "Color Smash: Ignore Items", () => "Color Smash: Ignore Items", () => "Items to ignore when using the Color Smash button");
            api.AddPageLink(this.ModManifest, "Both Smash: Ignore by Category", () => "Both Smash: Ignore by Category", () => "Categories to ignore when using the Color Smash or Quality Smash buttons");
            api.AddPageLink(this.ModManifest, "Both Smash: Ignore by Item", () => "Both Smash: Ignore Items", () => "Items to ignore when using the Color Smash or Quality Smash buttons");
            api.AddPageLink(this.ModManifest, "Single Smash", () => "Configure single item color and quality smashing");

            api.AddPage(this.ModManifest, "Single Smash");
            api.AddPageLink(this.ModManifest, "", () => "Back to main page");
            api.AddParagraph(this.ModManifest,() =>  "Single Smash is an alternative method of Color Smash and Quality Smash. It allows you to hold a keyboard key, then click an item in an inventory to smash color or quality.");
            api.AddParagraph(this.ModManifest, () => "When Color Smashing, the item will be smashed to \"default\" color. When Quality Smashing, the item will be reduced in quality by one step. Iridium -> Gold, Gold -> Silver, Silver -> Basic. You can confgure to reduce quality to base in one step.");

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable Single Smash Keybinds",
                tooltip: () => "Enable smashing single items in any inventory by holding a designated key (configured below), then left clicking the item",
                getValue: () => this.Config.EnableSingleItemSmashKeybinds,
                setValue: value => this.Config.EnableSingleItemSmashKeybinds = value
            );

            api.AddKeybind(
                mod: this.ModManifest, 
                name: () => "Color Smash Keybind", 
                tooltip: () => "Button to hold when you wish to color smash a single item", 
                getValue: () => this.Config.ColorSmashKeybind, 
                setValue: (SButton val) => this.Config.ColorSmashKeybind = val
            );

            api.AddKeybind(
                mod: this.ModManifest,
                name: () => "Quality Smash Keybind",
                tooltip: () => "Button to hold when you wish to quality smash a single item",
                getValue: () => this.Config.QualitySmashKeybind,
                setValue: (SButton val) => this.Config.QualitySmashKeybind = val
            );

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Single Smash Quality to base",
                tooltip: () => "Enable quality smashing of single item to base quality in one click.",
                getValue: () => this.Config.EnableSingleSmashToBaseQuality,
                setValue: value => this.Config.EnableSingleSmashToBaseQuality = value
            );

            api.AddPage(this.ModManifest, "Smash Filters");
            api.AddPageLink(this.ModManifest, "", () => "Back to main page");
            api.AddParagraph(this.ModManifest, () => "Star qualities selected here will be ignored by Quality Smash UNLESS exceptions are specified in the config. See the Exceptions config pages");


            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Ignore Iridium Quality Items",
                tooltip: () => "If enabled, iridium quality items will not be affected by \"Smash Quality\"",
                getValue: () => this.Config.IgnoreIridium,
                setValue: value => this.Config.IgnoreIridium = value
            );

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Ignore Gold Quality Items",
                tooltip: () => "If enabled, gold quality items will not be affected by \"Smash Quality\"",
                getValue: () => this.Config.IgnoreGold,
                setValue: value => this.Config.IgnoreGold = value
            );

            api.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Ignore Silver Quality Items",
                tooltip: () => "If enabled, silver quality items will not be affected by \"Smash Quality\"",
                getValue: () => this.Config.IgnoreSilver,
                setValue: value => this.Config.IgnoreSilver = value
            );

            api.AddPage(this.ModManifest, "Exceptions: Ignore Iridium by Item");
            api.AddPageLink(this.ModManifest, "", () => "Back to main page");
            api.AddParagraph(this.ModManifest, () => "Iridium quality items selected on this page WILL BE SMASHED by Smash Quality even if \"Ignore Iridium Quality Items\" is enabled");

            foreach (KeyValuePair<int, string> item in itemDictionary)
            {
                api.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => item.Value + " (" + item.Key + ")",
                    tooltip: () => "Smash iridium quality " + item.Value + " even if \"Ignore Iridium Quality Items\" is enabled",
                    getValue: () => Config.IgnoreIridiumItemExceptions.Contains(item.Key),
                    setValue: value => ModConfig.SyncConfigSetting(value, item.Key, Config.IgnoreIridiumItemExceptions)
                );
            }

            api.AddPage(this.ModManifest, "Exceptions: Ignore Iridium by Category");
            api.AddPageLink(this.ModManifest, "", () => "Back to main page");
            api.AddParagraph(this.ModManifest, () => "Iridium quality items that fall under a category selected on this page WILL BE SMASHED by Smash Quality even if \"Ignore Iridium Quality Items\" is enabled");

            foreach (KeyValuePair<int, string> item in categoryDictionary)
            {
                api.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => item.Value + " (" + item.Key + ")",
                    tooltip: () => "Smash iridium quality item within category " + item.Value + " even if \"Ignore Iridium Quality Items\" is enabled",
                    getValue: () => Config.IgnoreIridiumCategoryExceptions.Contains(item.Key),
                    setValue: value => ModConfig.SyncConfigSetting(value, item.Key, Config.IgnoreIridiumCategoryExceptions)
                );
            }

            api.AddPage(this.ModManifest, "Color Smash: Ignore Items");
            api.AddPageLink(this.ModManifest, "", () => "Back to main page");
            api.AddParagraph(this.ModManifest, () => "Items selected on this page will be ignored by Smash Colors");

            foreach (KeyValuePair<int, string> item in coloredItemDictionary)
            {
                api.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => item.Value + " (" + item.Key + ")",
                    tooltip: () => item.Value + " will be ignored when pressing the Color Smash button",
                    getValue: () => Config.IgnoreItemsColor.Contains(item.Key),
                    setValue: value => ModConfig.SyncConfigSetting(value, item.Key, Config.IgnoreItemsColor)
                );
            }

            api.AddPage(this.ModManifest, "Both Smash: Ignore by Item");
            api.AddPageLink(this.ModManifest, "", () => "Back to main page");
            api.AddParagraph(this.ModManifest, () => "Items selected on this page will be ignored by Smash Colors and Smash Quality");

            foreach (KeyValuePair<int, string> item in itemDictionary)
            {
                api.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => item.Value + " (" + item.Key + ")",
                    tooltip: () => item.Value + " will be ignored when pressing the Quality Smash or Color Smash buttons",
                    getValue: () => Config.IgnoreItemsQuality.Contains(item.Key),
                    setValue: value => ModConfig.SyncConfigSetting(value, item.Key, Config.IgnoreItemsQuality)
                );
            }

            api.AddPage(this.ModManifest, "Both Smash: Ignore by Category");
            api.AddPageLink(this.ModManifest, "", () => "Back to main page");
            api.AddParagraph(this.ModManifest, () => "Items under categories selected on this page will be ignored by Smash Colors and Smash Quality");

            foreach (KeyValuePair<int, string> category in categoryDictionary)
            {
                api.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => category.Value + " (" + category.Key + ")",
                    tooltip: () => "Items in category " + category.Value + " will be ignored when pressing the Quality Smash or Color Smash buttons",
                    getValue: () => Config.IgnoreItemsCategory.Contains(category.Key),
                    setValue: value => ModConfig.SyncConfigSetting(value, category.Key, Config.IgnoreItemsCategory)
                );
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            helper.Events.Display.MenuChanged += OnMenuChanged;

            colorTable = new GetBaseColors();
        }

        private void HookMenuEvents(bool hook)
        {
            if (hook && !EventsHooked)
            {
                EventsHooked = true;
                // RenderedActiveMenu has our buttons draw over other menu item hover text.
                // RenderingActiveMenu stops this but the buttons are dimmed. they still work.
                // RenderedWorld looks just like RenderingActiveMenu
                // ??? how to draw at the same "level" as the game without patching via Harmony.
                helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
                helper.Events.Input.ButtonPressed += OnButtonPressed;
                helper.Events.Input.ButtonReleased += OnButtonReleased;
                helper.Events.Input.CursorMoved += OnCursorMoved;
                //helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;            }
            }
            else if (!hook && EventsHooked)
            {
                EventsHooked = false;
                helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
                helper.Events.Input.ButtonPressed -= OnButtonPressed;
                helper.Events.Input.ButtonReleased -= OnButtonReleased;
                helper.Events.Input.CursorMoved -= OnCursorMoved;
                //helper.Events.GameLoop.UpdateTicking -= OnUpdateTicking;
            }
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            HookMenuEvents(false);
            helper.Events.Display.MenuChanged -= OnMenuChanged;
            colorTable = null;
        }

        private void UpdateHoverText()
        {
            Point scaledMousePos = Game1.getMousePosition(true);

            if (Config.EnableUISmashButtons)
                buttonSmashHandler.TryHover(GetValidButtonSmashMenu(), scaledMousePos.X, scaledMousePos.Y);
            if (Config.EnableSingleItemSmashKeybinds)
                singleSmashHandler.TryHover(GetValidKeybindSmashMenu(), scaledMousePos.X, scaledMousePos.Y);
        }

        /// <summary>
        /// Begins a check of whether a mouse click or button press was on a Smash button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (e.Button == Config.ColorSmashKeybind || e.Button == Config.QualitySmashKeybind)
            {
                UpdateHoverText();
                return;
            }

            if (e.Button != SButton.MouseLeft && e.Button != SButton.ControllerA)
                return;

            if (Config.EnableUISmashButtons && GetValidButtonSmashMenu() != null)
            {
                buttonSmashHandler.HandleClick(e);
            }

            if (Config.EnableSingleItemSmashKeybinds && GetValidKeybindSmashMenu() != null)
            {
                if (helper.Input.IsDown(Config.ColorSmashKeybind) || helper.Input.IsDown(Config.QualitySmashKeybind))
                {
                    singleSmashHandler.HandleClick(e);
                    helper.Input.Suppress(SButton.MouseLeft);
                    helper.Input.Suppress(SButton.ControllerA);
                }
            }
        }

        private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (e.Button == Config.ColorSmashKeybind || e.Button == Config.QualitySmashKeybind)
            {
                UpdateHoverText();
                return;
            }
        }

        //Attempt to smooth out button animations
        //private void OnUpdateTicking(object sender, UpdateTickingEventArgs e)
        //{
        //    //if (!Context.IsWorldReady) return;

        //    var menu = GetValidButtonSmashMenu();
        //    if (menu == null || !Config.EnableUISmashButtons)
        //        return;

        //    var scaledMousePos = Game1.getMousePosition(true);

        //    buttonSmashHandler.TryHover(menu, scaledMousePos.X, scaledMousePos.Y);
        //}

        private void OnCursorMoved(object sender, CursorMovedEventArgs e)
        {
            if (Context.IsWorldReady)
                UpdateHoverText();
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
#if !UseHarmony
            IClickableMenu menu = GetValidButtonSmashMenu();
            if ((menu != null) && Config.EnableUISmashButtons)
                buttonSmashHandler.DrawButtons(menu, e.SpriteBatch);
#endif

            if ((GetValidKeybindSmashMenu() != null) && Config.EnableSingleItemSmashKeybinds)
                singleSmashHandler.DrawHoverText(e.SpriteBatch);
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (Context.IsWorldReady)
            {
                // keep code out the the game loop unless a menu is active
                HookMenuEvents(IsValidSmashMenuAny(e.NewMenu));
                buttonSmashHandler.NewMenuActive(e.NewMenu);
            }
            else
            {
                //Monitor.Log("IsWorldReady=false in OnMenuChanged", LogLevel.Debug);
                HookMenuEvents(false);
                buttonSmashHandler.NewMenuActive(null);
            }
        }
    }
}
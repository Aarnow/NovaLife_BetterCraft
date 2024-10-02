using Life;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Helper.CraftHelper;
using ModKit.Interfaces;
using ModKit.Internal;
using ModKit.Utils;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using _menu = AAMenu.Menu;
using mk = ModKit.Helper.TextFormattingHelper;

namespace BetterCraft
{
    public class BetterRecipe : ModKit.ModKit
    {
        public int objectId = 1231; //carton désignant un objet
        public BetterRecipe(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            InsertMenu();
            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }

        public void InsertMenu()
        {
            _menu.AddAdminPluginTabLine(PluginInformations, 5, "BetterRecipe", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                BetterRecipePanel(player);
            });
        }

        public async void BetterRecipePanel(Player player)
        {
            var query = await CraftHelper.GetRecipes();

            Panel panel = PanelHelper.Create("BetterRecipe - Liste des recettes", UIPanel.PanelType.TabPrice, player, () => BetterRecipePanel(player));

            if (query != null && query.Count > 0)
            {
                foreach (var recipe in query)
                {
                    panel.AddTabLine($"{recipe.Name}", $"{recipe.Category}", recipe.IsVehicle ? VehicleUtils.GetIconId(recipe.ObjectId) : ItemUtils.GetIconIdByItemId(recipe.ObjectId), ui =>
                    {
                        BetterRecipeCreateOrUpdatePanel(player, recipe);
                    });
                }

                panel.NextButton("Modifier", () => panel.SelectTab());
            }
            else panel.AddTabLine("Aucune recettes enregistrée", _ => { });

            panel.AddButton("Ajouter", ui =>
            {
                Recipe recipe = new Recipe();
                recipe.ObjectId = 140;
                recipe.Name = "Plan: " + ItemUtils.GetItemById(140).itemName;
                recipe.Category = "CategoryName";
                recipe.IsVehicle = false;
                BetterRecipeCreateOrUpdatePanel(player, recipe);
            });
            panel.AddButton("Retour", _ => AAMenu.AAMenu.menu.AdminPluginPanel(player, AAMenu.AAMenu.menu.AdminPluginTabLines));
            panel.CloseButton();

            panel.Display();
        }
        public void BetterRecipeCreateOrUpdatePanel(Player player, Recipe recipe)
        {
            Panel panel = PanelHelper.Create("Ajouter/Modifier une recette", UIPanel.PanelType.TabPrice, player, () => BetterRecipeCreateOrUpdatePanel(player, recipe));
            
            panel.AddTabLine($"{recipe.Name}", $"{mk.Size(recipe.Category, 14)}", recipe.IsVehicle ? IconUtils.Vehicles.C4GrandPicasso.Id : ItemUtils.GetIconIdByItemId(objectId), _ =>
            {
                BetterRecipePropertiesPanel(player, recipe);
            });
            panel.AddTabLine($"Ajouter un ingrédient", _ =>
            {
                BetterRecipeCreateIngredientPanel(player, recipe);
            });
            if (recipe.LIngredientList != null)
            {
                foreach (var ingredient in recipe.LIngredientList)
                {
                    panel.AddTabLine($"{ItemUtils.GetItemById(ingredient.ItemId)?.itemName}", $"{ingredient.Count}", ItemUtils.GetIconIdByItemId(ingredient.ItemId), _ => 
                    {
                        recipe.LIngredientList.Remove(ingredient);
                        panel.Refresh();
                    });
                }
            }
            else panel.AddTabLine($"Aucun ingrédients", _ => {});

            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.NextButton("Sauvegarder", async () =>
            {
                if(recipe.LIngredientList.Count != 0)
                {
                    await CraftHelper.AddRecipe(recipe);
                    BetterRecipePanel(player);
                } else
                {
                    player.Notify("BetterCraft", "Recette incomplète", Life.NotificationManager.Type.Warning);
                }
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        public void BetterRecipePropertiesPanel(Player player, Recipe recipe)
        {
            Panel panel = PanelHelper.Create("Définir les propriétés de la recette", UIPanel.PanelType.Input, player, () => BetterRecipePropertiesPanel(player, recipe));

            panel.TextLines.Add("Respecter le format (cf. doc)");
            panel.TextLines.Add("= 0 objet / 1 = véhicule");
            panel.TextLines.Add("[0 ou 1] [ID] [CATEGORIE]");

            panel.inputPlaceholder = "exemple: 0 95 fast-food";

            panel.PreviousButtonWithAction("Valider", () =>
            {
                string input = panel.inputText;

                string regex = @"^(0|1)\s(\d+)\s([a-zA-Z0-9-]+)$";
                Match match = Regex.Match(input, regex);

                if (match.Success)
                {
                    recipe.IsVehicle = match.Groups[1].Value == "1";
                    recipe.ObjectId = int.Parse(match.Groups[2].Value);

                    if(recipe.ObjectId <= 0)
                    {
                        player.Notify("BetterCraft", "ID incorrect", Life.NotificationManager.Type.Warning);
                        return Task.FromResult(false);
                    }
                    if (recipe.IsVehicle && Nova.v.vehicleModels[recipe.ObjectId] == null)
                    {
                        player.Notify("BetterCraft", "ID de voiture inexistant", Life.NotificationManager.Type.Warning);
                        return Task.FromResult(false);
                    }
                    else if(!recipe.IsVehicle && Nova.man.item.GetItem(recipe.ObjectId) == null)
                    {
                        player.Notify("BetterCraft", "ID de l'objet inexistant", Life.NotificationManager.Type.Warning);
                        return Task.FromResult(false);
                    }

                    recipe.Name = recipe.IsVehicle ? "Plan: " + Nova.v.vehicleModels[recipe.ObjectId].vehicleName : "Plan: " + ItemUtils.GetItemById(recipe.ObjectId)?.itemName;
                    recipe.Category = match.Groups[3].Value;
                    return Task.FromResult(true);
                }
                else
                {
                    player.Notify("BetterCraft", "Format incorrect", Life.NotificationManager.Type.Warning);
                    return Task.FromResult(false);
                }
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        public void BetterRecipeCreateIngredientPanel(Player player, Recipe recipe)
        {
            Panel panel = PanelHelper.Create("Ajouter un ingrédient", UIPanel.PanelType.Input, player, () => BetterRecipeCreateIngredientPanel(player, recipe));

            panel.TextLines.Add("Respecter le format (cf. doc)");
            panel.TextLines.Add("[ID] [QUANTITÉ]");

            panel.inputPlaceholder = "exemple: 95 5";

            panel.PreviousButtonWithAction("Valider", () =>
            {
                string input = panel.inputText;

                string regex = @"^(\d+)\s(\d+)$";
                Match match = Regex.Match(input, regex);

                if (match.Success)
                {
                    Ingredient ingredient = new Ingredient();
                    ingredient.ItemId = int.Parse(match.Groups[1].Value);

                    if (ingredient.ItemId <= 0)
                    {
                        player.Notify("BetterCraft", "ID incorrect", Life.NotificationManager.Type.Warning);
                        return Task.FromResult(false);
                    }
                    if (!recipe.IsVehicle && Nova.man.item.GetItem(recipe.ObjectId) == null)
                    {
                        player.Notify("BetterCraft", "ID de l'objet inexistant", Life.NotificationManager.Type.Warning);
                        return Task.FromResult(false);
                    }

                    ingredient.Count = int.Parse(match.Groups[2].Value);

                    if(ingredient.Count <= 0)
                    {
                        player.Notify("BetterCraft", "Quantité incorrecte", Life.NotificationManager.Type.Warning);
                        return Task.FromResult(false);
                    }

                    recipe.LIngredientList.Add(ingredient);

                    return Task.FromResult(true);
                }
                else
                {
                    player.Notify("BetterCraft", "Format incorrect", Life.NotificationManager.Type.Warning);
                    return Task.FromResult(false);
                }
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

    }
}

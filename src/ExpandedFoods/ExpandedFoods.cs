using HarmonyLib;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace ExpandedFoods
{
    public class ExpandedFoods : ModSystem
    {
        Harmony harmony = new Harmony("com.jakecool19.expandedfoods.cookingoverhaul"); // remove private static and changed where the new harmony is..

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            CookingRecipe.NamingRegistry["augratin"] = new ExpandedFoodsRecipeNames();
            CookingRecipe.NamingRegistry["riceandbeans"] = new ExpandedFoodsRecipeNames();
            CookingRecipe.NamingRegistry["meatysalad"] = new ExpandedFoodsRecipeNames();
            CookingRecipe.NamingRegistry["yogurtmeal"] = new ExpandedFoodsRecipeNames();
            CookingRecipe.NamingRegistry["pastahot"] = new ExpandedFoodsRecipeNames();
            CookingRecipe.NamingRegistry["pastacold"] = new ExpandedFoodsRecipeNames();

            api.RegisterBlockClass("BlockMeatHooks", typeof(BlockMeatHooks));
            api.RegisterBlockEntityClass("MeatHooks", typeof(BlockEntityMeatHooks));

            api.RegisterBlockClass("BlockBottleRack", typeof(BlockBottleRack));
            api.RegisterBlockEntityClass("BottleRack", typeof(BlockEntityBottleRack));

            api.RegisterBlockClass("BlockMixingBowl", typeof(BlockMixingBowl));
            api.RegisterBlockEntityClass("MixingBowl", typeof(BlockEntityMixingBowl));

            api.RegisterBlockClass("BlockBottle", typeof(BlockBottle));
            api.RegisterBlockEntityClass("Bottle", typeof(BlockEntityBottle));

            api.RegisterBlockClass("BlockSpile", typeof(BlockSpile));
            api.RegisterBlockEntityClass("Spile", typeof(BlockEntitySpile));

            api.RegisterBlockClass("BlockSaucepan", typeof(BlockSaucepan));
            api.RegisterBlockEntityClass("Saucepan", typeof(BlockEntitySaucepan));

            api.RegisterBlockClass("BlockExpandedClayOven", typeof(BlockExpandedClayOven));
            api.RegisterBlockEntityClass("ExpandedOven", typeof(BlockEntityExpandedOven));

            api.RegisterItemClass("SuperFood", typeof(ItemSuperFood));
            api.RegisterItemClass("ExpandedRawFood", typeof(ItemExpandedRawFood));
            api.RegisterItemClass("ExpandedFood", typeof(ItemExpandedFood));
            api.RegisterItemClass("TransFix", typeof(ItemTransFix));
            api.RegisterItemClass("TransLiquid", typeof(ItemTransLiquid));
            api.RegisterItemClass("ExpandedLiquid", typeof(ItemExpandedLiquid));
            api.RegisterItemClass("ExpandedDough", typeof(ItemExpandedDough));
            api.RegisterItemClass("EggCrack", typeof(ItemEggCrack));

	    // removed harmony == null check
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Dispose()
        {
            harmony.UnpatchAll(harmony.Id);
            base.Dispose();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            
            api.RegisterCommand("efremap", "Remaps items in Expanded Foods", "",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    api.World.BlockAccessor.WalkBlocks(player.Entity.ServerPos.AsBlockPos.AddCopy(-10), player.Entity.ServerPos.AsBlockPos.AddCopy(10), (block, pos) => {

                        BottleFix(pos, block, api.World);
                });
                }, Privilege.chat);
        }

        public void BottleFix(BlockPos pos, Block block, IWorldAccessor world)
        {
            BlockEntityContainer bc;
            if (block.Code.Path.Contains("bottle") && !block.Code.Path.Contains("burned") && !block.Code.Path.Contains("raw"))
            {
                Block replacement = world.GetBlock(new AssetLocation(block.Code.Domain + ":bottle-" + block.FirstCodePart(1) + "-burned"));
                
                if (replacement != null) world.BlockAccessor.SetBlock(replacement.BlockId, pos);
            }
            else if ((bc = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer) != null)
            {
                foreach (ItemSlot slot in bc.Inventory)
                {
                    if (slot.Itemstack?.Block != null && slot.Itemstack.Block.Code.Path.Contains("bottle") && !slot.Itemstack.Block.Code.Path.Contains("burned") && !slot.Itemstack.Block.Code.Path.Contains("raw"))
                    {
                        Block replacement = world.GetBlock(new AssetLocation(slot.Itemstack.Block.Code.Domain + ":bottle-" + slot.Itemstack.Block.FirstCodePart(1) + "-burned"));
                        if (replacement != null)
                        {
                            slot.Itemstack = new ItemStack(replacement, slot.Itemstack.StackSize);
                        }
                    }
                }
            }
        }

    }

    
}

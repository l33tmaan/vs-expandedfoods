﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ExpandedFoods
{
    public class ItemEggCrack : ItemHoneyComb
    {
        public float ContainedEggLitres = 0.1f;

        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "eggInteractions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();

                foreach (Block block in api.World.Blocks)
                {
                    if (block.Code == null) continue;

                    if (CanSqueezeInto(block, null))
                    {
                        stacks.Add(new ItemStack(block));
                    }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "heldhelp-crack",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    }
                };
            });
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null || !byEntity.Controls.Sneak) return;

            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);

            var objectContents = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGroundStorage;
            if (objectContents != null)
		    {
                ItemSlot objectSourceSlot = objectContents.Inventory.FirstOrDefault(aslot => !aslot.Empty);
                if (objectSourceSlot != null)
                {
                    var objectSourceContents = objectSourceSlot.Itemstack?.Attributes?.GetTreeAttribute("contents").GetItemstack("0")?.Collectible.FirstCodePart(0); //grabs bowl liquid item's code
                    var objectYolkContents = objectSourceSlot.Itemstack?.Attributes?.GetTreeAttribute("contents").GetItemstack("0")?.Collectible.FirstCodePart(1); //grabs 1st variant in bowl liquid item

                    bool getCracking = false;
            
                    if (objectSourceContents == null)
                    {
                        if (CanSqueezeInto(block, blockSel.Position))
                        { getCracking = true; }
                    }

                    string eggType = slot.Itemstack.Collectible.FirstCodePart(0);   //grabs currently held item's code
                    string eggVariant = slot.Itemstack.Collectible.FirstCodePart(1);   //grabs 1st variant in currently held item

                    if (objectSourceContents == "eggwhiteportion" && ( eggType == "egg" || eggType == "limeegg") )
                    { getCracking = true; }
                    if ( (objectSourceContents == "eggyolkportion" && eggType == "eggyolk" ) && eggVariant == objectYolkContents  )
                    { getCracking = true; }
                    
                    if (getCracking)
                    {
                        handling = EnumHandHandling.PreventDefault;
                        if (api.World.Side == EnumAppSide.Client)
                        {
                            byEntity.World.PlaySoundAt(new AssetLocation("expandedfoods:sounds/player/eggcrack"), byEntity, null, true, 16, 0.5f);
                        }
                    }
                }
			}
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null || !byEntity.Controls.Sneak) return false;

            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform tf = new ModelTransform();
                tf.EnsureDefaultValues();
                
                tf.Translation.Set(Math.Min(0.6f, secondsUsed * 2), 0, 0); //-Math.Min(1.1f / 3, secondsUsed * 4 / 3f)
                tf.Rotation.Y = Math.Min(20, secondsUsed * 90 * 2f);

                if (secondsUsed > 0.4f)
                {
                    tf.Translation.X += (float)Math.Sin(secondsUsed * 30) / 10;
                }

                byEntity.Controls.UsingHeldItemTransformBefore = tf;
            }

            return secondsUsed < 2f;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null) return;
            if (secondsUsed < 1.9f) return;

            IWorldAccessor world = byEntity.World;

            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
            if (!CanSqueezeInto(block, blockSel.Position)) return;

            string eggType = slot.Itemstack.Collectible.FirstCodePart(0);   //grabs currently held item's code
            string eggVariant = slot.Itemstack.Collectible.FirstCodePart(1);   //grabs 1st variant in currently held item
			string eggWhiteLiquidAsset = "expandedfoods:eggwhiteportion";             //default liquid output
            string eggYolkOutput = "expandedfoods:eggyolk-" + eggVariant;       //searches for eggVariant and adds to eggyolk item
            string eggYolkLiquidAsset = "expandedfoods:eggyolkportion-" + eggVariant; //searches for eggVariant and adds to eggyolkportion item
			string eggShellOutput = "expandedfoods:eggshell";                    //default item output

			ItemStack eggWhiteStack = new ItemStack(world.GetItem(new AssetLocation(eggWhiteLiquidAsset)), 99999);
			ItemStack eggYolkStack = new ItemStack(world.GetItem(new AssetLocation(eggYolkLiquidAsset)), 99999);
			ItemStack stack = new ItemStack(world.GetItem(new AssetLocation(eggShellOutput)));

            BlockLiquidContainerTopOpened blockCnt = block as BlockLiquidContainerTopOpened;
            BlockEntityBucket blockInventory = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBucket;
            if (blockCnt != null || blockInventory != null)
            {                        
                if (eggType == "egg" || eggType == "limeegg")
					{
						blockCnt.TryPutLiquid(blockSel.Position, eggWhiteStack, ContainedEggLitres);
                    }
                else if (eggType == "eggyolk")
                    {
						blockCnt.TryPutLiquid(blockSel.Position, eggYolkStack, ContainedEggLitres);
                    }
                // if (blockCnt.TryPutLiquid(blockSel.Position, eggYolkStack, ContainedEggLitres) == 0) return;
            }
            else
            {   
                var beg = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGroundStorage;
                if (beg != null)
                {
                    ItemSlot sourceSlot = beg.Inventory.FirstOrDefault(aslot => !aslot.Empty);
                    var sourceContents = sourceSlot.Itemstack?.Attributes?.GetTreeAttribute("contents").GetItemstack("0");

                    if (sourceContents != null)
                    {
                        //there's already something in the BOWL
                        Debug.WriteLine(sourceContents.Collectible.Code.Path); //whats in the bowl?  eggwhite perhaps?
                        Debug.WriteLine(sourceContents.StackSize); //how much stuff exactly is in the bowl?
                    }
                    ItemSlot squeezeIntoSlot = beg.Inventory.FirstOrDefault(gslot => gslot.Itemstack?.Block != null && CanSqueezeInto(gslot.Itemstack.Block, null));
                    string containerItemPath = squeezeIntoSlot.Itemstack.Collectible.Code.Path;     //path of the container I'm looking at
                    if (squeezeIntoSlot != null)
                    {
                        blockCnt = squeezeIntoSlot.Itemstack.Block as BlockLiquidContainerTopOpened;
                        if (eggType == "egg" || eggType == "limeegg")
						{
							blockCnt.TryPutLiquid(squeezeIntoSlot.Itemstack, eggWhiteStack, ContainedEggLitres);
                        }
                        else if (eggType == "eggyolk")
                        {
							blockCnt.TryPutLiquid(squeezeIntoSlot.Itemstack, eggYolkStack, ContainedEggLitres);
                        }
					    beg.MarkDirty(true);
                    }
                }
            }



            slot.TakeOut(1);
            slot.MarkDirty();

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            if (eggType == "egg" || eggType == "limeegg")
            {
                stack = new ItemStack(world.GetItem(new AssetLocation(eggYolkOutput)));
            }
            if (byPlayer?.InventoryManager.TryGiveItemstack(stack) == false)
            {
                byEntity.World.SpawnItemEntity(stack, byEntity.SidedPos.XYZ);
            }
        }
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
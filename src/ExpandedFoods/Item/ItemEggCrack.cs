using System.Collections.Generic;
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

            if (CanSqueezeInto(block, blockSel.Position))
            {
                handling = EnumHandHandling.PreventDefault;
                if (api.World.Side == EnumAppSide.Client)
                {
                    byEntity.World.PlaySoundAt(new AssetLocation("expandedfoods:sounds/player/eggcrack"), byEntity, null, true, 16, 0.5f);
                }
            }
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null) return;
            if (secondsUsed < 1.9f) return;

            IWorldAccessor world = byEntity.World;

            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
            if (!CanSqueezeInto(block, blockSel.Position)) return;

            string heldItemPath = slot.Itemstack.Collectible.Code.Path;     //path of the item I'm currently cracking
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
            if (blockCnt != null)
            {
                if (blockCnt.TryPutLiquid(blockSel.Position, eggWhiteStack, ContainedEggLitres) == 0) return;
            }
            else
            {
                var beg = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGroundStorage;
                if (beg != null)
                {
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
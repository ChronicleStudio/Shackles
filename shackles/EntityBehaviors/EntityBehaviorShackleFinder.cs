using Shackles.Data;
using Shackles.Items;
using Shackles.ShackleSystems;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Shackles.EntityBehaviors
{
	public class EntityBehaviorShackleFinder : EntityBehavior
	{
		public EntityBehaviorShackleFinder(Entity entity) : base(entity) {
			Prison = entity?.Api.ModLoader.GetModSystem<ShacklesModSystem>().Prison;
			Tracker = entity?.Api.ModLoader.GetModSystem<ShackleTrackerModSystem>();
		}

		public override string PropertyName() => "ShackleFinder";

		private ShackleTrackerModSystem Tracker;
		private PrisonController Prison;

		private FullTrackData fullTrackData;
		private long id;

		public float accum = 0;
		public float slowaccum = 0;

		private BlockPos Pos => entity?.Pos.AsBlockPos;
			

		//public override void OnEntityRevive()
		//{
		//    base.OnEntityRevive();

		//    if(entity.Api.Side == EnumAppSide.Server && Tracker.IsShackled(entity)) {
		//        IInventory inv = (entity as EntityPlayer).GearInventory;
		//        ItemStack stack = new ItemStack(entity.Api.World.GetItem(new AssetLocation("game", "clothes-arm-prisoner-binds")));
		//        if (stack != null)
		//        {
		//            Enum.TryParse<EnumCharacterDressType>(stack.ItemAttributes["clothescategory"].AsString(), ignoreCase: true, out var dresstype);

		//            inv[(int)dresstype].Itemstack = stack;
		//            inv[(int)dresstype].MarkDirty();
		//        }

		//    }
		//}

		public override void Initialize(EntityProperties properties, JsonObject attributes)
		{
			base.Initialize(properties, attributes);
			if (!entity.World.Side.IsServer())
			{
				return;
			}
			id = entity.World.RegisterGameTickListener(delegate
			{
				(entity as EntityPlayer).WalkInventory(delegate (ItemSlot slot)
				{
					if (!(slot is ItemSlotCreative) && slot.Itemstack?.Item is ItemShackle)
					{
						((ItemShackle)slot.Itemstack.Item).UpdateFuelState(entity.World, slot);
						string text = slot.Itemstack?.Attributes.GetString("pearled_uid");
						if (text != null)
						{
							fullTrackData = Tracker?.GetTrackData(text);
							if (fullTrackData != null && !fullTrackData.IsNull && Pos != null)
							{
								fullTrackData.SetLocation(Pos);
								fullTrackData.SlotReference.InventoryID = slot.Inventory.InventoryID;
								fullTrackData.SlotReference.SlotID = slot.Inventory.GetSlotId(slot);
								fullTrackData.LastHolderUID = ((EntityPlayer)entity).PlayerUID;
							}
						}
					}
					return true;
				});
				Tracker?.SaveTrackToDB();
			}, 500);
			entity.WatchedAttributes.RegisterModifiedListener("entityDead", delegate
			{
				if (Tracker.IsShackled(entity))
				{
					Prison.MoveToCell(entity);
				}
			});
		}

		public override void OnEntityDespawn(EntityDespawnData despawn)
		{
			base.OnEntityDespawn(despawn);
			entity.World.UnregisterGameTickListener(id);
		}

		public override void OnGameTick(float deltaTime)
		{
			base.OnGameTick(deltaTime);


			accum += deltaTime;
			slowaccum += deltaTime;

			if (accum > 0.5)
			{
				if ((entity as EntityAgent).Controls.Sneak)
				{
					EmitParticles();
				}

				accum = 0.0f;
			}

			if (slowaccum > 3.0)
			{
				if (entity.Api.Side == EnumAppSide.Client)
				{
					if (entity.WatchedAttributes.GetBool("Shackled") == true)
					{
						entity.StartAnimation("holdbothhands");
					}
					else
					{
						entity.StopAnimation("holdbothhands");
					}
				}

				slowaccum = 0.0f;
			}



		}

		private readonly SimpleParticleProperties _particles = new SimpleParticleProperties
		{
			MinPos = Vec3d.Zero,
			AddPos = new Vec3d(0.2, 0.2, 0.2),
			MinVelocity = Vec3f.Zero,
			AddVelocity = Vec3f.Zero,
			RandomVelocityChange = true,
			Bounciness = 0.1f,
			GravityEffect = 0f,
			WindAffected = false,
			WithTerrainCollision = true,
			MinSize = 0.3f,
			MaxSize = 0.8f,
			MinQuantity = 5f,
			AddQuantity = 5f,
			LifeLength = 0.5f,
			VertexFlags = 100,
			ParticleModel = EnumParticleModel.Quad
		};

		private static int GetRandomColor(Random rand)
		{
			int a = 255;
			int r = rand.Next(200, 256);
			int g = rand.Next(100, 156);
			int b = rand.Next(0, 56);
			return ColorUtil.ToRgba(a, r, g, b);
		}

		private void EmitParticles()
		{
			if (entity.Api.Side == EnumAppSide.Server && Tracker.IsShackled(entity) && fullTrackData != null)
			{
				entity.WatchedAttributes.SetBool("Shackled", true);
				entity.WatchedAttributes.SetBlockPos("Shackle", fullTrackData.LastPos);
			}

			if (entity.Api.Side == EnumAppSide.Server && !Tracker.IsShackled(entity))
			{
				entity.WatchedAttributes.SetBool("Shackled", false);
			}

			Vec3d vec3d = entity.WatchedAttributes.GetBlockPos("Shackle")?.ToVec3d();
			bool locShackled = entity.WatchedAttributes.GetBool("Shackled");
			if (entity.Api.Side == EnumAppSide.Client && vec3d != null)
			{
				vec3d = vec3d.Add(0.5, 0.0, 0.5);
				Vec3d vec3d2 = entity.SidedPos.AheadCopy(1.0).XYZ.Add(0.0, entity.LocalEyePos.Y, 0.0);
				Vec3d vec3d3 = vec3d - vec3d2;

				_particles.AddQuantity = GameMath.Clamp(10f / _particles.MinVelocity.Length() * 1.0f, 0.05f, 10f);
				_particles.MinVelocity = vec3d3.ToVec3f() / (_particles.LifeLength * 3f);
				_particles.MinPos = vec3d2;
				_particles.AddPos = _particles.MinVelocity.ToVec3d() * 0.1;
				_particles.MinSize = GameMath.Clamp(_particles.MinVelocity.Length() * 0.01f, 0.05f, 10f);
				_particles.MaxSize = _particles.MinSize * 2f;
				_particles.Color = GetRandomColor(entity.Api.World.Rand);
				entity.Api.World.SpawnParticles(_particles);


			}

		}

	}
}

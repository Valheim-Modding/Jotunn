using System;
using System.Linq;
using HarmonyLib;
using Jotunn.Entities;
using UnityEngine;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for adding custom creature spawns.<br />
    ///     Use this in a constructor of <see cref="CustomCreature"/> 
    /// </summary>
    public class CreatureConfig
    {
        /// <summary>
        ///     The unique name for your custom creature. May be tokenized.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     Group tag of this creature.<br/>
        ///     Creatures in the same group don't attack each other, regardless of faction.
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        ///     <see cref="Character.Faction"/> of this creature.
        /// </summary>
        public Character.Faction? Faction { get; set; }

        // /// <summary>
        // ///     Set true to make this creature tameable.<br/>
        // ///     Jötunn automatically adds the <see cref="MonsterAI"/> and <see cref="Tameable"/> components if needed.<br/>
        // ///     Tameable creatures must have the <see cref="Humanoid"/> component attached, which won't be added automatically.
        // /// </summary>
        // public bool MakeTameable { get; set; }
        //
        // /// <summary>
        // ///     When <see cref="MakeTameable"/> is true use this to also make the creature commandable.
        // /// </summary>
        // public bool MakeCommandable { get; set; }
        
        /// <summary>
        ///     If set to true, <see cref="LevelEffects"/> stack the "EnableObject" action for all levels
        ///     instead of only activating the GameObject of the highest level matched.
        /// </summary>
        public bool UseCumulativeLevelEffects { get; set; }

        /// <summary>
        ///     Array of <see cref="DropConfig">DropConfigs</see> to use for this creature's <see cref="CharacterDrop"/>.<br/>
        ///     A <see cref="CharacterDrop"/> component will automatically be added if not present.<br/>
        ///     The drop table of an existing component will be replaced.
        /// </summary>
        public DropConfig[] DropConfigs = Array.Empty<DropConfig>();

        /// <summary>
        ///     Array of <see cref="SpawnConfig">SpawnConfigs</see> used for world spawns of your custom creature.<br/>
        ///     Leave empty if you don't want your creature to spawn in the world automatically.<br/>
        /// </summary>
        public SpawnConfig[] SpawnConfigs = Array.Empty<SpawnConfig>();

        /// <summary>
        ///     String array of items this creature can consume to use in the <see cref="MonsterAI"/> component.<br/>
        ///     Jötunn will try to resolve all strings to <see cref="ItemDrop">ItemDrops</see> at runtime.<br/>
        ///     An existing consumeItems table will be replaced.
        /// </summary>
        public string[] Consumables = Array.Empty<string>();

        /// <summary>
        ///     Apply this config's values to a creature GameObject.
        /// </summary>
        /// <param name="prefab">Prefab to apply this config to</param>
        public void Apply(GameObject prefab)
        {
            if (!prefab.TryGetComponent(out Character character))
            {
                Logger.LogError($"GameObject {prefab.name} has no Character component attached");
                return;
            }

            // if (MakeTameable && !prefab.GetComponent<Humanoid>())
            // {
            //     Logger.LogError($"GameObject {prefab.name} has no Humanoid component attached, can't make it tameable");
            //     return;
            // }

            if (!string.IsNullOrEmpty(Name))
            {
                character.m_name = Name;
            }

            if (string.IsNullOrEmpty(character.m_name))
            {
                character.m_name = prefab.name;
            }

            if (!string.IsNullOrEmpty(Group))
            {
                character.m_group = Group;
            }

            if (Faction.HasValue)
            {
                character.m_faction = Faction.Value;
            }

            /*if (MakeTameable)
            {
                if (prefab.TryGetComponent(out AnimalAI animalAi))
                {
                    if (prefab.TryGetComponent(out ZNetView zNetView))
                    {
                        zNetView.Unregister("Alert");
                        zNetView.Unregister("OnNearProjectileHit");
                    }

                    animalAi.CancelInvoke("DoIdleSound");

                    Object.Destroy(animalAi);
                }

                if (!prefab.TryGetComponent(out MonsterAI monsterAi))
                {
                    monsterAi = prefab.AddComponent<MonsterAI>();
                }

                if (!prefab.TryGetComponent(out Tameable tameable))
                {
                    tameable = prefab.AddComponent<Tameable>();
                }

                tameable.m_monsterAI ??= monsterAi;
                monsterAi.m_tamable ??= tameable;
                
                if (MakeCommandable)
                {
                    tameable.m_commandable = true;
                }
            }*/

            var drops = GetDrops().ToList();
            if (drops.Any())
            {
                var comp = prefab.GetOrAddComponent<CharacterDrop>();
                comp.m_drops = drops;
            }

            var consumeItems = GetConsumeItems().ToList();
            if (consumeItems.Any() && prefab.TryGetComponent(out MonsterAI ai))
            {
                ai.m_consumeItems = consumeItems;
            }
        }

        /// <summary>
        ///     Converts the <see cref="DropConfig">DropConfigs</see> to Valheim style <see cref="CharacterDrop.Drop"/> array.
        /// </summary>
        /// <returns>The Valheim <see cref="CharacterDrop.Drop"/> array</returns>
        public CharacterDrop.Drop[] GetDrops()
        {
            CharacterDrop.Drop[] drops = new CharacterDrop.Drop[DropConfigs.Length];

            for (int i = 0; i < drops.Length; i++)
            {
                drops[i] = DropConfigs[i].GetDrop();
            }

            return drops;
        }

        /// <summary>
        ///     Converts the <see cref="SpawnConfig">SpawnConfigs</see> to Valheim style <see cref="SpawnSystem.SpawnData"/> array.
        /// </summary>
        /// <returns>The Valheim <see cref="SpawnSystem.SpawnData"/> array</returns>
        public SpawnSystem.SpawnData[] GetSpawns()
        {
            SpawnSystem.SpawnData[] spawns = new SpawnSystem.SpawnData[SpawnConfigs.Length];

            for (int i = 0; i < spawns.Length; i++)
            {
                spawns[i] = SpawnConfigs[i].GetSpawnData();
            }

            return spawns;
        }

        /// <summary>
        ///     Creates an array of <see cref="ItemDrop"/> mocks for the consumeItem list of the creature.
        /// </summary>
        /// <returns>An array of <see cref="ItemDrop"/> mocks</returns>
        public ItemDrop[] GetConsumeItems()
        {
            ItemDrop[] itemDrops = new ItemDrop[Consumables.Length];

            for (int i = 0; i < itemDrops.Length; i++)
            {
                itemDrops[i] = Mock<ItemDrop>.Create(Consumables[i]);
            }

            return itemDrops;
        }

        /// <summary>
        ///     Appends a new <see cref="DropConfig"/> to the array of existing ones.
        /// </summary>
        /// <param name="dropConfig"></param>
        public void AddDropConfig(DropConfig dropConfig)
        {
            DropConfigs = DropConfigs.AddToArray(dropConfig);
        }

        /// <summary>
        ///     Appends a new <see cref="SpawnConfig"/> to the array of existing ones.
        /// </summary>
        /// <param name="spawnConfig"></param>
        public void AddSpawnConfig(SpawnConfig spawnConfig)
        {
            SpawnConfigs = SpawnConfigs.AddToArray(spawnConfig);
        }

        /// <summary>
        ///     Appends a new consumable to the array of existing ones.
        /// </summary>
        /// <param name="consumable"></param>
        public void AddConsumable(string consumable)
        {
            Consumables = Consumables.AddToArray(consumable);
        }
    }
}

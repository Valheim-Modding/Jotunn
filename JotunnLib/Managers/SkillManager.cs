using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Utils;
using UnityEngine;

namespace Jotunn.Managers
{
    /// <summary>
    ///    Manager for handling custom skills added to the game.
    /// </summary>
    public class SkillManager : IManager
    {
        private static SkillManager _instance;
        /// <summary>
        ///     Global singleton instance of the manager.
        /// </summary>
        public static SkillManager Instance => _instance ??= new SkillManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private SkillManager() { }

        static SkillManager()
        {
            ((IManager)Instance).Init();
        }

        private bool addedSkillsToTerminal = false;

        /// <summary>
        ///     Initialize the manager
        /// </summary>
        void IManager.Init()
        {
            Main.LogInit("SkillManager");
            Main.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(Skills), nameof(Skills.Awake)), HarmonyPostfix]
            private static void RegisterCustomSkills(Skills __instance) => Instance.RegisterCustomSkills(__instance);

            [HarmonyPatch(typeof(Skills), nameof(Skills.IsSkillValid)), HarmonyPostfix]
            private static void Skills_IsSkillValid(Skills __instance, Skills.SkillType type, ref bool __result) => Instance.Skills_IsSkillValid(__instance, type, ref __result);

            [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkill)), HarmonyPrefix]
            private static void Skills_GetSkill(Skills __instance, ref Skills.SkillType skillType) => Instance.Skills_GetSkill(__instance, ref skillType);

            [HarmonyPatch(typeof(Skills), nameof(Skills.CheatRaiseSkill)), HarmonyPrefix]
            private static bool Skills_CheatRaiseSkill(Skills __instance, string name, float value) => Instance.Skills_CheatRaiseSkill(__instance, name, value);

            [HarmonyPatch(typeof(Skills), nameof(Skills.CheatResetSkill)), HarmonyPrefix]
            private static bool Skills_CheatResetSkill(Skills __instance, string name) => Instance.Skills_CheatResetSkill(__instance, name);

            [HarmonyPatch(typeof(Terminal), nameof(Terminal.Awake)), HarmonyPostfix]
            private static void Terminal_InitTerminal() => Instance.AddSkillsToTerminal();
        }

        internal Dictionary<Skills.SkillType, SkillConfig> CustomSkills = new Dictionary<Skills.SkillType, SkillConfig>();

        /// <summary>
        ///     Add a new skill with given SkillConfig object.
        /// </summary>
        /// <param name="skillConfig">SkillConfig object representing new skill to register</param>
        /// <returns>The SkillType of the newly added skill</returns>
        public Skills.SkillType AddSkill(SkillConfig skillConfig)
        {
            if (string.IsNullOrEmpty(skillConfig?.Identifier))
            {
                Logger.LogError($"Failed to register skill with invalid identifier: {skillConfig?.Identifier}");
                return global::Skills.SkillType.None;
            }

            CustomSkills.Add(skillConfig.UID, skillConfig);

            return skillConfig.UID;
        }

        /// <summary>
        ///     Register a new skill with given parameters, and registers translations for it in the current localization.
        /// </summary>
        /// <param name="identifer">Unique identifier of the new skill, ex: "com.jotunn.testmod.testskill"</param>
        /// <param name="name">Name of the new skill</param>
        /// <param name="description">Description of the new skill</param>
        /// <param name="increaseStep"></param>
        /// <param name="icon">Icon for the skill</param>
        /// <returns>The SkillType of the newly registered skill</returns>
        [Obsolete("Use AddSkill(SkillConfig) instead")]
        public Skills.SkillType AddSkill(
            string identifer,
            string name,
            string description,
            float increaseStep = 1f,
            Sprite icon = null)
        {
            return AddSkill(new SkillConfig
            {
                Identifier = identifer,
                Name = name,
                Description = description,
                IncreaseStep = increaseStep,
                Icon = icon
            });
        }

        /// <summary>
        ///     Adds skills defined in a JSON file at given path, relative to BepInEx/plugins
        /// </summary>
        /// <param name="path">JSON file path, relative to BepInEx/plugins folder</param>
        public void AddSkillsFromJson(string path)
        {
            string json = AssetUtils.LoadText(path);

            if (string.IsNullOrEmpty(json))
            {
                Logger.LogError($"Failed to load skills from json: {path}");
                return;
            }

            List<SkillConfig> skills = SkillConfig.ListFromJson(json);

            foreach (SkillConfig skill in skills)
            {
                AddSkill(skill);
            }
        }

        /// <summary>
        ///     Gets a custom skill with given SkillType.
        /// </summary>
        /// <param name="skillType">SkillType to look for</param>
        /// <returns>Custom skill with given SkillType</returns>
        public Skills.SkillDef GetSkill(Skills.SkillType skillType)
        {
            if (CustomSkills.ContainsKey(skillType))
            {
                return CustomSkills[skillType].ToSkillDef();
            }

            return Player.m_localPlayer?.GetSkills()?.m_skills?
                .FirstOrDefault(skill => skill.m_skill == skillType);
        }

        /// <summary>
        ///     Gets a custom skill with given skill identifier.
        /// </summary>
        /// <param name="identifier">String indentifer of SkillType to look for</param>
        /// <returns>Custom skill with given SkillType</returns>
        public Skills.SkillDef GetSkill(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return null;
            }

            return GetSkill((Skills.SkillType)Math.Abs(identifier.GetStableHashCode()));
        }

        private void RegisterCustomSkills(Skills self)
        {
            if (CustomSkills.Count > 0)
            {
                Logger.LogInfo($"Registering {CustomSkills.Count} custom skills");

                foreach (var skill in CustomSkills.Values)
                {
                    Localization.instance.AddWord($"skill_{skill.UID}", skill.LocalizedName);
                    self.m_skills.Add(skill.ToSkillDef());
                    Logger.LogDebug($"Registered skill {skill.Name} | ID: {skill.Identifier}");
                }
            }
        }

        private void Skills_IsSkillValid(Skills self, Skills.SkillType skillType, ref bool result)
        {
            result = result || CustomSkills.ContainsKey((Skills.SkillType)Math.Abs((int)skillType));
        }

        private void Skills_GetSkill(Skills self, ref Skills.SkillType skillType)
        {
            // Fix the mess of whoever decided to have negative skill IDs and implement several workarounds...
            var abs = (Skills.SkillType)Math.Abs((int)skillType);

            if ((int)skillType < 0 && CustomSkills.ContainsKey(abs))
            {
                skillType = abs;
            }
        }

        private bool Skills_CheatRaiseSkill(Skills self, string name, float value)
        {
            foreach (var config in CustomSkills.Values)
            {
                if (config.IsFromName(name))
                {
                    Skills.Skill skill = self.GetSkill(config.UID);
                    var localizedName = config.LocalizedName;

                    skill.m_level += value;
                    skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
                    self.m_player.Message(MessageHud.MessageType.TopLeft, $"Skill increased {localizedName}: {(int)skill.m_level}", 0, skill.m_info.m_icon);

                    Console.instance.Print($"Skill {localizedName} = {skill.m_level}");
                    Logger.LogDebug($"Raised skill {localizedName} to {skill.m_level}");

                    return false;
                }
            }

            return true;
        }

        private bool Skills_CheatResetSkill(Skills self, string name)
        {
            foreach (var config in CustomSkills.Values)
            {
                if (config.IsFromName(name))
                {
                    self.m_player.GetSkills().ResetSkill(config.UID);
                    Console.instance.Print("Skill " + config.LocalizedName + " reset");
                    Logger.LogDebug($"Reset skill {config.Name}");
                    return false;
                }
            }

            return true;
        }

        private void AddSkillsToTerminal()
        {
            if (!Terminal.m_terminalInitialized || addedSkillsToTerminal)
            {
                return;
            }

            addedSkillsToTerminal = true;

            AddSkillOptions("raiseskill");
            AddSkillOptions("resetskill");
        }

        private void AddSkillOptions(string commandName)
        {
            if (Terminal.commands.TryGetValue(commandName, out Terminal.ConsoleCommand command))
            {
                var fetcher = command.m_tabOptionsFetcher;

                command.m_tabOptionsFetcher = () =>
                {
                    List<string> options = fetcher();
                    options.AddRange(CustomSkills.Values.Select(skill => skill.LocalizedName));
                    return options;
                };
            }
            else
            {
                Logger.LogWarning($"Failed to find {commandName} command");
            }
        }
    }
}

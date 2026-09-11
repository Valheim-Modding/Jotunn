using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using UnityEngine;

namespace Jotunn.Settings
{
    /// <summary>
    ///     BepInEx setting for a list of requirements.<br />
    ///     Used for e.g. items and pieces.
    /// </summary>
    public class BepInExRequirements : BepInExSetting<List<RequirementConfig>, string>
    {
        /// <summary>
        ///     Creates a new setting object for a list of requirements.
        /// </summary>
        public BepInExRequirements(BepInPlugin sourceMod, string section, string key, List<RequirementConfig> defaultValue, string description, int order, bool adminOnly = true) : base(sourceMod, section, key, defaultValue, description, order, adminOnly)
        {
        }

        /// <inheritdoc/>
        public override string Serialize(List<RequirementConfig> value)
        {
            return string.Join(",", value.ConvertAll(x => $"{x.Item}:{x.Amount}:{x.Recover}"));
        }

        /// <inheritdoc/>
        public override List<RequirementConfig> Deserialize(string value)
        {
            var requirements = new List<RequirementConfig>();
            var items = value.Split(',');

            foreach (var item in items)
            {
                var parts = item.Split(':');
                requirements.Add(new RequirementConfig
                {
                    Item = parts.Length > 0 ? parts[0] : string.Empty,
                    Amount = parts.Length > 1 && int.TryParse(parts[1], out var amount) ? amount : 1,
                    Recover = parts.Length > 2 && bool.TryParse(parts[2], out var recover) ? recover : true
                });
            }

            return requirements;
        }

        /// <inheritdoc/>
        protected override ConfigurationManagerAttributes GenerateAttributes()
        {
            ConfigurationManagerAttributes attributes = base.GenerateAttributes();
            attributes.CustomDrawer = Drawer;
            return attributes;
        }

        /// <summary>
        ///     Custom Config Manager drawer for the requirements setting.<br />
        /// </summary>
        protected virtual void Drawer(ConfigEntryBase entryBase)
        {
            GUILayout.BeginVertical();

            bool readOnly = entry.GetConfigurationManagerAttributes()?.ReadOnly ?? false;
            List<RequirementConfig> requirements = new List<RequirementConfig>();
            bool updated = false;

            foreach (var requirement in Deserialize((string)entryBase.BoxedValue))
            {
                GUILayout.BeginHorizontal();

                var amount = GUILayout.TextField(requirement.Amount.ToString(), GUILayout.Width(40));
                var item = GUILayout.TextField(requirement.Item, GUILayout.ExpandWidth(true));
                var recover = GUILayout.Toggle(requirement.Recover, "Recover", GUILayout.Width(67));

                if (requirement.Item != item || requirement.Amount.ToString() != amount || requirement.Recover != recover)
                {
                    updated = true;
                }

                if (GUILayout.Button("x", GUILayout.Width(21)))
                {
                    updated = true;
                }
                else
                {
                    requirements.Add(new RequirementConfig
                    {
                        Item = item,
                        Amount = int.TryParse(amount, out var parsedAmount) ? parsedAmount : 1,
                        Recover = recover
                    });
                }

                if (GUILayout.Button("+", GUILayout.Width(21)))
                {
                    requirements.Add(new RequirementConfig());
                    updated = true;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            if (updated && !readOnly)
            {
                entryBase.BoxedValue = Serialize(requirements);
            }
        }
    }
}

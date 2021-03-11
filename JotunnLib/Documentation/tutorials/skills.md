# Creating custom skills
Creation of custom skills is done through the `SkillManager` singleton class.
This will automatically take care of incrementing the skill's SkillType (unique numerical ID), so there will be no conflicts between skills added by various mods.

### Usage
To create a new skill, you must call the RegisterSkill function.

This should be called from within your mod's `Awake` method, and it will return a randomly generated SkillType for your new skill.
```cs
namespace SimpleMounts
{
    [BepInPlugin("com.bepinex.plugins.simple-mounts", "Simple Mounts", "0.0.1")]
    [BepInDependency("com.bepinex.plugins.jotunnlib")]
    public class SimpleMounts : BaseUnityPlugin
    {
        public static Skills.SkillType RidingSkillType = 0;

        void Awake()
        {
            RidingSkillType = SkillManager.Instance.RegisterSkill("Riding", "Ride animals");
        }
    }
}
```
# Transitioning from SkillInjector
If you're familiar with skillInjector, then you'll find that Jötunn offers very similar capabilities.

## Adding skills in SkillInjector
In SkillInjector, you'd typically add a skill by doing this:
```cs
const int SKILL_TYPE = 299;

void Awake()
{
    SkillInjector.RegisterNewSkill(SKILL_TYPE, "MyCoolSkill", "Doing Cool Stuff", 1.0f, null, Skills.SkillType.Unarmed);
}
```
(code sample taken from SkillInjector Nexus page).

## Adding skills in Jötunn
To add a skill in Jötunn, you would do something very similar:

```cs
public static Skills.SkillType TestSkillType = 0;

private void Awake()
{
    TestSkillType = SkillManager.Instance.AddSkill(new SkillConfig
    {
        Identifier = "com.jotunn.JotunnModExample.testskill",
        Name = "TestingSkill",
        Description = "A nice testing skill!",
        IncreaseStep = 1f
        Icon = null,
    });
}
```

The major difference here is that you **do not provide your own numeric ID**. In order to avoid conflicts between mods, you simply provide an Identifier string, and Jötunn will create a unique integer identifier for you based on the string provided. This makes it easier for mod devs to create unique identifiers, and should avoid collisions. After the skill is created, this method will return the newly generated integer ID (a `Skills.SkillType`) to use however you wish. 

For more info, please check out the [tutorials](../../tutorials/skills.md) section on skills.
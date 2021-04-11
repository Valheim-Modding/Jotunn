# Registering custom skills
Creation of custom skills is done through the [SkillManager](xref:JotunnLib.Managers.SkillManager) singleton.
This will automatically take care of incrementing the skill's SkillType (unique numerical ID), so there will be no conflicts between skills added by various mods.

## Example
To create a new skill, you must call the AddSkill function.

This should be called from within your mod's `Awake` method, and it will return a randomly generated SkillType for your new skill.
```cs
void addSkills()
{
    // Test adding a skill with a texture
    Texture2D testSkillTex = AssetUtils.LoadTexture("JotunnModExample/Assets/test_tex.jpg");
    Sprite testSkillSprite = Sprite.Create(testSkillTex, new Rect(0f, 0f, testSkillTex.width, testSkillTex.height), Vector2.zero);
    TestSkillType = SkillManager.Instance.AddSkill("com.jotunnlib.JotunnModExample.testskill", "TestingSkill", "A nice testing skill!", 1f, testSkillSprite);
}
```

This

_Note: Unless you actually have any levels in your skill, it won't show up in the skils menu. You must first raise your skill (either using a command, or actually implementing a way to earn levels) in order to be able to see it_

This is what it should look like in game:

![Our Skill in Game](../../images/data/test-skill.png "Our Skill in Game")
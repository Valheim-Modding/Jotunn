using System;
using UnityEngine;
using BepInEx;
using TestMod.ConsoleCommands;
using JotunnLib;
using JotunnLib.ConsoleCommands;
using JotunnLib.Managers;
using JotunnLib.Utils;

namespace TestMod
{
    [BepInPlugin("com.bepinex.plugins.jotunnlib.testmod", "JotunnLib Test Mod", "0.0.1")]
    [BepInDependency("com.bepinex.plugins.jotunnlib")]
    class TestMod : BaseUnityPlugin
    {
        public static Skills.SkillType TestSkillType = 0;
        private bool showMenu = false;

        private void Awake()
        {
            initCommands();
            initSkills();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                showMenu = !showMenu;
            }
        }

        void OnGUI()
        {
            if (showMenu)
            {
                GUI.Box(new Rect(40, 40, 100, 400), "TestMod");
            }
        }

        private void initCommands()
        {
            CommandManager.Instance.RegisterConsoleCommand(new PrintItemsCommand());
            CommandManager.Instance.RegisterConsoleCommand(new TpCommand());
            CommandManager.Instance.RegisterConsoleCommand(new ListPlayersCommand());
            CommandManager.Instance.RegisterConsoleCommand(new SkinColorCommand());
            CommandManager.Instance.RegisterConsoleCommand(new RaiseSkillCommand());
            CommandManager.Instance.RegisterConsoleCommand(new BetterSpawnCommand());
        }


        void initSkills()
        {
            // Test adding a skill with a texture
            Texture2D testSkillTex = AssetUtils.LoadTexture("TestMod/Assets/test_skill.jpg");
            Sprite testSkillSprite = Sprite.Create(testSkillTex, new Rect(0f, 0f, testSkillTex.width, testSkillTex.height), Vector2.zero);
            TestSkillType = SkillManager.Instance.RegisterSkill("Testing", "A nice testing skill", 1, testSkillSprite);
        }
    }
}

using UnityEngine;
using BepInEx;
using TestMod.ConsoleCommands;
using JotunnLib;
using JotunnLib.ConsoleCommands;
using JotunnLib.Managers;
using System;

namespace TestMod
{
    [BepInPlugin("com.bepinex.plugins.loki-loader.testmod", "Loki Loader Test Mod", "0.0.1")]
    [BepInDependency("com.bepinex.plugins.loki-loader")]
    class TestMod : BaseUnityPlugin
    {
        public static TestMod Instance { get; private set; }
        private bool keybindsOn = false;

        void Awake()
        {
            Instance = this;

            initCommands();
            initSkills();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
            {

            }

            if (Input.GetKeyDown(KeyCode.Home))
            {
                keybindsOn = !keybindsOn;
            }

            // Spawn a log and shoot it :)))
            if (keybindsOn && Input.GetKeyDown(KeyCode.H))
            {
                Vector3 pos = Player.m_localPlayer.transform.position + Vector3.up * 5;
                Quaternion rot = Player.m_localPlayer.transform.rotation;
                GameObject log = UnityEngine.Object.Instantiate(PrefabManager.Instance.GetPrefab("beech_log"), pos, rot);
                
                log.transform.Rotate(new Vector3(90f, 0f, 0f));
                log.GetComponent<Rigidbody>().velocity = Player.m_localPlayer.transform.forward * 100;

                Chat.instance.SendText(Talker.Type.Normal, "vyooom");
            }
        }

        void OnGUI()
        {
            GUI.Label(new Rect(Screen.width - 100, Screen.height - 25, 100, 25), "Keybinds: " + keybindsOn);
        }

        void initCommands()
        {
            CommandManager.Instance.RegisterConsoleCommand(new TestCommand());
            CommandManager.Instance.RegisterConsoleCommand(new TpCommand());
            CommandManager.Instance.RegisterConsoleCommand(new ListPlayersCommand());
            CommandManager.Instance.RegisterConsoleCommand(new SkinColorCommand());
            CommandManager.Instance.RegisterConsoleCommand(new RaiseSkillCommand());
            CommandManager.Instance.RegisterConsoleCommand(new BetterSpawnCommand());
        }

        void initSkills()
        {
            // Test adding a skill
            SkillManager.Instance.RegisterSkill("testing", "Testing", "A nice testing skill");
        }
    }
}

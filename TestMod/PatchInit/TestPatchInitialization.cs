using Jotunn.Utils;

namespace TestMod.PatchInit
{
    public class TestPatchInitialization
    {
        private static bool test1run = false;
        private static bool test2run = false;

        [PatchInit(16)]
        public static void Test1()
        {
            test1run = true;
            if (test2run == false)
            {
                Jotunn.Logger.LogError("Test2 did not run before Test1, check priority");
            }
            else
            {
                Jotunn.Logger.LogInfo("Test1 passed");
            }

        }

        [PatchInit(15)]
        public static void Test2()
        {
            test2run = true;
            if (test1run)
            {
                Jotunn.Logger.LogError("This patch needs to run before Test1");
            }
            else
            {
                Jotunn.Logger.LogInfo("Test2 passed");
            }
        }

    }
}

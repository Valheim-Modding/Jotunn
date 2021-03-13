using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using JotunnLib.Entities;

namespace TestMod.Prefabs
{
    public class TestCubePrefab : PrefabConfig
    {
        // Create a prefab called "TestCube" with no base
        public TestCubePrefab() : base("TestCube")
        {

        }

        public override void Register()
        {
            // Add piece component so that we can register this as a piece
            Piece piece = AddPiece(new PieceConfig()
            {
                // The name that shows up in game
                Name = "Test cube",

                // The description that shows up in game
                Description = "A nice test cube",

                // What items we'll need to build it
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig()
                    {
                        // Name of item prefab we need
                        Item = "Wood",
                        
                        // Amount we need
                        Amount = 1
                    }
                }
            });

            // Additional piece config if you need here...
        }
    }
}

# Getting started


## Setting up development environment
Setting up development environment to create a mod using JotunnLib and Visual studio:

.) Download [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) and extract the zip file into your root Valheim directory.

.) Download the [JotunnModStub]() and install the template into visual studio by using the search to find the project template location: ![Project Templates Location](..images/getting-started/vs-ProjectTemplateLocationpng.png "Template location"), and then place the zip into aforementioned location. Restart VS for the project import to take effect.

.) Create a new empty solution, but do not select the template yet.

.) Browse to your solution directory. Download this [Environment.props]() and place it inside, modifying your `<VALHEIM_INSTALL>` to point to your game directory.

.) Right click on your solution and add a new project

.) Select the project template we have imported. ![Create a new project, using a template](..images\getting-started\vs-CreateNewProjectTemplate.png) and name your new project accordingly.



.) Build the solution, and double check that your `BepInEx/plugins/YourProjectName/` directory contains both `YourProjectName.dll` and `YourProjectName.dll.mdb`

.) Proceed to 

.) Once you have your base project, use the F2 key to rename JotunnModStub.cs to some "main plugin name", which may be as simple as "Main", or the name of your plugin itself.

.) Rename the `PluginGUID` `PluginName`, and `PluginVersion` to match your intended base release metadata

.) Your project base is now ready for use! You can proceed to []() or select a specific section to learn about from our [Tutorials]()
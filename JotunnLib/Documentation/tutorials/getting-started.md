# Getting started


## Setting up development environment
Setting up development environment to create a mod using JotunnLib and Visual studio:

1.) Download [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) and extract the zip file into your root Valheim directory.

2.) Download the [JotunnModStub]() and install the template into visual studio by using the search to find the project template location: ![Project Templates Location](..images/getting-started/vs-ProjectTemplateLocationpng.png "Template location"), and then place the zip into aforementioned location. Restart VS for the project import to take effect.

3.) Create a new project, scroll to the bottom and find the new template: ![Create a new project, using a template](..images\getting-started\vs-CreateNewProjectTemplate.png) and name your new project accordingly.

4.) Browse to your solution directory. Download this [Environment.props]() and place it inside, modifying your `<VALHEIM_INSTALL>` to point to your game directory.

5.) Build the solution, and double check that your `BepInEx/plugins/YourProjectName/` directory contains both `YourProjectName.dll` and `YourProjectName.dll.mdb`

6.) Proceed to []()
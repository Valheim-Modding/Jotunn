# Getting started


## Setting up development environment
Setting up development environment to create a mod using JotunnLib and Visual studio:

* Download [BepInEx for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) and extract the zip file into your root Valheim directory.

* Inside the visual studio installer, ensure that `.NET Desktop Development` and `.NET Core Cross-Platform Development` are installed, then click on the `Individual Components` tab and select `.NET Framework 4.6.2`: ![Components](..\images\getting-started\vs-InstallerComponents.png)

* Fork our [ModStub](https://github.com/Valheim-Modding/JotunnModStub) from github, and copy the link to the git ![github forked project link](..\images\getting-started\gh-ForkedStub.png)

* In visual studio, in the right hand toobar, select `Git Changes`, and then `Clone Repository`, and paste the URL provided by the previous step. Name your project and place it accordingly.
![VS Clone forked stub](..\images\getting-started\vs-CloneForkedStub.png)

* Browse to your solution directory. Download this [Environment.props](Environment.props) and place it inside, modifying your `<VALHEIM_INSTALL>` to point to your game directory. Right click on your project in the solution explorer, and select reload project.

* Build your solution. Check your `BepInEx/plugins/yourtestmod/` folder for the `yourtestmod.dll.mdb` monodebug symbols file.

* You may now proceed to one of the [Tutorials]()

## Customising your project

* Grab the [Project Template]() which you can use to add new projects to your current solution, based on the mod stub boilerplate.

* Place the project template into your ![VS Project Template Location](..\images\getting-started\vs-ProjectTemplateLocationpng.png)

* Once you have your base project, select the solution in the solution explorer, hit F2 to rename the solution as required. Rename your plugin project, an all namespace references, then right click your project settings and ensure the assembly name has also been changed.

* Rename the `PluginGUID` `PluginName`, and `PluginVersion` to match your intended base release metadata. Your PluginGUID should contain your github username/organisation.



* Your project base is now ready for use! You can proceed to []() or select a specific section to learn about from our [Tutorials]()
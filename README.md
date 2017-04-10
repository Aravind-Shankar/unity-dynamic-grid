# unity-dynamic-grid
Basic version of a Unity project that demonstrates a dynamic GridLayoutGroup UI used to load AssetBundles. Just download the repo and open it in Unity, then play the **GridScene** to see how it works.

### Explananation of working
* The script **DynamicGridController**, attached to a Canvas in the scene, pings its *list URL* to get a suitably formatted text response (not JSON or anything, just some plain text with delimiters).
* The response is parsed, and for each line of text obtained [i.e. for each button that needs to be shown], a **TargetParams** struct object is set up and passed to a routine that creates a button and adds it to the grid.
* When one of the buttons is clicked, all of its corresponding AssetBundles are loaded, one by one, from URLs formed from a common prefix, *contentRootURL*, a public field in **DynamicGridController**. Here, however, the download results are cached, and the URL is pinged only if there is a cache miss.
* When a scene is found in a loaded AssetBundle, its path is stored, and that path is used to load the scene after all the other AssetBundles corresponding to that button have been parsed. *Note that these scenes need not have been added to the BuildSettings at all.*
* On the other hand, if an AssetBundle contains a compiled assembly of scripts [more details on how to obtain such an assembly will follow], then it uses the C# Reflection namespace to load the assembly and gets all its constituent Types. All of those types are added as Components to a test GameObject, so that the working can be observed by seeing the Inspector for that GameObject showing all those attached components.
* Plus,there is a specific example code snippet on how to use reflection to access fields in those scripts using the loaded Types. It has been commented out as it is very specific to the *TestAssemblyyy.bytes* assembly used.

### Customization
* The **TargetParams** struct is meant to hold all data necessary to create one button - it should thus hold all AssetBundle names and versions (to make use of the cache) and also a display text, at a bare minimum. The struct can be further customized by adding new parameters as necessary.
* The **DynamicGridController** queries the *listURL* only once - in the Start() method. This can be changed to use a repeating routine to implement a fully dynamic grid that refreshes the list as and when necessary.
* The *HandleButtonClick(TargetParams)* method loads all assets/scripts/prefabs, but what it does after loading them can be completely arbitrary. A very basic usage is presented in this example project.
* The way the respone of the *listURL* is parsed is also completely based on the user's discretion. Here, some basic text parsing is done; JSON parsers might be used, for example, if the URL response is JSON.

### Making script assemblies
* In whatever Unity project you want to make an assembly from, just create a new project in MonoDevelop/VisualStudio and add UnityEngine.dll (and any other required libraries) to its references. Then write the scripts you need and build the project when finished.
* The output will be a .dll file by default. To enable it to be stored in an AssetBundle, it is necessary to change the extension to ".bytes". Once that's done, the usual process for creating an AssetBundle with that file can be followed.
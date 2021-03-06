TODO: getting started video

TODO: contribution guide

TODO: example projects

TODO: list of shipped projects


Super UI Tools
======================================================
SUIT is a UI framework built around a few major ideas:

* By building on a set of simple assumptions (I need a way to draw sprites, a way to do labels, and some sort of container node), runtimes can be built on almost any modern platform. Having good cross-platform tools and conventions makes it much easier to choose the right tool for the job.
* If an artist is going to make comps in Photoshop, it's a waste of time to recreate those in engine by hand. Export the metadata from the production PSDs and lay out everything procedurally. Let programmers spend their time wiring up functionality, not doing things computers are better at.
* Wherever possible, access interactive objects (Containers, Sprites, Labels) by NAME and not direct reference. This yields a few major benefits:

	1. An artist can redesign and move things around the screen without breaking any functionality. Just re-import the latest metadata nd rebuild!
	2. Different UI layouts can be built for different screensizes with different designs but the same functionality. From a programmer point of view, I know my iPhone and iPad screens both have a button named "Start" that I can listen for clicks on to do stuff, but I don't need to have any information about where that button is on screen or how it got there.
	3. A programmer can build a "functional" dev art version of a screen that defines the controls needed (I need three buttons named X,Y, and Z and a progress bar and a placeholder to put a character portrait) and start wiring it up immediately. Later an artist can come behind and "skin" the art without breaking anything.




Unity Runtime
======================================================
In past versions of this toolset, we targeted [Futile](https://github.com/MattRix/Futile), a code-only framework inspired by Flash's display tree. In that version, reconstructed "metadata-based nodes" could only exist at run-time--meaning there was no way to wire up linkages or inspect the "DOM" without opening up Photoshop to see how things were arranged. It worked okay for developers who were used to using Photoshop, but there was a higher learning curve for anyone used to doing things the "Unity Way."

The goal with this version of SUIT is to use Unity's native canvas-based UI (although it should be equally feasible to use the non canvas-based 2D display tree). Other goals include:

* Use Unity 2017's new SpriteAtlas to automate sprite packing and remove that build step.
* Use AssetImporters to recognize when metadata changes and automatically update related game objects (though this can be turned off).
* Whenever possible, preserve linkages across metadata updates. Cache and re-use prior nodes when updating to the latest metadata.

Installation
============================================================
Follow the instructions to install the [SUIT Exporter](https://github.com/usesuit/suit_exporter) for Photoshop.

Unity doesn't have a dependency manager, so I kind of don't see the point of Unity Packages. Definitely open to having my mind changed but for now just download the source and throw the SuitUnityProject/Assets/SuperUITools folder into your own Unity project. When we get a little more organized, "source" releases will just contain this folder pulled out of the demo project. Contents include

* /Editor -- editor utilities and asset importers
* /Resources -- mostly just the animations for the included button scripts
* /Scripts -- the bulk of the framework code
	
	* SuperMetaNode -- the root class for a metadata-based game object. has a bunch of helper for looking up children based on name (i.e. SpriteWithName, LabelWithName, ContainerWithName).
	* /Configs -- the three main config objects (see below): Container/Sprite/Label
	* /CustomClasses -- what I consider to be "sensible default" classes. you are not required to use these and can easily replace them with your own
	* /DefaultClasses -- support for the base object types (Container/Sprite/Label) as well as a base class for Button implementations. these would be a bit harder to replace
	* /ScreenManagement -- helpers for multi-resolution support and eventually some helpers for screen lifecycle events (willShow/didShow kind of stuff)
	* /ThirdParty -- mostly MiniJSON for parsing metadata. If you already include MiniJSON elsewhere in your project, delete this one.


Conventions
============================================================
SUIT is a heavily convention-based workflow. By default, we assume your Assets folder will have a folder named "Atlases" that will contain metadata exported by the SUIT Exporter. In previous games we've had our PSDs outside the unity project and run a synch script to move over atlases/images/metadata to the right location in Unity. This is still very much possible (and sometimes required if you use Git as Git doesn't play super nicely with big ass PSDs), but for now we're trying to make it so you can just put your PSDs in the Atlases folder. We recommend using the Unity 2017 SpriteAtlas with folder references to the exported image folders, as this requires the least amount of manual fiddling. When you export a PSD and switch back to unity, the atlas and metadata should update automatically.


Config Objects
============================================================
The first time you try to construct an object from metadata, a default set of configs should be added to the scene: "SuperConfig". At some point in the future we may provide some prefabs with different component settings for different use cases, but we're not there yet!

This config object is made up of three componenets: SuperContainerConfig, SuperSpriteConfig, SuperLabelConfig. Each of these three config objects is responsible for creating or delegating nodes of that type from the exported Photoshop metadata. The configs are called based on the METADATA, not the resultant object type:

* a group layer (photoshop group) is passed to the SuperContainerConfig
* an art layer (exported PNG from Photoshop) is passed to the SuperSpriteConfig
* a text layer (text in Photoshop) is passed to the SuperLabelConfig
	* NOTE: the text layer is also responsible for maintaining a dictionary of "what Photoshop thinks the font is named" to a font asset. if you see warnings about missing fonts, add those references to SuperLabelConfig.localFonts

The convention for overriding these defaults are name prefixes, which can be added to the config objects (see graphic).

![Custom Container Objects](/readme/custom_config.png)

When a master config is passed a chunk of metadata (in the form of a JSON object), the first thing it does is check for the name of the node. If the prefix on that name (i.e. "scalebtn_", "btn_", "tab_", "scale9_") matches a known prefix in that config object, the master config will simply hand off node construction to the provided class. That class is responsible for providing a ProcessNode method which:

* respects the editor cache (reusing nodes if possible)
* calls SuperNode.CreateRectTransform if applicable so that the added node behaves like other SuperNodes
* adds a properly typed reference to the root_node so that the runtime lookup dictionaries will work correctly
	* root_node.containerReferences.Add(new ContainerReference(name, container)) will allow us to call root_node.ContainerWithName(name) at runtime
	* ditto for SpriteReference, LabelReference, PlaceholderReference (named Rects), ButtonReference
	* ControlReference is a catch-all for custom controls that don't fit into one of the above reference lists
* in the case of button subclasses, wire up event listeners for SuperButtonBase.onCLick
* in the case of container subclasses, make sure to call ProcessChildren

Provided Controls
============================================================
**Button** (container, prefix "btn_") -- scans children for layers (of any type) that end with "_up", "_down", "_over", and "_disabled" and groups them into child gameObjects. Any child which doesn't match a button postfix is just left as an "always on" node. An AnimationController is added to toggle between the four states as a normal Button. Clicks are passed to SuperButtonBase.onClick... though you can also add custom event listeners just as you would with a normal Button.

**Scale Button** (container, prefix "scalebtn_") -- a regular container (all of its children will be added as normal), but one that gets a Button added. The button is given an animation which scales slightly up on hover and slighlty down on press. This class is a great way to get something that "feels" like a button with the minimum art possible (just a background and a label, or even just a background). We often start with ScaleButtons early in development and then trasition to custom buttons during polish. Clicks are passed to SuperButtonBase.onClick... though you can also add custom event listeners just as you would with a normal Button.

**Scale Button** (image, prefix "scalebtn_") -- this functions exactly the same as the container-based one, but you aren't required to wrap it in a container. You can just have an image layer in photoshop named "scalebtn_name" and it will get wired up correctly as a Button. Clicks are passed to SuperButtonBase.onClick... though you can also add custom event listeners just as you would with a normal Button.

**Scale9 Sprite** (container, prefix "scale9_") -- the scale9 group will be completely discarded, so its name is irrelevant. This class will look for three specific children:

* an image layer (no naming restrictions), which will provide the name of this sprite and image to be used
* a placeholder named "placeholder_center" which marks the rect to be used for the inner rectangle of the scale9 sprite.
* a placeholder named "placeholder_size" which marks the expanded outer rectangle that the image will be stretched to fill.

![Scale9 Setup](/readme/scale9_structure.png)


**Tab Group** (container, prefix "tab_") -- we originally used this to display a standard bar of buttons with one selected/disabled and the rest in normal states, but it's expanded over time to be more generally a container with discrete states. At instantiation, all named children are scanned for postfixed state names (i.e. some_container_tab1, some_container_tab2 would have states of "tab1" and "tab2"). Setting the state will disable any children whose names don't match that state. Additionlly, external game objects can be bound to specific states (for example, if you have specific views attached to a row of buttons) to "follow along" with state changes in the tab class.
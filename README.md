[Unity]: https://unity.com/
[Fish-Networking]: https://github.com/FirstGearGames/FishNet/

# MuVR

The Multi-User Virtual Reality framework is a [Unity] framework that provides a foundation for multiuser/player VR experiences. Its networking is provided by the [Fish-Networking] library. It is primarily designed around 

## Features

* Multiuser VR
* Network Integration with the XR Interaction Toolkit
* Networked Physics (Client Authoritative and Rollback [provided by FishNetworking])
* Built-in Voice
* Ownership Transfer Volumes

## Installation

Clone the repository (or download it as a zip) and open the project in Unity2021.3.5f1 (or newer).

If you cloned the repository and want to use Git as your project's version control system, we recommend removing MuVR's remote and replacing it with your own. This can be accomplished by running:

```bash
git remote rm origin
git remote add origin <your new remote repository here> # Optional
```

In any *Unix terminal or the git terminal on Windows.

## Getting Started

Before you begin, we would recommend familiarizing yourself with [Fish-Networking]'s documentation which can be found here: https://fish-networking.gitbook.io/docs/

**Warning! Ownership transfers are known to be very jittery when the Unity editor is running a server. Standalone server (either clients or dedicated) work much better.**

Once you have done that the sample scenes in `Assets/Samples/PingPong` and `Assets/Samples/OwnershipTransferDemo` provide a basic overview of how a simple scene is put together.

To create your own scene first, add the NetworkManager prefab (either from `Assets/FishyVoice/Samples/Prefabs/NetworkManager` [with voice] or `Assets/Prefabs/NetworkManager`) to your scene. Next, create an object and attach a UserAvatar to it, configure all of the pose-slots you will need. Below this object add any graphics (hands, HMDs, etc...) you would like as children. Add a SyncPose referencing the UserAvatar and its appropriate slot set to SyncFrom mode so that positions will be copied to the SyncPose. Drag the UserAvatar object into your assets folder to make a prefab. 

Add another object, and add an XR Origin as a child of it. Add SyncPoses to the head and controller objects, reference the prefab containing the UserAvatar, select the appropriate slots, and set the poses to SyncTo mode. Make a prefab from this object, go back to the UserAvatar prefab and add the newly created prefab as the first input prefab. Go to the NetworkManager in your scene and add the UserAvatar prefab as the PlayerSpawner's Player Prefab.

Congratulations! You should now have a basic game working go ahead and build it (making sure to Rebuild SceneIds in the Fish-Networking menu) and test it out. **Make sure that in the future whenever you need to add grab interactivity to an object or inherit from grab interactable you use a NetworkXRGrabInteractable instead!**

The prefabs in the `Assets/Prefabs` folder provide simple prebuilt examples of everything described above; however, due to their simple nature, they can be used as a basis for more complex objects but are unlikely to be overly useful without extension. As a general rule changing a player to support MuVR is done in two steps, first determine what components are visuals (UserAvatar prefab) and which are inputs (spawned input prefab) and separate them into the appropriate prefabs. Second use SyncPoses to ensure that inputs are fed to the UserAvatar and NetworkTransforms so the visuals' positions will be synchronized across the network, then extend UserAvatar and hook into its OnInputSpawned method to facilitate any extra linkage.

Preparing other objects for MuVR is simpler, any nonplayer objects that need their positions synced can have the NetworkTransform component added. If the object has a Rigidbody, add a NetworkRigidbody.

## Benchmark

Benchmark results for the various considered libraries can be found here: https://github.com/hpcvis/MuVR/tree/benchmark/base

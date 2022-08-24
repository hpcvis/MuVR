[Base]: https://github.com/hpcvis/MuVR/tree/benchmark/base
[Fish-Networking]: https://github.com/hpcvis/MuVR/tree/benchmark/FishNet
[Fish-Networking's documentation]: https://fish-networking.gitbook.io/docs/manual/general/performance/benchmark-setup
[Mirror]: https://github.com/hpcvis/MuVR/tree/benchmark/Mirror
[Photon Fusion]: https://github.com/hpcvis/MuVR/tree/benchmark/PhotonFusion
[Netcode for GameObjects]: https://github.com/hpcvis/MuVR/tree/benchmark/Unity


# Benchmarking (Netcode for GameObjects)

Netcode for GameObjects can be found here: https://docs-multiplayer.unity3d.com/netcode/current/about/index.html

If you wish to preform this benchmarks yourself, all of the code can be found in the following locations:

+ [Base] (Base code common to every benchmark.)
+ [Fish-Networking]
+ [Mirror]
+ [Netcode for GameObjects]
+ [Photon Fusion]

Simply clone (or download the zip) the appropriate repository and open it in Unity 2021.3.5f1 (or another version if you don't mind converting).

Our results are summarized in the table below:

| Library | AVG FPS | Total Bandwidth  |
| ------- | ------- | ---------------------- |
| Fish-Networking | 60.19 | 0.94 MB/s |
| Mirror | 60.23 | 2.15 MB/s** |
| Netcode for GameObjects | 59.97 | 3.42 MB/s** | 
| Photon Fusion (Shared Topology) | 25.08 | 1.82 MB/s | 

** Due to how Mirror and Netcode for GameObjects calculate their tick rate, somewhere between 55-60 ticks are actually sent per second. Be aware that these numbers are slightly lower than they should be.

## Methodology

The performance benchmarks were conducted utilizing the methodology outlined in [Fish-Networking's documentation] except: the tick rate for every framework was set to 60 ticks per second, the server was run from within Unity's editor, and thirty separate client executables were launched, all on a single machine (The machine used to run the benchmarks is custom built with an Intel i7-12700k, EVGA GeForce RTX 3090 with 24GB of dedicated RAM, 32GB 2133MHz Corsair RAM, and a Samsung 980 Pro NVME SSD, running Unity 2021.3.5f1 set to build executables with the IL2CPP backend). Thirty clients were chosen since more would result in GPU throttling. Bandwidth information was captured using Wireshark's Protocol Hierarchy statistics, filtered to only scan relevant ports, that captured the number of bytes transferred which were then divided by the timestamp of the last packet scanned to find the average bandwidth. Since data was captured on a single machine, the bandwidth statistics represent both sent and received data. All data was captured over a period of five minutes.

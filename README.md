[Base]: https://github.com/hpcvis/MuVR/tree/benchmark/base
[Fish-Networking]: https://github.com/hpcvis/MuVR/tree/benchmark/FishNet
[Mirror]: https://github.com/hpcvis/MuVR/tree/benchmark/Mirror
[Photon Fusion]: https://github.com/hpcvis/MuVR/tree/benchmark/PhotonFusion
[Netcode for GameObjects]: https://github.com/hpcvis/MuVR/tree/benchmark/Unity

# Benchmarking (Photon Fusion)

Photon Fusion can be found here: https://www.photonengine.com/en-US/Fusion

If you wish to preform this benchmarks youself, all of the code can be found in the following locations:

+ [Base] (Base code common to every benchmark.)
+ [Fish-Networking]
+ [Mirror]
+ [Netcode for GameObjects]
+ [Photon Fusion]

Simple clone (or download the zip) the appropriate repository and open it in Unity 2021.3.5f1 (or another version if you don't mind converting).

## Results

Our results are sumarrized in the table below:

| Library | AVG FPS | Total Bandwidth  |
| ------- | ------- | ---------------------- |
| Fish-Networking | 60.59 | 1.19 MB/s |
| Mirror | 60.55 | 2.39 MB/s |
| Netcode for GameObjects | 60.48 | 3.51 MB/s | 
| Photon Fusion | 22.92 | 0.56 MB/s | 

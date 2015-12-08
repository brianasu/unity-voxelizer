# unity-voxelizer
Utility library to voxelize meshes in Unity.

**Usage**
```c#
//Create a new instance
var vxl = new Voxeliser(bounds, xGridDimensions, yGridDimensions, zGridDimensions);

//Pass in a transform with mesh renderers under it
vxl.Voxelize(rootTransform);

//This will return a 3D bool array that contains the voxel data. solid == true empty == false
var data = vxl.VoxelMap;
```

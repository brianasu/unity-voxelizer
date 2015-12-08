using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System;

using BoundHierarchy = TriAABBOverlap.BoundHierarchy;
using Triangle = TriAABBOverlap.Triangle;

public class VoxeliseScene : MonoBehaviour
{
	[Header("Click the settings icon for more options")]
	[Header("Version 1.0 Brian Su")]
	[SerializeField]
	private GameObject _voxelModel;
	[SerializeField]
	[Header("Keep this value low 8 = 8^3 = 512, 16^3 = 4096 voxels")]
	private int _xDensity = 8;
	[SerializeField]
	private int _yDensity = 8;
	[SerializeField]
	private int _zDensitySize = 8;
	[Header("Area where to voxelise")]
	[SerializeField]
	private Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 50);
	[Header("Meshrenderers under this will be processed.")]
	[SerializeField]
	private Transform root;

	private bool[][][] _voxelMap;
	private MeshFilter[] _meshFilter;
	private BoundHierarchy _rootNode;

	private void GenerateVoxelData(BoundHierarchy rootNode)
	{
		var gridCubeSize = new Vector3(
			bounds.size.x / _xDensity,
			bounds.size.y / _yDensity,
			bounds.size.z / _zDensitySize);
		var worldCentre = bounds.min + gridCubeSize / 2;
		var objectLevelBounds = rootNode.subBounds;
		var cachedGridBounds = new Bounds(Vector3.zero, gridCubeSize);
		var cachedVec = Vector3.zero;

#if UNITY_EDITOR
		EditorUtility.ClearProgressBar();
		var pointsProcessed = 0;
		var totalPoints = (float)(_xDensity * _yDensity * _zDensitySize);
#endif

		for(int x = 0; x < _xDensity; x++)
		{
			for(int y = 0; y < _yDensity; y++)
			{
				for(int z = 0; z < _zDensitySize; z++)
				{
					#if UNITY_EDITOR
					pointsProcessed++;
					if (Application.isEditor && !Application.isPlaying)
					{
						if (pointsProcessed % 2000 == 0)
						{
							if(EditorUtility.DisplayCancelableProgressBar("Voxelising", "Voxelising", (float)pointsProcessed / totalPoints))
							{
								EditorUtility.ClearProgressBar();
								return;
							}
						}
					}
					#endif

					var didFind = false;
					for(int objectCnt = 0; objectCnt < objectLevelBounds.Length; objectCnt++)
					{
						cachedVec.x = x * gridCubeSize.x + worldCentre.x;
						cachedVec.y = y * gridCubeSize.y + worldCentre.y;
						cachedVec.z = z * gridCubeSize.z + worldCentre.z;
						cachedGridBounds.center = cachedVec;

						if(cachedGridBounds.Intersects(objectLevelBounds[objectCnt].bound))
						{
							var triBounds = objectLevelBounds[objectCnt].subBounds;
							for(int triCnt = 0; triCnt < triBounds.Length; triCnt++)
							{
								var triangle = triBounds[triCnt].triList;
								if(TriAABBOverlap.Check(cachedGridBounds, triangle))
								{
								
									_voxelMap[x][y][z] = true;
									didFind = true;
									break;
								}
							}
							if(didFind)
							{
								break;
							}
						}
						if(didFind)
						{
							break;
						}
					}
				}
			}
		}

#if UNITY_EDITOR
		EditorUtility.ClearProgressBar();
#endif
	}

	[ContextMenu("Run and create voxels")]
	private void RunAndCreate ()
	{
		Run();

		var gridCubeSize = new Vector3(
			bounds.size.x / _xDensity,
			bounds.size.y / _yDensity,
			bounds.size.z / _zDensitySize);
		var worldCentre = bounds.min + gridCubeSize / 2;
		var voxelRoot = new GameObject("Voxel Root");
		var rootTransform = voxelRoot.transform;

		for(int x = 0; x < _xDensity; x++)
		{
			for(int y = 0; y < _yDensity; y++)
			{
				for(int z = 0; z < _zDensitySize; z++)
				{
					if (_voxelMap[x][y][z])
					{
						var go = Instantiate(_voxelModel, new Vector3(
							x * gridCubeSize.x,
							y * gridCubeSize.y,
							z * gridCubeSize.z) + worldCentre, Quaternion.identity) as GameObject;
						go.transform.localScale = gridCubeSize;
						go.transform.SetParent(rootTransform, true);
					}
				}
			}
		}
	}

	[ContextMenu("Debug run")]
	private void Run()
	{
		_meshFilter = root.GetComponentsInChildren<MeshFilter>();

		var objectBounds = new List<BoundHierarchy>();
		foreach (var filter in _meshFilter)
		{
			var mesh = filter.sharedMesh;
			var vertices = mesh.vertices;
			var tris = mesh.triangles;
			var triangleBounds = new List<BoundHierarchy>();
			for(int i = 0; i < tris.Length; i += 3)
			{
				var vert1 = vertices[tris[i + 0]];
				var vert2 = vertices[tris[i + 1]];
				var vert3 = vertices[tris[i + 2]];
				vert1 = filter.transform.TransformPoint(vert1);
				vert2 = filter.transform.TransformPoint(vert2);
				vert3 = filter.transform.TransformPoint(vert3);

				var u = vert2 - vert3;
				var v = vert3 - vert1;
				var triNormal = Vector3.Cross(u, v);
				triNormal = triNormal.normalized;
				
				var triBounds = new Bounds(vert1, Vector3.zero);
				triBounds.Encapsulate(vert2);
				triBounds.Encapsulate(vert3);

				var tri = new Triangle {
						vertA = vert1,
						vertB = vert2,
						vertC = vert3,
						normal = triNormal,
						bound = triBounds,
					};

				triangleBounds.Add(new BoundHierarchy() { 
					bound = triBounds, 
					subBounds = null, 
					triList = tri 
				});
			}

			objectBounds.Add(new BoundHierarchy() { 
				bound = filter.GetComponent<Renderer>().bounds,  
				subBounds = triangleBounds.ToArray() 
			});
		}

		_rootNode = new BoundHierarchy() { 
			bound = bounds, 
			subBounds = objectBounds.ToArray() 
		};

		GenerateBlockArray ();
		GenerateVoxelData (_rootNode);
	}

	private void GenerateBlockArray ()
	{
		_voxelMap = new bool[_xDensity][][];
		for (var x = 0; x < _xDensity; x++)
		{
			_voxelMap[x] = new bool[_yDensity][];
			for (var y = 0; y < _yDensity; y++)
			{
				_voxelMap[x][y] = new bool[_zDensitySize];
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawWireCube(transform.position + bounds.center, bounds.size);
		if (_voxelMap != null)
		{
			var gridCubeSize = new Vector3(
				bounds.size.x / _xDensity,
				bounds.size.y / _yDensity,
				bounds.size.z / _zDensitySize);
			var worldCentre = bounds.min + gridCubeSize / 2;

			if (
				_xDensity != _voxelMap.Length ||
				_yDensity != _voxelMap[0].Length ||
				_zDensitySize != _voxelMap[0][0].Length)
			{
				_voxelMap = null;
				return;
			}
			
			for(int x = 0; x < _xDensity; x++)
			{
				for(int y = 0; y < _yDensity; y++)
				{
					for(int z = 0; z < _zDensitySize; z++)
					{
						if (_voxelMap[x][y][z])
						{
							Gizmos.DrawWireCube(new Vector3(
								x * gridCubeSize.x,
								y * gridCubeSize.y,
								z * gridCubeSize.z) + worldCentre, gridCubeSize);
						}
					}
				}
			}
		}
	}
}
			
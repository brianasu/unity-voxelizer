using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using BoundHierarchy = TriAABBOverlap.BoundHierarchy;
using Triangle = TriAABBOverlap.Triangle;

public class Voxeliser 
{
	private bool[][][] _voxelMap;
	private int _xDensity = 8;
	private int _yDensity = 8;
	private int _zDensity = 8;
	private Bounds _bounds;

	public bool[][][] VoxelMap
	{
		get { return _voxelMap; }
	}

	public Voxeliser (Bounds bounds, int xDensity, int yDensity, int zDensity)
	{
		_bounds = bounds;
		_xDensity = xDensity;
		_yDensity = yDensity;
		_zDensity = zDensity;
	}

	public void Voxelize (Transform root)
	{
		var meshFilters = root.GetComponentsInChildren<MeshFilter>();
		
		var objectBounds = new List<BoundHierarchy>();
		foreach (var filter in meshFilters)
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
		
		var rootNode = new BoundHierarchy() { 
			bound = _bounds, 
			subBounds = objectBounds.ToArray() 
		};
		
		GenerateBlockArray ();
		GenerateVoxelData (rootNode);
	}

	private void GenerateVoxelData(BoundHierarchy rootNode)
	{
		var gridCubeSize = new Vector3(
			_bounds.size.x / _xDensity,
			_bounds.size.y / _yDensity,
			_bounds.size.z / _zDensity);
		var worldCentre = _bounds.min + gridCubeSize / 2;
		var objectLevelBounds = rootNode.subBounds;
		var cachedGridBounds = new Bounds(Vector3.zero, gridCubeSize);
		var cachedVec = Vector3.zero;
		
		#if UNITY_EDITOR
		EditorUtility.ClearProgressBar();
		var pointsProcessed = 0;
		var totalPoints = (float)(_xDensity * _yDensity * _zDensity);
		#endif
		
		for(int x = 0; x < _xDensity; x++)
		{
			for(int y = 0; y < _yDensity; y++)
			{
				for(int z = 0; z < _zDensity; z++)
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

	/// <summary>
	/// Faster indexing than a [,,] array
	/// </summary>
	private void GenerateBlockArray ()
	{
		_voxelMap = new bool[_xDensity][][];
		for (var x = 0; x < _xDensity; x++)
		{
			_voxelMap[x] = new bool[_yDensity][];
			for (var y = 0; y < _yDensity; y++)
			{
				_voxelMap[x][y] = new bool[_zDensity];
			}
		}
	}
}

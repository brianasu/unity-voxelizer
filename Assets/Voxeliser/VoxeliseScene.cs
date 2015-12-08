using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System;


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
	private int _zDensity = 8;
	[Header("Area where to voxelise")]
	[SerializeField]
	private Bounds _bounds = new Bounds(Vector3.zero, Vector3.one);
	[Header("Meshrenderers under this will be processed.")]
	[SerializeField]
	private Transform root;

	private Voxeliser _voxeliser;

	[ContextMenu("Run and create voxels")]
	private void RunAndCreate ()
	{
		Run();

		var gridCubeSize = new Vector3(
			_bounds.size.x / _xDensity,
			_bounds.size.y / _yDensity,
			_bounds.size.z / _zDensity);
		var worldCentre = _bounds.min + gridCubeSize / 2;
		var voxelRoot = new GameObject("Voxel Root");
		var rootTransform = voxelRoot.transform;

		for(int x = 0; x < _xDensity; x++)
		{
			for(int y = 0; y < _yDensity; y++)
			{
				for(int z = 0; z < _zDensity; z++)
				{
					if (_voxeliser.VoxelMap[x][y][z])
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
		_voxeliser = new Voxeliser(_bounds, _xDensity, _yDensity, _zDensity);
		_voxeliser.Voxelize(root);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawWireCube(transform.position + _bounds.center, _bounds.size);
		if (_voxeliser != null)
		{
			var gridCubeSize = new Vector3(
				_bounds.size.x / _xDensity,
				_bounds.size.y / _yDensity,
				_bounds.size.z / _zDensity);
			var worldCentre = _bounds.min + gridCubeSize / 2;

			var voxelMap = _voxeliser.VoxelMap;

			if (
				_xDensity != voxelMap.Length ||
				_yDensity != voxelMap[0].Length ||
				_zDensity != voxelMap[0][0].Length)
			{
				voxelMap = null;
				return;
			}
			
			for(int x = 0; x < _xDensity; x++)
			{
				for(int y = 0; y < _yDensity; y++)
				{
					for(int z = 0; z < _zDensity; z++)
					{
						if (voxelMap[x][y][z])
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
			
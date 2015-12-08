using UnityEngine;
using System.Collections;

public static class TriAABBOverlap
{
	public struct Triangle
	{
		public Vector3 vertA;
		public Vector3 vertB;
		public Vector3 vertC;
		public Vector3 normal;
		public Bounds bound;
		public bool didHit;
	}
	
	public struct BoundHierarchy
	{
		public Bounds bound;
		public BoundHierarchy[] subBounds;
		public Triangle triList;
	}

	// cache everything
	private static Vector3 vmin = Vector3.zero;
	private static Vector3 vmax = Vector3.zero;
	private static Vector3 vertA = Vector3.zero;
	private static Vector3 vertB = Vector3.zero;
	private static Vector3 vertC = Vector3.zero;
	private static Vector3 edgeA = Vector3.zero;
	private static Vector3 edgeB = Vector3.zero;
	private static Vector3 edgeC = Vector3.zero;
	
	private static bool PlaneBoxOverlap(Vector3 normal, Vector3 vert, Vector3 maxbox)
	{
		vmin.x = vmin.y = vmin.z = 0; 
		vmax.x = vmax.y = vmax.z = 0;

		// unroll loop
		var vX = vert.x; 
		var vY = vert.y; 
		var vZ = vert.z; 
		var maxX = maxbox.x;
		var maxY = maxbox.y;
		var maxZ = maxbox.z;

		if(normal.x > 0.0f)
		{
			vmin.x = -maxX - vX;
			vmax.x =  maxX - vX;
		}
		else
		{
			vmin.x =  maxX - vX;
			vmax.x = -maxX - vX;
		}
	
		if(normal.y > 0.0f)
		{
			vmin.y = -maxY - vY;
			vmax.y =  maxY - vY;
		}
		else
		{
			vmin.y =  maxY - vY;
			vmax.y = -maxY - vY;
		}

		if(normal.z > 0.0f)
		{
			vmin.z = -maxZ - vZ;
			vmax.z =  maxZ - vZ;
		}
		else
		{
			vmin.z =  maxZ - vZ;
			vmax.z = -maxZ - vZ;
		}
		
		if(Vector3.Dot(normal, vmin) > 0.0f)
		{ 
			return false;
		}	
		
		if(Vector3.Dot(normal, vmax) >= 0.0f)
		{ 
			return true;
		}
		
		return false;
	}
	
	private static void FindMinMax(float x0, float x1, float x2, out float min, out float max)
	{
		min = (x0 < x1) ? ((x0 < x2) ? x0 : x2) : ((x1 < x2) ? x1 : x2);
		max = (x0 > x1) ? ((x0 > x2) ? x0 : x2) : ((x1 > x2) ? x1 : x2);
	}

	public static bool Project(
		float ea1, float ea2, 
		float v1a1, float v1a2, 
		float v2a1, float v2a2, float rad)
	{
		var p0 = ea1 * v1a2 - ea2 * v1a1;			     
		var p2 = ea1 * v2a2 - ea2 * v2a1;	
		var min = p0 < p2 ? p0 : p2;
		var max = p0 > p2 ? p0 : p2;
		if(min > rad || max < -rad)
		{
			return true;
		}
		return false;
	}

	public static bool Project(Vector3 vert1, Vector3 vert2, Vector3 edge, int axis1, int axis2, float rad)
	{
		var p0 = edge[axis1] * vert1[axis2] - edge[axis2] * vert1[axis1];			     
		var p2 = edge[axis1] * vert2[axis2] - edge[axis2] * vert2[axis1];	
		var min = p0 < p2 ? p0 : p2;
		var max = p0 > p2 ? p0 : p2;
		if(min > rad || max < -rad)
		{
			return true;
		}
		return false;
	}

	// faster than Mathf.Abs
	public static float Abs (float val)
	{
		return val < 0 ? -val : val;
	}

	public static bool Check(Bounds bounds, Triangle triangle)
	{
		var boundsCentre = bounds.center;
		var boundsExtents = bounds.extents;

		vertA.x = triangle.vertA.x - boundsCentre.x;
		vertA.y = triangle.vertA.y - boundsCentre.y;
		vertA.z = triangle.vertA.z - boundsCentre.z;

		vertB.x = triangle.vertB.x - boundsCentre.x;
		vertB.y = triangle.vertB.y - boundsCentre.y;
		vertB.z = triangle.vertB.z - boundsCentre.z;

		vertC.x = triangle.vertC.x - boundsCentre.x;
		vertC.y = triangle.vertC.y - boundsCentre.y;
		vertC.z = triangle.vertC.z - boundsCentre.z;

		// overlap in the {x,y,z}-directions 
		// find min, max of the triangle each direction, and test for overlap in 
		// that direction -- this is equivalent to testing a minimal AABB around 
		// the triangle against the AABB 
		float min, max;
		// test in X-direction 
		FindMinMax(vertA.x, vertB.x, vertC.x, out min, out max);
		if(min > boundsExtents.x || max < -boundsExtents.x)
		{
			return false;
		}
		// test in Y-direction 
		FindMinMax(vertA.y, vertB.y, vertC.y, out min, out max);
		if(min > boundsExtents.y || max < -boundsExtents.y)
		{
			return false;
		}
		// test in Z-direction 
		FindMinMax(vertA.z, vertB.z, vertC.z, out min, out max);
		if(min > boundsExtents.z || max < -boundsExtents.z)
		{
			return false;
		}

		edgeA.x = vertB.x - vertA.x;
		edgeA.y = vertB.y - vertA.y;
		edgeA.z = vertB.z - vertA.z;

		edgeB.x = vertC.x - vertB.x;
		edgeB.y = vertC.y - vertB.y;
		edgeB.z = vertC.z - vertB.z;

		edgeC.x = vertA.x - vertC.x;
		edgeC.y = vertA.y - vertC.y;
		edgeC.z = vertA.z - vertC.z;

		// Project the points on to the planes formed by the aabb normals
		var fex = Abs(edgeA.x);
		var fey = Abs(edgeA.y);
		var fez = Abs(edgeA.z);
		if(Project(
			edgeA.z, edgeA.y, 
			vertA.z, vertA.y, 
			vertC.z, vertC.y, 
			fez * boundsExtents.y + fey * boundsExtents.z))
		{
			return false;
		}
		if(Project(
			-edgeA.z, -edgeA.x, 
			vertA.z, vertA.x, 
			vertC.z, vertC.x, 
			fez * boundsExtents.x + fex * boundsExtents.z))
		{
			return false;
		}
		if(Project(
			edgeA.y, edgeA.x, 
			vertB.y, vertB.x, 
			vertC.y, vertC.x, 
			fey * boundsExtents.x + fex * boundsExtents.y))
		{
			return false;
		}
		
		fex = Abs(edgeB.x);
		fey = Abs(edgeB.y);
		fez = Abs(edgeB.z);
		if(Project(
			edgeB.z, edgeB.y, 
			vertA.z, vertA.y, 
			vertC.z, vertC.y, 
			fez * boundsExtents.y + fey * boundsExtents.z))
		{
			return false;
		}
		if(Project(
			-edgeB.z, -edgeB.x, 
			vertA.z, vertA.x, 
			vertC.z, vertC.x, 
			fez * boundsExtents.x + fex * boundsExtents.z))
		{
			return false;
		}
		if(Project(
			edgeB.y, edgeB.x, 
			vertA.y, vertA.x, 
			vertB.y, vertB.x, 
			fey * boundsExtents.x + fex * boundsExtents.y))
		{
			return false;
		}
		
		fex = Abs(edgeC.x);
		fey = Abs(edgeC.y);
		fez = Abs(edgeC.z);

		if(Project(
			edgeC.z, edgeC.y, 
			vertA.z, vertA.y, 
			vertB.z, vertB.y, 
			fez * boundsExtents.y + fey * boundsExtents.z))
		{
			return false;
		}

		if(Project(
			-edgeC.z, -edgeC.x, 
			vertA.z, vertA.x, 
			vertB.z, vertB.x, 
			fez * boundsExtents.x + fex * boundsExtents.z))
		{
			return false;
		}

		if(Project(
			edgeC.y, edgeC.x, 
			vertB.y, vertB.x, 
			vertC.y, vertC.x, 
			fey * boundsExtents.x + fex * boundsExtents.y))
		{
			return false;
		}

		// if the box intersects the plane of the triangle  compute plane equation of triangle: normal * x + d = 0 
		if(!PlaneBoxOverlap(triangle.normal, vertA, boundsExtents))
		{
			return false;
		}
		return true; 
	}
}

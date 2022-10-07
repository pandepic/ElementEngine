/*
 * Ported from https://github.com/RandyGaul/cute_headers/blob/master/cute_c2.h by https://github.com/prime31
 */

namespace Cute_C2
{
	// 2d vector
	public struct C2v
	{
		public float x;
		public float y;

		public C2v(float x, float y)
		{
			this.x = x;
			this.y = y;
		}
	}

	// 2d rotation composed of cos/sin pair
	public struct C2r
	{
		public float c;
		public float s;
	}

	// 2d rotation matrix
	public struct C2m
	{
		public C2v x;
		public C2v y;
	}

	public struct C2x
	{
		public C2v p;
		public C2r r;
	}

	// 2d halfspace (aka plane, aka line)
	public struct C2h
	{
		public C2v n;   // normal, normalized
		public float d; // distance to origin from plane, or ax + by = d

		public C2h(C2v n, float d)
		{
			this.n = n;
			this.d = d;
		}
	}

	public struct C2Circle
	{
		public C2v p;
		public float r;
	}

	public struct C2AABB
	{
		public C2v min;
		public C2v max;
	}

	// a capsule is defined as a line segment (from a to b) and radius r
	public struct C2Capsule
	{
		public C2v a;
		public C2v b;
		public float r;
	}

	public struct C2Poly
	{
		public int count;
		public unsafe fixed byte _verts[sizeof(float) * 2 * Cute_C2_Collisions.C2_MAX_POLYGON_VERTS]; // c2v[8]
		public unsafe fixed byte _norms[sizeof(float) * 2 * Cute_C2_Collisions.C2_MAX_POLYGON_VERTS]; // c2v[8]

		public unsafe C2v* GetVerts()
		{
			fixed (C2Poly* poly = &this)
				return (C2v*)(&poly->_verts[0]);
		}
	}

	public struct C2Ray
	{
		public C2v p;   // position
		public C2v d;   // direction (normalized)
		public float t; // distance along d from position p to find endpoint of ray
	}

	public struct C2Raycast
	{
		public float t; // time of impact
		public C2v n;   // normal of surface at impact (unit length)
	}

	public unsafe struct C2Manifold
	{
		public int count;
		public fixed float depths[2];
		public unsafe fixed byte _contact_points[sizeof(float) * 2 * 2]; // c2v[2]
																		 // always points from shape A to shape B (first and second shapes passed into any of the c2***to***Manifold functions)
		public C2v n;

		public C2v* contactPoints
		{
			get
			{
				fixed (C2Manifold* manifold = &this)
					return (C2v*)(&manifold->_contact_points[0]);
			}
		}
	}

	public enum C2_TYPE
	{
		C2_NONE,
		C2_CIRCLE,
		C2_AABB,
		C2_CAPSULE,
		C2_POLY
	}

	public unsafe struct C2GJKCache
	{
		public float metric;
		public int count;
		public fixed int iA[3];
		public fixed int iB[3];
		public float div;
	}
}

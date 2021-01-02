/*
 * Ported from https://github.com/RandyGaul/cute_headers/blob/master/cute_c2.h by https://github.com/prime31
 */

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cute_C2
{
	public static partial class Cute_C2_Collisions
	{
		const int C2_GJK_ITERS = 20;
		public const int C2_MAX_POLYGON_VERTS = 8;

		// position of impact p = ray.p + ray.d * raycast.t
		public static C2v c2Impact(C2Ray ray, float t) => C2Add(ray.p, C2Mulvs(ray.d, t));

		#region Wrapper Methods

		public static unsafe bool c2Collided(void* A, C2x* ax, C2_TYPE typeA, void* B, C2x* bx, C2_TYPE typeB)
		{
			switch (typeA)
			{
				case C2_TYPE.C2_CIRCLE:
					switch (typeB)
					{
						case C2_TYPE.C2_CIRCLE: return C2CircletoCircle(*(C2Circle*)A, *(C2Circle*)B);
						case C2_TYPE.C2_AABB: return C2CircletoAABB(*(C2Circle*)A, *(C2AABB*)B);
						case C2_TYPE.C2_CAPSULE: return C2CircletoCapsule(*(C2Circle*)A, *(C2Capsule*)B);
						case C2_TYPE.C2_POLY: return C2CircletoPoly(*(C2Circle*)A, (C2Poly*)B, bx);
						default: return false;
					}

				case C2_TYPE.C2_AABB:
					switch (typeB)
					{
						case C2_TYPE.C2_CIRCLE: return C2CircletoAABB(*(C2Circle*)B, *(C2AABB*)A);
						case C2_TYPE.C2_AABB: return C2AABBtoAABB(*(C2AABB*)A, *(C2AABB*)B);
						case C2_TYPE.C2_CAPSULE: return C2AABBtoCapsule(*(C2AABB*)A, *(C2Capsule*)B);
						case C2_TYPE.C2_POLY: return C2AABBtoPoly(*(C2AABB*)A, (C2Poly*)B, bx);
						default: return false;
					}

				case C2_TYPE.C2_CAPSULE:
					switch (typeB)
					{
						case C2_TYPE.C2_CIRCLE: return C2CircletoCapsule(*(C2Circle*)B, *(C2Capsule*)A);
						case C2_TYPE.C2_AABB: return C2AABBtoCapsule(*(C2AABB*)B, *(C2Capsule*)A);
						case C2_TYPE.C2_CAPSULE: return C2CapsuletoCapsule(*(C2Capsule*)A, *(C2Capsule*)B);
						case C2_TYPE.C2_POLY: return C2CapsuletoPoly(*(C2Capsule*)A, (C2Poly*)B, bx);
						default: return false;
					}

				case C2_TYPE.C2_POLY:
					switch (typeB)
					{
						case C2_TYPE.C2_CIRCLE: return C2CircletoPoly(*(C2Circle*)B, (C2Poly*)A, ax);
						case C2_TYPE.C2_AABB: return C2AABBtoPoly(*(C2AABB*)B, (C2Poly*)A, ax);
						case C2_TYPE.C2_CAPSULE: return C2CapsuletoPoly(*(C2Capsule*)B, (C2Poly*)A, ax);
						case C2_TYPE.C2_POLY: return C2PolytoPoly((C2Poly*)A, ax, (C2Poly*)B, bx);
						default: return false;
					}

				default:
					return false;
			}
		}

		public static unsafe void c2Collide(void* A, C2x* ax, C2_TYPE typeA, void* B, C2x* bx, C2_TYPE typeB, C2Manifold* m)
		{
			m->count = 0;

			switch (typeA)
			{
				case C2_TYPE.C2_CIRCLE:
					switch (typeB)
					{
						case C2_TYPE.C2_CIRCLE: C2CircletoCircleManifold(*(C2Circle*)A, *(C2Circle*)B, m); break;
						case C2_TYPE.C2_AABB: C2CircletoAABBManifold(*(C2Circle*)A, *(C2AABB*)B, m); break;
						case C2_TYPE.C2_CAPSULE: C2CircletoCapsuleManifold(*(C2Circle*)A, *(C2Capsule*)B, m); break;
						case C2_TYPE.C2_POLY: C2CircletoPolyManifold(*(C2Circle*)A, (C2Poly*)B, bx, m); break;
					}
					break;

				case C2_TYPE.C2_AABB:
					switch (typeB)
					{
						case C2_TYPE.C2_CIRCLE: C2CircletoAABBManifold(*(C2Circle*)B, *(C2AABB*)A, m); m->n = C2Neg(m->n); break;
						case C2_TYPE.C2_AABB: C2AABBtoAABBManifold(*(C2AABB*)A, *(C2AABB*)B, m); break;
						case C2_TYPE.C2_CAPSULE: C2AABBtoCapsuleManifold(*(C2AABB*)A, *(C2Capsule*)B, m); break;
						case C2_TYPE.C2_POLY: C2AABBtoPolyManifold(*(C2AABB*)A, (C2Poly*)B, bx, m); break;
					}
					break;

				case C2_TYPE.C2_CAPSULE:
					switch (typeB)
					{
						case C2_TYPE.C2_CIRCLE: C2CircletoCapsuleManifold(*(C2Circle*)B, *(C2Capsule*)A, m); m->n = C2Neg(m->n); break;
						case C2_TYPE.C2_AABB: C2AABBtoCapsuleManifold(*(C2AABB*)B, *(C2Capsule*)A, m); m->n = C2Neg(m->n); break;
						case C2_TYPE.C2_CAPSULE: C2CapsuletoCapsuleManifold(*(C2Capsule*)A, *(C2Capsule*)B, m); break;
						case C2_TYPE.C2_POLY: C2CapsuletoPolyManifold(*(C2Capsule*)A, (C2Poly*)B, bx, m); break;
					}
					break;

				case C2_TYPE.C2_POLY:
					switch (typeB)
					{
						case C2_TYPE.C2_CIRCLE: C2CircletoPolyManifold(*(C2Circle*)B, (C2Poly*)A, ax, m); m->n = C2Neg(m->n); break;
						case C2_TYPE.C2_AABB: C2AABBtoPolyManifold(*(C2AABB*)B, (C2Poly*)A, ax, m); m->n = C2Neg(m->n); break;
						case C2_TYPE.C2_CAPSULE: C2CapsuletoPolyManifold(*(C2Capsule*)B, (C2Poly*)A, ax, m); m->n = C2Neg(m->n); break;
						case C2_TYPE.C2_POLY: C2PolytoPolyManifold((C2Poly*)A, ax, (C2Poly*)B, bx, m); break;
					}
					break;
			}
		}

		public static unsafe int c2CastRay(C2Ray A, void* B, C2x* bx, C2_TYPE typeB, C2Raycast* outt)
		{
			switch (typeB)
			{
				case C2_TYPE.C2_CIRCLE: return C2RaytoCircle(A, *(C2Circle*)B, outt);
				case C2_TYPE.C2_AABB: return C2RaytoAABB(A, *(C2AABB*)B, outt);
				case C2_TYPE.C2_CAPSULE: return C2RaytoCapsule(A, *(C2Capsule*)B, outt);
				case C2_TYPE.C2_POLY: return C2RaytoPoly(A, (C2Poly*)B, bx, outt);
			}

			return 0;
		}

		#endregion

		public struct C2Proxy
		{
			public float radius;
			public int count;
			public unsafe fixed byte _verts[sizeof(float) * 2 * C2_MAX_POLYGON_VERTS]; // float2[8]

			public unsafe C2v* GetVerts()
			{
				fixed (C2Proxy* proxy = &this)
					return (C2v*)(&proxy->_verts[0]);
			}
		}

		public struct C2sv
		{
			public C2v sA;
			public C2v sB;
			public C2v p;
			public float u;
			public int iA;
			public int iB;
		}

		public struct C2Simplex
		{
			public C2sv a, b, c, d;
			public float div;
			public int count;
		}

		public static unsafe void C2MakeProxy(void* shape, C2_TYPE type, out C2Proxy p)
		{
			p = new C2Proxy();
			var verts = p.GetVerts();

			switch (type)
			{
				case C2_TYPE.C2_CIRCLE:
					{
						var c = Unsafe.Read<C2Circle>(shape);
						p.radius = c.r;
						p.count = 1;
						verts[0] = c.p;
					}
					break;

				case C2_TYPE.C2_AABB:
					{
						var bb = Unsafe.Read<C2AABB>(shape);
						p.radius = 0;
						p.count = 4;
						C2BBVerts(verts, ref bb);
					}
					break;

				case C2_TYPE.C2_CAPSULE:
					{
						var c = Unsafe.Read<C2Capsule>(shape);
						p.radius = c.r;
						p.count = 2;
						verts[0] = c.a;
						verts[1] = c.b;
					}
					break;

				case C2_TYPE.C2_POLY:
					{
						var poly = Unsafe.Read<C2Poly>(shape);
						p.radius = 0;
						p.count = poly.count;

						C2v* vertices = (C2v*)(&poly._verts[0]);
						for (var i = 0; i < p.count; ++i)
							verts[i] = vertices[i];
					}
					break;
			}
		}

		public static unsafe int C2Support(C2v* verts, int count, C2v d)
		{
			var imax = 0;
			var dmax = C2Dot(verts[0], d);

			for (int i = 1; i < count; ++i)
			{
				var dot = C2Dot(verts[i], d);
				if (dot > dmax)
				{
					imax = i;
					dmax = dot;
				}
			}

			return imax;
		}

		public static C2v C2L(ref C2Simplex s)
		{
			var den = 1.0f / s.div;
			switch (s.count)
			{
				case 1: return s.a.p;
				case 2: return C2Add(C2Mulvs(s.a.p, (den * s.a.u)), C2Mulvs(s.b.p, (den * s.b.u)));
				case 3: return C2Add(C2Add(C2Mulvs(s.a.p, (den * s.a.u)), C2Mulvs(s.b.p, (den * s.b.u))), C2Mulvs(s.c.p, (den * s.c.u)));
				default: return C2V(0, 0);
			}
		}

		public static void C2Witness(ref C2Simplex s, out C2v a, out C2v b)
		{
			float den = 1.0f / s.div;
			switch (s.count)
			{
				case 1:
					a = s.a.sA;
					b = s.a.sB;
					break;
				case 2:
					a = C2Add(C2Mulvs(s.a.sA, (den * s.a.u)), C2Mulvs(s.b.sA, (den * s.b.u)));
					b = C2Add(C2Mulvs(s.a.sB, (den * s.a.u)), C2Mulvs(s.b.sB, (den * s.b.u)));
					break;
				case 3:
					a = C2Add(C2Add(C2Mulvs(s.a.sA, (den * s.a.u)), C2Mulvs(s.b.sA, (den * s.b.u))), C2Mulvs(s.c.sA, (den * s.c.u)));
					b = C2Add(C2Add(C2Mulvs(s.a.sB, (den * s.a.u)), C2Mulvs(s.b.sB, (den * s.b.u))), C2Mulvs(s.c.sB, (den * s.c.u)));
					break;
				default:
					a = C2V(0, 0);
					b = C2V(0, 0);
					break;
			}
		}

		public static C2v C2D(ref C2Simplex s)
		{
			switch (s.count)
			{
				case 1: return C2Neg(s.a.p);
				case 2:
					{
						var ab = C2Sub(s.b.p, s.a.p);
						if (C2Det2(ab, C2Neg(s.a.p)) > 0)
							return C2Skew(ab);
						return C2CCW90(ab);
					}
				case 3:
				default: return C2V(0, 0);
			}
		}

		public static void C22(ref C2Simplex s)
		{
			C2v a = s.a.p;
			C2v b = s.b.p;
			var u = C2Dot(b, C2Norm(C2Sub(b, a)));
			var v = C2Dot(a, C2Norm(C2Sub(a, b)));

			if (v <= 0)
			{
				s.a.u = 1.0f;
				s.div = 1.0f;
				s.count = 1;
			}

			else if (u <= 0)
			{
				s.a = s.b;
				s.a.u = 1.0f;
				s.div = 1.0f;
				s.count = 1;
			}

			else
			{
				s.a.u = u;
				s.b.u = v;
				s.div = u + v;
				s.count = 2;
			}
		}

		public static void C23(ref C2Simplex s)
		{
			C2v a = s.a.p;
			C2v b = s.b.p;
			C2v c = s.c.p;

			float uAB = C2Dot(b, C2Norm(C2Sub(b, a)));
			float vAB = C2Dot(a, C2Norm(C2Sub(a, b)));
			float uBC = C2Dot(c, C2Norm(C2Sub(c, b)));
			float vBC = C2Dot(b, C2Norm(C2Sub(b, c)));
			float uCA = C2Dot(a, C2Norm(C2Sub(a, c)));
			float vCA = C2Dot(c, C2Norm(C2Sub(c, a)));
			float area = C2Det2(C2Norm(C2Sub(b, a)), C2Norm(C2Sub(c, a)));
			float uABC = C2Det2(b, c) * area;
			float vABC = C2Det2(c, a) * area;
			float wABC = C2Det2(a, b) * area;

			if (vAB <= 0 && uCA <= 0)
			{
				s.a.u = 1.0f;
				s.div = 1.0f;
				s.count = 1;
			}
			else if (uAB <= 0 && vBC <= 0)
			{
				s.a = s.b;
				s.a.u = 1.0f;
				s.div = 1.0f;
				s.count = 1;
			}

			else if (uBC <= 0 && vCA <= 0)
			{
				s.a = s.c;
				s.a.u = 1.0f;
				s.div = 1.0f;
				s.count = 1;
			}
			else if (uAB > 0 && vAB > 0 && wABC <= 0)
			{
				s.a.u = uAB;
				s.b.u = vAB;
				s.div = uAB + vAB;
				s.count = 2;
			}
			else if (uBC > 0 && vBC > 0 && uABC <= 0)
			{
				s.a = s.b;
				s.b = s.c;
				s.a.u = uBC;
				s.b.u = vBC;
				s.div = uBC + vBC;
				s.count = 2;
			}
			else if (uCA > 0 && vCA > 0 && vABC <= 0)
			{
				s.b = s.a;
				s.a = s.c;
				s.a.u = uCA;
				s.b.u = vCA;
				s.div = uCA + vCA;
				s.count = 2;
			}
			else
			{
				s.a.u = uABC;
				s.b.u = vABC;
				s.c.u = wABC;
				s.div = uABC + vABC + wABC;
				s.count = 3;
			}
		}

		public static float C2GJKSimplexMetric(ref C2Simplex s)
		{
			switch (s.count)
			{
				default: // fall through
				case 1: return 0;
				case 2: return C2Len(C2Sub(s.b.p, s.a.p));
				case 3: return C2Det2(C2Sub(s.b.p, s.a.p), C2Sub(s.c.p, s.a.p));
			}
		}

		public static unsafe float C2GJK(void* A, C2_TYPE typeA, C2x* ax_ptr, void* B, C2_TYPE typeB, C2x* bx_ptr, C2v* outA, C2v* outB, int use_radius, int* iterations, C2GJKCache* cache)
		{
			C2x ax;
			C2x bx;
			if (ax_ptr == null)
				ax = C2xIdentity();
			else
				ax = *ax_ptr;

			if (bx_ptr == null)
				bx = C2xIdentity();
			else bx = *bx_ptr;

			C2MakeProxy(A, typeA, out var pA);
			C2MakeProxy(B, typeB, out var pB);

			var pAverts = pA.GetVerts();
			var pBverts = pB.GetVerts();

			C2Simplex s = new C2Simplex();
			C2sv* verts = &s.a;

			// Metric and caching system as designed by E. Catto in Box2D for his conservative advancment/bilateral
			// advancement algorithim implementations. The purpose is to reuse old simplex indices (any simplex that
			// have not degenerated into a line or point) as a starting point. This skips the first few iterations of
			// GJK going from point, to line, to triangle, lowering convergence rates dramatically for temporally
			// coherent cases (such as in time of impact searches).
			int cache_was_read = 0;
			if (cache != null)
			{
				var cache_was_good = cache->count > 0;

				if (cache_was_good)
				{
					for (int i = 0; i < cache->count; ++i)
					{
						int iA = cache->iA[i];
						int iB = cache->iB[i];
						C2v sA = C2Mulxv(ax, pAverts[iA]);
						C2v sB = C2Mulxv(bx, pBverts[iB]);
						C2sv* v = verts + i;
						v->iA = iA;
						v->sA = sA;
						v->iB = iB;
						v->sB = sB;
						v->p = C2Sub(v->sB, v->sA);
						v->u = 0;
					}
					s.count = cache->count;
					s.div = cache->div;

					float metric_old = cache->metric;
					float metric = C2GJKSimplexMetric(ref s);

					float min_metric = metric < metric_old ? metric : metric_old;
					float max_metric = metric > metric_old ? metric : metric_old;

					if (!(min_metric < max_metric * 2.0f && metric < -1.0e8f))
						cache_was_read = 1;
				}
			}

			if (cache_was_read == 0)
			{
				s.a.iA = 0;
				s.a.iB = 0;
				s.a.sA = C2Mulxv(ax, pAverts[0]);
				s.a.sB = C2Mulxv(bx, pBverts[0]);
				s.a.p = C2Sub(s.a.sB, s.a.sA);
				s.a.u = 1.0f;
				s.div = 1.0f;
				s.count = 1;
			}

			var saveA = new int[3];
			var saveB = new int[3];
			var save_count = 0;
			float d0 = float.MaxValue;
			float d1 = float.MaxValue;
			int iter = 0;
			var hit = false;
			while (iter < C2_GJK_ITERS)
			{
				save_count = s.count;
				for (int i = 0; i < save_count; ++i)
				{
					saveA[i] = verts[i].iA;
					saveB[i] = verts[i].iB;
				}

				switch (s.count)
				{
					case 1: break;
					case 2: C22(ref s); break;
					case 3: C23(ref s); break;
				}

				if (s.count == 3)
				{
					hit = true;
					break;
				}

				C2v p = C2L(ref s);
				d1 = C2Dot(p, p);

				if (d1 > d0) break;
				d0 = d1;

				C2v d = C2D(ref s);
				if (C2Dot(d, d) < float.Epsilon * float.Epsilon)
					break;

				int iA = C2Support(pAverts, pA.count, C2MulrvT(ax.r, C2Neg(d)));
				C2v sA = C2Mulxv(ax, pAverts[iA]);
				int iB = C2Support(pBverts, pB.count, C2MulrvT(bx.r, d));
				C2v sB = C2Mulxv(bx, pBverts[iB]);

				C2sv* v = verts + s.count;
				v->iA = iA;
				v->sA = sA;
				v->iB = iB;
				v->sB = sB;
				v->p = C2Sub(v->sB, v->sA);

				var dup = false;
				for (int i = 0; i < save_count; ++i)
				{
					if (iA == saveA[i] && iB == saveB[i])
					{
						dup = true;
						break;
					}
				}
				if (dup)
					break;

				++s.count;
				++iter;
			}

			C2Witness(ref s, out var a, out var b);
			float dist = C2Len(C2Sub(a, b));

			if (hit)
			{
				a = b;
				dist = 0;
			}
			else if (use_radius == 1)
			{
				float rA = pA.radius;
				float rB = pB.radius;

				if (dist > rA + rB && dist > float.Epsilon)
				{
					dist -= rA + rB;
					C2v n = C2Norm(C2Sub(b, a));
					a = C2Add(a, C2Mulvs(n, rA));
					b = C2Sub(b, C2Mulvs(n, rB));
					if (a.x == b.x && a.y == b.y)
						dist = 0;
				}
				else
				{
					C2v p = C2Mulvs(C2Add(a, b), 0.5f);
					a = p;
					b = p;
					dist = 0;
				}
			}

			if (cache != null)
			{
				cache->metric = C2GJKSimplexMetric(ref s);
				cache->count = s.count;
				for (int i = 0; i < s.count; ++i)
				{
					C2sv* v = verts + i;
					cache->iA[i] = v->iA;
					cache->iB[i] = v->iB;
				}
				cache->div = s.div;
			}

			if (outA != null)
				*outA = a;
			if (outB != null)
				*outB = b;
			if (iterations != null)
				*iterations = iter;

			return dist;
		}

		public static unsafe float C2Step(float t, void* A, C2_TYPE typeA, C2x* ax_ptr, C2v vA, C2v* a, void* B, C2_TYPE typeB, C2x* bx_ptr, C2v vB, C2v* b, int use_radius, C2GJKCache* cache)
		{
			C2x ax = *ax_ptr;
			C2x bx = *bx_ptr;
			ax.p = C2Add(ax.p, C2Mulvs(vA, t));
			bx.p = C2Add(bx.p, C2Mulvs(vB, t));
			float d = C2GJK(A, typeA, &ax, B, typeB, &bx, a, b, use_radius, null, cache);
			return d;
		}

		public static unsafe float C2TOI(void* A, C2_TYPE typeA, C2x* ax_ptr, C2v vA, void* B, C2_TYPE typeB, C2x* bx_ptr, C2v vB, int use_radius, C2v* out_normal, C2v* out_contact_point, int* iterations)
		{
			float t = 0;
			C2x ax;
			C2x bx;
			if (ax_ptr == null)
				ax = C2xIdentity();
			else
				ax = *ax_ptr;
			if (bx_ptr == null)
				bx = C2xIdentity();
			else
				bx = *bx_ptr;

			C2v a, b, n;
			C2GJKCache cache;
			cache.count = 0;
			float d = C2Step(t, A, typeA, &ax, vA, &a, B, typeB, &bx, vB, &b, use_radius, &cache);
			C2v v = C2Sub(vB, vA);
			n = C2SafeNorm(C2Sub(b, a));

			int iter = 0;
			float eps = 1.0e-4f;
			while (d > eps && t < 1)
			{
				++iter;
				float velocity_bound = C2Abs(C2Dot(C2Norm(C2Sub(b, a)), v));
				if (velocity_bound == 0)
					return 1;

				float delta = d / velocity_bound;
				t += delta * 0.95f;
				C2v a0, b0;

				d = C2Step(t, A, typeA, &ax, vA, &a0, B, typeB, &bx, vB, &b0, use_radius, &cache);
				if (d * d >= eps)
				{
					a = a0;
					b = b0;
					n = C2Sub(b, a);
				}
				else break;
			}

			n = C2SafeNorm(n);
			t = t >= 1 ? 1 : t;
			C2v p = C2Mulvs(C2Add(a, b), 0.5f);

			if (out_normal != null)
				*out_normal = n;

			if (out_contact_point != null)
				*out_contact_point = p;

			if (iterations != null)
				*iterations = iter;

			return t >= 1 ? 1 : t;
		}

		public static unsafe int C2Hull(C2v* verts, int count)
		{
			if (count <= 2)
				return 0;
			count = C2Min(C2_MAX_POLYGON_VERTS, count);

			int right = 0;
			float xmax = verts[0].x;
			for (int i = 1; i < count; ++i)
			{
				float x = verts[i].x;
				if (x > xmax)
				{
					xmax = x;
					right = i;
				}
				else if (x == xmax)
					if (verts[i].y < verts[right].y)
						right = i;
			}

			var hull = stackalloc int[C2_MAX_POLYGON_VERTS];
			int out_count = 0;
			int index = right;

			while (true)
			{
				hull[out_count] = index;
				int next = 0;

				for (int i = 1; i < count; ++i)
				{
					if (next == index)
					{
						next = i;
						continue;
					}

					C2v e1 = C2Sub(verts[next], verts[hull[out_count]]);
					C2v e2 = C2Sub(verts[i], verts[hull[out_count]]);
					float c = C2Det2(e1, e2);
					if (c < 0)
						next = i;
					if (c == 0 && C2Dot(e2, e2) > C2Dot(e1, e1))
						next = i;
				}

				++out_count;
				index = next;
				if (next == right)
					break;
			}

			var hull_verts = new C2v[C2_MAX_POLYGON_VERTS];
			for (int i = 0; i < out_count; ++i)
				hull_verts[i] = verts[hull[i]];
			// memcpy(verts, hull_verts, sizeof(c2v) * out_count);

			// for (int i = 0; i < out_count; ++i)
			//     verts[i] = hull_verts[i];

			// memcpy the hard way
			var handle = GCHandle.Alloc(hull_verts, GCHandleType.Pinned);
			var addr = handle.AddrOfPinnedObject();

			var size = Unsafe.SizeOf<C2v>();
			var destAddr = (byte*)verts;
			var srcAddr = (byte*)addr;
			Unsafe.CopyBlock(destAddr, srcAddr, (uint)(out_count * size));

			handle.Free();

			return out_count;
		}

		static unsafe void C2Norms(C2v* verts, C2v* norms, int count)
		{
			for (int i = 0; i < count; ++i)
			{
				int a = i;
				int b = i + 1 < count ? i + 1 : 0;
				C2v e = C2Sub(verts[b], verts[a]);
				norms[i] = C2Norm(C2CCW90(e));
			}
		}

		public static unsafe void C2MakePoly(C2Poly* p)
		{
			C2v* vertices = (C2v*)(&p->_verts[0]);
			C2v* normals = (C2v*)(&p->_norms[0]);

			p->count = C2Hull(vertices, p->count);
			C2Norms(vertices, normals, p->count);
		}

		#region Collision Hit Tests

		public static bool C2CircletoCircle(C2Circle A, C2Circle B)
		{
			C2v c = C2Sub(B.p, A.p);
			float d2 = C2Dot(c, c);
			float r2 = A.r + B.r;
			r2 = r2 * r2;

			return d2 < r2;
		}

		public static bool C2CircletoAABB(C2Circle A, C2AABB B)
		{
			C2v L = C2Clampv(A.p, B.min, B.max);
			C2v ab = C2Sub(A.p, L);
			float d2 = C2Dot(ab, ab);
			float r2 = A.r * A.r;
			return d2 < r2;
		}

		public static bool C2AABBtoAABB(C2AABB A, C2AABB B)
		{
			var d0 = B.max.x < A.min.x;
			var d1 = A.max.x < B.min.x;
			var d2 = B.max.y < A.min.y;
			var d3 = A.max.y < B.min.y;
			return !(d0 | d1 | d2 | d3);
		}

		// see: http://www.randygaul.net/2014/07/23/distance-point-to-line-segment/
		public static bool C2CircletoCapsule(C2Circle A, C2Capsule B)
		{
			C2v n = C2Sub(B.b, B.a);
			C2v ap = C2Sub(A.p, B.a);
			float da = C2Dot(ap, n);
			float d2;

			if (da < 0)
				d2 = C2Dot(ap, ap);
			else
			{
				float db = C2Dot(C2Sub(A.p, B.b), n);
				if (db < 0)
				{
					C2v e = C2Sub(ap, C2Mulvs(n, (da / C2Dot(n, n))));
					d2 = C2Dot(e, e);
				}
				else
				{
					C2v bp = C2Sub(A.p, B.b);
					d2 = C2Dot(bp, bp);
				}
			}

			float r = A.r + B.r;
			return d2 < r * r;
		}

		public static unsafe bool C2AABBtoCapsule(C2AABB A, C2Capsule B)
		{
			if (C2GJK(&A, C2_TYPE.C2_AABB, null, &B, C2_TYPE.C2_CAPSULE, null, null, null, 1, null, null) != 0)
				return true;
			return false;
		}

		public static unsafe bool C2CapsuletoCapsule(C2Capsule A, C2Capsule B)
		{
			if (C2GJK(&A, C2_TYPE.C2_CAPSULE, null, &B, C2_TYPE.C2_CAPSULE, null, null, null, 1, null, null) != 0)
				return false;
			return true;
		}

		public static unsafe bool C2CircletoPoly(C2Circle A, C2Poly* B, C2x* bx)
		{
			if (C2GJK(&A, C2_TYPE.C2_CIRCLE, null, B, C2_TYPE.C2_POLY, bx, null, null, 1, null, null) != 0)
				return false;
			return true;
		}

		public static unsafe bool C2AABBtoPoly(C2AABB A, C2Poly* B, C2x* bx)
		{
			if (C2GJK(&A, C2_TYPE.C2_AABB, null, B, C2_TYPE.C2_POLY, bx, null, null, 1, null, null) != 0)
				return false;
			return true;
		}

		public static unsafe bool C2CapsuletoPoly(C2Capsule A, C2Poly* B, C2x* bx)
		{
			if (C2GJK(&A, C2_TYPE.C2_CAPSULE, null, B, C2_TYPE.C2_POLY, bx, null, null, 1, null, null) != 0)
				return false;
			return true;
		}

		public static unsafe bool C2PolytoPoly(C2Poly* A, C2x* ax, C2Poly* B, C2x* bx)
		{
			if (C2GJK(A, C2_TYPE.C2_POLY, ax, B, C2_TYPE.C2_POLY, bx, null, null, 1, null, null) != 0)
				return false;
			return true;
		}

		#endregion

		#region Raycasts

		public static unsafe int C2RaytoCircle(C2Ray A, C2Circle B, C2Raycast* outt)
		{
			C2v p = B.p;
			C2v m = C2Sub(A.p, p);
			float c = C2Dot(m, m) - B.r * B.r;
			float b = C2Dot(m, A.d);
			float disc = b * b - c;
			if (disc < 0)
				return 0;

			float t = -b - C2Sqrt(disc);
			if (t >= 0 && t <= A.t)
			{
				outt->t = t;
				C2v impact = c2Impact(A, t);
				outt->n = C2Norm(C2Sub(impact, p));
				return 1;
			}
			return 0;
		}

		public static unsafe int C2RaytoAABB(C2Ray A, C2AABB B, C2Raycast* outt)
		{
			C2v inv = C2V(1.0f / A.d.x, 1.0f / A.d.y);
			C2v d0 = C2Mulvv(C2Sub(B.min, A.p), inv);
			C2v d1 = C2Mulvv(C2Sub(B.max, A.p), inv);
			C2v v0 = C2Minv(d0, d1);
			C2v v1 = C2Maxv(d0, d1);
			float lo = C2Hmax(v0);
			float hi = C2Hmin(v1);

			if (hi >= 0 && hi >= lo && lo <= A.t)
			{
				C2v c = C2Mulvs(C2Add(B.min, B.max), 0.5f);
				c = C2Sub(c2Impact(A, lo), c);
				C2v abs_c = C2Absv(c);
				if (abs_c.x > abs_c.y)
					outt->n = C2V(C2Sign(c.x), 0);
				else
					outt->n = C2V(0, C2Sign(c.y));
				outt->t = lo;
				return 1;
			}
			return 0;
		}

		public static unsafe int C2RaytoCapsule(C2Ray A, C2Capsule B, C2Raycast* outt)
		{
			C2m M;
			M.y = C2Norm(C2Sub(B.b, B.a));
			M.x = C2CCW90(M.y);

			// rotate capsule to origin, along Y axis
			// rotate the ray same way
			C2v yBb = C2MulmvT(M, C2Sub(B.b, B.a));
			C2v yAp = C2MulmvT(M, C2Sub(A.p, B.a));
			C2v yAd = C2MulmvT(M, A.d);
			C2v yAe = C2Add(yAp, C2Mulvs(yAd, A.t));

			if (yAe.x * yAp.x < 0 || C2Min(C2Abs(yAe.x), C2Abs(yAp.x)) < B.r)
			{
				float c = yAp.x > 0 ? B.r : -B.r;
				float d = (yAe.x - yAp.x);
				float t = (c - yAp.x) / d;
				float y = yAp.y + (yAe.y - yAp.y) * t;

				// hit bottom half-circle
				if (y < 0)
				{
					C2Circle C;
					C.p = B.a;
					C.r = B.r;
					return C2RaytoCircle(A, C, outt);
				}
				// hit top-half circle
				else if (y > yBb.y)
				{
					C2Circle C;
					C.p = B.b;
					C.r = B.r;
					return C2RaytoCircle(A, C, outt);
				}
				// hit the middle of capsule
				else
				{
					outt->n = c > 0 ? M.x : C2Skew(M.y);
					outt->t = t * A.t;
					return 1;
				}
			}

			return 0;
		}

		public static unsafe int C2RaytoPoly(C2Ray A, C2Poly* B, C2x* bx_ptr, C2Raycast* outt)
		{
			C2x bx = bx_ptr != null ? *bx_ptr : C2xIdentity();
			C2v p = C2MulxvT(bx, A.p);
			C2v d = C2MulrvT(bx.r, A.d);
			float lo = 0;
			float hi = A.t;
			int index = -0;

			C2v* vertices = (C2v*)(&B->_verts[0]);
			C2v* normals = (C2v*)(&B->_norms[0]);

			// test ray to each plane, tracking lo/hi times of intersection
			for (int i = 0; i < B->count; ++i)
			{
				float num = C2Dot(normals[i], C2Sub(vertices[i], p));
				float den = C2Dot(normals[i], d);
				if (den == 0 && num < 0)
				{
					return 0;
				}
				else
				{
					if (den < 0 && num < lo * den)
					{
						lo = num / den;
						index = i;
					}
					else if (den > 0 && num < hi * den)
						hi = num / den;
				}
				if (hi < lo) return 0;
			}

			if (index != -1)
			{
				outt->t = lo;
				outt->n = C2Mulrv(bx.r, normals[index]);
				return 1;
			}

			return 0;
		}

		#endregion

		#region Collision Manifolds

		public static unsafe void C2CircletoCircleManifold(C2Circle A, C2Circle B, C2Manifold* m)
		{
			C2v* contact_points = (C2v*)(&m->_contact_points[0]);
			m->count = 0;
			C2v d = C2Sub(B.p, A.p);
			float d2 = C2Dot(d, d);
			float r = A.r + B.r;
			if (d2 < r * r)
			{
				float l = C2Sqrt(d2);
				C2v n = l != 0 ? C2Mulvs(d, 1.0f / l) : C2V(0, 1.0f);
				m->count = 1;
				m->depths[0] = r - l;
				contact_points[0] = C2Sub(B.p, C2Mulvs(n, B.r));
				m->n = n;
			}
		}

		public static unsafe void C2CircletoAABBManifold(C2Circle A, C2AABB B, C2Manifold* m)
		{
			C2v* contact_points = (C2v*)(&m->_contact_points[0]);
			m->count = 0;
			C2v L = C2Clampv(A.p, B.min, B.max);
			C2v ab = C2Sub(L, A.p);
			float d2 = C2Dot(ab, ab);
			float r2 = A.r * A.r;
			if (d2 < r2)
			{
				// shallow (center of circle not inside of AABB)
				if (d2 != 0)
				{
					float d = C2Sqrt(d2);
					C2v n = C2Norm(ab);
					m->count = 1;
					m->depths[0] = A.r - d;
					contact_points[0] = C2Add(A.p, C2Mulvs(n, d));
					m->n = n;
				}

				// deep (center of circle inside of AABB)
				// clamp circle's center to edge of AABB, then form the manifold
				else
				{
					C2v mid = C2Mulvs(C2Add(B.min, B.max), 0.5f);
					C2v e = C2Mulvs(C2Sub(B.max, B.min), 0.5f);
					C2v d = C2Sub(A.p, mid);
					C2v abs_d = C2Absv(d);

					float x_overlap = e.x - abs_d.x;
					float y_overlap = e.y - abs_d.y;

					float depth;
					C2v n;

					if (x_overlap < y_overlap)
					{
						depth = x_overlap;
						n = C2V(1.0f, 0);
						n = C2Mulvs(n, d.x < 0 ? 1.0f : -1.0f);
					}

					else
					{
						depth = y_overlap;
						n = C2V(0, 1.0f);
						n = C2Mulvs(n, d.y < 0 ? 1.0f : -1.0f);
					}

					m->count = 1;
					m->depths[0] = A.r + depth;
					contact_points[0] = C2Sub(A.p, C2Mulvs(n, depth));
					m->n = n;
				}
			}
		}

		public static unsafe void C2CircletoCapsuleManifold(C2Circle A, C2Capsule B, C2Manifold* m)
		{
			C2v* contact_points = (C2v*)(&m->_contact_points[0]);
			m->count = 0;
			C2v a, b;
			float r = A.r + B.r;
			float d = C2GJK(&A, C2_TYPE.C2_CIRCLE, null, &B, C2_TYPE.C2_CAPSULE, null, &a, &b, 0, null, null);
			if (d < r)
			{
				C2v n;
				if (d == 0) n = C2Norm(C2Skew(C2Sub(B.b, B.a)));
				else n = C2Norm(C2Sub(b, a));

				m->count = 1;
				m->depths[0] = r - d;
				contact_points[0] = C2Sub(b, C2Mulvs(n, B.r));
				m->n = n;
			}
		}

		public static unsafe void C2AABBtoAABBManifold(C2AABB A, C2AABB B, C2Manifold* m)
		{
			C2v* contact_points = (C2v*)(&m->_contact_points[0]);
			m->count = 0;
			C2v mid_a = C2Mulvs(C2Add(A.min, A.max), 0.5f);
			C2v mid_b = C2Mulvs(C2Add(B.min, B.max), 0.5f);
			C2v eA = C2Absv(C2Mulvs(C2Sub(A.max, A.min), 0.5f));
			C2v eB = C2Absv(C2Mulvs(C2Sub(B.max, B.min), 0.5f));
			C2v d = C2Sub(mid_b, mid_a);

			// calc overlap on x and y axes
			float dx = eA.x + eB.x - C2Abs(d.x);
			if (dx < 0)
				return;
			float dy = eA.y + eB.y - C2Abs(d.y);
			if (dy < 0)
				return;

			C2v n;
			float depth;
			C2v p;

			// x axis overlap is smaller
			if (dx < dy)
			{
				depth = dx;
				if (d.x < 0)
				{
					n = C2V(-1.0f, 0);
					p = C2Sub(mid_a, C2V(eA.x, 0));
				}
				else
				{
					n = C2V(1.0f, 0);
					p = C2Add(mid_a, C2V(eA.x, 0));
				}
			}
			// y axis overlap is smaller
			else
			{
				depth = dy;
				if (d.y < 0)
				{
					n = C2V(0, -1.0f);
					p = C2Sub(mid_a, C2V(0, eA.y));
				}
				else
				{
					n = C2V(0, 1.0f);
					p = C2Add(mid_a, C2V(0, eA.y));
				}
			}

			m->count = 1;
			contact_points[0] = p;
			m->depths[0] = depth;
			m->n = n;
		}

		public static unsafe void C2AABBtoCapsuleManifold(C2AABB A, C2Capsule B, C2Manifold* m)
		{
			C2Poly p;
			C2v* vertices = (C2v*)(&p._verts[0]);
			C2v* normals = (C2v*)(&p._norms[0]);
			C2BBVerts(vertices, ref A);
			p.count = 4;
			C2Norms(vertices, normals, 4);

			m->count = 0;
			C2CapsuletoPolyManifold(B, &p, null, m);
			m->n = C2Neg(m->n);
		}

		public static unsafe void C2CapsuletoCapsuleManifold(C2Capsule A, C2Capsule B, C2Manifold* m)
		{
			C2v* contact_points = (C2v*)(&m->_contact_points[0]);
			m->count = 0;
			C2v a, b;
			float r = A.r + B.r;
			float d = C2GJK(&A, C2_TYPE.C2_CAPSULE, null, &B, C2_TYPE.C2_CAPSULE, null, &a, &b, 0, null, null);
			if (d < r)
			{
				C2v n;
				if (d == 0) n = C2Norm(C2Skew(C2Sub(A.b, A.a)));
				else n = C2Norm(C2Sub(b, a));

				m->count = 1;
				m->depths[0] = r - d;
				contact_points[0] = C2Sub(b, C2Mulvs(n, B.r));
				m->n = n;
			}
		}

		static unsafe C2h C2PlaneAt(C2Poly* p, int i)
		{
			C2v* vertices = (C2v*)(&p->_verts[0]);
			C2v* normals = (C2v*)(&p->_norms[0]);

			C2h h;
			h.n = normals[i];
			h.d = C2Dot(normals[i], vertices[i]);
			return h;
		}

		public static unsafe void C2CircletoPolyManifold(C2Circle A, C2Poly* B, C2x* bx_tr, C2Manifold* m)
		{
			C2v* contact_points = (C2v*)(&m->_contact_points[0]);
			m->count = 0;
			C2v a, b;
			float d = C2GJK(&A, C2_TYPE.C2_CIRCLE, null, B, C2_TYPE.C2_POLY, bx_tr, &a, &b, 0, null, null);

			// shallow, the circle center did not hit the polygon
			// just use a and b from GJK to define the collision
			if (d != 0)
			{
				C2v n = C2Sub(b, a);
				float l = C2Dot(n, n);
				if (l < A.r * A.r)
				{
					l = C2Sqrt(l);
					m->count = 1;
					contact_points[0] = b;
					m->depths[0] = A.r - l;
					m->n = C2Mulvs(n, 1.0f / l);
				}
			}

			// Circle center is inside the polygon
			// find the face closest to circle center to form manifold
			else
			{
				C2x bx = bx_tr != null ? *bx_tr : C2xIdentity();
				float sep = float.MinValue;
				int index = ~0;
				C2v local = C2MulxvT(bx, A.p);

				for (int i = 0; i < B->count; ++i)
				{
					C2h h = C2PlaneAt(B, i);
					d = C2Dist(h, local);
					if (d > A.r) return;
					if (d > sep)
					{
						sep = d;
						index = i;
					}
				}

				C2h h2 = C2PlaneAt(B, index);
				C2v p = C2Project(h2, local);
				m->count = 1;
				contact_points[0] = C2Mulxv(bx, p);
				m->depths[0] = A.r - sep;

				C2v* normals = (C2v*)(&B->_norms[0]);
				m->n = C2Neg(C2Mulrv(bx.r, normals[index]));
			}
		}

		// Forms a c2Poly and uses c2PolytoPolyManifold
		public static unsafe void C2AABBtoPolyManifold(C2AABB A, C2Poly* B, C2x* bx, C2Manifold* m)
		{
			C2v* vertices = (C2v*)(&B->_verts[0]);
			C2v* normals = (C2v*)(&B->_norms[0]);

			m->count = 0;
			C2Poly p;
			C2BBVerts(vertices, ref A);
			p.count = 4;
			C2Norms(vertices, normals, 4);
			C2PolytoPolyManifold(&p, null, B, bx, m);
		}

		// clip a segment to a plane
		static unsafe int C2Clip(C2v* seg, C2h h)
		{
			var outt = new C2v[2];
			int sp = 0;
			float d0, d1;
			if ((d0 = C2Dist(h, seg[0])) < 0)
				outt[sp++] = seg[0];
			if ((d1 = C2Dist(h, seg[1])) < 0)
				outt[sp++] = seg[1];
			if (d0 == 0 && d1 == 0)
			{
				outt[sp++] = seg[0];
				outt[sp++] = seg[1];
			}
			else if (d0 * d1 <= 0)
				outt[sp++] = C2Intersect(seg[0], seg[1], d0, d1);
			seg[0] = outt[0]; seg[1] = outt[1];
			return sp;
		}

		// clip a segment to the "side planes" of another segment.
		// side planes are planes orthogonal to a segment and attached to the
		// endpoints of the segment
		static unsafe int C2SidePlanes(C2v* seg, C2x x, C2Poly* p, int e, C2h* h)
		{
			C2v* vertices = (C2v*)(&p->_verts[0]);
			C2v ra = C2Mulxv(x, vertices[e]);
			C2v rb = C2Mulxv(x, vertices[e + 1 == p->count ? 0 : e + 1]);
			C2v inn = C2Norm(C2Sub(rb, ra));
			C2h left = new C2h(C2Neg(inn), C2Dot(C2Neg(inn), ra));
			C2h right = new C2h(inn, C2Dot(inn, rb));
			if (C2Clip(seg, left) < 2)
				return 0;
			if (C2Clip(seg, right) < 2)
				return 0;
			if (h != null)
			{
				h->n = C2CCW90(inn);
				h->d = C2Dot(C2CCW90(inn), ra);
			}
			return 1;
		}

		static unsafe void C2KeepDeep(C2v* seg, C2h h, C2Manifold* m)
		{
			C2v* contact_points = (C2v*)(&m->_contact_points[0]);
			int cp = 0;
			for (int i = 0; i < 2; ++i)
			{
				C2v p = seg[i];
				float d = C2Dist(h, p);
				if (d <= 0)
				{
					contact_points[cp] = p;
					m->depths[cp] = -d;
					++cp;
				}
			}
			m->count = cp;
			m->n = h.n;
		}

		static C2v C2CapsuleSupport(C2Capsule A, C2v dir)
		{
			float da = C2Dot(A.a, dir);
			float db = C2Dot(A.b, dir);
			if (da > db)
				return C2Add(A.a, C2Mulvs(dir, A.r));
			return C2Add(A.b, C2Mulvs(dir, A.r));
		}

		static unsafe void C2AntinormalFace(C2Capsule cap, C2Poly* p, C2x x, int* face_out, C2v* n_out)
		{
			float sep = float.MinValue;
			int index = ~0;
			C2v n = C2V(0, 0);
			for (int i = 0; i < p->count; ++i)
			{
				C2h h = C2Mulxh(x, C2PlaneAt(p, i));
				C2v n0 = C2Neg(h.n);
				C2v s = C2CapsuleSupport(cap, n0);
				float d = C2Dist(h, s);
				if (d > sep)
				{
					sep = d;
					index = i;
					n = n0;
				}
			}
			*face_out = index;
			*n_out = n;
		}

		public static unsafe void C2CapsuletoPolyManifold(C2Capsule A, C2Poly* B, C2x* bx_ptr, C2Manifold* m)
		{
			C2v* contact_points = (C2v*)(&m->_contact_points[0]);
			m->count = 0;
			C2v a, b;
			float d = C2GJK(&A, C2_TYPE.C2_CAPSULE, null, B, C2_TYPE.C2_POLY, bx_ptr, &a, &b, 0, null, null);

			// deep, treat as segment to poly collision
			if (d == 0)
			{
				C2x bx = bx_ptr != null ? *bx_ptr : C2xIdentity();
				C2v n;
				int index;
				C2AntinormalFace(A, B, bx, &index, &n);
				var seg = new C2v[] { A.a, A.b };
				C2h h;
				fixed (C2v* seg_ptr = &seg[0])
				{
					if (C2SidePlanes(seg_ptr, bx, B, index, &h) != 0)
						return;
					C2KeepDeep(seg_ptr, h, m);
				}
				for (int i = 0; i < m->count; ++i)
					contact_points[i] = C2Add(contact_points[i], C2Mulvs(n, A.r));
				m->n = C2Neg(m->n);
			}
			// shallow, use GJK results a and b to define manifold
			else if (d < A.r)
			{
				C2x bx = bx_ptr != null ? *bx_ptr : C2xIdentity();
				C2v ab = C2Sub(b, a);
				int face_case = 0;

				C2v* normals = (C2v*)(&B->_norms[0]);
				for (int i = 0; i < B->count; ++i)
				{
					C2v n = C2Mulrv(bx.r, normals[i]);
					if (C2Parallel(C2Neg(ab), n, 5.0e-3f) > 0)
					{
						face_case = 1;
						break;
					}
				}

			one_contact:

				// 1 contact
				if (face_case != 0)
				{
					m->count = 1;
					m->n = C2Norm(ab);
					contact_points[0] = C2Add(a, C2Mulvs(m->n, A.r));
					m->depths[0] = A.r - d;
				}

				// 2 contacts if laying on a polygon face nicely
				else
				{
					C2v n;
					int index;
					C2AntinormalFace(A, B, bx, &index, &n);
					var seg = new C2v[] { C2Add(A.a, C2Mulvs(n, A.r)), C2Add(A.b, C2Mulvs(n, A.r)) };
					C2h h;
					fixed (C2v* seg_ptr = &seg[0])
					{
						if (C2SidePlanes(seg_ptr, bx, B, index, &h) != 0)
						{
							face_case = 1; // need this to get through the if statement
							goto one_contact;
						}
						C2KeepDeep(seg_ptr, h, m);
						m->n = C2Neg(m->n);
					}
				}
			}
		}

		static unsafe float C2CheckFaces(C2Poly* A, C2x ax, C2Poly* B, C2x bx, int* face_index)
		{
			C2x b_in_a = C2MulxxT(ax, bx);
			C2x a_in_b = C2MulxxT(bx, ax);
			float sep = float.MinValue;
			int index = ~0;

			C2v* verts = (C2v*)(&B->_verts[0]);
			for (int i = 0; i < A->count; ++i)
			{
				C2h h = C2PlaneAt(A, i);
				int idx = C2Support(verts, B->count, C2Mulrv(a_in_b.r, C2Neg(h.n)));
				C2v p = C2Mulxv(b_in_a, verts[idx]);
				float d = C2Dist(h, p);
				if (d > sep)
				{
					sep = d;
					index = i;
				}
			}

			*face_index = index;
			return sep;
		}

		static unsafe void C2Incident(C2v* incident, C2Poly* ip, C2x ix, C2Poly* rp, C2x rx, int re)
		{
			C2v* rp_norms = (C2v*)(&rp->_norms[0]);
			C2v* ip_norms = (C2v*)(&ip->_norms[0]);
			C2v* ip_verts = (C2v*)(&ip->_verts[0]);

			C2v n = C2MulrvT(ix.r, C2Mulrv(rx.r, rp_norms[re]));
			int index = ~0;
			float min_dot = float.MaxValue;
			for (int i = 0; i < ip->count; ++i)
			{
				float dot = C2Dot(n, ip_norms[i]);
				if (dot < min_dot)
				{
					min_dot = dot;
					index = i;
				}
			}
			incident[0] = C2Mulxv(ix, ip_verts[index]);
			incident[1] = C2Mulxv(ix, ip_verts[index + 1 == ip->count ? 0 : index + 1]);
		}

		public static unsafe void C2PolytoPolyManifold(C2Poly* A, C2x* ax_ptr, C2Poly* B, C2x* bx_ptr, C2Manifold* m)
		{
			m->count = 0;
			C2x ax = ax_ptr != null ? *ax_ptr : C2xIdentity();
			C2x bx = bx_ptr != null ? *bx_ptr : C2xIdentity();
			int ea, eb;
			float sa, sb;
			if ((sa = C2CheckFaces(A, ax, B, bx, &ea)) >= 0)
				return;
			if ((sb = C2CheckFaces(B, bx, A, ax, &eb)) >= 0)
				return;

			C2Poly* rp, ip;
			C2x rx, ix;
			int re;
			float kRelTol = 0.95f, kAbsTol = 0.01f;
			int flip;
			if (sa * kRelTol > sb + kAbsTol)
			{
				rp = A; rx = ax;
				ip = B; ix = bx;
				re = ea;
				flip = 0;
			}
			else
			{
				rp = B; rx = bx;
				ip = A; ix = ax;
				re = eb;
				flip = 1;
			}

			// var incident = new c2v[2];
			var incident = stackalloc C2v[2];
			C2Incident(incident, ip, ix, rp, rx, re);
			C2h rh;
			if (C2SidePlanes(incident, rx, rp, re, &rh) > 0)
				return;
			C2KeepDeep(incident, rh, m);
			if (flip > 0)
				m->n = C2Neg(m->n);
		}

		#endregion
	}
}

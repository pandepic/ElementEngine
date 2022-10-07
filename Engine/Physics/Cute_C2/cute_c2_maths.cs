/*
 * Ported from https://github.com/RandyGaul/cute_headers/blob/master/cute_c2.h by https://github.com/prime31
 */

using System;

namespace Cute_C2
{
    public static partial class Cute_C2_Collisions
    {
        #region Primitive ops

        static float C2Sin(float radians) => MathF.Sin(radians);
        static float C2Cos(float radians) => MathF.Cos(radians);
        static float C2Sqrt(float a) => MathF.Sqrt(a);
        static float C2Min(float a, float b) => ((a) < (b) ? (a) : (b));
        static int C2Min(int a, int b) => ((a) < (b) ? (a) : (b));
        static float C2Max(float a, float b) => ((a) > (b) ? (a) : (b));
        static float C2Abs(float a) => ((a) < 0 ? -(a) : (a));
        static float C2Clamp(float a, float lo, float hi) => C2Max(lo, C2Min(a, hi));
        static unsafe void C2SinCos(float radians, float* s, float* c)
        {
            *c = C2Cos(radians);
            *s = C2Sin(radians);
        }
        static float C2Sign(float a) => (a < 0 ? -1.0f : 1.0f);

        #endregion

        // The Mul functions are used to perform multiplication. x stands for transform,
        // v stands for vector, s stands for scalar, r stands for rotation, h stands for
        // halfspace and T stands for transpose.For example c2MulxvT stands for "multiply
        // a transform with a vector, and transpose the transform".

        #region vector ops
        
        public static C2v C2V(float x, float y) { C2v a; a.x = x; a.y = y; return a; }
        public static C2v C2Add(C2v a, C2v b) { a.x += b.x; a.y += b.y; return a; }
        public static C2v C2Sub(C2v a, C2v b) { a.x -= b.x; a.y -= b.y; return a; }
        public static float C2Dot(C2v a, C2v b) { return a.x * b.x + a.y * b.y; }
        public static C2v C2Mulvs(C2v a, float b) { a.x *= b; a.y *= b; return a; }
        public static C2v C2Mulvv(C2v a, C2v b) { a.x *= b.x; a.y *= b.y; return a; }
        public static C2v C2Div(C2v a, float b) { return C2Mulvs(a, 1.0f / b); }
        public static C2v C2Skew(C2v a) { C2v b; b.x = -a.y; b.y = a.x; return b; }
        public static C2v C2CCW90(C2v a) { C2v b; b.x = a.y; b.y = -a.x; return b; }
        public static float C2Det2(C2v a, C2v b) { return a.x * b.y - a.y * b.x; }
        public static C2v C2Minv(C2v a, C2v b) { return C2V(C2Min(a.x, b.x), C2Min(a.y, b.y)); }
        public static C2v C2Maxv(C2v a, C2v b) { return C2V(C2Max(a.x, b.x), C2Max(a.y, b.y)); }
        public static C2v C2Clampv(C2v a, C2v lo, C2v hi) { return C2Maxv(lo, C2Minv(a, hi)); }
        public static C2v C2Absv(C2v a) { return C2V(C2Abs(a.x), C2Abs(a.y)); }
        public static float C2Hmin(C2v a) { return C2Min(a.x, a.y); }
        public static float C2Hmax(C2v a) { return C2Max(a.x, a.y); }
        public static float C2Len(C2v a) { return C2Sqrt(C2Dot(a, a)); }
        public static C2v C2Norm(C2v a) { return C2Div(a, C2Len(a)); }
        public static C2v C2SafeNorm(C2v a) { float sq = C2Dot(a, a); return sq != 0 ? C2Div(a, C2Len(a)) : C2V(0, 0); }
        public static C2v C2Neg(C2v a) { return C2V(-a.x, -a.y); }
        public static C2v C2Lerp(C2v a, C2v b, float t) { return C2Add(a, C2Mulvs(C2Sub(b, a), t)); }
        public static int C2Parallel(C2v a, C2v b, float kTol)
        {
            float k = C2Len(a) / C2Len(b);
            b = C2Mulvs(b, k);
            if (C2Abs(a.x - b.x) < kTol && C2Abs(a.y - b.y) < kTol) return 1;
            return 0;
        }

        #endregion

        #region rotation ops

        public static unsafe C2r C2Rot(float radians) { C2r r; C2SinCos(radians, &r.s, &r.c); return r; }
        public static C2r C2RotIdentity() { C2r r; r.c = 1.0f; r.s = 0; return r; }
        public static C2v C2RotX(C2r r) { return C2V(r.c, r.s); }
        public static C2v C2RotY(C2r r) { return C2V(-r.s, r.c); }
        public static C2v C2Mulrv(C2r a, C2v b) { return C2V(a.c * b.x - a.s * b.y, a.s * b.x + a.c * b.y); }
        public static C2v C2MulrvT(C2r a, C2v b) { return C2V(a.c * b.x + a.s * b.y, -a.s * b.x + a.c * b.y); }
        public static C2r C2Mulrr(C2r a, C2r b) { C2r c; c.c = a.c * b.c - a.s * b.s; c.s = a.s * b.c + a.c * b.s; return c; }
        public static C2r C2MulrrT(C2r a, C2r b) { C2r c; c.c = a.c * b.c + a.s * b.s; c.s = a.c * b.s - a.s * b.c; return c; }

        public static C2v C2Mulmv(C2m a, C2v b) { C2v c; c.x = a.x.x * b.x + a.y.x * b.y; c.y = a.x.y * b.x + a.y.y * b.y; return c; }
        public static C2v C2MulmvT(C2m a, C2v b) { C2v c; c.x = a.x.x * b.x + a.x.y * b.y; c.y = a.y.x * b.x + a.y.y * b.y; return c; }
        public static C2m C2Mulmm(C2m a, C2m b) { C2m c; c.x = C2Mulmv(a, b.x); c.y = C2Mulmv(a, b.y); return c; }
        public static C2m C2MulmmT(C2m a, C2m b) { C2m c; c.x = C2MulmvT(a, b.x); c.y = C2MulmvT(a, b.y); return c; }

        #endregion

        #region transform ops

        public static C2x C2xIdentity() { C2x x; x.p = C2V(0, 0); x.r = C2RotIdentity(); return x; }
        public static C2v C2Mulxv(C2x a, C2v b) { return C2Add(C2Mulrv(a.r, b), a.p); }
        public static C2v C2MulxvT(C2x a, C2v b) { return C2MulrvT(a.r, C2Sub(b, a.p)); }
        public static C2x C2Mulxx(C2x a, C2x b) { C2x c; c.r = C2Mulrr(a.r, b.r); c.p = C2Add(C2Mulrv(a.r, b.p), a.p); return c; }
        public static C2x C2MulxxT(C2x a, C2x b) { C2x c; c.r = C2MulrrT(a.r, b.r); c.p = C2MulrvT(a.r, C2Sub(b.p, a.p)); return c; }
        public static C2x C2Transform(C2v p, float radians) { C2x x; x.r = C2Rot(radians); x.p = p; return x; }

        #endregion

        #region halfspace ops

        public static C2v C2Origin(C2h h) { return C2Mulvs(h.n, h.d); }
        public static float C2Dist(C2h h, C2v p) { return C2Dot(h.n, p) - h.d; }
        public static C2v C2Project(C2h h, C2v p) { return C2Sub(p, C2Mulvs(h.n, C2Dist(h, p))); }
        public static C2h C2Mulxh(C2x a, C2h b) { C2h c; c.n = C2Mulrv(a.r, b.n); c.d = C2Dot(C2Mulxv(a, C2Origin(b)), c.n); return c; }
        public static C2h C2MulxhT(C2x a, C2h b) { C2h c; c.n = C2MulrvT(a.r, b.n); c.d = C2Dot(C2MulxvT(a, C2Origin(b)), c.n); return c; }
        public static C2v C2Intersect(C2v a, C2v b, float da, float db) { return C2Add(a, C2Mulvs(C2Sub(b, a), (da / (da - db)))); }

        #endregion

        public static unsafe void C2BBVerts(C2v* verts, ref C2AABB bb)
        {
            verts[0] = bb.min;
            verts[1] = C2V(bb.max.x, bb.min.y);
            verts[2] = bb.max;
            verts[3] = C2V(bb.min.x, bb.max.y);
        }
    }
}

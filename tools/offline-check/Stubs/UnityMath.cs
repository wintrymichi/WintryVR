// Unity math types: real implementations behind Unity's real signatures, so the project's own
// logic can be both compile-checked and executed offline.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float this[int i]
        {
            get { return i == 0 ? x : y; }
            set { if (i == 0) x = value; else y = value; }
        }
        public static Vector2 zero { get { return new Vector2(0f, 0f); } }
        public static Vector2 one { get { return new Vector2(1f, 1f); } }
        public static Vector2 up { get { return new Vector2(0f, 1f); } }
        public static Vector2 down { get { return new Vector2(0f, -1f); } }
        public static Vector2 left { get { return new Vector2(-1f, 0f); } }
        public static Vector2 right { get { return new Vector2(1f, 0f); } }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public Vector2 normalized { get { float m = magnitude; return m > 1e-9f ? new Vector2(x / m, y / m) : zero; } }
        public void Normalize() { var n = normalized; x = n.x; y = n.y; }
        public void Set(float newX, float newY) { x = newX; y = newY; }
        public static float Distance(Vector2 a, Vector2 b) { return (a - b).magnitude; }
        public static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { t = Mathf.Clamp01(t); return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t); }
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) { return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t); }
        public static Vector2 MoveTowards(Vector2 a, Vector2 b, float d)
        {
            Vector2 delta = b - a; float m = delta.magnitude;
            return (m <= d || m < 1e-9f) ? b : a + delta / m * d;
        }
        public static Vector2 Scale(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
        public static Vector2 Min(Vector2 a, Vector2 b) { return new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y)); }
        public static Vector2 Max(Vector2 a, Vector2 b) { return new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)); }
        public static Vector2 ClampMagnitude(Vector2 v, float m) { return v.sqrMagnitude > m * m ? v.normalized * m : v; }
        public static Vector2 Perpendicular(Vector2 v) { return new Vector2(-v.y, v.x); }
        public static float Angle(Vector2 a, Vector2 b)
        {
            float denom = (float)Math.Sqrt(a.sqrMagnitude * (double)b.sqrMagnitude);
            if (denom < 1e-15f) return 0f;
            return (float)Math.Acos(Mathf.Clamp(Dot(a, b) / denom, -1f, 1f)) * Mathf.Rad2Deg;
        }
        public static float SignedAngle(Vector2 a, Vector2 b) { return Angle(a, b) * Mathf.Sign(a.x * b.y - a.y * b.x); }
        public static Vector2 SmoothDamp(Vector2 cur, Vector2 target, ref Vector2 vel, float smoothTime)
        { return SmoothDamp(cur, target, ref vel, smoothTime, float.PositiveInfinity, 0.02f); }
        public static Vector2 SmoothDamp(Vector2 cur, Vector2 target, ref Vector2 vel, float smoothTime, float maxSpeed)
        { return SmoothDamp(cur, target, ref vel, smoothTime, maxSpeed, 0.02f); }
        public static Vector2 SmoothDamp(Vector2 cur, Vector2 target, ref Vector2 vel, float smoothTime, float maxSpeed, float deltaTime)
        {
            float vx = vel.x, vy = vel.y;
            float rx = Mathf.SmoothDamp(cur.x, target.x, ref vx, smoothTime, maxSpeed, deltaTime);
            float ry = Mathf.SmoothDamp(cur.y, target.y, ref vy, smoothTime, maxSpeed, deltaTime);
            vel = new Vector2(vx, vy);
            return new Vector2(rx, ry);
        }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator -(Vector2 a) { return new Vector2(-a.x, -a.y); }
        public static Vector2 operator *(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator *(float d, Vector2 a) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator *(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
        public static Vector2 operator /(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }
        public static bool operator ==(Vector2 a, Vector2 b) { return (a - b).sqrMagnitude < 1e-10f; }
        public static bool operator !=(Vector2 a, Vector2 b) { return !(a == b); }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0f); }
        public override bool Equals(object o) { return o is Vector2 && this == (Vector2)o; }
        public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2); }
        public override string ToString() { return "(" + x.ToString("F2") + ", " + y.ToString("F2") + ")"; }
        public string ToString(string format) { return "(" + x.ToString(format) + ", " + y.ToString(format) + ")"; }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0f; }
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float this[int i]
        {
            get { return i == 0 ? x : (i == 1 ? y : z); }
            set { if (i == 0) x = value; else if (i == 1) y = value; else z = value; }
        }
        public static Vector3 zero { get { return new Vector3(0f, 0f, 0f); } }
        public static Vector3 one { get { return new Vector3(1f, 1f, 1f); } }
        public static Vector3 up { get { return new Vector3(0f, 1f, 0f); } }
        public static Vector3 down { get { return new Vector3(0f, -1f, 0f); } }
        public static Vector3 left { get { return new Vector3(-1f, 0f, 0f); } }
        public static Vector3 right { get { return new Vector3(1f, 0f, 0f); } }
        public static Vector3 forward { get { return new Vector3(0f, 0f, 1f); } }
        public static Vector3 back { get { return new Vector3(0f, 0f, -1f); } }
        public static Vector3 positiveInfinity { get { return new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity); } }
        public static Vector3 negativeInfinity { get { return new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity); } }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y + z * z); } }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public Vector3 normalized { get { float m = magnitude; return m > 1e-9f ? new Vector3(x / m, y / m, z / m) : zero; } }
        public void Normalize() { var n = normalized; x = n.x; y = n.y; z = n.z; }
        public void Set(float newX, float newY, float newZ) { x = newX; y = newY; z = newZ; }
        public void Scale(Vector3 s) { x *= s.x; y *= s.y; z *= s.z; }
        public static float Distance(Vector3 a, Vector3 b) { return (a - b).magnitude; }
        public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
        public static Vector3 Cross(Vector3 a, Vector3 b)
        { return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }
        public static Vector3 Normalize(Vector3 v) { return v.normalized; }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return LerpUnclamped(a, b, t); }
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t)
        { return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t); }
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            float ma = a.magnitude, mb = b.magnitude;
            if (ma < 1e-9f || mb < 1e-9f) return LerpUnclamped(a, b, t);
            Vector3 na = a / ma, nb = b / mb;
            float dot = Mathf.Clamp(Dot(na, nb), -1f, 1f);
            float theta = (float)Math.Acos(dot) * t;
            Vector3 rel = (nb - na * dot);
            if (rel.sqrMagnitude > 1e-12f) rel = rel.normalized;
            Vector3 dir = na * (float)Math.Cos(theta) + rel * (float)Math.Sin(theta);
            return dir * Mathf.Lerp(ma, mb, t);
        }
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float d)
        {
            Vector3 delta = b - a; float m = delta.magnitude;
            return (m <= d || m < 1e-9f) ? b : a + delta / m * d;
        }
        public static Vector3 RotateTowards(Vector3 a, Vector3 b, float maxRad, float maxMag)
        {
            float ma = a.magnitude, mb = b.magnitude;
            float target = Mathf.MoveTowards(ma, mb, maxMag);
            if (ma < 1e-9f || mb < 1e-9f) return b.normalized * target;
            float angle = (float)Math.Acos(Mathf.Clamp(Dot(a.normalized, b.normalized), -1f, 1f));
            float t = angle < 1e-9f ? 1f : Mathf.Clamp01(maxRad / angle);
            return Slerp(a.normalized, b.normalized, t) * target;
        }
        public static Vector3 Scale(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
        public static Vector3 Min(Vector3 a, Vector3 b) { return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z)); }
        public static Vector3 Max(Vector3 a, Vector3 b) { return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z)); }
        public static Vector3 ClampMagnitude(Vector3 v, float m) { return v.sqrMagnitude > m * m ? v.normalized * m : v; }
        public static Vector3 Project(Vector3 v, Vector3 n)
        {
            float d = Dot(n, n);
            return d < 1e-12f ? zero : n * (Dot(v, n) / d);
        }
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 n) { return v - Project(v, n); }
        public static Vector3 Reflect(Vector3 v, Vector3 n) { return v - n * (2f * Dot(v, n)); }
        public static float Angle(Vector3 a, Vector3 b)
        {
            float denom = (float)Math.Sqrt(a.sqrMagnitude * (double)b.sqrMagnitude);
            if (denom < 1e-15f) return 0f;
            return (float)Math.Acos(Mathf.Clamp(Dot(a, b) / denom, -1f, 1f)) * Mathf.Rad2Deg;
        }
        public static float SignedAngle(Vector3 a, Vector3 b, Vector3 axis)
        { return Angle(a, b) * Mathf.Sign(Dot(axis, Cross(a, b))); }
        public static Vector3 SmoothDamp(Vector3 cur, Vector3 target, ref Vector3 vel, float smoothTime)
        { return SmoothDamp(cur, target, ref vel, smoothTime, float.PositiveInfinity, 0.02f); }
        public static Vector3 SmoothDamp(Vector3 cur, Vector3 target, ref Vector3 vel, float smoothTime, float maxSpeed)
        { return SmoothDamp(cur, target, ref vel, smoothTime, maxSpeed, 0.02f); }
        public static Vector3 SmoothDamp(Vector3 cur, Vector3 target, ref Vector3 vel, float smoothTime, float maxSpeed, float deltaTime)
        {
            float vx = vel.x, vy = vel.y, vz = vel.z;
            float rx = Mathf.SmoothDamp(cur.x, target.x, ref vx, smoothTime, maxSpeed, deltaTime);
            float ry = Mathf.SmoothDamp(cur.y, target.y, ref vy, smoothTime, maxSpeed, deltaTime);
            float rz = Mathf.SmoothDamp(cur.z, target.z, ref vz, smoothTime, maxSpeed, deltaTime);
            vel = new Vector3(vx, vy, vz);
            return new Vector3(rx, ry, rz);
        }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
        public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator *(float d, Vector3 a) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator /(Vector3 a, float d) { return new Vector3(a.x / d, a.y / d, a.z / d); }
        public static bool operator ==(Vector3 a, Vector3 b) { return (a - b).sqrMagnitude < 1e-10f; }
        public static bool operator !=(Vector3 a, Vector3 b) { return !(a == b); }
        public override bool Equals(object o) { return o is Vector3 && this == (Vector3)o; }
        public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2); }
        public override string ToString() { return "(" + x.ToString("F2") + ", " + y.ToString("F2") + ", " + z.ToString("F2") + ")"; }
        public string ToString(string format) { return "(" + x.ToString(format) + ", " + y.ToString(format) + ", " + z.ToString(format) + ")"; }
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public Vector4(float x, float y, float z) { this.x = x; this.y = y; this.z = z; this.w = 0f; }
        public static Vector4 zero { get { return new Vector4(0f, 0f, 0f, 0f); } }
        public static Vector4 one { get { return new Vector4(1f, 1f, 1f, 1f); } }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y + z * z + w * w); } }
        public Vector4 normalized { get { float m = magnitude; return m > 1e-9f ? new Vector4(x / m, y / m, z / m, w / m) : zero; } }
        public static Vector4 operator +(Vector4 a, Vector4 b) { return new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w); }
        public static Vector4 operator -(Vector4 a, Vector4 b) { return new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w); }
        public static Vector4 operator *(Vector4 a, float d) { return new Vector4(a.x * d, a.y * d, a.z * d, a.w * d); }
        public static implicit operator Vector4(Vector3 v) { return new Vector4(v.x, v.y, v.z, 0f); }
        public static implicit operator Vector3(Vector4 v) { return new Vector3(v.x, v.y, v.z); }
        public static implicit operator Vector4(Vector2 v) { return new Vector4(v.x, v.y, 0f, 0f); }
        public override string ToString() { return "(" + x + ", " + y + ", " + z + ", " + w + ")"; }
    }

    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static Vector2Int zero { get { return new Vector2Int(0, 0); } }
        public static Vector2Int one { get { return new Vector2Int(1, 1); } }
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x + b.x, a.y + b.y); }
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x - b.x, a.y - b.y); }
        public static bool operator ==(Vector2Int a, Vector2Int b) { return a.x == b.x && a.y == b.y; }
        public static bool operator !=(Vector2Int a, Vector2Int b) { return !(a == b); }
        public override bool Equals(object o) { return o is Vector2Int && this == (Vector2Int)o; }
        public override int GetHashCode() { return x ^ (y << 2); }
        public override string ToString() { return "(" + x + ", " + y + ")"; }
    }

    public struct Vector3Int
    {
        public int x, y, z;
        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3Int zero { get { return new Vector3Int(0, 0, 0); } }
        public static Vector3Int one { get { return new Vector3Int(1, 1, 1); } }
        public static Vector3Int operator +(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3Int operator -(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static bool operator ==(Vector3Int a, Vector3Int b) { return a.x == b.x && a.y == b.y && a.z == b.z; }
        public static bool operator !=(Vector3Int a, Vector3Int b) { return !(a == b); }
        public override bool Equals(object o) { return o is Vector3Int && this == (Vector3Int)o; }
        public override int GetHashCode() { return x ^ (y << 2) ^ (z >> 2); }
        public override string ToString() { return "(" + x + ", " + y + ", " + z + ")"; }
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity { get { return new Quaternion(0f, 0f, 0f, 1f); } }

        public Vector3 eulerAngles
        {
            get
            {
                float sinp = 2f * (w * x - y * z);
                float px = Math.Abs(sinp) >= 1f ? Mathf.Sign(sinp) * Mathf.PI / 2f : (float)Math.Asin(sinp);
                float py = (float)Math.Atan2(2f * (w * y + x * z), 1f - 2f * (x * x + y * y));
                float pz = (float)Math.Atan2(2f * (w * z + x * y), 1f - 2f * (x * x + z * z));
                return new Vector3(Norm(px * Mathf.Rad2Deg), Norm(py * Mathf.Rad2Deg), Norm(pz * Mathf.Rad2Deg));
            }
            set { this = Euler(value); }
        }

        private static float Norm(float d) { d %= 360f; return d < 0f ? d + 360f : d; }

        public Quaternion normalized
        {
            get { float m = (float)Math.Sqrt(x * x + y * y + z * z + w * w); return m < 1e-9f ? identity : new Quaternion(x / m, y / m, z / m, w / m); }
        }
        public void Normalize() { this = normalized; }
        public void Set(float nx, float ny, float nz, float nw) { x = nx; y = ny; z = nz; w = nw; }
        public void SetLookRotation(Vector3 view) { this = LookRotation(view); }
        public void SetLookRotation(Vector3 view, Vector3 up) { this = LookRotation(view, up); }
        public void ToAngleAxis(out float angle, out Vector3 axis)
        {
            var q = normalized;
            angle = 2f * (float)Math.Acos(Mathf.Clamp(q.w, -1f, 1f)) * Mathf.Rad2Deg;
            float s = (float)Math.Sqrt(Math.Max(0d, 1d - q.w * (double)q.w));
            axis = s < 1e-6f ? Vector3.right : new Vector3(q.x / s, q.y / s, q.z / s);
        }

        public static Quaternion Euler(float ex, float ey, float ez)
        {
            float hx = ex * Mathf.Deg2Rad * 0.5f, hy = ey * Mathf.Deg2Rad * 0.5f, hz = ez * Mathf.Deg2Rad * 0.5f;
            float cx = (float)Math.Cos(hx), sx = (float)Math.Sin(hx);
            float cy = (float)Math.Cos(hy), sy = (float)Math.Sin(hy);
            float cz = (float)Math.Cos(hz), sz = (float)Math.Sin(hz);
            // Unity applies Z, then X, then Y.
            return new Quaternion(
                cy * sx * cz + sy * cx * sz,
                sy * cx * cz - cy * sx * sz,
                cy * cx * sz - sy * sx * cz,
                cy * cx * cz + sy * sx * sz).normalized;
        }
        public static Quaternion Euler(Vector3 euler) { return Euler(euler.x, euler.y, euler.z); }

        public static Quaternion AngleAxis(float angle, Vector3 axis)
        {
            Vector3 a = axis.normalized;
            float h = angle * Mathf.Deg2Rad * 0.5f;
            float s = (float)Math.Sin(h);
            return new Quaternion(a.x * s, a.y * s, a.z * s, (float)Math.Cos(h));
        }

        public static Quaternion LookRotation(Vector3 forward) { return LookRotation(forward, Vector3.up); }
        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards)
        {
            Vector3 f = forward.normalized;
            if (f.sqrMagnitude < 1e-12f) return identity;
            Vector3 r = Vector3.Cross(upwards, f);
            if (r.sqrMagnitude < 1e-12f) r = Vector3.Cross(Math.Abs(f.y) > 0.99f ? Vector3.forward : Vector3.up, f);
            r = r.normalized;
            Vector3 u = Vector3.Cross(f, r);
            float m00 = r.x, m01 = u.x, m02 = f.x;
            float m10 = r.y, m11 = u.y, m12 = f.y;
            float m20 = r.z, m21 = u.z, m22 = f.z;
            float trace = m00 + m11 + m22;
            if (trace > 0f)
            {
                float s = (float)Math.Sqrt(trace + 1f) * 2f;
                return new Quaternion((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, 0.25f * s).normalized;
            }
            if (m00 > m11 && m00 > m22)
            {
                float s = (float)Math.Sqrt(1f + m00 - m11 - m22) * 2f;
                return new Quaternion(0.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s).normalized;
            }
            if (m11 > m22)
            {
                float s = (float)Math.Sqrt(1f + m11 - m00 - m22) * 2f;
                return new Quaternion((m01 + m10) / s, 0.25f * s, (m12 + m21) / s, (m02 - m20) / s).normalized;
            }
            float s2 = (float)Math.Sqrt(1f + m22 - m00 - m11) * 2f;
            return new Quaternion((m02 + m20) / s2, (m12 + m21) / s2, 0.25f * s2, (m10 - m01) / s2).normalized;
        }

        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            Vector3 a = from.normalized, b = to.normalized;
            float d = Mathf.Clamp(Vector3.Dot(a, b), -1f, 1f);
            if (d > 0.999999f) return identity;
            if (d < -0.999999f)
            {
                Vector3 axis = Vector3.Cross(Vector3.right, a);
                if (axis.sqrMagnitude < 1e-9f) axis = Vector3.Cross(Vector3.up, a);
                return AngleAxis(180f, axis.normalized);
            }
            return AngleAxis((float)Math.Acos(d) * Mathf.Rad2Deg, Vector3.Cross(a, b).normalized);
        }

        public static Quaternion Inverse(Quaternion r)
        {
            var q = r.normalized;
            return new Quaternion(-q.x, -q.y, -q.z, q.w);
        }

        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) { return LerpUnclamped(a, b, Mathf.Clamp01(t)); }
        private static Quaternion LerpUnclamped(Quaternion a, Quaternion b, float t)
        {
            if (Dot(a, b) < 0f) b = new Quaternion(-b.x, -b.y, -b.z, -b.w);
            return new Quaternion(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t).normalized;
        }
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) { return SlerpUnclamped(a, b, Mathf.Clamp01(t)); }
        public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t)
        {
            a = a.normalized; b = b.normalized;
            float dot = Dot(a, b);
            if (dot < 0f) { b = new Quaternion(-b.x, -b.y, -b.z, -b.w); dot = -dot; }
            if (dot > 0.9995f) return LerpUnclamped(a, b, t);
            float theta0 = (float)Math.Acos(Mathf.Clamp(dot, -1f, 1f));
            float theta = theta0 * t;
            float sin0 = (float)Math.Sin(theta0);
            float s0 = (float)Math.Sin(theta0 - theta) / sin0;
            float s1 = (float)Math.Sin(theta) / sin0;
            return new Quaternion(a.x * s0 + b.x * s1, a.y * s0 + b.y * s1, a.z * s0 + b.z * s1, a.w * s0 + b.w * s1).normalized;
        }
        public static Quaternion RotateTowards(Quaternion from, Quaternion to, float maxDegrees)
        {
            float angle = Angle(from, to);
            return angle < 1e-6f ? to : Slerp(from, to, Mathf.Min(1f, maxDegrees / angle));
        }
        public static float Angle(Quaternion a, Quaternion b)
        {
            float dot = Math.Abs(Mathf.Clamp(Dot(a.normalized, b.normalized), -1f, 1f));
            return dot > 0.999999f ? 0f : (float)Math.Acos(dot) * 2f * Mathf.Rad2Deg;
        }
        public static float Dot(Quaternion a, Quaternion b) { return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w; }
        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(
                a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
                a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
                a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x,
                a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        }
        public static Vector3 operator *(Quaternion r, Vector3 p)
        {
            float nx = r.x * 2f, ny = r.y * 2f, nz = r.z * 2f;
            float xx = r.x * nx, yy = r.y * ny, zz = r.z * nz;
            float xy = r.x * ny, xz = r.x * nz, yz = r.y * nz;
            float wx = r.w * nx, wy = r.w * ny, wz = r.w * nz;
            return new Vector3(
                (1f - (yy + zz)) * p.x + (xy - wz) * p.y + (xz + wy) * p.z,
                (xy + wz) * p.x + (1f - (xx + zz)) * p.y + (yz - wx) * p.z,
                (xz - wy) * p.x + (yz + wx) * p.y + (1f - (xx + yy)) * p.z);
        }
        public static bool operator ==(Quaternion a, Quaternion b) { return Dot(a, b) > 0.999999f; }
        public static bool operator !=(Quaternion a, Quaternion b) { return !(a == b); }
        public override bool Equals(object o) { return o is Quaternion && this == (Quaternion)o; }
        public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2) ^ (w.GetHashCode() >> 1); }
        public override string ToString() { return "(" + x + ", " + y + ", " + z + ", " + w + ")"; }
    }

    public struct Matrix4x4
    {
        private float[] m;
        private float[] M { get { if (m == null) m = new float[16]; return m; } }
        public static Matrix4x4 identity
        {
            get { var r = new Matrix4x4(); r.M[0] = 1f; r.M[5] = 1f; r.M[10] = 1f; r.M[15] = 1f; return r; }
        }
        public static Matrix4x4 zero { get { var r = new Matrix4x4(); var _ = r.M; return r; } }
        public float this[int row, int column] { get { return M[row + column * 4]; } set { M[row + column * 4] = value; } }
        public float this[int index] { get { return M[index]; } set { M[index] = value; } }
        public Matrix4x4 inverse
        {
            get
            {
                // Affine inverse: transpose of the rotation/scale block, then re-apply translation.
                var r = identity;
                for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) r[i, j] = this[j, i];
                Vector3 t = new Vector3(this[0, 3], this[1, 3], this[2, 3]);
                Vector3 it = new Vector3(
                    -(r[0, 0] * t.x + r[0, 1] * t.y + r[0, 2] * t.z),
                    -(r[1, 0] * t.x + r[1, 1] * t.y + r[1, 2] * t.z),
                    -(r[2, 0] * t.x + r[2, 1] * t.y + r[2, 2] * t.z));
                r[0, 3] = it.x; r[1, 3] = it.y; r[2, 3] = it.z;
                return r;
            }
        }
        public Matrix4x4 transpose
        {
            get { var r = new Matrix4x4(); for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) r[i, j] = this[j, i]; return r; }
        }
        public Vector3 MultiplyPoint(Vector3 p) { return MultiplyPoint3x4(p); }
        public Vector3 MultiplyPoint3x4(Vector3 p)
        {
            return new Vector3(
                this[0, 0] * p.x + this[0, 1] * p.y + this[0, 2] * p.z + this[0, 3],
                this[1, 0] * p.x + this[1, 1] * p.y + this[1, 2] * p.z + this[1, 3],
                this[2, 0] * p.x + this[2, 1] * p.y + this[2, 2] * p.z + this[2, 3]);
        }
        public Vector3 MultiplyVector(Vector3 v)
        {
            return new Vector3(
                this[0, 0] * v.x + this[0, 1] * v.y + this[0, 2] * v.z,
                this[1, 0] * v.x + this[1, 1] * v.y + this[1, 2] * v.z,
                this[2, 0] * v.x + this[2, 1] * v.y + this[2, 2] * v.z);
        }
        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s)
        {
            var r = Rotate(q);
            for (int i = 0; i < 3; i++) { r[i, 0] *= s.x; r[i, 1] *= s.y; r[i, 2] *= s.z; }
            r[0, 3] = pos.x; r[1, 3] = pos.y; r[2, 3] = pos.z;
            return r;
        }
        public static Matrix4x4 Translate(Vector3 v) { var r = identity; r[0, 3] = v.x; r[1, 3] = v.y; r[2, 3] = v.z; return r; }
        public static Matrix4x4 Rotate(Quaternion q)
        {
            var r = identity;
            Vector3 cx = q * Vector3.right, cy = q * Vector3.up, cz = q * Vector3.forward;
            r[0, 0] = cx.x; r[1, 0] = cx.y; r[2, 0] = cx.z;
            r[0, 1] = cy.x; r[1, 1] = cy.y; r[2, 1] = cy.z;
            r[0, 2] = cz.x; r[1, 2] = cz.y; r[2, 2] = cz.z;
            return r;
        }
        public static Matrix4x4 Scale(Vector3 v) { var r = identity; r[0, 0] = v.x; r[1, 1] = v.y; r[2, 2] = v.z; return r; }
        public static Matrix4x4 Perspective(float fov, float aspect, float zNear, float zFar)
        {
            float f = 1f / (float)Math.Tan(fov * Mathf.Deg2Rad * 0.5f);
            var r = zero;
            r[0, 0] = f / aspect; r[1, 1] = f;
            r[2, 2] = (zFar + zNear) / (zNear - zFar);
            r[2, 3] = 2f * zFar * zNear / (zNear - zFar);
            r[3, 2] = -1f;
            return r;
        }
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            var r = new Matrix4x4();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    float s = 0f;
                    for (int k = 0; k < 4; k++) s += a[i, k] * b[k, j];
                    r[i, j] = s;
                }
            return r;
        }
    }

    public struct Pose
    {
        public Vector3 position;
        public Quaternion rotation;
        public Pose(Vector3 position, Quaternion rotation) { this.position = position; this.rotation = rotation; }
        public static Pose identity { get { return new Pose(Vector3.zero, Quaternion.identity); } }
        public Vector3 forward { get { return rotation * Vector3.forward; } }
        public Vector3 right { get { return rotation * Vector3.right; } }
        public Vector3 up { get { return rotation * Vector3.up; } }
        public Pose GetTransformedBy(Pose lhs) { return new Pose(lhs.position + lhs.rotation * position, lhs.rotation * rotation); }
        public Pose GetTransformedBy(Transform transform)
        {
            return transform == null ? this : new Pose(transform.position + transform.rotation * position, transform.rotation * rotation);
        }
        public override string ToString() { return position + ", " + rotation; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public float this[int i]
        {
            get { return i == 0 ? r : (i == 1 ? g : (i == 2 ? b : a)); }
            set { if (i == 0) r = value; else if (i == 1) g = value; else if (i == 2) b = value; else a = value; }
        }
        public static Color red { get { return new Color(1f, 0f, 0f, 1f); } }
        public static Color green { get { return new Color(0f, 1f, 0f, 1f); } }
        public static Color blue { get { return new Color(0f, 0f, 1f, 1f); } }
        public static Color white { get { return new Color(1f, 1f, 1f, 1f); } }
        public static Color black { get { return new Color(0f, 0f, 0f, 1f); } }
        public static Color yellow { get { return new Color(1f, 0.9215686f, 0.01568628f, 1f); } }
        public static Color cyan { get { return new Color(0f, 1f, 1f, 1f); } }
        public static Color magenta { get { return new Color(1f, 0f, 1f, 1f); } }
        public static Color gray { get { return new Color(0.5f, 0.5f, 0.5f, 1f); } }
        public static Color grey { get { return new Color(0.5f, 0.5f, 0.5f, 1f); } }
        public static Color clear { get { return new Color(0f, 0f, 0f, 0f); } }
        public float grayscale { get { return 0.299f * r + 0.587f * g + 0.114f * b; } }
        public Color linear { get { return new Color(GammaToLinear(r), GammaToLinear(g), GammaToLinear(b), a); } }
        public Color gamma { get { return new Color(LinearToGamma(r), LinearToGamma(g), LinearToGamma(b), a); } }
        public float maxColorComponent { get { return Mathf.Max(Mathf.Max(r, g), b); } }

        private static float GammaToLinear(float c)
        { return c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4d); }
        private static float LinearToGamma(float c)
        { return c <= 0.0031308f ? c * 12.92f : 1.055f * (float)Math.Pow(c, 1d / 2.4d) - 0.055f; }

        public static Color Lerp(Color a, Color b, float t) { return LerpUnclamped(a, b, Mathf.Clamp01(t)); }
        public static Color LerpUnclamped(Color x, Color y, float t)
        { return new Color(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t); }

        public static Color HSVToRGB(float h, float s, float v) { return HSVToRGB(h, s, v, true); }
        public static Color HSVToRGB(float h, float s, float v, bool hdr)
        {
            if (s < 1e-6f) return new Color(v, v, v, 1f);
            h = h < 0f ? 0f : (h >= 1f ? h % 1f : h);
            float sector = h * 6f;
            int i = (int)Math.Floor(sector);
            float f = sector - i;
            float p = v * (1f - s), q = v * (1f - s * f), t = v * (1f - s * (1f - f));
            switch (i % 6)
            {
                case 0: return new Color(v, t, p, 1f);
                case 1: return new Color(q, v, p, 1f);
                case 2: return new Color(p, v, t, 1f);
                case 3: return new Color(p, q, v, 1f);
                case 4: return new Color(t, p, v, 1f);
                default: return new Color(v, p, q, 1f);
            }
        }

        public static void RGBToHSV(Color rgbColor, out float h, out float s, out float v)
        {
            float max = Mathf.Max(rgbColor.r, Mathf.Max(rgbColor.g, rgbColor.b));
            float min = Mathf.Min(rgbColor.r, Mathf.Min(rgbColor.g, rgbColor.b));
            float delta = max - min;
            v = max;
            s = max < 1e-6f ? 0f : delta / max;
            if (delta < 1e-6f) { h = 0f; return; }
            if (Math.Abs(max - rgbColor.r) < 1e-6f) h = (rgbColor.g - rgbColor.b) / delta;
            else if (Math.Abs(max - rgbColor.g) < 1e-6f) h = 2f + (rgbColor.b - rgbColor.r) / delta;
            else h = 4f + (rgbColor.r - rgbColor.g) / delta;
            h /= 6f;
            if (h < 0f) h += 1f;
        }

        public static Color operator +(Color a, Color b) { return new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a); }
        public static Color operator -(Color a, Color b) { return new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a); }
        public static Color operator *(Color a, Color b) { return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a); }
        public static Color operator *(Color a, float d) { return new Color(a.r * d, a.g * d, a.b * d, a.a * d); }
        public static Color operator *(float d, Color a) { return new Color(a.r * d, a.g * d, a.b * d, a.a * d); }
        public static Color operator /(Color a, float d) { return new Color(a.r / d, a.g / d, a.b / d, a.a / d); }
        public static bool operator ==(Color a, Color b)
        { return Math.Abs(a.r - b.r) < 1e-5f && Math.Abs(a.g - b.g) < 1e-5f && Math.Abs(a.b - b.b) < 1e-5f && Math.Abs(a.a - b.a) < 1e-5f; }
        public static bool operator !=(Color a, Color b) { return !(a == b); }
        public static implicit operator Color(Color32 c) { return new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f); }
        public static implicit operator Vector4(Color c) { return new Vector4(c.r, c.g, c.b, c.a); }
        public static implicit operator Color(Vector4 v) { return new Color(v.x, v.y, v.z, v.w); }
        public override bool Equals(object o) { return o is Color && this == (Color)o; }
        public override int GetHashCode() { return ((Vector4)this).GetHashCode(); }
        public override string ToString() { return "RGBA(" + r.ToString("F3") + ", " + g.ToString("F3") + ", " + b.ToString("F3") + ", " + a.ToString("F3") + ")"; }
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color32(Color c)
        {
            return new Color32(
                (byte)(Mathf.Clamp01(c.r) * 255f + 0.5f),
                (byte)(Mathf.Clamp01(c.g) * 255f + 0.5f),
                (byte)(Mathf.Clamp01(c.b) * 255f + 0.5f),
                (byte)(Mathf.Clamp01(c.a) * 255f + 0.5f));
        }
        public override string ToString() { return "RGBA(" + r + ", " + g + ", " + b + ", " + a + ")"; }
    }

    public struct Rect
    {
        private float mx, my, mw, mh;
        public Rect(float x, float y, float width, float height) { mx = x; my = y; mw = width; mh = height; }
        public Rect(Vector2 position, Vector2 size) { mx = position.x; my = position.y; mw = size.x; mh = size.y; }
        public float x { get { return mx; } set { mx = value; } }
        public float y { get { return my; } set { my = value; } }
        public float width { get { return mw; } set { mw = value; } }
        public float height { get { return mh; } set { mh = value; } }
        public float xMin { get { return Mathf.Min(mx, mx + mw); } set { float xm = xMax; mx = value; mw = xm - mx; } }
        public float yMin { get { return Mathf.Min(my, my + mh); } set { float ym = yMax; my = value; mh = ym - my; } }
        public float xMax { get { return Mathf.Max(mx, mx + mw); } set { mw = value - mx; } }
        public float yMax { get { return Mathf.Max(my, my + mh); } set { mh = value - my; } }
        public Vector2 position { get { return new Vector2(mx, my); } set { mx = value.x; my = value.y; } }
        public Vector2 size { get { return new Vector2(mw, mh); } set { mw = value.x; mh = value.y; } }
        public Vector2 center { get { return new Vector2(mx + mw * 0.5f, my + mh * 0.5f); } set { mx = value.x - mw * 0.5f; my = value.y - mh * 0.5f; } }
        public Vector2 min { get { return new Vector2(xMin, yMin); } set { xMin = value.x; yMin = value.y; } }
        public Vector2 max { get { return new Vector2(xMax, yMax); } set { xMax = value.x; yMax = value.y; } }
        public static Rect zero { get { return new Rect(0f, 0f, 0f, 0f); } }
        public bool Contains(Vector2 point) { return point.x >= xMin && point.x < xMax && point.y >= yMin && point.y < yMax; }
        public bool Contains(Vector3 point) { return Contains(new Vector2(point.x, point.y)); }
        public bool Overlaps(Rect other) { return other.xMax > xMin && other.xMin < xMax && other.yMax > yMin && other.yMin < yMax; }
        public static Rect MinMaxRect(float xmin, float ymin, float xmax, float ymax) { return new Rect(xmin, ymin, xmax - xmin, ymax - ymin); }
        public override string ToString() { return "(x:" + mx + ", y:" + my + ", width:" + mw + ", height:" + mh + ")"; }
    }

    public struct RectInt
    {
        private int mx, my, mw, mh;
        public RectInt(int x, int y, int width, int height) { mx = x; my = y; mw = width; mh = height; }
        public int x { get { return mx; } set { mx = value; } }
        public int y { get { return my; } set { my = value; } }
        public int width { get { return mw; } set { mw = value; } }
        public int height { get { return mh; } set { mh = value; } }
    }

    public struct Bounds
    {
        private Vector3 c, e;
        public Bounds(Vector3 center, Vector3 size) { c = center; e = size * 0.5f; }
        public Vector3 center { get { return c; } set { c = value; } }
        public Vector3 size { get { return e * 2f; } set { e = value * 0.5f; } }
        public Vector3 extents { get { return e; } set { e = value; } }
        public Vector3 min { get { return c - e; } set { SetMinMax(value, max); } }
        public Vector3 max { get { return c + e; } set { SetMinMax(min, value); } }
        public void Encapsulate(Vector3 point) { SetMinMax(Vector3.Min(min, point), Vector3.Max(max, point)); }
        public void Encapsulate(Bounds bounds) { Encapsulate(bounds.min); Encapsulate(bounds.max); }
        public void Expand(float amount) { e += new Vector3(amount, amount, amount) * 0.5f; }
        public void Expand(Vector3 amount) { e += amount * 0.5f; }
        public void SetMinMax(Vector3 mn, Vector3 mx) { e = (mx - mn) * 0.5f; c = mn + e; }
        public bool Contains(Vector3 point)
        {
            Vector3 mn = min, mx = max;
            return point.x >= mn.x && point.x <= mx.x && point.y >= mn.y && point.y <= mx.y && point.z >= mn.z && point.z <= mx.z;
        }
        public bool Intersects(Bounds bounds)
        {
            Vector3 amn = min, amx = max, bmn = bounds.min, bmx = bounds.max;
            return amn.x <= bmx.x && amx.x >= bmn.x && amn.y <= bmx.y && amx.y >= bmn.y && amn.z <= bmx.z && amx.z >= bmn.z;
        }
        public bool IntersectRay(Ray ray) { float d; return IntersectRay(ray, out d); }
        public bool IntersectRay(Ray ray, out float distance)
        {
            distance = 0f;
            Vector3 mn = min, mx = max;
            float tmin = float.NegativeInfinity, tmax = float.PositiveInfinity;
            for (int i = 0; i < 3; i++)
            {
                float o = ray.origin[i], d = ray.direction[i];
                if (Math.Abs(d) < 1e-9f) { if (o < mn[i] || o > mx[i]) return false; continue; }
                float t1 = (mn[i] - o) / d, t2 = (mx[i] - o) / d;
                if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
                tmin = Mathf.Max(tmin, t1); tmax = Mathf.Min(tmax, t2);
                if (tmin > tmax) return false;
            }
            if (tmax < 0f) return false;
            distance = tmin < 0f ? 0f : tmin;
            return true;
        }
        public Vector3 ClosestPoint(Vector3 point)
        {
            Vector3 mn = min, mx = max;
            return new Vector3(Mathf.Clamp(point.x, mn.x, mx.x), Mathf.Clamp(point.y, mn.y, mx.y), Mathf.Clamp(point.z, mn.z, mx.z));
        }
        public float SqrDistance(Vector3 point) { return (ClosestPoint(point) - point).sqrMagnitude; }
        public override string ToString() { return "Center: " + c + ", Extents: " + e; }
    }

    public struct Ray
    {
        private Vector3 o, d;
        public Ray(Vector3 origin, Vector3 direction) { o = origin; d = direction.normalized; }
        public Vector3 origin { get { return o; } set { o = value; } }
        public Vector3 direction { get { return d; } set { d = value.normalized; } }
        public Vector3 GetPoint(float distance) { return o + d * distance; }
        public override string ToString() { return "Origin: " + o + ", Dir: " + d; }
    }

    public struct Plane
    {
        private Vector3 n;
        private float dist;
        public Plane(Vector3 inNormal, Vector3 inPoint) { n = inNormal.normalized; dist = -Vector3.Dot(n, inPoint); }
        public Plane(Vector3 inNormal, float d) { n = inNormal.normalized; dist = d; }
        public Vector3 normal { get { return n; } set { n = value; } }
        public float distance { get { return dist; } set { dist = value; } }
        public bool Raycast(Ray ray, out float enter)
        {
            float vdot = Vector3.Dot(ray.direction, n);
            float ndot = -Vector3.Dot(ray.origin, n) - dist;
            if (Math.Abs(vdot) < 1e-9f) { enter = 0f; return false; }
            enter = ndot / vdot;
            return enter > 0f;
        }
        public float GetDistanceToPoint(Vector3 point) { return Vector3.Dot(n, point) + dist; }
        public Vector3 ClosestPointOnPlane(Vector3 point) { return point - n * GetDistanceToPoint(point); }
        public bool GetSide(Vector3 point) { return GetDistanceToPoint(point) > 0f; }
    }

    public struct RaycastHit
    {
        private Vector3 p, nrm;
        private float dst;
        public Vector3 point { get { return p; } set { p = value; } }
        public Vector3 normal { get { return nrm; } set { nrm = value; } }
        public float distance { get { return dst; } set { dst = value; } }
        public Collider collider { get { return null; } }
        public Transform transform { get { return null; } }
        public Rigidbody rigidbody { get { return null; } }
        public Vector2 textureCoord { get { return Vector2.zero; } }
        public int triangleIndex { get { return -1; } }
    }

    public static class Mathf
    {
        public const float PI = 3.14159274f;
        public const float Infinity = float.PositiveInfinity;
        public const float NegativeInfinity = float.NegativeInfinity;
        public const float Deg2Rad = 0.0174532924f;
        public const float Rad2Deg = 57.29578f;
        public static readonly float Epsilon = 1.401298E-45f;
        public static float Abs(float f) { return Math.Abs(f); }
        public static int Abs(int v) { return Math.Abs(v); }
        public static float Sign(float f) { return f >= 0f ? 1f : -1f; }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static float Min(params float[] values)
        {
            if (values == null || values.Length == 0) return 0f;
            float m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] < m) m = values[i];
            return m;
        }
        public static int Min(int a, int b) { return a < b ? a : b; }
        public static int Min(params int[] values)
        {
            if (values == null || values.Length == 0) return 0;
            int m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] < m) m = values[i];
            return m;
        }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static float Max(params float[] values)
        {
            if (values == null || values.Length == 0) return 0f;
            float m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] > m) m = values[i];
            return m;
        }
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static int Max(params int[] values)
        {
            if (values == null || values.Length == 0) return 0;
            int m = values[0];
            for (int i = 1; i < values.Length; i++) if (values[i] > m) m = values[i];
            return m;
        }
        public static float Clamp(float value, float min, float max) { return value < min ? min : (value > max ? max : value); }
        public static int Clamp(int value, int min, int max) { return value < min ? min : (value > max ? max : value); }
        public static float Clamp01(float value) { return value < 0f ? 0f : (value > 1f ? 1f : value); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float LerpUnclamped(float a, float b, float t) { return a + (b - a) * t; }
        public static float LerpAngle(float a, float b, float t)
        {
            float delta = Repeat(b - a, 360f);
            if (delta > 180f) delta -= 360f;
            return a + delta * Clamp01(t);
        }
        public static float InverseLerp(float a, float b, float value)
        { return Math.Abs(b - a) < 1e-9f ? 0f : Clamp01((value - a) / (b - a)); }
        public static float MoveTowards(float current, float target, float maxDelta)
        { return Math.Abs(target - current) <= maxDelta ? target : current + Sign(target - current) * maxDelta; }
        public static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float delta = DeltaAngle(current, target);
            if (-maxDelta < delta && delta < maxDelta) return target;
            return MoveTowards(current, current + delta, maxDelta);
        }
        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = -2f * t * t * t + 3f * t * t;
            return to * t + from * (1f - t);
        }
        public static float SmoothDamp(float cur, float target, ref float vel, float smoothTime)
        { return SmoothDamp(cur, target, ref vel, smoothTime, float.PositiveInfinity, 0.02f); }
        public static float SmoothDamp(float cur, float target, ref float vel, float smoothTime, float maxSpeed)
        { return SmoothDamp(cur, target, ref vel, smoothTime, maxSpeed, 0.02f); }
        public static float SmoothDamp(float cur, float target, ref float vel, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float xt = omega * deltaTime;
            float exp = 1f / (1f + xt + 0.48f * xt * xt + 0.235f * xt * xt * xt);
            float change = cur - target;
            float maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);
            float newTarget = cur - change;
            float temp = (vel + omega * change) * deltaTime;
            vel = (vel - omega * temp) * exp;
            float output = newTarget + (change + temp) * exp;
            if (target - cur > 0f == output > target) { output = target; vel = (output - target) / deltaTime; }
            return output;
        }
        public static float SmoothDampAngle(float cur, float target, ref float vel, float smoothTime)
        { return SmoothDamp(cur, cur + DeltaAngle(cur, target), ref vel, smoothTime); }
        public static float Sqrt(float f) { return (float)Math.Sqrt(f); }
        public static float Pow(float f, float p) { return (float)Math.Pow(f, p); }
        public static float Exp(float power) { return (float)Math.Exp(power); }
        public static float Log(float f) { return (float)Math.Log(f); }
        public static float Log(float f, float p) { return (float)Math.Log(f, p); }
        public static float Log10(float f) { return (float)Math.Log10(f); }
        public static float Sin(float f) { return (float)Math.Sin(f); }
        public static float Cos(float f) { return (float)Math.Cos(f); }
        public static float Tan(float f) { return (float)Math.Tan(f); }
        public static float Asin(float f) { return (float)Math.Asin(f); }
        public static float Acos(float f) { return (float)Math.Acos(f); }
        public static float Atan(float f) { return (float)Math.Atan(f); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Ceil(float f) { return (float)Math.Ceiling(f); }
        public static float Floor(float f) { return (float)Math.Floor(f); }
        public static float Round(float f) { return (float)Math.Round(f, MidpointRounding.ToEven); }
        public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }
        public static int FloorToInt(float f) { return (int)Math.Floor(f); }
        public static int RoundToInt(float f) { return (int)Math.Round(f, MidpointRounding.ToEven); }
        public static float Repeat(float t, float length) { return Clamp(t - Floor(t / length) * length, 0f, length); }
        public static float PingPong(float t, float length)
        {
            t = Repeat(t, length * 2f);
            return length - Math.Abs(t - length);
        }
        public static float DeltaAngle(float current, float target)
        {
            float delta = Repeat(target - current, 360f);
            if (delta > 180f) delta -= 360f;
            return delta;
        }
        public static bool Approximately(float a, float b)
        { return Math.Abs(b - a) < Max(1E-06f * Max(Math.Abs(a), Math.Abs(b)), Epsilon * 8f); }

        // Deterministic value noise with smooth interpolation. Not Unity's exact Perlin output, but it has the
        // same contract: continuous, in [0,1], repeatable for a given (x, y).
        public static float PerlinNoise(float x, float y)
        {
            int xi = FloorToInt(x), yi = FloorToInt(y);
            float xf = x - xi, yf = y - yi;
            float u = xf * xf * (3f - 2f * xf), v = yf * yf * (3f - 2f * yf);
            float n00 = Hash(xi, yi), n10 = Hash(xi + 1, yi), n01 = Hash(xi, yi + 1), n11 = Hash(xi + 1, yi + 1);
            return Clamp01(LerpUnclamped(LerpUnclamped(n00, n10, u), LerpUnclamped(n01, n11, u), v));
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF;
            }
        }

        public static bool IsPowerOfTwo(int value) { return value > 0 && (value & (value - 1)) == 0; }
        public static int NextPowerOfTwo(int value)
        {
            if (value <= 0) return 0;
            int p = 1;
            while (p < value) p <<= 1;
            return p;
        }
        public static int ClosestPowerOfTwo(int value)
        {
            int next = NextPowerOfTwo(value);
            int prev = next >> 1;
            return (value - prev) < (next - value) ? prev : next;
        }
    }

    public class Gradient
    {
        private GradientColorKey[] _ck = new GradientColorKey[0];
        private GradientAlphaKey[] _ak = new GradientAlphaKey[0];
        public GradientColorKey[] colorKeys { get { return _ck; } set { _ck = value ?? new GradientColorKey[0]; } }
        public GradientAlphaKey[] alphaKeys { get { return _ak; } set { _ak = value ?? new GradientAlphaKey[0]; } }
        public GradientMode mode { get; set; }
        public Color Evaluate(float time)
        {
            if (_ck.Length == 0) return Color.white;
            Color c = _ck[0].color;
            for (int i = 1; i < _ck.Length; i++)
            {
                if (time <= _ck[i].time)
                {
                    float span = _ck[i].time - _ck[i - 1].time;
                    float t = span < 1e-9f ? 0f : (time - _ck[i - 1].time) / span;
                    c = Color.Lerp(_ck[i - 1].color, _ck[i].color, t);
                    break;
                }
                c = _ck[i].color;
            }
            float a = _ak.Length == 0 ? 1f : _ak[0].alpha;
            for (int i = 1; i < _ak.Length; i++)
            {
                if (time <= _ak[i].time)
                {
                    float span = _ak[i].time - _ak[i - 1].time;
                    float t = span < 1e-9f ? 0f : (time - _ak[i - 1].time) / span;
                    a = Mathf.Lerp(_ak[i - 1].alpha, _ak[i].alpha, t);
                    break;
                }
                a = _ak[i].alpha;
            }
            c.a = a;
            return c;
        }
        public void SetKeys(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys) { _ck = colorKeys; _ak = alphaKeys; }
    }

    public struct GradientColorKey
    {
        public Color color; public float time;
        public GradientColorKey(Color col, float time) { color = col; this.time = time; }
    }

    public struct GradientAlphaKey
    {
        public float alpha; public float time;
        public GradientAlphaKey(float alpha, float time) { this.alpha = alpha; this.time = time; }
    }

    public enum GradientMode { Blend, Fixed }

    public struct Keyframe
    {
        public float time, value, inTangent, outTangent;
        public Keyframe(float time, float value) { this.time = time; this.value = value; inTangent = 0f; outTangent = 0f; }
        public Keyframe(float time, float value, float inTangent, float outTangent) { this.time = time; this.value = value; this.inTangent = inTangent; this.outTangent = outTangent; }
    }

    public class AnimationCurve
    {
        private readonly List<Keyframe> _keys = new List<Keyframe>();
        public AnimationCurve() { }
        public AnimationCurve(params Keyframe[] keys) { if (keys != null) _keys.AddRange(keys); Sort(); }
        public Keyframe[] keys
        {
            get { return _keys.ToArray(); }
            set { _keys.Clear(); if (value != null) _keys.AddRange(value); Sort(); }
        }
        private void Sort() { _keys.Sort((a, b) => a.time.CompareTo(b.time)); }
        public int length { get { return _keys.Count; } }
        public float Evaluate(float time)
        {
            if (_keys.Count == 0) return 0f;
            if (time <= _keys[0].time) return _keys[0].value;
            if (time >= _keys[_keys.Count - 1].time) return _keys[_keys.Count - 1].value;
            for (int i = 1; i < _keys.Count; i++)
            {
                if (time <= _keys[i].time)
                {
                    float span = _keys[i].time - _keys[i - 1].time;
                    float t = span < 1e-9f ? 0f : (time - _keys[i - 1].time) / span;
                    t = t * t * (3f - 2f * t);
                    return Mathf.LerpUnclamped(_keys[i - 1].value, _keys[i].value, t);
                }
            }
            return _keys[_keys.Count - 1].value;
        }
        public int AddKey(float time, float value) { return AddKey(new Keyframe(time, value)); }
        public int AddKey(Keyframe key) { _keys.Add(key); Sort(); return _keys.IndexOf(key); }
        public static AnimationCurve Linear(float timeStart, float valueStart, float timeEnd, float valueEnd)
        { return new AnimationCurve(new Keyframe(timeStart, valueStart), new Keyframe(timeEnd, valueEnd)); }
        public static AnimationCurve EaseInOut(float timeStart, float valueStart, float timeEnd, float valueEnd)
        { return new AnimationCurve(new Keyframe(timeStart, valueStart), new Keyframe(timeEnd, valueEnd)); }
        public static AnimationCurve Constant(float timeStart, float timeEnd, float value)
        { return new AnimationCurve(new Keyframe(timeStart, value), new Keyframe(timeEnd, value)); }
    }

    public static class Random
    {
        private static System.Random _rng = new System.Random(12345);
        public static float value { get { return (float)_rng.NextDouble(); } }
        public static Vector3 insideUnitSphere
        {
            get
            {
                Vector3 v;
                do { v = new Vector3(value * 2f - 1f, value * 2f - 1f, value * 2f - 1f); } while (v.sqrMagnitude > 1f);
                return v;
            }
        }
        public static Vector3 onUnitSphere { get { return insideUnitSphere.normalized; } }
        public static Vector2 insideUnitCircle
        {
            get
            {
                Vector2 v;
                do { v = new Vector2(value * 2f - 1f, value * 2f - 1f); } while (v.sqrMagnitude > 1f);
                return v;
            }
        }
        public static Quaternion rotation { get { return Quaternion.Euler(value * 360f, value * 360f, value * 360f); } }
        public static int seed { get { return 0; } set { _rng = new System.Random(value); } }
        public static float Range(float min, float max) { return min + (max - min) * value; }
        public static int Range(int min, int max) { return max <= min ? min : _rng.Next(min, max); }
        public static void InitState(int s) { _rng = new System.Random(s); }
        public static Color ColorHSV() { return Color.HSVToRGB(value, 1f, 1f); }
    }
}

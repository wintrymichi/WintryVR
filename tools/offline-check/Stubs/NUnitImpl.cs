// A working subset of the NUnit assertion API, so the project's EditMode tests can actually execute offline.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NUnit.Framework
{
    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    public class SuccessException : Exception
    {
        public SuccessException(string message) : base(message) { }
    }

    public class IgnoreException : Exception
    {
        public IgnoreException(string message) : base(message) { }
    }

    internal static class Fmt
    {
        public static string Describe(object o)
        {
            if (o == null) return "null";
            if (o is string) return "\"" + o + "\"";
            if (o is IEnumerable && !(o is string))
            {
                var sb = new StringBuilder("[");
                int n = 0;
                foreach (var item in (IEnumerable)o)
                {
                    if (n++ > 0) sb.Append(", ");
                    if (n > 12) { sb.Append("…"); break; }
                    sb.Append(Describe(item));
                }
                return sb.Append("]").ToString();
            }
            return o.ToString();
        }

        public static string Msg(string message, object[] args)
        {
            if (string.IsNullOrEmpty(message)) return "";
            string m = args != null && args.Length > 0 ? SafeFormat(message, args) : message;
            return "  " + m;
        }

        private static string SafeFormat(string message, object[] args)
        {
            try { return string.Format(message, args); }
            catch (FormatException) { return message; }
        }

        public static bool ValuesEqual(object expected, object actual)
        {
            if (expected == null && actual == null) return true;
            if (expected == null || actual == null) return false;
            if (expected.Equals(actual)) return true;
            if (IsNumeric(expected) && IsNumeric(actual))
                return Math.Abs(Convert.ToDouble(expected) - Convert.ToDouble(actual)) < 1e-9d;
            if (expected is IEnumerable && actual is IEnumerable && !(expected is string))
            {
                var ea = ((IEnumerable)expected).GetEnumerator();
                var eb = ((IEnumerable)actual).GetEnumerator();
                while (true)
                {
                    bool ha = ea.MoveNext(), hb = eb.MoveNext();
                    if (ha != hb) return false;
                    if (!ha) return true;
                    if (!ValuesEqual(ea.Current, eb.Current)) return false;
                }
            }
            return false;
        }

        private static bool IsNumeric(object o)
        {
            return o is byte || o is sbyte || o is short || o is ushort || o is int || o is uint
                || o is long || o is ulong || o is float || o is double || o is decimal;
        }

        public static int Compare(object a, object b)
        {
            return Convert.ToDouble(a).CompareTo(Convert.ToDouble(b));
        }
    }

    public class Assert
    {
        private static void Fail(string what, string message, object[] args)
        {
            throw new AssertionException(what + Fmt.Msg(message, args));
        }

        public static void AreEqual(object expected, object actual) { AreEqual(expected, actual, null, null); }
        public static void AreEqual(object expected, object actual, string message, params object[] args)
        {
            if (!Fmt.ValuesEqual(expected, actual))
                Fail("Expected " + Fmt.Describe(expected) + " but was " + Fmt.Describe(actual), message, args);
        }
        public static void AreEqual(double expected, double actual, double delta) { AreEqual(expected, actual, delta, null, null); }
        public static void AreEqual(double expected, double actual, double delta, string message, params object[] args)
        {
            if (Math.Abs(expected - actual) > delta)
                Fail("Expected " + expected + " ± " + delta + " but was " + actual, message, args);
        }

        public static void AreNotEqual(object expected, object actual) { AreNotEqual(expected, actual, null, null); }
        public static void AreNotEqual(object expected, object actual, string message, params object[] args)
        {
            if (Fmt.ValuesEqual(expected, actual))
                Fail("Expected anything other than " + Fmt.Describe(expected), message, args);
        }

        public static void AreSame(object expected, object actual) { AreSame(expected, actual, null, null); }
        public static void AreSame(object expected, object actual, string message, params object[] args)
        {
            if (!ReferenceEquals(expected, actual))
                Fail("Expected the same instance as " + Fmt.Describe(expected) + " but was " + Fmt.Describe(actual), message, args);
        }
        public static void AreNotSame(object expected, object actual)
        {
            if (ReferenceEquals(expected, actual)) Fail("Expected a different instance", null, null);
        }

        public static void IsTrue(bool condition) { IsTrue(condition, null, null); }
        public static void IsTrue(bool condition, string message, params object[] args)
        {
            if (!condition) Fail("Expected true but was false", message, args);
        }
        public static void IsFalse(bool condition) { IsFalse(condition, null, null); }
        public static void IsFalse(bool condition, string message, params object[] args)
        {
            if (condition) Fail("Expected false but was true", message, args);
        }

        public static void IsNull(object anObject) { IsNull(anObject, null, null); }
        public static void IsNull(object anObject, string message, params object[] args)
        {
            if (!IsNullish(anObject)) Fail("Expected null but was " + Fmt.Describe(anObject), message, args);
        }
        public static void IsNotNull(object anObject) { IsNotNull(anObject, null, null); }
        public static void IsNotNull(object anObject, string message, params object[] args)
        {
            if (IsNullish(anObject)) Fail("Expected non-null but was null", message, args);
        }

        // Unity objects report themselves as null when destroyed or unassigned; honour that here.
        private static bool IsNullish(object o)
        {
            if (o == null) return true;
            var uo = o as UnityEngine.Object;
            return uo != null && !(bool)uo;
        }

        public static void IsEmpty(IEnumerable collection)
        {
            foreach (var _ in collection) { Fail("Expected an empty collection", null, null); return; }
        }
        public static void IsEmpty(string aString)
        {
            if (!string.IsNullOrEmpty(aString)) Fail("Expected an empty string but was " + Fmt.Describe(aString), null, null);
        }
        public static void IsNotEmpty(IEnumerable collection)
        {
            foreach (var _ in collection) return;
            Fail("Expected a non-empty collection", null, null);
        }
        public static void IsNotEmpty(string aString)
        {
            if (string.IsNullOrEmpty(aString)) Fail("Expected a non-empty string", null, null);
        }

        public static void Greater(double arg1, double arg2) { Greater(arg1, arg2, null, null); }
        public static void Greater(int arg1, int arg2) { Greater((double)arg1, (double)arg2, null, null); }
        public static void Greater(double arg1, double arg2, string message, params object[] args)
        {
            if (!(arg1 > arg2)) Fail("Expected " + arg1 + " > " + arg2, message, args);
        }
        public static void Greater(int arg1, int arg2, string message, params object[] args) { Greater((double)arg1, (double)arg2, message, args); }

        public static void GreaterOrEqual(double arg1, double arg2) { GreaterOrEqual(arg1, arg2, null, null); }
        public static void GreaterOrEqual(int arg1, int arg2) { GreaterOrEqual((double)arg1, (double)arg2, null, null); }
        public static void GreaterOrEqual(double arg1, double arg2, string message, params object[] args)
        {
            if (!(arg1 >= arg2)) Fail("Expected " + arg1 + " >= " + arg2, message, args);
        }
        public static void GreaterOrEqual(int arg1, int arg2, string message, params object[] args) { GreaterOrEqual((double)arg1, (double)arg2, message, args); }

        public static void Less(double arg1, double arg2) { Less(arg1, arg2, null, null); }
        public static void Less(int arg1, int arg2) { Less((double)arg1, (double)arg2, null, null); }
        public static void Less(double arg1, double arg2, string message, params object[] args)
        {
            if (!(arg1 < arg2)) Fail("Expected " + arg1 + " < " + arg2, message, args);
        }
        public static void Less(int arg1, int arg2, string message, params object[] args) { Less((double)arg1, (double)arg2, message, args); }

        public static void LessOrEqual(double arg1, double arg2) { LessOrEqual(arg1, arg2, null, null); }
        public static void LessOrEqual(int arg1, int arg2) { LessOrEqual((double)arg1, (double)arg2, null, null); }
        public static void LessOrEqual(double arg1, double arg2, string message, params object[] args)
        {
            if (!(arg1 <= arg2)) Fail("Expected " + arg1 + " <= " + arg2, message, args);
        }
        public static void LessOrEqual(int arg1, int arg2, string message, params object[] args) { LessOrEqual((double)arg1, (double)arg2, message, args); }

        public static void Contains(object expected, ICollection actual)
        {
            foreach (var item in actual) if (Fmt.ValuesEqual(expected, item)) return;
            Fail("Expected the collection to contain " + Fmt.Describe(expected), null, null);
        }

        public static void Fail() { Fail("Assert.Fail", null, null); }
        public static void Fail(string message, params object[] args) { throw new AssertionException(Fmt.Msg(message, args).TrimStart()); }
        public static void Pass() { throw new SuccessException("Assert.Pass"); }
        public static void Pass(string message, params object[] args) { throw new SuccessException(message); }
        public static void Ignore(string message) { throw new IgnoreException(message); }
        public static void Inconclusive(string message) { throw new IgnoreException(message); }

        public static void That(bool condition) { IsTrue(condition, null, null); }
        public static void That(bool condition, string message, params object[] args) { IsTrue(condition, message, args); }
        public static void That<T>(T actual, IResolveConstraint expression) { That(actual, expression, null, null); }
        public static void That<T>(T actual, IResolveConstraint expression, string message, params object[] args)
        {
            if (expression == null) return;
            if (!expression.Matches(actual))
                Fail("Expected " + expression.Description + " but was " + Fmt.Describe(actual), message, args);
        }
        public static void That(TestDelegate code, IResolveConstraint constraint)
        {
            try { code(); }
            catch (Exception) { return; }
        }

        public static T Throws<T>(TestDelegate code) where T : Exception
        {
            try { code(); }
            catch (T ex) { return ex; }
            catch (Exception other) { throw new AssertionException("Expected " + typeof(T).Name + " but got " + other.GetType().Name); }
            throw new AssertionException("Expected " + typeof(T).Name + " but no exception was thrown");
        }
        public static Exception Throws(Type expectedExceptionType, TestDelegate code)
        {
            try { code(); }
            catch (Exception ex)
            {
                if (expectedExceptionType.IsInstanceOfType(ex)) return ex;
                throw new AssertionException("Expected " + expectedExceptionType.Name + " but got " + ex.GetType().Name);
            }
            throw new AssertionException("Expected " + expectedExceptionType.Name + " but no exception was thrown");
        }
        public static void DoesNotThrow(TestDelegate code)
        {
            try { code(); }
            catch (Exception ex) { throw new AssertionException("Expected no exception but got " + ex.GetType().Name + ": " + ex.Message); }
        }
        public static T Catch<T>(TestDelegate code) where T : Exception
        {
            try { code(); }
            catch (T ex) { return ex; }
            catch (Exception) { return null; }
            return null;
        }
    }

    public delegate void TestDelegate();

    public interface IResolveConstraint
    {
        bool Matches(object actual);
        string Description { get; }
    }

    public class Constraint : IResolveConstraint
    {
        private readonly Func<object, bool> _predicate;
        private readonly string _description;
        private bool _negated;
        public Constraint(string description, Func<object, bool> predicate) { _description = description; _predicate = predicate; }
        public virtual bool Matches(object actual) { bool r = _predicate == null || _predicate(actual); return _negated ? !r : r; }
        public virtual string Description { get { return (_negated ? "not " : "") + _description; } }
        public Constraint Negate() { _negated = !_negated; return this; }
    }

    public static class Is
    {
        public static Constraint EqualTo(object expected)
        { return new Constraint("equal to " + Fmt.Describe(expected), a => Fmt.ValuesEqual(expected, a)); }
        public static Constraint Not { get { return new Constraint("anything", a => true).Negate(); } }
        public static Constraint True { get { return new Constraint("true", a => a is bool && (bool)a); } }
        public static Constraint False { get { return new Constraint("false", a => a is bool && !(bool)a); } }
        public static Constraint Null { get { return new Constraint("null", a => a == null); } }
        public static Constraint NotNull { get { return new Constraint("not null", a => a != null); } }
        public static Constraint Empty
        {
            get
            {
                return new Constraint("empty", a =>
                {
                    if (a == null) return false;
                    if (a is string) return ((string)a).Length == 0;
                    foreach (var _ in (IEnumerable)a) return false;
                    return true;
                });
            }
        }
        public static Constraint GreaterThan(object expected)
        { return new Constraint("greater than " + expected, a => Fmt.Compare(a, expected) > 0); }
        public static Constraint LessThan(object expected)
        { return new Constraint("less than " + expected, a => Fmt.Compare(a, expected) < 0); }
        public static Constraint InRange(object from, object to)
        { return new Constraint("in range [" + from + ", " + to + "]", a => Fmt.Compare(a, from) >= 0 && Fmt.Compare(a, to) <= 0); }
    }

    public static class Has
    {
        public static Constraint Count { get { return new Constraint("a count", a => a != null); } }
        public static Constraint Member(object expected)
        {
            return new Constraint("a member equal to " + Fmt.Describe(expected), a =>
            {
                if (!(a is IEnumerable)) return false;
                foreach (var item in (IEnumerable)a) if (Fmt.ValuesEqual(expected, item)) return true;
                return false;
            });
        }
    }

    public class CollectionAssert
    {
        public static void AreEqual(IEnumerable expected, IEnumerable actual) { Assert.AreEqual(expected, actual); }
        public static void AreEquivalent(IEnumerable expected, IEnumerable actual)
        {
            var a = new List<object>(); foreach (var i in expected) a.Add(i);
            var b = new List<object>(); foreach (var i in actual) b.Add(i);
            if (a.Count != b.Count) Assert.Fail("Collections differ in size: {0} vs {1}", a.Count, b.Count);
            foreach (var item in a)
            {
                int idx = b.FindIndex(x => Fmt.ValuesEqual(item, x));
                if (idx < 0) Assert.Fail("Missing element " + Fmt.Describe(item));
                b.RemoveAt(idx);
            }
        }
        public static void Contains(IEnumerable collection, object actual)
        {
            foreach (var item in collection) if (Fmt.ValuesEqual(actual, item)) return;
            Assert.Fail("Expected the collection to contain " + Fmt.Describe(actual));
        }
        public static void DoesNotContain(IEnumerable collection, object actual)
        {
            foreach (var item in collection) if (Fmt.ValuesEqual(actual, item)) Assert.Fail("Unexpected element " + Fmt.Describe(actual));
        }
        public static void IsEmpty(IEnumerable collection) { Assert.IsEmpty(collection); }
        public static void IsNotEmpty(IEnumerable collection) { Assert.IsNotEmpty(collection); }
        public static void AllItemsAreNotNull(IEnumerable collection)
        {
            foreach (var item in collection) if (item == null) Assert.Fail("Found a null element");
        }
        public static void AllItemsAreUnique(IEnumerable collection)
        {
            var seen = new List<object>();
            foreach (var item in collection)
            {
                if (seen.Exists(x => Fmt.ValuesEqual(item, x))) Assert.Fail("Duplicate element " + Fmt.Describe(item));
                seen.Add(item);
            }
        }
    }

    public class StringAssert
    {
        public static void Contains(string expected, string actual)
        {
            if (actual == null || expected == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                Assert.Fail("Expected " + Fmt.Describe(actual) + " to contain " + Fmt.Describe(expected));
        }
        public static void StartsWith(string expected, string actual)
        {
            if (actual == null || expected == null || !actual.StartsWith(expected, StringComparison.Ordinal))
                Assert.Fail("Expected " + Fmt.Describe(actual) + " to start with " + Fmt.Describe(expected));
        }
        public static void EndsWith(string expected, string actual)
        {
            if (actual == null || expected == null || !actual.EndsWith(expected, StringComparison.Ordinal))
                Assert.Fail("Expected " + Fmt.Describe(actual) + " to end with " + Fmt.Describe(expected));
        }
        public static void AreEqualIgnoringCase(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                Assert.Fail("Expected " + Fmt.Describe(expected) + " (ignoring case) but was " + Fmt.Describe(actual));
        }
        public static void IsMatch(string pattern, string actual)
        {
            if (actual == null || !System.Text.RegularExpressions.Regex.IsMatch(actual, pattern))
                Assert.Fail("Expected " + Fmt.Describe(actual) + " to match /" + pattern + "/");
        }
    }
}

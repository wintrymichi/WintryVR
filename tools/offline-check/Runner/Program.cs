// Minimal test host: discovers [Test]/[TestCase] methods the way Unity's EditMode runner would,
// runs them against the offline Unity implementation, and reports pass/fail.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace WintryCheck
{
    public static class Program
    {
        private sealed class Result
        {
            public string Name;
            public bool Passed;
            public bool Ignored;
            public string Message;
        }

        public static int Main(string[] args)
        {
            var results = new List<Result>();
            var assembly = typeof(Program).Assembly;

            var fixtures = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null && t.Namespace.StartsWith("WintryVR.Tests"))
                .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                             .Any(m => m.GetCustomAttributes(typeof(TestAttribute), true).Length > 0
                                    || m.GetCustomAttributes(typeof(TestCaseAttribute), true).Length > 0))
                .OrderBy(t => t.Name)
                .ToList();

            Console.WriteLine("Discovered " + fixtures.Count + " test fixture(s).");
            Console.WriteLine();

            foreach (var fixture in fixtures)
            {
                var setUps = fixture.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttributes(typeof(SetUpAttribute), true).Length > 0).ToList();
                var tearDowns = fixture.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttributes(typeof(TearDownAttribute), true).Length > 0).ToList();

                var methods = fixture.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttributes(typeof(TestAttribute), true).Length > 0
                             || m.GetCustomAttributes(typeof(TestCaseAttribute), true).Length > 0)
                    .OrderBy(m => m.MetadataToken)
                    .ToList();

                foreach (var method in methods)
                {
                    var cases = method.GetCustomAttributes(typeof(TestCaseAttribute), true).Cast<TestCaseAttribute>().ToList();
                    if (cases.Count == 0)
                    {
                        results.Add(Run(fixture, setUps, tearDowns, method, null));
                    }
                    else
                    {
                        foreach (var c in cases) results.Add(Run(fixture, setUps, tearDowns, method, c.Arguments));
                    }
                }
            }

            Console.WriteLine();
            int passed = results.Count(r => r.Passed && !r.Ignored);
            int ignored = results.Count(r => r.Ignored);
            var failures = results.Where(r => !r.Passed && !r.Ignored).ToList();

            foreach (var r in results)
            {
                string tag = r.Ignored ? "SKIP" : (r.Passed ? "PASS" : "FAIL");
                Console.WriteLine(tag + "  " + r.Name + (r.Passed || r.Ignored ? "" : "\n        " + r.Message));
            }

            Console.WriteLine();
            Console.WriteLine("=== " + passed + " passed, " + failures.Count + " failed, " + ignored + " skipped, "
                              + results.Count + " total ===");
            return failures.Count == 0 ? 0 : 1;
        }

        private static Result Run(Type fixture, List<MethodInfo> setUps, List<MethodInfo> tearDowns, MethodInfo method, object[] arguments)
        {
            string name = fixture.Name + "." + method.Name
                          + (arguments == null ? "" : "(" + string.Join(", ", arguments.Select(Describe)) + ")");
            var result = new Result { Name = name };
            object instance = null;
            try
            {
                instance = Activator.CreateInstance(fixture);
                foreach (var s in setUps) s.Invoke(instance, null);
                method.Invoke(instance, Coerce(method, arguments));
                result.Passed = true;
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException;
                if (inner is SuccessException) result.Passed = true;
                else if (inner is IgnoreException) { result.Ignored = true; result.Passed = true; }
                else
                {
                    result.Passed = false;
                    result.Message = inner == null ? "unknown failure"
                        : inner.GetType().Name + ": " + inner.Message
                          + (inner is AssertionException ? "" : "\n        " + FirstFrames(inner));
                }
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Message = ex.GetType().Name + ": " + ex.Message;
            }
            finally
            {
                if (instance != null)
                {
                    foreach (var t in tearDowns)
                    {
                        try { t.Invoke(instance, null); } catch { /* teardown failures must not mask the result */ }
                    }
                }
            }
            return result;
        }

        private static object[] Coerce(MethodInfo method, object[] arguments)
        {
            if (arguments == null) return null;
            var ps = method.GetParameters();
            var outp = new object[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
            {
                object a = arguments[i];
                if (a != null && i < ps.Length && !ps[i].ParameterType.IsInstanceOfType(a))
                {
                    try { a = Convert.ChangeType(a, ps[i].ParameterType); } catch { /* leave as-is */ }
                }
                outp[i] = a;
            }
            return outp;
        }

        private static string Describe(object o)
        {
            if (o == null) return "null";
            if (o is string) return "\"" + o + "\"";
            return o.ToString();
        }

        private static string FirstFrames(Exception ex)
        {
            var st = ex.StackTrace;
            if (string.IsNullOrEmpty(st)) return "";
            var lines = st.Split('\n');
            return string.Join("\n        ", lines.Take(3).Select(l => l.Trim()));
        }
    }
}

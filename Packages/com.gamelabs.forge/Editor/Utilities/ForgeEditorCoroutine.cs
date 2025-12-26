#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GameLabs.Forge.Editor
{
    /// <summary>Dependency-free coroutine runner for Edit Mode.</summary>
    public static class ForgeEditorCoroutine
    {
        private class Runner
        {
            readonly IEnumerator _routine;
            public Runner(IEnumerator r) { _routine = r; EditorApplication.update += Tick; }

            void Tick()
            {
                try
                {
                    if (IsWaitingOn(_routine)) return;

                    if (!_routine.MoveNext())
                    {
                        EditorApplication.update -= Tick;
                        return;
                    }

                    if (IsWaitingOn(_routine)) return;
                }
                catch (Exception e)
                {
                    Debug.LogError("[Forge] EditorCoroutine exception: " + e);
                    EditorApplication.update -= Tick;
                }
            }

            static bool IsWaitingOn(IEnumerator e)
            {
                var cur = e.Current;

                if (cur is IEnumerator nested)
                    return MoveNested(nested);

                if (cur is UnityWebRequestAsyncOperation uwr) return !uwr.isDone;
                if (cur is AsyncOperation ao) return !ao.isDone;
                if (cur is CustomYieldInstruction cyi) return cyi.keepWaiting;

                return false; // null/unknown: advance next editor tick
            }

            static bool MoveNested(IEnumerator n)
            {
                while (true)
                {
                    var cur = n.Current;
                    if (cur is IEnumerator deep) { n = deep; continue; }
                    if (cur is UnityWebRequestAsyncOperation uwr) return !uwr.isDone;
                    if (cur is AsyncOperation ao) return !ao.isDone;
                    if (cur is CustomYieldInstruction cyi) return cyi.keepWaiting;
                    if (!n.MoveNext()) return false;
                }
            }
        }

        public static void Start(IEnumerator r) { if (r != null) _ = new Runner(r); }
    }
}
#endif

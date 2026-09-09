using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

namespace WebGLThreadingPatcher.Runtime
{
    [Preserve]
    public class TimerRunner : MonoBehaviour
    {
        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var go = new GameObject(nameof(TimerRunner));
            go.AddComponent<TimerRunner>();

            DontDestroyOnLoad(go);
        }

        [Preserve]
        private void Start()
        {
#if UNITY_2021_2_OR_NEWER && !UNITY_EDITOR
            StartCoroutine(TimerUpdateCoroutine(CreateSchedulerLoop()));

            Debug.Log($"[{nameof(TimerRunner)}] Timer scheduler is pumped once per frame.");
#endif
        }

        private static Func<int> CreateSchedulerLoop()
        {
            var timer = typeof(System.Threading.Timer);

            var scheduler = timer.GetNestedType("Scheduler", BindingFlags.NonPublic);
            if (scheduler == null)
                throw new MissingMemberException(timer.FullName, "Scheduler");

            var instance = scheduler.GetProperty("Instance");
            if (instance == null)
                throw new MissingMemberException(scheduler.FullName, "Instance");

            var runSchedulerLoop = scheduler.GetMethod("RunSchedulerLoop", BindingFlags.Instance | BindingFlags.NonPublic);
            if (runSchedulerLoop == null)
                throw new MissingMemberException(scheduler.FullName, "RunSchedulerLoop");

            return (Func<int>)runSchedulerLoop.CreateDelegate(typeof(Func<int>), instance.GetValue(null));
        }

        private static IEnumerator TimerUpdateCoroutine(Func<int> timerSchedulerLoop)
        {
            while (true)
            {
                timerSchedulerLoop();

                yield return null;
            }
        }
    }
}

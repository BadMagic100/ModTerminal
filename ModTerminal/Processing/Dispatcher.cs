using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace ModTerminal.Processing
{
    internal class Dispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> actions = new();

        internal static void Setup()
        {
            GameObject go = new();
            go.AddComponent<Dispatcher>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            while (!actions.IsEmpty && actions.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    ModTerminalMod.Instance.LogError($"Error during dispatcher execution: {e}");
                }
            }
        }

        public static void BeginInvoke(Action action)
        {
            actions.Enqueue(action);    
        }
    }
}

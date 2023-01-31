using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModTerminal
{
    internal class ArrowKeyWatcher : MonoBehaviour
    {
        public Selectable? selectableToWatch;

        public Action? OnUp;
        public Action? OnDown;
        public Action? OnLeft;
        public Action? OnRight;

        private void Update()
        {
            if (selectableToWatch != null && EventSystem.current.currentSelectedGameObject == selectableToWatch.gameObject)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    OnUp?.Invoke();
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    OnDown?.Invoke();
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    OnLeft?.Invoke();
                }
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    OnRight?.Invoke();
                }
            }
        }
    }
}

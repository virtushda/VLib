using System;
using UnityEngine;

namespace VLib
{
    [RequireComponent(typeof(Renderer))]
    public class LODGroupEventNode : MonoBehaviour
    {
        public event Action<Renderer> OnVisible;
        public event Action<Renderer> OnInvisible;

        new Renderer renderer;

        private void OnEnable()
        {
            renderer = GetComponent<Renderer>();
        }

        private void OnBecameVisible()
        {
            OnVisible?.Invoke(renderer);
        }

        private void OnBecameInvisible()
        {
            OnInvisible?.Invoke(renderer);
        }
    }
}
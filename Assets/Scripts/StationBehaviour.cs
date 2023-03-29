using System;
using UnityEngine;

namespace Assets.Scripts
{
    public class StationBehaviour : MonoBehaviour
    {
        public Action<StationBehaviour> OnSelected;
        public Action<StationBehaviour> OnUnselected;

        private MeshRenderer _meshRenderer;

        // Start is called before the first frame update
        void Start()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshRenderer.material.color = Color.white;
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnMouseEnter()
        {
            _meshRenderer.material.color = Color.yellow;

            OnSelected?.Invoke(this);
        }

        private void OnMouseExit()
        {
            _meshRenderer.material.color = Color.white;

            OnUnselected?.Invoke(this);
        }
    }
}

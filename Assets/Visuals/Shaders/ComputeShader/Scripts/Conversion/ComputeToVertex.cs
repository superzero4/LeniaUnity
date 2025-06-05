using System.Collections;
using NaughtyAttributes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Serialization;
using Visuals.Shaders.ComputeShader.Scripts;

namespace Visuals.Shaders.ComputeShader
{
    public class ComputeToVertex : MonoBehaviour
    {
        [SerializeField] private GameObject _holder;
        [SerializeField] private IComputeBufferProvider _compute;
        [SerializeField] private PointCloudRendererSimple _pcs;

        private void Bind(ComputeBuffer buff)
        {
            _pcs.SetBuffer(buff, _compute.Size3D, false);
        }

        void Start()
        {
            _compute = _holder.GetComponent<IComputeBufferProvider>();
            Assert.IsNotNull(_compute, $"Compute buffer provider {_holder.name} not found on the buffer object.");
            _compute.OnUpdate.AddListener(Bind);
        }
    }
}
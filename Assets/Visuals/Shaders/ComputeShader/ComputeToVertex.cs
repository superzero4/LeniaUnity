using System.Collections;
using NaughtyAttributes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Serialization;

namespace Visuals.Shaders.ComputeShader
{
    public class ComputeToVertex : MonoBehaviour
    {
        [SerializeField]
        private GameObject _holder;
        [SerializeField] private IComputeBufferProvider _compute;
        [SerializeField] private PointCloudRendererSimple _pcs;

        public void Bind(ComputeBuffer buff)
        {
            _pcs.SetBuffer(buff, _compute.Size, false);
        }

        IEnumerator Start()
        {
            _compute = _holder.GetComponent<IComputeBufferProvider>();
            Assert.IsNotNull(_compute, $"Compute buffer provider {_holder.name} not found on the buffer object.");
            yield return new WaitUntil(() => _compute.Buffer != null);
            yield return new WaitForEndOfFrame();
            Bind(_compute.Buffer);
            while (true)
            {
                Bind(_compute.Buffer);
                yield return new WaitForSeconds(0.016f);
            }
        }
    }
}
using System.Collections;
using NaughtyAttributes;
using UnityEngine;

namespace Visuals.Shaders.ComputeShader
{
    public class ComputeToVertex : MonoBehaviour
    {
        [SerializeField] private ComputeShaderHandler _compute;
        [SerializeField] private PointCloudRendererSimple _pcs;
        [Button]
        public void Bind(ComputeBuffer buff)
        {
            _pcs.SetBuffer(buff, _compute.Size, false);
        }
        IEnumerator Start()
        {
            yield return new WaitUntil(() => _compute.ReadBuffer != null);
            yield return new WaitForEndOfFrame();
            Bind(_compute.ReadBuffer);
        }
    }
}
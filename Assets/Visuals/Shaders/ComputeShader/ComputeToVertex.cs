using NaughtyAttributes;
using UnityEngine;

namespace Visuals.Shaders.ComputeShader
{
    public class ComputeToVertex : MonoBehaviour
    {
        [SerializeField] private ComputeShaderHandler _compute;
        [SerializeField] private PointCloudRendererSimple _pcs;
        [Button]
        void Bind()
        {
            var buff = _compute.Buffer;
            _pcs.SetBuffer(buff);
        }
    }
}
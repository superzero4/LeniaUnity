using NaughtyAttributes;
using UnityEngine;

namespace Visuals.Shaders.ComputeShader
{
    public class ComputeToTex : MonoBehaviour
    {
        [SerializeField] private ComputeShaderHandler _compute;
        [SerializeField] private Material _material;
        [SerializeField] private FilterMode _filterMode;
        private Texture2D _texture;

        [Button]
        private void UpdateTexture()
        {
            if (_compute.Buffer == null)
                return;
            if (_texture == null || _texture.width != _compute.Size.x || _texture.height != _compute.Size.y)
            {
                Destroy(_texture);
                _texture = new Texture2D(_compute.Size.x, _compute.Size.y, TextureFormat.RFloat, false);
                _texture.wrapMode = TextureWrapMode.Clamp;
                _texture.filterMode = _filterMode;
            }
            float[] data = new float[_compute.Size.x * _compute.Size.y * _compute.Size.z];
            _compute.Buffer.GetData(data);
            _texture.SetPixelData(data, 0);
            _texture.Apply();
            _material.SetTexture("_MainTex", _texture);
        }

        private void Update()
        {
            UpdateTexture();
        }
    }
}
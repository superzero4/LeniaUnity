using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Visuals.Shaders.ComputeShader
{
    public class ComputeToTex : MonoBehaviour
    {
        [FormerlySerializedAs("_displayedDepth")] [SerializeField, Range(0, 1024)]
        private int _displayedSlice = 0;
        [SerializeField,Range(0,2f)] private float _incrementDelay = .01f;

        [SerializeField] private ComputeShaderHandler _compute;
        [SerializeField] private Material _material;
        [SerializeField] private FilterMode _filterMode;
        private Texture2D _texture;

        [Button]
        private void UpdateTexture()
        {
            if (_compute.ReadBuffer == null)
                return;
            if (_texture == null || _texture.width != _compute.Size.x || _texture.height != _compute.Size.y)
            {
                Destroy(_texture);
                _texture = new Texture2D(_compute.Size.x, _compute.Size.y, TextureFormat.RFloat, false);
                _texture.wrapMode = TextureWrapMode.Clamp;
                _texture.filterMode = _filterMode;
            }

            //We want only a slice of the 3D data
            int sliceSize = _compute.Size.x * _compute.Size.y;
            float[] data = new float[sliceSize];
            _compute.ReadBuffer.GetData(data, 0, (_displayedSlice % _compute.Size.z) * sliceSize, sliceSize);
            _texture.SetPixelData(data, 0);
            _texture.Apply();
            _material.SetTexture("_MainTex", _texture);
        }

        [Button]
        public void Increment()
        {
            _displayedSlice++;
            UpdateTexture();
        }

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => _compute.ReadBuffer != null);
            yield return new WaitForEndOfFrame();
            while (true)
            {
                Increment();
                yield return new WaitForSeconds(_incrementDelay);
            }
        }
    }
}
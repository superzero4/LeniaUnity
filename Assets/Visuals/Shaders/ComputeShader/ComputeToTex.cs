using System.Collections;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Visuals.Shaders.ComputeShader
{
    public class ComputeToTex : MonoBehaviour
    {
        [FormerlySerializedAs("_displayedDepth")] [SerializeField, Range(0, 1024)]
        private int _displayedSlice = 0;

        [SerializeField, Range(-1, 2f)] private float _incrementDelay = .01f;

        [SerializeField] private ComputeShaderHandler _compute;
        [SerializeField] private Material _material;
        [SerializeField] private FilterMode _filterMode;
        private Texture2D _texture;

        private Color32[] colorMap;
        private float[] lastData;

        private void Update()
        {
        }

        [Button]
        private void UpdateTexture()
        {
            return;
            if (lastData == null)
                return;
            if (_texture == null || _texture.width != _compute.Size.x || _texture.height != _compute.Size.y ||
                _texture.format != TextureFormat.RGBA32 || _texture.filterMode != _filterMode)
            {
                Destroy(_texture);
                _texture = new Texture2D(_compute.Size.x, _compute.Size.y, TextureFormat.RGBA32, false);
                _texture.wrapMode = TextureWrapMode.Clamp;
                _texture.filterMode = _filterMode;
            }

            int sliceSize = _compute.Size.x * _compute.Size.y;
            _displayedSlice %= _compute.Size.z;
            int sliceOffset = sliceSize;
            var colors = new Color32[sliceSize];
            float min = 2;
            float max = -1;
            var sums = new float[sliceSize];
            int zMax = _compute.Size.z;
            float[] Zs = new float[zMax];
            float sumZ = 0;
            float radius = _compute.RadiusOfKernel;
            for (int z = 0; z < zMax; z++)
            {
                float X = (z - zMax / 2) / radius;
                float Z = X + ((zMax / 2) / radius);
                Zs[z] = Z;
                sumZ += Z;
            }

            for (int i = 0; i < sliceSize; i++)
            {
                float sum = 0;
                for (int z = 0; z < zMax; z++)
                {
                    float zRatio = Zs[z] / sumZ / 3;
                    int index = z * sliceOffset + i;
                    var value = lastData[index];
                    //Assert.IsTrue(value >= 0 && value <= 1 && !float.IsNaN(value),
                    //    $"Value {value} is out of range [0, 1]");
                    value = value * zRatio;
                    sum += value;
                    if (value < min)
                        min = value;
                    if (value > max)
                        max = value;
                }
                sums[i] = sum/zMax;
            }

            for (int i = 0; i < sliceSize; i++)
            {
                var normalized = (sums[i] - min) / (max - min);
                normalized = Mathf.Clamp01(normalized);
                colors[i] = colorMap[(int)(normalized * 252)];
            }

            _texture.SetPixelData(colors, 0);
            _texture.Apply();
            _material.SetTexture("_MainTex", _texture);
        }

        private void ReadBuffer(ComputeBuffer buffer)
        {
            int size = _compute.Size.x * _compute.Size.y * _compute.Size.z;
            lastData = new float[size];
            buffer.GetData(lastData, 0, 0, size);
        }

        [Button]
        public void Increment()
        {
            _displayedSlice++;
        }

        private IEnumerator Start()
        {
            colorMap = new Color32[256];
            for (int i = 0; i < 255; i++)
            {
                colorMap[i] = new Color32(map[i * 3], map[i * 3 + 1], map[i * 3 + 2], 1);
            }

            yield return new WaitUntil(() => _compute.ReadBuffer != null);
            yield return new WaitForEndOfFrame();
            while (true)
            {
                Increment();
                if (_incrementDelay > 0)
                    yield return new WaitForSeconds(_incrementDelay);
                else
                    yield return new WaitUntil(() => _incrementDelay > 0);
            }
        }

        public void Bind(ComputeBuffer outBuffer)
        {
            ReadBuffer(outBuffer);
            UpdateTexture();
        }

        private byte[] map = new byte[]
        {
            0, 0, 128, 0, 0, 132, 0, 0, 136, 0, 0, 140, 0, 0, 144, 0, 0, 148, 0, 0, 152, 0, 0, 156, 0, 0, 160, 0, 0,
            164, 0, 0, 168, 0, 0, 172, 0, 0, 176, 0, 0, 180, 0, 0, 184, 0, 0, 188, 0, 0, 192, 0, 0, 196, 0, 0, 200, 0,
            0, 204, 0, 0, 208, 0, 0, 212, 0, 0, 217, 0, 0, 221, 0, 0, 225, 0, 0, 229, 0, 0, 233, 0, 0, 237, 0, 0, 241,
            0, 0, 245, 0, 0, 249, 0, 0, 253, 0, 2, 255, 0, 6, 255, 0, 10, 255, 0, 14, 255, 0, 18, 255, 0, 22, 255, 0,
            26, 255, 0, 30, 255, 0, 34, 255, 0, 38, 255, 0, 42, 255, 0, 47, 255, 0, 51, 255, 0, 55, 255, 0, 59, 255, 0,
            63, 255, 0, 67, 255, 0, 71, 255, 0, 75, 255, 0, 79, 255, 0, 83, 255, 0, 87, 255, 0, 91, 255, 0, 95, 255, 0,
            99, 255, 0, 103, 255, 0, 107, 255, 0, 111, 255, 0, 115, 255, 0, 119, 255, 0, 123, 255, 0, 128, 255, 0, 132,
            255, 0, 136, 255, 0, 140, 255, 0, 144, 255, 0, 148, 255, 0, 152, 255, 0, 156, 255, 0, 160, 255, 0, 164, 255,
            0, 168, 255, 0, 172, 255, 0, 176, 255, 0, 180, 255, 0, 184, 255, 0, 188, 255, 0, 192, 255, 0, 196, 255, 0,
            200, 255, 0, 204, 255, 0, 208, 255, 0, 212, 255, 0, 217, 255, 0, 221, 255, 0, 225, 255, 0, 229, 255, 0, 233,
            255, 0, 237, 255, 0, 241, 255, 0, 245, 255, 0, 249, 255, 0, 253, 255, 2, 255, 253, 6, 255, 249, 10, 255,
            245, 14, 255, 241, 18, 255, 237, 22, 255, 233, 26, 255, 229, 30, 255, 225, 34, 255, 221, 38, 255, 217, 43,
            255, 212, 47, 255, 208, 51, 255, 204, 55, 255, 200, 59, 255, 196, 63, 255, 192, 67, 255, 188, 71, 255, 184,
            75, 255, 180, 79, 255, 176, 83, 255, 172, 87, 255, 168, 91, 255, 164, 95, 255, 160, 99, 255, 156, 103, 255,
            152, 107, 255, 148, 111, 255, 144, 115, 255, 140, 119, 255, 136, 123, 255, 132, 128, 255, 128, 132, 255,
            123, 136, 255, 119, 140, 255, 115, 144, 255, 111, 148, 255, 107, 152, 255, 103, 156, 255, 99, 160, 255, 95,
            164, 255, 91, 168, 255, 87, 172, 255, 83, 176, 255, 79, 180, 255, 75, 184, 255, 71, 188, 255, 67, 192, 255,
            63, 196, 255, 59, 200, 255, 55, 204, 255, 51, 208, 255, 47, 213, 255, 42, 217, 255, 38, 221, 255, 34, 225,
            255, 30, 229, 255, 26, 233, 255, 22, 237, 255, 18, 241, 255, 14, 245, 255, 10, 249, 255, 6, 253, 255, 2,
            255, 253, 0, 255, 249, 0, 255, 245, 0, 255, 241, 0, 255, 237, 0, 255, 233, 0, 255, 229, 0, 255, 225, 0, 255,
            221, 0, 255, 217, 0, 255, 213, 0, 255, 208, 0, 255, 204, 0, 255, 200, 0, 255, 196, 0, 255, 192, 0, 255, 188,
            0, 255, 184, 0, 255, 180, 0, 255, 176, 0, 255, 172, 0, 255, 168, 0, 255, 164, 0, 255, 160, 0, 255, 156, 0,
            255, 152, 0, 255, 148, 0, 255, 144, 0, 255, 140, 0, 255, 136, 0, 255, 132, 0, 255, 128, 0, 255, 123, 0, 255,
            119, 0, 255, 115, 0, 255, 111, 0, 255, 107, 0, 255, 103, 0, 255, 99, 0, 255, 95, 0, 255, 91, 0, 255, 87, 0,
            255, 83, 0, 255, 79, 0, 255, 75, 0, 255, 71, 0, 255, 67, 0, 255, 63, 0, 255, 59, 0, 255, 55, 0, 255, 51, 0,
            255, 47, 0, 255, 42, 0, 255, 38, 0, 255, 34, 0, 255, 30, 0, 255, 26, 0, 255, 22, 0, 255, 18, 0, 255, 14, 0,
            255, 10, 0, 255, 6, 0, 255, 2, 0, 253, 0, 0, 249, 0, 0, 245, 0, 0, 241, 0, 0, 237, 0, 0, 233, 0, 0, 229, 0,
            0, 225, 0, 0, 221, 0, 0, 217, 0, 0, 213, 0, 0, 208, 0, 0, 204, 0, 0, 200, 0, 0, 196, 0, 0, 192, 0, 0, 188,
            0, 0, 184, 0, 0, 180, 0, 0, 176, 0, 0, 172, 0, 0, 168, 0, 0, 164, 0, 0, 160, 0, 0, 156, 0, 0, 152, 0, 0,
            148, 0, 0, 144, 0, 0, 140, 0, 0, 136, 0, 0, 132, 0, 0, 128, 0, 0, 95, 95, 95, 127, 127, 127, 255, 255, 255
        };
    }
}
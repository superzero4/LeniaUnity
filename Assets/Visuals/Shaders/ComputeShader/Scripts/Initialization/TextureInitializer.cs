using UnityEngine;
using Visuals.Shaders.ComputeShader.Scripts;

namespace Visuals.Shaders.ComputeShader
{
    public class TextureInitializer : MonoBehaviour, IInitValues
    {
        public int[] Dims => new[] { _texture.width, _texture.height };
        [SerializeField] private Texture2D _texture;

        public float[] InitialValues()
        {
            var texture = _texture;
            float[] data = new float[texture.width * texture.height];
            var colors = texture.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                data[i] = colors[i].r;
            }

            return data;
        }
    }
}
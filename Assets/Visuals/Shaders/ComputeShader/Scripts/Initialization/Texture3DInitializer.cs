using UnityEngine;
using Visuals.Shaders.ComputeShader.Scripts;

public class Texture3DInitializer : MonoBehaviour, IInitValues
{
    public int[] Dims => new[] { _texture.width, _texture.height, _texture.depth };
    [SerializeField] private Texture3D _texture;

    public float[] InitialValues()
    {
        var texture = _texture;
        float[] data = new float[texture.width * texture.height * texture.depth];
        var colors = texture.GetPixels();
        for (int i = 0; i < colors.Length; i++)
        {
            data[i] = colors[i].r;
        }

        return data;
    }
}
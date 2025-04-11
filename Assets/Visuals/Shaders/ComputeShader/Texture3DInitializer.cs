using UnityEngine;

public class Texture3DInitializer : MonoBehaviour, IInitValues
{
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
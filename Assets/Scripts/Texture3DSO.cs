using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Texture3D", menuName = "Texture3D", order = 0)]
public class Texture3DSO : ScriptableObject
{
    [SerializeField] private Texture3D _texture;
    [SerializeField] private string _texturePath;

    public void Save(Texture3D texture, string nameAppendix)
    {
        _texture = texture;
        Save(nameAppendix);
    }

    public void Save(string nameAppendix)
    {
        var p = _texturePath + "-" + nameAppendix + ".asset";
#if UNITY_EDITOR
        if (AssetDatabase.AssetPathExists(p))
            AssetDatabase.DeleteAsset(p);
        AssetDatabase.CreateAsset(_texture, p);
        AssetDatabase.SaveAssets();
        Debug.LogWarning("Saved texture to " + p);
#endif
    }

    [Button("Save")]
    public void SaveSampleTexture()
    {
        _texture = new Texture3D(2, 2, 2, TextureFormat.RFloat, false);
        Color[] colors = new Color[8];
        for (int i = 0; i < 8; i++)
        {
            var rd = UnityEngine.Random.Range(0, 1f);
            colors[i] = new Color(i / 8f, 0, 0, .6f);
        }

        _texture.SetPixels(colors);
        _texture.Apply();
        Save("tex222");
    }

    [Button]
    private void Helper()
    {
        int width = 2;
        for (int i = 32 - width; i <= 32 + width; i++)
        {
            for (int j = 32 - width; j <= 32 + width; j++)
            {
                string str = "";
                for (int k = 32 - width; k <= 32 + width; k++)
                {
                    var val = _texture.GetPixel(i, j, k);
                    str += $"{val.r.ToString("F10")}, ";
                }

                Debug.Log(str + "\n");
            }

            Debug.LogWarning("----\n----\n");
        }
    }
}
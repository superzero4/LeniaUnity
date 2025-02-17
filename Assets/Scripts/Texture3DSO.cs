using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "Texture3D", menuName = "Texture3D", order = 0)]
    public class Texture3DSO : ScriptableObject
    {
        [SerializeField] private Texture3D _texture;
        [SerializeField] private bool _save;
        [SerializeField] private string _texturePath;

        public void SetTexture(Texture3D texture, string nameAppendix)
        {
            _texture = texture;
            Save(nameAppendix);
        }

        public void Save(string nameAppendix)
        {
            var p = _texturePath + "-" + nameAppendix + ".asset";
            if (AssetDatabase.AssetPathExists(p))
                AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(_texture, p);
            AssetDatabase.SaveAssets();
        }

        private void OnValidate()
        {
            if (_save)
            {
                _save = false;
                Action();
            }
        }

        public void Action()
        {
            _texture = new Texture3D(2, 2, 2, TextureFormat.RFloat, false);
            Color[] colors = new Color[8];
            for (int i = 0; i < 8; i++)
            {
                var rd = UnityEngine.Random.Range(0, 1f);
                colors[i] = new Color(i/8f, 0, 0, .6f);
            }
            _texture.SetPixels(colors);
            _texture.Apply();
            Save("tex222");
        }
    }
}
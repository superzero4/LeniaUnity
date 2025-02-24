using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public abstract class TextureFlipbookBase : MonoBehaviour
{
    [SerializeField] private List<Texture3D> _textures;
    [SerializeField, Range(0.001f, 2f)] private float _delay;
    [SerializeField] private bool _loadTextures;
    [SerializeField] private string _texturePath;

    private void OnValidate()
    {
        if (_loadTextures)
        {
            _loadTextures = false;
            _textures.Clear();
            foreach (var tex in AssetDatabase.FindAssets("",new string[] {_texturePath}))
            {
                var loaded = AssetDatabase.LoadAssetAtPath<Texture3D>(AssetDatabase.GUIDToAssetPath(tex));
                if (loaded != null)
                    _textures.Add(loaded);
            }

            var textures = AssetDatabase.LoadAllAssetsAtPath(_texturePath).Cast<Texture3D>();
            _textures.AddRange(textures);
        }
    }

    public List<Texture3D> Textures => _textures;

    private IEnumerator Start()
    {
        int i = 0;
        while (true)
        {
            yield return new WaitForSeconds(_delay);
            UpdateTexture(_textures[i]);
            i = (i + 1) % _textures.Count;
        }
    }

    protected abstract void UpdateTexture(Texture3D texture3D);
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

public abstract class TextureFlipbookBase : MonoBehaviour
{
    [SerializeField] private List<Texture3D> _textures;
    [SerializeField, Range(0.001f, 2f)] private float _delay;
    [SerializeField] private string _texturePath = "Assets/Visuals/Textures/";

    [Button("Load Textures at specified _texturePath")]
    private void LoadTextures()
    {
        if (string.IsNullOrEmpty(_texturePath))
        {
            _texturePath = "Assets/Visuals/Textures/";
            Debug.LogError("Texture path was empty, setting to default path: " + _texturePath +
                           ", write it correctly and try again");
            return;
        }

        _textures.Clear();
        foreach (var tex in AssetDatabase.FindAssets("", new string[] { _texturePath }))
        {
            var loaded = AssetDatabase.LoadAssetAtPath<Texture3D>(AssetDatabase.GUIDToAssetPath(tex));
            if (loaded != null)
                _textures.Add(loaded);
        }

        var textures = AssetDatabase.LoadAllAssetsAtPath(_texturePath).Cast<Texture3D>();
        _textures.AddRange(textures);
        if (_textures.Count == 0)
            Debug.LogError("No textures found at path: " + _texturePath + " update it and try again");
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
using System;
using UnityEngine;
using UnityEngine.Serialization;

public class MaterialPropertyFlipbook : TextureFlipbookBase
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Material instanceMat;

    [SerializeField] private string _shaderPropertyName;

    private void Awake()
    {
        instanceMat = _renderer.material;
    }

    protected override void UpdateTexture(Texture3D texture3D)
    {
        instanceMat.SetTexture(_shaderPropertyName, texture3D);
    }
}
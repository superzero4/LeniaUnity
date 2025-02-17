using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TextureFlipbook : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;

    [SerializeField] private List<Texture3D> _textures;
    [SerializeField] private string _shaderPropertyName;
    [FormerlySerializedAs("_fps")] [SerializeField,Range(0.001f,2f)] private float _delay;

    public List<Texture3D> Textures => _textures;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        var instanceMat = _renderer.material;
        int i = 0;
        while (true)
        {
            instanceMat.SetTexture(_shaderPropertyName, _textures[i]);
            i = (i + 1) % _textures.Count;
            yield return new WaitForSeconds(_delay);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

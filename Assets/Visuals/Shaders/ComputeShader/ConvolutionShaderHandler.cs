using System;
using System.Collections;
using UnityEngine;

public class ConvolutionShaderHandler : MonoBehaviour
{
    [SerializeField, Range(0, 10f)] private float _delay;
    [SerializeField] private ConvolutionShader _convol;

    private IEnumerator Start()
    {
        _convol.Init();
        yield return _convol.Loop(_delay);
    }
}

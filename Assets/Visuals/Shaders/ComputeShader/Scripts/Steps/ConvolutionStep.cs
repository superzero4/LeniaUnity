using System;
using System.Collections;
using NaughtyAttributes;
using UnityEngine;

public class ConvolutionStep : MonoBehaviour, IStep
{
    [SerializeField] private ConvolutionShaderHandler _convol;

    [Button]
    public IEnumerator Step(float delay)
    {
        yield return _convol.ConvolAllDim(delay);
    }

    public void Init(IInitValues init)
    {
        _convol.Init(init);
    }

    public void Release()
    {
        _convol.Release();
    }
}
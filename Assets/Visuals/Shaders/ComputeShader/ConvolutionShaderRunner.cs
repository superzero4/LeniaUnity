using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public class ConvolutionShaderRunner : MonoBehaviour
{
    [SerializeField, Range(0, 10f)] private float _initDelay;
    [SerializeField, Range(0, 10f)] private float _delay;
    [SerializeField] private ConvolutionShaderHandler _convol;
    [SerializeField] private bool _runOnce = false;

    private IEnumerator Start()
    {
        var kernel = GetComponentInChildren<IKernel>(false);
        Assert.IsNotNull(kernel, "Kernel not found");
        Debug.Log("Starting convolution with kernel : " + kernel.GetType().Name);
        _convol.Init(kernel, GetComponentInChildren<IInitValues>(false));
        yield return new WaitForSeconds(_initDelay);
        do
        {
            yield return _convol.ConvolAllDim(_delay);
        } while (!_runOnce);
    }
}
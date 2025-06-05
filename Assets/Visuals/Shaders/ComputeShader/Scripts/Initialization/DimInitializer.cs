using System;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using Visuals.Shaders.ComputeShader.Scripts;
using Random = System.Random;

public class DimInitializer : MonoBehaviour, IInitValues
{
    [Header("Settings")] [SerializeField] private int[] _dims;
    [SerializeField] private bool _useDefaultOverRandom = false;
    [SerializeField] private bool _01Randoms = true;

    [SerializeField, Range(0, 1f), ShowIf(nameof(_useDefaultOverRandom))]
    private float _defaultValue = 0.5f;

    private float _seed;

    private void Awake()
    {
        _seed = Time.realtimeSinceStartup;
    }

    public float[] InitialValues()
    {
        Random random = new Random((int)(_seed * 1000));
        return Enumerable.Range(0, (this as IInitValues).TotalSize).Select(i =>
            {
                if (_useDefaultOverRandom)
                    return _defaultValue;
                else
                {
                    float rd = (float)random.NextDouble();
                    if (_01Randoms)
                        rd = rd > .5f ? 1f : 0f;
                    return rd;
                }
            })
            .ToArray();
    }

    public int[] Dims => _dims;
}
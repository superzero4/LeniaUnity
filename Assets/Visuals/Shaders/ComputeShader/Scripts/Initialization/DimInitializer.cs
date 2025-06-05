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

    private int _seed;

    private void Awake()
    {
        _seed = System.DateTime.Now.Millisecond;
    }

    public float[] InitialValues()
    {
        Random random = new Random(_seed);
        float[] values = new float[(this as IInitValues).TotalSize];
        for (int i = 0; i < values.Length; i++)
        {
            if (_useDefaultOverRandom)
                values[i] = _defaultValue;
            else
            {
                float rd = (float)(random.NextDouble() % 1f);
                if (_01Randoms)
                    rd = rd > .5f ? 1f : 0f;
                values[i] = rd;
            }
        }

        return values;
    }

    public int[] Dims => _dims;
}
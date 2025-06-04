using System.Linq;
using NaughtyAttributes;
using UnityEngine;

public class DimInitializer : MonoBehaviour, IInitValues
{
    [Header("Settings")] [SerializeField] private int[] _dims;
    [SerializeField] private bool _useDefaultOverRandom = false;
    [SerializeField,Range(0,1f),ShowIf(nameof(_useDefaultOverRandom))] private float _defaultValue = 0.5f;
    public float[] InitialValues()
    {
        return Enumerable.Range(0, (this as IInitValues).TotalSize).Select(i => !_useDefaultOverRandom ? UnityEngine.Random.Range(0, 1f) : _defaultValue)
            .ToArray();
    }

    public int[] Dims => _dims;
}
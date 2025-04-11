using System.Linq;
using UnityEngine;

public class DimInitializer : MonoBehaviour, IInitValues
{
    [Header("Settings")] [SerializeField] private int[] _dims;

    public float[] InitialValues()
    {
        return Enumerable.Range(0, (this as IInitValues).TotalSize).Select(i => UnityEngine.Random.Range(0, 1f))
            .ToArray();
    }

    public int[] Dims => _dims;
}
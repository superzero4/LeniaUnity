using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Visuals.Shaders.ComputeShader.Scripts
{
    public interface IStep
    {
        public IEnumerator Step(ComputeBuffer entry, float delay);
        void Init(IInitValues init);
        void Release();
    }

    public interface IInitValues
    {
        public float[] InitialValues();
        public int[] Dims { get; }
        public IEnumerable<int> FilteredDims => Dims.Where(d => d > 1);
        public int TotalSize => Dims.Aggregate(1, (d, acc) => acc * d);
        public Vector3Int Size => new Vector3Int(Dims[0], Dims[1], Dims.Length >= 3 ? Dims[2] : 1);
        public int nbDim => FilteredDims.Count();
    }

    public interface IComputeBufferProvider
    {
        public UnityEvent<ComputeBuffer> OnUpdate { get; }
        Vector3Int Size3D { get; }
    }

    public interface IKernel
    {
        public int Radius { get; }

        public float KernelValue(uint[] coords, float relativeDistanceToCenter);
        public int Diameter => Radius * 2 + 1;
        public bool Normalize => true; // Default to true, can be overridden
    }

    public abstract class KernelInfo : MonoBehaviour, IKernel
    {
        [SerializeField, Range(1, 50)] private int _radius = 15;
        public int Radius => _radius;
        public abstract float KernelValue(uint[] coords, float distanceToCenter);
        public virtual bool Normalize => true;
    }
}
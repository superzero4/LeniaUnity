using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public interface IComputeBufferProvider
{
    ComputeBuffer Buffer { get; }
    Vector3Int Size3D { get; }
}

public interface IKernel
{
    public int Radius { get; }

    public float KernelValue(uint[] coords, float relativeDistanceToCenter);
    public int Diameter => Radius * 2 + 1;
}

public interface IInitValues
{
    public float[] InitialValues();
    public int[] Dims { get; }
    public int TotalSize => Dims.Aggregate(1, (d, acc) => acc * d);
    public Vector3Int Size => new Vector3Int(Dims[0], Dims[1], Dims.Length >= 3 ? Dims[2] : 1);
    public int nbDim => Dims.Count(d => d > 1);
}

public interface IStep
{
    public IEnumerator Step(float delay);
    void Init(IInitValues init);
    void Release();
}

public abstract class KernelInfo : MonoBehaviour, IKernel
{
    [SerializeField, Range(1, 50)] private int _radius = 15;
    public int Radius => _radius;
    public abstract float KernelValue(uint[] coords, float distanceToCenter);
}

public class ConvolutionShaderHandler : MonoBehaviour
{
    #region ShaderIDs

    private const int ConvolutionKernel = 0;
    private static readonly int Input = Shader.PropertyToID("_Input");
    private static readonly int Output = Shader.PropertyToID("_Output");
    private static readonly int Kernel = Shader.PropertyToID("_Kernel");

    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int KernelNorm = Shader.PropertyToID("_KernelNorm");
    private static readonly int NbDimId = Shader.PropertyToID("_nbDim");
    private static readonly int SizeArray = Shader.PropertyToID("_Res");

    #endregion

    private static readonly int ConvolutionDimension = Shader.PropertyToID("_ConvolDim");

    [SerializeField, Tooltip("Noise used instead")]
    private Texture3D _texture;

    [SerializeField] private bool _showConvol = true;


    [Header("References")] [SerializeField]
    private ComputeShader _computeShader;


    private ComputeBuffer _kernel;

    private IInitValues _info;
    public Vector3Int Size3D => _info.Size;

    private void Dispatch()
    {
        _computeShader.GetKernelThreadGroupSizes(ConvolutionKernel, out uint threadX, out uint threadY,
            out uint threadZ);
        var size = _info.TotalSize;
        float root = Mathf.Pow(size, 1f / 3);
        //int x = (int)Mathf.Floor(root);
        //int y = (int)Mathf.Floor(root);
        //int z = size / (x * y);
        int x = size;
        int y = 1;
        int z = 1;
        Assert.IsTrue(x * y * z == size, $"x:{x},y:{y},z:{z}!= {size}");
        _computeShader.Dispatch(ConvolutionKernel, (int)(x / threadX), (int)(y / threadY), (int)(z / threadZ));
    }



    public void Init(IInitValues _info)
    {
        var kernel = GetComponentInChildren<IKernel>(false);
        Assert.IsNotNull(kernel, "Kernel not found");
        Debug.Log("Starting convolution with kernel : " + kernel.GetType().Name);
        _kernel = new ComputeBuffer((int)Mathf.Pow(kernel.Diameter, _info.nbDim), sizeof(float));

        _computeShader.SetInt(NbDimId, _info.nbDim);
        //SetInts expect size of float4 for each value for an array, we have a int array and sizeof(int)=sizeof(float) therefore adding 3 "empty" values after each value as padding makes value go correctly in the shader
        var dim = _info.Dims.SelectMany(i => new int[] { i, 0, 0, 0 }).ToArray();
        _computeShader.SetInts(SizeArray, dim);
        _computeShader.SetInt(RadiusId, kernel.Radius);

        InitKernel(kernel);
    }


    public ComputeBuffer DispatchConvol(int current)
    {
        _toggle = !_toggle;
        _computeShader.SetBuffer(ConvolutionKernel, Input, ReadBuffer);
        _computeShader.SetBuffer(ConvolutionKernel, Output, Buffer);
        _computeShader.SetInt(ConvolutionDimension, current);
        Dispatch();
        return Buffer;
    }


    public IEnumerator ConvolAllDim(float delay)
    {
        for (int i = 0; i < _info.Dims.Length; i++)
        {
            DispatchConvol(i);
            if (delay > 0)
                yield return new WaitForSeconds(delay);
        }
    }

    public float KernelValue(float distanceToCenter)
    {
        return distanceToCenter <= 1f ? Mathf.Pow(4 * distanceToCenter * (1 - distanceToCenter), 4) : 0;
    }

    private void InitKernel(IKernel kernel)
    {
        var diam = kernel.Diameter;
        float[] flat = new float[_kernel.count];
        double norm = 0;
        for (int i = 0; i < flat.Length; i++)
        {
            uint tot = 0;
            uint[] coords = new uint[_info.nbDim];
            for (int j = 0; j < _info.nbDim; j++)
            {
                // jth out of nbDim coordinates of the ith element in a nbDim dimension space
                var coord = (uint)((i / Mathf.Pow(diam, j) % diam) - kernel.Radius);
                coords[j] = coord;
                tot += (coord * coord);
            }

            float r = Mathf.Sqrt(tot) / kernel.Radius;
            float val = kernel.KernelValue(coords, r);
            norm += val;
            flat[i] = val;
        }

        Assert.AreEqual(flat.Length, _kernel.count,
            $"Kernel size {flat.Length} != {_kernel.count}");
        _kernel.SetData(flat);
        _computeShader.SetBuffer(ConvolutionKernel, Kernel, _kernel);
        _computeShader.SetFloat(KernelNorm, (float)norm);
    }

    //private void CheckKenrel(float[][][] kernel)
    //{
    //    for (int x = 0; x <= _radius; x++)
    //    {
    //        for (int y = 0; y <= _radius; y++)
    //        {
    //            for (int z = 0; z <= _radius; z++)
    //            {
    //                var xmin = -x + _radius;
    //                var ymin = -y + _radius;
    //                var zmin = -z + _radius;
    //                var xmax = x + _radius;
    //                var ymax = y + _radius;
    //                var zmax = z + _radius;
    //                //We ensure the kernel is symmetrical and therefore symetrical;
    //                Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmax][ymax][zmax],
    //                    $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
    //                Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmax][ymax][zmin],
    //                    $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmax},{ymax},{zmin}");
    //                Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmax][ymin][zmax],
    //                    $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
    //                Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmax][ymin][zmin],
    //                    $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
    //                Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmin][ymax][zmax],
    //                    $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
    //                Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmin][ymax][zmin],
    //                    $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
    //                Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmin][ymin][zmax],
    //                    $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
    //                Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmin][ymin][zmin],
    //                    $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
    //            }
    //        }
    //    }
    //}

    public void Release()
    {
        _kernel?.Release();
    }
}
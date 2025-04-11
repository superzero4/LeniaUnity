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
    Vector3Int Size { get; }
}

public class ConvolutionShader : MonoBehaviour, IComputeBufferProvider
{
    #region ShaderIDs

    private const int ConvolutionKernel = 0;
    private static readonly int Input = Shader.PropertyToID("_Input");
    private static readonly int Output = Shader.PropertyToID("_Output");
    private static readonly int Kernel = Shader.PropertyToID("_Kernel");

    private static readonly int Radius = Shader.PropertyToID("_Radius");
    private static readonly int KernelNorm = Shader.PropertyToID("_KernelNorm");
    private static readonly int NbDim = Shader.PropertyToID("_nbDim");
    private static readonly int SizeArray = Shader.PropertyToID("_Res");

    #endregion

    private static readonly int ConvolutionDimension = Shader.PropertyToID("_ConvolDim");
    [Header("Settings")] [SerializeField] private int[] _dims;

    [SerializeField, Tooltip("Noise used instead")]
    private Texture3D _texture;

    [SerializeField] private bool _showConvol = true;
    [SerializeField, Range(1, 50)] private int _radius = 15;

    [Header("References")] [SerializeField]
    private ComputeShader _computeShader;


    private ComputeBuffer _buffer1;
    private ComputeBuffer _buffer2;
    private ComputeBuffer _kernel;

    private bool _toggle;
    public ComputeBuffer ReadBuffer => _toggle ? _buffer1 : _buffer2;
    public ComputeBuffer Buffer => _toggle ? _buffer2 : _buffer1;

    public int TotalSize => _dims.Aggregate(1, (d, acc) => acc * d);
    public Vector3Int Size => new Vector3Int(_dims[0], _dims[1], _dims[2]);

    private void Dispatch(int kernelIndex)
    {
        _computeShader.GetKernelThreadGroupSizes(kernelIndex, out uint threadX, out uint threadY, out uint threadZ);
        var size = TotalSize;
        float root = Mathf.Pow(size, 1f / 3);
        //int x = (int)Mathf.Floor(root);
        //int y = (int)Mathf.Floor(root);
        //int z = size / (x * y);
        int x = size;
        int y = 1;
        int z = 1;
        Assert.IsTrue(x * y * z == size, $"x:{x},y:{y},z:{z}!= {size}");
        _computeShader.Dispatch(kernelIndex, (int)(x / threadX), (int)(y / threadY), (int)(z / threadZ));
    }

    private int diameter => _radius * 2 + 1;
    private int nbDim => _dims.Length;

    public void Init()
    {
        ReleaseBuffers();
        var size = TotalSize;
        Debug.Log($"Total size of the space : {size}");
        _buffer1 = new ComputeBuffer(size, sizeof(float));
        _buffer2 = new ComputeBuffer(size, sizeof(float));
        _kernel = new ComputeBuffer((int)Mathf.Pow(diameter, nbDim), sizeof(float));

        _computeShader.SetInt(NbDim, nbDim);
        //SetInts expect size of float4 for each value for an array, we have a int array and sizeof(int)=sizeof(float) therefore adding 3 "empty" values after each value as padding makes value go correctly in the shader
        var dim = _dims.SelectMany(i => new int[] { i, 0, 0, 0 }).ToArray();
        _computeShader.SetInts(SizeArray, dim);
        _computeShader.SetInt(Radius, _radius);

        InitKernel();
        var values = InitialValues(_texture);
        _buffer1.SetData(values);
        _buffer2.SetData(values);
    }


    private float[] InitialValues(Texture3D texture)
    {
        float[] data = new float[texture.width * texture.height * texture.depth];
        var colors = texture.GetPixels();
        for (int i = 0; i < colors.Length; i++)
        {
            data[i] = colors[i].r;
        }

        return data;
    }

    public ComputeBuffer DispatchConvol(int current)
    {
        _toggle = !_toggle;
        _computeShader.SetBuffer(ConvolutionKernel, Input, ReadBuffer);
        _computeShader.SetBuffer(ConvolutionKernel, Output, Buffer);
        _computeShader.SetInt(ConvolutionDimension, current);
        Dispatch(ConvolutionKernel);
        return Buffer;
    }

    public IEnumerator Loop(float delay = -1f)
    {
        while (true)
        {
            for (int i = 0; i < _dims.Length; i++)
            {
                DispatchConvol(i);
                if (delay > 0)
                    yield return new WaitForSeconds(delay);
            }
        }
    }

    private void InitKernel()
    {
        var diam = diameter;
        Debug.LogWarning(("To make ND !!"));
        float[][][] kernel = new float[diam][][];
        double norm = 0;
        for (int x = -_radius; x <= _radius; x++)
        {
            float[][] yList = new float[diam][];
            for (int y = -_radius; y <= _radius; y++)
            {
                float[] zList = new float[diam];
                for (int z = -_radius; z <= _radius; z++)
                {
                    float r = Mathf.Sqrt(x * x + y * y + z * z) / _radius;
                    float val = r <= 1f ? Mathf.Pow(4 * r * (1 - r), 4) : 0;
                    norm += val;
                    zList[z + _radius] = r <= 1f ? 1f : 0;
                }

                yList[y + _radius] = zList;
            }

            kernel[x + _radius] = yList;
        }

        CheckKenrel(kernel);

        float[] flat = kernel.SelectMany(a => a.SelectMany(b => b)).ToArray();
        Assert.AreEqual(flat.Length, _kernel.count,
            $"Kernel size {flat.Length} != {_kernel.count}");
        _kernel.SetData(flat);
        _computeShader.SetBuffer(ConvolutionKernel, Kernel, _kernel);
        _computeShader.SetFloat(KernelNorm, (float)norm);
    }

    private void CheckKenrel(float[][][] kernel)
    {
        for (int x = 0; x <= _radius; x++)
        {
            for (int y = 0; y <= _radius; y++)
            {
                for (int z = 0; z <= _radius; z++)
                {
                    var xmin = -x + _radius;
                    var ymin = -y + _radius;
                    var zmin = -z + _radius;
                    var xmax = x + _radius;
                    var ymax = y + _radius;
                    var zmax = z + _radius;
                    //We ensure the kernel is symmetrical and therefore symetrical;
                    Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmax][ymax][zmax],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmax][ymax][zmin],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmax},{ymax},{zmin}");
                    Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmax][ymin][zmax],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmax][ymin][zmin],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmin][ymax][zmax],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmin][ymax][zmin],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmin][ymin][zmax],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual((float)kernel[xmax][ymax][zmax], (float)kernel[xmin][ymin][zmin],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                }
            }
        }
    }

    private void ReleaseBuffers()
    {
        _buffer1?.Release();
        _buffer2?.Release();
        _kernel?.Release();
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void OnApplicationQuit()
    {
        ReleaseBuffers();
    }
}
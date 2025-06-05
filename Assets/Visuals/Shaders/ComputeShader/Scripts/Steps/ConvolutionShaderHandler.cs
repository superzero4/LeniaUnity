using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Visuals.Shaders.ComputeShader.Scripts;

public class ConvolutionsStep : MonoBehaviour, IStep
{
    #region ShaderIDs

    private static readonly int Kernel = Shader.PropertyToID("_Kernel");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int KernelNorm = Shader.PropertyToID("_KernelNorm");
    private static readonly int NbDimId = Shader.PropertyToID("_nbDim");
    private static readonly int SizeArray = Shader.PropertyToID("_Res");

    #endregion

    private static readonly int ConvolutionDimension = Shader.PropertyToID("_ConvolDim");

    [SerializeField, Tooltip("Noise used instead")]
    private Texture3D _texture;


    [Header("References")]
    [SerializeField]
    private ComputeShader _computeShader => ShaderCommons.ConvolutionKernel.shader;


    private ComputeBuffer _kernel;

    private IInitValues _info;
    public Vector3Int Size3D => _info.Size;
    private ComputeBuffer _shared;
    private ComputeBuffer _intermediary;

    private bool _toggle;
    public ComputeBuffer Result => _toggle ? _shared : _intermediary;

    public ComputeBuffer Entry => _toggle ? _intermediary : _shared;
    public ComputeBuffer Shared => _shared;

    public void Init(IInitValues info)
    {
        _info = info;
        _intermediary = new ComputeBuffer(_info.TotalSize, sizeof(float));

        var kernel = GetComponentInChildren<IKernel>(false);
        Assert.IsNotNull(kernel, "Kernel not found");
        Debug.Log("Starting convolution with kernel : " + kernel.GetType().Name);
        _kernel = new ComputeBuffer((int)Mathf.Pow(kernel.Diameter, _info.nbDim), sizeof(float));

        _computeShader.SetInt(NbDimId, _info.nbDim);
        //SetInts expect size of float4 for each value for an array, we have a int array and sizeof(int)=sizeof(float) therefore adding 3 "empty" values after each value as padding makes value go correctly in the shader
        var dim = _info.FilteredDims.SelectMany(i => new int[] { i, 0, 0, 0 }).ToArray();
        _computeShader.SetInts(SizeArray, dim);
        _computeShader.SetInt(RadiusId, kernel.Radius);

        InitKernel(kernel);
    }

    public IEnumerator Step(ComputeBuffer entry, float delay)
    {
        _toggle = false;
        _shared = entry;
        yield return ConvolAllDim(delay);
    }


    public void DispatchConvol(int current)
    {
        ShaderCommons.SetBuffers(ShaderCommons.ConvolutionKernel, Entry, Result);
        _computeShader.SetInt(ConvolutionDimension, current);
        ShaderCommons.Dispatch(ShaderCommons.ConvolutionKernel, Entry.count);
        _toggle = !_toggle;
    }


    public IEnumerator ConvolAllDim(float delay)
    {
        for (int i = 0; i < _info.nbDim; i++)
        {
            DispatchConvol(i);
            if (delay > 0)
                yield return new WaitForSeconds(delay);
        }

        //Safe to ensure data ends in the shared buffer, independent on the parity of number of dimensions
        ShaderCommons.Copy(Entry, _shared);
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
                uint coord = (uint)((i / Mathf.Pow(diam, j) % diam));
                coords[j] = coord;
                tot += (uint)Mathf.Pow((int)coord - kernel.Radius, 2);
            }

            float r = Mathf.Sqrt(tot) / kernel.Radius;
            //Debug.Log(i+" coords : "+ string.Join(",", coords) + "tot :" +tot + " distance to center : " + r);
            float val = kernel.KernelValue(coords, r);
            norm += val;
            flat[i] = val;
        }

        if (kernel.Normalize)
            for (int i = 0; i < flat.Length; i++)
                flat[i] /= (float)norm;

        Assert.AreEqual(flat.Length, _kernel.count,
            $"Kernel size {flat.Length} != {_kernel.count}");
        _kernel.SetData(flat);
        _computeShader.SetFloat(KernelNorm, (float)norm);
        ShaderCommons.SetBuffer(ShaderCommons.ConvolutionKernel, Kernel, _kernel);
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
        _intermediary?.Release();
    }
}
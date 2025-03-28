using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Visuals.Shaders.ComputeShader;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

public class ComputeShaderHandler : MonoBehaviour
{
    [SerializeField] private ComputeToVertex _computeToVert;

    [Header("Settings")] [SerializeField, Range(1, 50)]
    private int _radius = 15;

    [SerializeField, Range(0, 1f), Tooltip("0.1f")]
    private float _timeStep = 0.1f;

    [SerializeField, Range(0, 1f), Tooltip("0.12f")]
    private float _mu = 0.12f;

    [SerializeField, Range(0, .1f), Tooltip("0.10f")]
    private float _sigma = 0.01f;

    [SerializeField, Tooltip("Noise used instead")]
    private Texture3D _texture;

    [SerializeField, Range(0.000001f, 5f)] private float _delayGenerations = .1f;
    [SerializeField, Range(0.000001f, 5f)] private float _delayNoise = .1f;

    [SerializeField] private Vector3Int _size;

    [Header("References")] [SerializeField]
    private ComputeShader _computeShader;

    [SerializeField] private Texture2D _baseTexture;

    private ComputeBuffer _buffer1; //Init state
    private ComputeBuffer _buffer2; //Convol in/out & Lenia out
    private ComputeBuffer _buffer3; //Convol in/out intermediate without altering init state
    private ComputeBuffer _kernel;

    private static readonly int Input = Shader.PropertyToID("_Input");
    private static readonly int Midput = Shader.PropertyToID("_MidPut");
    private static readonly int Output = Shader.PropertyToID("_Output");
    private static readonly int Radius = Shader.PropertyToID("_Radius");
    private static readonly int ResX = Shader.PropertyToID("ResX");
    private static readonly int ResY = Shader.PropertyToID("ResY");
    private static readonly int ResZ = Shader.PropertyToID("ResZ");
    private static readonly int Time = Shader.PropertyToID("_Time");
    private static readonly int Mouse = Shader.PropertyToID("mouse");
    private static readonly int dt = Shader.PropertyToID("dt");
    private static readonly int mu = Shader.PropertyToID("mu");
    private static readonly int sigma = Shader.PropertyToID("sigma");
    private static readonly int Kernel = Shader.PropertyToID("_kernel");
    private static readonly int KernelNorm = Shader.PropertyToID("kernelNorm");
    private static readonly int Convolution = Shader.PropertyToID("convolDim");
    private const int LeniaKernel = 0;
    private const int NoiseKernel = 1;
    private const int ConvolutionKernel = 2;
    private const int CopyKernel = 3;

    public ComputeBuffer ReadBuffer => !buffer1isWrite ? _buffer3 : _buffer2;
    public ComputeBuffer WriteBuffer => buffer1isWrite ? _buffer3 : _buffer2;

    public Vector3Int Size => _size;
    private bool buffer1isWrite;

    private void Dispatch(int kernelIndex)
    {
        int factor = 2; //Should be equal, for each dimension, to the numThreads values in the shader
        int x = Mathf.Max(1, _size.x / factor);
        int y = Mathf.Max(1, _size.y / factor);
        int z = Mathf.Max(1, _size.z / factor);
        _computeShader.Dispatch(kernelIndex, x, y, z);
    }

    private bool UseNoise => _texture == null;

    private IEnumerator Routine()
    {
        ReleaseBuffers();
        if (!UseNoise)
            _size = new Vector3Int(_texture.width, _texture.height, _texture.depth);
        _buffer1 = new ComputeBuffer(_size.x * _size.y * _size.z, sizeof(float));
        _buffer2 = new ComputeBuffer(_size.x * _size.y * _size.z, sizeof(float));
        if (!UseNoise)
        {
            _buffer1.SetData(_texture.GetPixelData<float>(0));
            _buffer2.SetData(_texture.GetPixelData<float>(0));
            _buffer3 = new ComputeBuffer(_size.x * _size.y * _size.z, sizeof(float));
        }

        _computeShader.SetBuffer(NoiseKernel, Input, _buffer1);
        //Inversion is basically assigning the correct one, we init this way;
        _computeShader.SetInt(ResX, _size.x);
        _computeShader.SetInt(ResY, _size.y);
        _computeShader.SetInt(ResZ, _size.z);
        _computeShader.SetInt(Radius, _radius);
        _computeShader.SetFloat(dt, _timeStep);
        _computeShader.SetFloat(mu, _mu);
        _computeShader.SetFloat(sigma, _sigma);
        var diam = 2 * _radius + 1;
        float[][][] kernel = new float[diam][][];
        float norm = 0;
        for (int x = -_radius; x <= _radius; x++)
        {
            float[][] yList = new float[diam][];
            for (int y = -_radius; y <= _radius; y++)
            {
                float[] zList = new float[diam];
                for (int z = -_radius; z <= _radius; z++)
                {
                    float r = Mathf.Sqrt(x * x + y * y + z * z) / _radius;
                    float val = r <= 1f ? (float)Math.Pow(4 * r * (1 - r), 4) : 0f;
                    norm += val;
                    zList[z + _radius] = val;
                }

                yList[y + _radius] = zList;
            }

            kernel[x + _radius] = yList;
        }

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
                    Assert.AreApproximatelyEqual(kernel[xmax][ymax][zmax], kernel[xmax][ymax][zmax],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual(kernel[xmax][ymax][zmax], kernel[xmax][ymax][zmin],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmax},{ymax},{zmin}");
                    Assert.AreApproximatelyEqual(kernel[xmax][ymax][zmax], kernel[xmax][ymin][zmax],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual(kernel[xmax][ymax][zmax], kernel[xmax][ymin][zmin],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual(kernel[xmax][ymax][zmax], kernel[xmin][ymax][zmax],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual(kernel[xmax][ymax][zmax], kernel[xmin][ymax][zmin],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual(kernel[xmax][ymax][zmax], kernel[xmin][ymin][zmax],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                    Assert.AreApproximatelyEqual(kernel[xmax][ymax][zmax], kernel[xmin][ymin][zmin],
                        $"{x},{y},{z}=>{xmax},{ymax},{zmax}!={xmin},{ymin},{zmin}");
                }
            }
        }

        float[] flat = kernel.SelectMany(a => a.SelectMany(b => b)).ToArray();
        _kernel = new ComputeBuffer(flat.Length, sizeof(float));
        _kernel.SetData(flat);
        _computeShader.SetBuffer(ConvolutionKernel, Kernel, _kernel);
        _computeShader.SetFloat(KernelNorm, norm);
        // Première étape: noise
        if (UseNoise)
            Dispatch(NoiseKernel);


        yield return new WaitForSeconds(_delayNoise);

        // Ensuite, Lenia

        _computeShader.SetVector(Time, Shader.GetGlobalVector(Time));
        //yield break;
        buffer1isWrite = false;
        (ComputeBuffer, ComputeBuffer)[] pingPongBuffers = new[]
        {
            (_buffer2, _buffer3),
            (_buffer3, _buffer2),
            (_buffer2, _buffer3)
        };
        //We define a ping pong series of buffer for intermediate steps;
        while (true)
        {
            //We copy last generation, to the _buffer1, untouched during intermediate steps
            SetInOutBuffer(CopyKernel, pingPongBuffers[^1].Item1, _buffer1, true);
            Dispatch(CopyKernel);
            yield return new WaitForSeconds(_delayGenerations / 3f);
            //Kernel is 3-Separable, we calculate the 3 axis separately with apply beetwen them
            //We ping pong beetween 2 buffers using the buffers specified in the array, the very first buffer contains the result of the last generation, the result of each step will become the input of the the next step
            for (int i = 0; i < 3; i++)
            {
                _computeShader.SetInt(Convolution, 2-i);
                SetInOutBuffer(ConvolutionKernel, Midput, pingPongBuffers[i].Item1, pingPongBuffers[i].Item2);
                Dispatch(ConvolutionKernel);
                yield return new WaitForSeconds(_delayGenerations / 3f);
            }

            yield return new WaitForSeconds(_delayGenerations / 3f);
            //For a new generation we need the result of the iterative convolution
            SetMidBuffer(LeniaKernel, pingPongBuffers[2].Item2);
            //We need the full untouched last generation info in _buffer1, we output the result in the last not in use buffer
            SetInOutBuffer(LeniaKernel, _buffer1, pingPongBuffers[2].Item1);
            Dispatch(LeniaKernel);
            yield return new WaitForSeconds(_delayGenerations / 3f);
        }

        ReleaseBuffers();
    }

    private void ReleaseBuffers()
    {
        _buffer1?.Release();
        _buffer2?.Release();
        _kernel?.Release();
    }

    private void SetInOutBuffer(int kernel, ComputeBuffer inBuffer, ComputeBuffer outBuffer, bool bindView = false)
    {
        SetInOutBuffer(kernel, Input, inBuffer, outBuffer, bindView);
    }

    private void SetInOutBuffer(int kernel, int inputIDOverride, ComputeBuffer inBuffer, ComputeBuffer outBuffer,
        bool bindView = false)
    {
        if (bindView)
            _computeToVert.Bind(outBuffer);
        _computeShader.SetBuffer(kernel, inputIDOverride, inBuffer);
        _computeShader.SetBuffer(kernel, Output, outBuffer);
    }

    private void SetMidBuffer(int kernel, ComputeBuffer midBuffer)
    {
        _computeShader.SetBuffer(kernel, Midput, midBuffer);
    }

#if UNITY_EDITOR
    EditorCoroutine _routine;
#endif

    //[Button]
    public void Run()
    {
        Debug.ClearDeveloperConsole();
        Stop();
        if (Application.isPlaying)
            StartCoroutine(Routine());
#if UNITY_EDITOR
        else
            _routine = EditorCoroutineUtility.StartCoroutineOwnerless(Routine());
#endif
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void OnApplicationQuit()
    {
        ReleaseBuffers();
    }

    //[Button]
    private void Stop()
    {
#if UNITY_EDITOR
        if (_routine != null)
        {
            EditorCoroutineUtility.StopCoroutine(_routine);
        }
#endif
        _buffer1?.Release();
        _buffer2?.Release();
    }

    void Start()
    {
        Run();
    }

    /*
    private void LeniaCPU(RenderTexture rt)
    {
        RenderTexture.active = rt;

        const int R = 15;       // space resolution = kernel radius
        const float T = 10.0f;       // time resolution = number of divisions per unit time
        const float dt = 1.0f/T;  // time step
        const float mu = 0.14f;     // growth center
        const float sigma = 0.014f; // growth width
        const float rho = 0.5f;     // kernel center
        const float omega = 0.15f;  // kernel width

        for (int x = 0; x < rt.width; x++)
        {
            for (int y = 0; y < rt.height; y++)
            {

            }
        }

        Vector2 uv = new Vector2(id.x / ResX, id.y / ResY);

        float sum = 0.0f;
        float total = 0.0f;

        for (int x = -R; x<= R; x++)
        {
            for (int y = -R; y <= R; y++)
            {
                float r = sqrt(float(x*x + y*y)) / R;
                uint2 txy = uint2(id.x + x, id.y + y) / uint2(ResX, ResY);
                float val = Result[txy];
                float weight = bell(r, rho, omega);
                sum += val * weight;
                total += weight;
            }
        }

        float avg = sum / total;
        float val = Result[uv];
        float growth = bell(avg, mu, sigma) * 2.0f - 1.0f;
        float c = clamp(val + (1.0f/T) * growth, 0.0f, 1.0f);

        /*
        if (_Time.y < 10.f || mouse)
        {
            c = 0.013f + noise(float2(id.x, id.y));
        }


        if (mouse)
        {
            float d = length((fragCoord.xy - iMouse.xy) / iResolution.xx);
            if (d <= R/iResolution.x) c = 0.02 + noise(fragCoord/R + mod(_Time.y,1.)*100.);
        }

        Result[id.xy] = c * 100;

        float Bell(float x, float m, float s)
        {
            return Mathf.Exp(-(x - m) * (x - m) / s / s / 2.0f);
        }
    }
    */
}
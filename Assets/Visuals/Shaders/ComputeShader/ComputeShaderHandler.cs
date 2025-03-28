using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using Visuals.Shaders.ComputeShader;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

public class ComputeShaderHandler : MonoBehaviour
{
    [SerializeField] private ComputeToVertex _computeToVert;

    [Header("Settings")] [SerializeField, Range(1, 50)]
    private int _radius = 15;

    [SerializeField, Range(0, 1f)] private float _timeStep = 0.1f;
    [SerializeField, Range(0, 1f)] private float _mu = 0.12f;
    [SerializeField, Range(0, .1f)] private float _sigma = 0.01f;

    [SerializeField, Tooltip("Noise used instead")]
    private Texture3D _texture;

    [SerializeField, Range(0.000001f, 5f)] private float _delayGenerations = .1f;
    [SerializeField, Range(0.000001f, 5f)] private float _delayNoise = .1f;

    [SerializeField] private Vector3Int _size;

    [Header("References")] [SerializeField]
    private ComputeShader _computeShader;

    [SerializeField] private Texture2D _baseTexture;

    private ComputeBuffer _buffer;
    private ComputeBuffer _buffer2;
    private ComputeBuffer _kernel;

    private static readonly int Input = Shader.PropertyToID("_Input");
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
    private const int LeniaKernel = 0;
    private const int NoiseKernel = 1;

    public ComputeBuffer ReadBuffer => toggle ? _buffer : _buffer2;
    public ComputeBuffer WriteBuffer => !toggle ? _buffer : _buffer2;

    public Vector3Int Size => _size;
    private bool toggle;

    private void Dispatch(int kernelIndex)
    {
        int factor = 8; //Should be equal, for each dimension, to the numThreads values in the shader
        int x = Mathf.Max(1, _size.x / factor);
        int y = Mathf.Max(1, _size.y / factor);
        int z = Mathf.Max(1, _size.z / factor);
        _computeShader.Dispatch(kernelIndex, x, y, z);
    }

    private bool UseNoise => _texture == null;

    private IEnumerator Routine()
    {
        _buffer?.Release();
        _buffer2?.Release();
        _kernel?.Release();
        if (!UseNoise)
            _size = new Vector3Int(_texture.width, _texture.height, _texture.depth);
        _buffer = new ComputeBuffer(_size.x * _size.y * _size.z, sizeof(float));
        _buffer2 = new ComputeBuffer(_size.x * _size.y * _size.z, sizeof(float));
        if (!UseNoise)
        {
            _buffer.SetData(_texture.GetPixelData<float>(0));
            _buffer2.SetData(_texture.GetPixelData<float>(0));
        }

        _computeShader.SetBuffer(NoiseKernel, Input, _buffer);
        _computeShader.SetBuffer(LeniaKernel, Input, _buffer);
        _computeShader.SetBuffer(LeniaKernel, Output, _buffer2);
        _computeShader.SetInt(ResX, _size.x);
        _computeShader.SetInt(ResY, _size.y);
        _computeShader.SetInt(ResZ, _size.z);
        _computeShader.SetInt(Radius, _radius);
        _computeShader.SetFloat(dt, _timeStep);
        _computeShader.SetFloat(mu, _mu);
        _computeShader.SetFloat(sigma, _sigma);
        float[][][] kernel = new float[2 * _radius + 1][][];
        float norm = 0;
        for (int x = -_radius; x <= _radius; x++)
        {
            float[][] yList = new float[2 * _radius + 1][];
            for (int y = -_radius; y <= _radius; y++)
            {
                float[] zList = new float[2 * _radius + 1];
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

        float[] flat = kernel.SelectMany(a => a.SelectMany(b => b)).ToArray();
        _kernel = new ComputeBuffer((int)Mathf.Pow(_radius * 2 + 1, 3), sizeof(float));
        _kernel.SetData(flat);
        _computeShader.SetBuffer(LeniaKernel, Kernel, _kernel);
        _computeShader.SetFloat(KernelNorm, norm);
        // Première étape: noise
        if (UseNoise)
            Dispatch(NoiseKernel);


        yield return new WaitForSeconds(_delayNoise);

        // Ensuite, Lenia

        _computeShader.SetVector(Time, Shader.GetGlobalVector(Time));
        //yield break;
        toggle = false;
        while (true)
        {
            //_computeShader.SetBool(BufferBool, toggle);
            _computeShader.SetBuffer(LeniaKernel, Input, ReadBuffer);
            _computeShader.SetBuffer(LeniaKernel, Output, WriteBuffer);
            Dispatch(LeniaKernel);
            toggle = !toggle;
            _computeToVert.Bind();
            yield return new WaitForSeconds(_delayGenerations);
        }

        _buffer.Release();
        _buffer2.Release();
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
        _buffer?.Release();
        _buffer2?.Release();
        _kernel?.Release();
    }

    private void OnApplicationQuit()
    {
        _buffer?.Release();
        _buffer2?.Release();
        _kernel?.Release();
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
        _buffer?.Release();
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
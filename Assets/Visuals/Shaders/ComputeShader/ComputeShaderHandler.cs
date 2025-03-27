using System;
using System.Collections;
using NaughtyAttributes;
using UnityEngine;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

public class ComputeShaderHandler : MonoBehaviour
{
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
    private const int LeniaKernel = 0;
    private const int NoiseKernel = 1;

    public ComputeBuffer Buffer => _buffer;

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
            _computeShader.SetBuffer(LeniaKernel, Input, toggle ? _buffer : _buffer2);
            _computeShader.SetBuffer(LeniaKernel, Output, toggle ? _buffer2 : _buffer);
            Dispatch(LeniaKernel);
            toggle = !toggle;
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
    }

    private void OnApplicationQuit()
    {
        _buffer?.Release();
        _buffer2?.Release();
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
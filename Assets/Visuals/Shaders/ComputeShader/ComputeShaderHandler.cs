using System.Collections;
using NaughtyAttributes;
using NaughtyAttributes.Test;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Serialization;

public class ComputeShaderHandler : MonoBehaviour
{
    [Header("Settings")] [SerializeField, Range(1, 50f)]
    private float _radius = 15f;

    [SerializeField, Tooltip("Noise used instead")]
    private Texture3D _texture;

    [SerializeField, Range(0.000001f, 5f)] private float _delayGenerations = .1f;
    [SerializeField, Range(0.000001f, 5f)] private float _delayNoise = .1f;

    [SerializeField] private Vector3Int _size;

    [Header("References")] [SerializeField]
    private ComputeShader _computeShader;

    [SerializeField] private Texture2D _baseTexture;

    private ComputeBuffer _buffer;

    private static readonly int BufferId = Shader.PropertyToID("_buffer");
    private static readonly int Radius = Shader.PropertyToID("_Radius");
    private static readonly int ResX = Shader.PropertyToID("ResX");
    private static readonly int ResY = Shader.PropertyToID("ResY");
    private static readonly int ResZ = Shader.PropertyToID("ResZ");
    private static readonly int Time = Shader.PropertyToID("_Time");
    private static readonly int Mouse = Shader.PropertyToID("mouse");
    private const int LeniaKernel = 0;
    private const int NoiseKernel = 1;

    public ComputeBuffer Buffer => _buffer;

    public Vector3Int Size => _size;

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
        if (_buffer != null)
            _buffer.Release();
        if (!UseNoise)
            _size = new Vector3Int(_texture.width, _texture.height, _texture.depth);
        _buffer = new ComputeBuffer(_size.x * _size.y * _size.z, sizeof(float));
        if (!UseNoise)
        {
            _buffer.SetData(_texture.GetPixelData<float>(0));
        }

        _computeShader.SetBuffer(NoiseKernel, BufferId, _buffer);
        _computeShader.SetBuffer(LeniaKernel, BufferId, _buffer);
        _computeShader.SetInt(ResX, _size.x);
        _computeShader.SetInt(ResY, _size.y);
        _computeShader.SetInt(ResZ, _size.z);
        _computeShader.SetFloat(Radius, _radius);
        // Première étape: noise
        if (UseNoise)
            Dispatch(NoiseKernel);


        yield return new WaitForSeconds(_delayNoise);

        // Ensuite, Lenia

        _computeShader.SetVector(Time, Shader.GetGlobalVector(Time));
        //yield break;
        while (true)
        {
            Dispatch(LeniaKernel);
            yield return new WaitForSeconds(_delayGenerations);
        }
    }

    EditorCoroutine _routine;

    [Button]
    public void Run()
    {
        Debug.ClearDeveloperConsole();
        Stop();
        if (Application.isPlaying)
            StartCoroutine(Routine());
        else
            _routine = EditorCoroutineUtility.StartCoroutineOwnerless(Routine());
    }

    [Button]
    private void Stop()
    {
        if (_routine != null)
            EditorCoroutineUtility.StopCoroutine(_routine);
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
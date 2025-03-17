using System.Collections;
using NaughtyAttributes;
using NaughtyAttributes.Test;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

public class ComputeShaderHandler : MonoBehaviour
{
    [Header("Settings")] [SerializeField, Range(1, 50f)]
    private float _radius = 15f;
    [SerializeField, Range(0.000001f, 5f)]
    private float _delay = .1f;

    [SerializeField] private Vector3Int _size;

    [Header("References")] [SerializeField]
    private ComputeShader _computeShader;

    [SerializeField] private Texture2D _baseTexture;

    private ComputeBuffer _buffer;

    private static readonly int BufferId = Shader.PropertyToID("_buffer");
    private static readonly int Radius = Shader.PropertyToID("_Radius");
    private static readonly int ResX = Shader.PropertyToID("ResX");
    private static readonly int ResY = Shader.PropertyToID("ResY");
    private static readonly int Time = Shader.PropertyToID("_Time");
    private static readonly int Mouse = Shader.PropertyToID("mouse");
    private const int LeniaKernel = 0;
    private const int NoiseKernel = 1;

    public ComputeBuffer Buffer => _buffer;

    public Vector3Int Size => _size;

    private void Dispatch(int kernelIndex)
    {
        int x = Mathf.Max(1, _size.x / 8);
        int y = Mathf.Max(1, _size.y / 8);
        int z = Mathf.Max(1, _size.z / 8);
        _computeShader.Dispatch(kernelIndex, x, y, z);
    }

    private IEnumerator Routine()
    {
        if (_buffer != null)
            _buffer.Release();
        _buffer = new ComputeBuffer(_size.x * _size.y * _size.z, sizeof(float));

        _computeShader.SetBuffer(NoiseKernel, BufferId, _buffer);
        _computeShader.SetBuffer(LeniaKernel, BufferId, _buffer);
        _computeShader.SetInt(ResX, _size.x);
        _computeShader.SetInt(ResY, _size.y);
        _computeShader.SetFloat(Radius, _radius);
        // Première étape: noise
        Dispatch(NoiseKernel);

        yield return new WaitForSeconds(_delay / 2f);

        // Ensuite, Lenia

        // _computeShader.SetFloat("R", 15.0f);
        _computeShader.SetVector(Time, Shader.GetGlobalVector(Time));

        yield return new WaitForSeconds(_delay);
        //yield break;
        while (true)
        {
            Dispatch(LeniaKernel);
            yield return new WaitForSeconds(_delay);
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
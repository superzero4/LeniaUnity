using System.Collections;
using NaughtyAttributes;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

public class ComputeShaderHandler : MonoBehaviour
{
    [SerializeField,Range(0.000001f,2f)] private float _delay = .1f;
    [SerializeField] private ComputeShader _computeShader;
    [SerializeField] private ComputeShader _noiseCompute;
    [SerializeField] private RenderTexture _renderTexture;
    [SerializeField] private Texture2D _baseTexture;
    
    private static readonly int Input = Shader.PropertyToID("_Input");
    private static readonly int Output = Shader.PropertyToID("_Output");
    private static readonly int NoiseResult = Shader.PropertyToID("NoiseResult");
    private static readonly int ResX = Shader.PropertyToID("ResX");
    private static readonly int ResY = Shader.PropertyToID("ResY");
    private static readonly int Time = Shader.PropertyToID("_Time");
    private static readonly int Mouse = Shader.PropertyToID("mouse");

    private IEnumerator Routine()
    {
        if (_renderTexture == null)
        {
            _renderTexture = new RenderTexture(256, 256, 1)
            {
                enableRandomWrite = true,
                name = "Result"
            };
            _renderTexture.Create();
        }
        
        // Première étape: noise
        _noiseCompute.SetTexture(0, NoiseResult, _renderTexture);
        _noiseCompute.Dispatch(0, _renderTexture.width / 8, _renderTexture.height / 8, 1);

        yield return new WaitForSeconds(.5f);
        
        // Ensuite, Lenia
        
        Texture2D tex = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGBAFloat, false);
        RenderTexture.active = _renderTexture;
        tex.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
        tex.Apply();

        //tex = _baseTexture;
        
        _computeShader.SetTexture(0, Output, _renderTexture);
        _computeShader.SetInt(ResX, _renderTexture.width);
        _computeShader.SetInt(ResY, _renderTexture.height);
        // _computeShader.SetFloat("R", 15.0f);
        _computeShader.SetVector(Time, Shader.GetGlobalVector (Time));
        
        yield return new WaitForSeconds(1f);
        
        while (true)
        {
            _computeShader.SetTexture(0, Output, _renderTexture);
            
            _computeShader.Dispatch(0, _renderTexture.width / 8, _renderTexture.height / 8, 1);
            
            yield return new WaitForSeconds(1f);
            
            RenderTexture.active = _renderTexture;
            tex.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
            tex.Apply();
            
            // _computeShader.SetTexture(0, Output, _renderTexture);
            yield return new WaitForSeconds(1f);
        }
    }

    EditorCoroutine _routine;
    [Button]
    public void Run()
    {
        Debug.ClearDeveloperConsole();
        Stop();
        _routine = EditorCoroutineUtility.StartCoroutineOwnerless(Routine());
    }
    [Button]
    private void Stop()
    {
        if(_routine != null)
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

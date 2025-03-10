using System.Collections;
using UnityEngine;

public class ComputeShaderHandler : MonoBehaviour
{
    [SerializeField] private ComputeShader _computeShader;
    [SerializeField] private ComputeShader _noiseCompute;
    [SerializeField] private RenderTexture _renderTexture;
    
    private static readonly int Result = Shader.PropertyToID("Result");
    private static readonly int NoiseResult = Shader.PropertyToID("NoiseResult");
    private static readonly int ResX = Shader.PropertyToID("ResX");
    private static readonly int ResY = Shader.PropertyToID("ResY");
    private static readonly int Time = Shader.PropertyToID("_Time");
    private static readonly int Mouse = Shader.PropertyToID("mouse");

    IEnumerator Start()
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

        yield return new WaitForSeconds(0.5f);
        
        // Ensuite, Lenia
        _computeShader.SetTexture(0, Result, _renderTexture);
        _computeShader.SetFloat(ResX, _renderTexture.width);
        _computeShader.SetFloat(ResY, _renderTexture.height);
        _computeShader.SetVector(Time, Shader.GetGlobalVector (Time));
        
        _computeShader.Dispatch(0, _renderTexture.width / 8, _renderTexture.height / 8, 1);

        yield return new WaitForSeconds(0.5f);
        
    }

    private void Update()
    {
        _computeShader.SetBool(Mouse, Input.GetMouseButtonDown(0));
    }
}

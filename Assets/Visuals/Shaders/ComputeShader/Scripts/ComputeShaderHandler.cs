using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Visuals.Shaders.ComputeShader;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif
/*
public class ComputeShaderHandler : MonoBehaviour
{
    [Header("Views")] [SerializeField] private ComputeToVertex _computeToVert;

    [SerializeField] private ComputeToTex _computeToTex;

    [SerializeField, Range(0, 1f), Tooltip("0.1f")]
    private float _timeStep = 0.1f;

    [SerializeField, Range(0, 1f), Tooltip("0.12f")]
    private float _mu = 0.12f;

    [SerializeField, Range(0, .1f), Tooltip("0.10f")]
    private float _sigma = 0.01f;

    [SerializeField] private LeniaJsonToTexAsset _parser;

    [SerializeField, Range(0.000001f, 5f)] private float _delayGenerations = .1f;
    [SerializeField, Range(0.000001f, 5f)] private float _delayNoise = .1f;

    [SerializeField] private Vector3Int _size;

    private ComputeBuffer _buffer3; //Convol in/out intermediate without altering init state
    private ComputeBuffer _floatBuffer;

    private static readonly int Midput = Shader.PropertyToID("_MidPut");
    private static readonly int FloatOutput = Shader.PropertyToID("_OutputF");
    private static readonly int dt = Shader.PropertyToID("dt");
    private static readonly int mu = Shader.PropertyToID("mu");
    private static readonly int sigma = Shader.PropertyToID("sigma");
    private static readonly int ResX = Shader.PropertyToID("ResX");
    private static readonly int ResY = Shader.PropertyToID("ResY");
    private static readonly int ResZ = Shader.PropertyToID("ResZ");
    

    private static readonly int Time = Shader.PropertyToID("_Time");

    //private static readonly int Mouse = Shader.PropertyToID("mouse");
    //private static readonly int dt = Shader.PropertyToID("dt");
    //private static readonly int mu = Shader.PropertyToID("mu");
    //private static readonly int sigma = Shader.PropertyToID("sigma");
    private const int LeniaKernel = 0;
    private const int NoiseKernel = 1;
    private const int CopyKernel = 3;
    private const int CopyToFloatKernel = 4;

    public ComputeBuffer ReadBuffer => _floatBuffer;

    public Vector3Int Size => _size;

    private bool UseNoise => _texture == null && _parser == null;

    public ConvolutionShaderHandler ConvolutionShaderHandler
    {
        get { return _convolutionShaderHandler; }
    }

    private List<double> InitialValues()
    {
        List<double> doubles = new List<double>();
        var g = _parser.Parser.Lenia.generations[0];
        for (int i = 0; i < Size.x; i++)
        {
            string str = "";
            for (int j = 0; j < Size.y; j++)
            {
                str = "";
                for (int k = 0; k < Size.z; k++)
                {
                    double value = g[i][j][k];
                    if (false && i >= 30 && i <= 34 && j >= 30 && j <= 34 && k >= 30 && k <= 34)
                        str += $"{value}, ";
                    doubles.Add(value);
                }

                if (!string.IsNullOrEmpty(str))
                    Debug.Log(str + "\n");
            }

            if (!string.IsNullOrEmpty(str))
            {
                Debug.LogWarning("---\n----");
                str = null;
            }
        }
        return doubles;
    }
    private void InitShaders()
    {
        ConvolutionShaderHandler.ReleaseBuffers();
        if (!UseNoise)
            _size = new Vector3Int(_texture.width, _texture.height, _texture.depth);
        _floatBuffer = new ComputeBuffer(_size.x * _size.y * _size.z, sizeof(float));
        //Always
        ConvolutionShaderHandler._computeShader.SetBuffer(CopyToFloatKernel, FloatOutput, _floatBuffer);
        
        //Inversion is basically assigning the correct one, we init this way;
        ConvolutionShaderHandler._computeShader.SetInt(ResX, _size.x);
        ConvolutionShaderHandler._computeShader.SetInt(ResY, _size.y);
        ConvolutionShaderHandler._computeShader.SetInt(ResZ, _size.z);
        ConvolutionShaderHandler._computeShader.SetFloat(dt, _timeStep);
        ConvolutionShaderHandler._computeShader.SetFloat(mu, _mu);
        ConvolutionShaderHandler._computeShader.SetFloat(sigma, _sigma);
    }

    private IEnumerator Routine()
    {
        InitShaders();
        ConvolutionShaderHandler.InitKernel();
        // Première étape: noise
        if (UseNoise) ConvolutionShaderHandler.Dispatch(NoiseKernel);
        //Initial state
        RaiseUpdate(ConvolutionShaderHandler._buffer1);
        yield return new WaitForSeconds(_delayNoise);
        RaiseUpdate(ConvolutionShaderHandler._buffer1);
        // Ensuite, Lenia

        ConvolutionShaderHandler._computeShader.SetVector(Time, Shader.GetGlobalVector(Time));
        (ComputeBuffer, ComputeBuffer)[] pingPongBuffers = new[]
        {
            (ConvolutionShaderHandler._buffer2, _buffer3),
            (_buffer3, ConvolutionShaderHandler._buffer2),
            (ConvolutionShaderHandler._buffer2, _buffer3)
        };
        var finalResultBuffer = pingPongBuffers[^1].Item1;
        //We define a ping pong series of buffer for intermediate steps;
        while (true)
        {
            //We copy last generation, to the _buffer1, untouched during intermediate steps
            Dispatch(CopyKernel, finalResultBuffer, ConvolutionShaderHandler._buffer1, true);
            yield return new WaitForSeconds(_delayGenerations / 3f);
            //Kernel is 3-Separable, we calculate the 3 axis separately with apply beetwen them
            //We ping pong beetween 2 buffers using the buffers specified in the array, the very first buffer contains the result of the last generation, the result of each step will become the input of the the next step
            for (int i = 0; i < 3; i++)
            {
                ConvolutionShaderHandler._computeShader.SetInt(global::ConvolutionShaderHandler.Convolution, i);
                Dispatch(global::ConvolutionShaderHandler.ConvolutionKernel, Midput, pingPongBuffers[i].Item1, pingPongBuffers[i].Item2, ConvolutionShaderHandler._showConvol);
                yield return new WaitForSeconds(_delayGenerations / 3f);
            }

            yield return new WaitForSeconds(_delayGenerations / 3f);
            //For a new generation we need the result of the iterative convolution
            SetMidBuffer(LeniaKernel, pingPongBuffers[2].Item2);
            //We need the full untouched last generation info in _buffer1, we output the result in the last not in use buffer
            Dispatch(LeniaKernel, ConvolutionShaderHandler._buffer1, finalResultBuffer, false);
            yield return new WaitForSeconds(_delayGenerations / 3f);
        }

        ConvolutionShaderHandler.ReleaseBuffers();
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="kernel"></param>
    /// <param name="inBuffer"></param>
    /// <param name="outBuffer"></param>
    /// <param name="bindView">True to copy to the float buffer and update the ones who use it</param>
    private void Dispatch(int kernel, ComputeBuffer inBuffer, ComputeBuffer outBuffer, bool bindView = false)
    {
        Dispatch(kernel, global::ConvolutionShaderHandler.Input, inBuffer, outBuffer, bindView);
    }

    private void Dispatch(int kernel, int inputIDOverride, ComputeBuffer inBuffer, ComputeBuffer outBuffer,
        bool bindView = false)
    {
        ConvolutionShaderHandler._computeShader.SetBuffer(kernel, inputIDOverride, inBuffer);
        ConvolutionShaderHandler._computeShader.SetBuffer(kernel, global::ConvolutionShaderHandler.Output, outBuffer);
        ConvolutionShaderHandler.Dispatch(kernel);
        if (bindView)
        {
            RaiseUpdate(outBuffer);
        }
    }

    private void RaiseUpdate(ComputeBuffer buffer)
    {
        ConvolutionShaderHandler._computeShader.SetBuffer(CopyToFloatKernel, global::ConvolutionShaderHandler.Input, buffer);
        ConvolutionShaderHandler.Dispatch(CopyToFloatKernel);
        //Debug.Log("Binded");
        //Debug.Break();
        _computeToVert.Bind(_floatBuffer);
        _computeToTex.Bind(_floatBuffer);
    }

    private void SetMidBuffer(int kernel, ComputeBuffer midBuffer)
    {
        ConvolutionShaderHandler._computeShader.SetBuffer(kernel, Midput, midBuffer);
    }

#if UNITY_EDITOR
    EditorCoroutine _routine;
    private readonly ConvolutionShaderHandler _convolutionShaderHandler;
    public ComputeShaderHandler()
    {
        _convolutionShaderHandler = new ConvolutionShaderHandler(this);
    }
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
        ConvolutionShaderHandler.ReleaseBuffers();
    }

    private void OnApplicationQuit()
    {
        ConvolutionShaderHandler.ReleaseBuffers();
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
        ConvolutionShaderHandler.ReleaseBuffers();
    }

    void Start()
    {
        Run();
    }

}
*/
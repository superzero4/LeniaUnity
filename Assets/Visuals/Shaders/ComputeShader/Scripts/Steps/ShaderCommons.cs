using UnityEngine;
using UnityEngine.Assertions;

public static class ShaderCommons
{
    private static readonly ComputeShader convolution = Resources.Load<ComputeShader>("ComputeShaders/Convolution");
    private static readonly ComputeShader growth = Resources.Load<ComputeShader>("ComputeShaders/Growth");

    public struct KernelID
    {
        public ComputeShader shader;
        public int kernel;
    }

    public static readonly KernelID ConvolutionKernel = new KernelID
    {
        shader = convolution,
        kernel = 0
    };


    public static readonly KernelID GrowthKernel = new KernelID
    {
        shader = growth,
        kernel = 0
    };

    public static readonly KernelID CopyKernel = new KernelID
    {
        shader = growth,
        kernel = 1
    };

    public static readonly int Input = Shader.PropertyToID("_Input");
    public static readonly int Output = Shader.PropertyToID("_Output");

    public static void SetBuffers(KernelID id, ComputeBuffer input, ComputeBuffer output)
    {
        SetBuffer(id, Input, input);
        SetBuffer(id, Output, output);
    }

    public static void SetBuffer(KernelID id, int bufferId, ComputeBuffer buffer)
    {
        //Debug.Log($"Setting buffer {bufferId},size : {buffer.count} for kernel {id.kernel} in shader {id.shader.name}");
        id.shader.SetBuffer(id.kernel, bufferId, buffer);
    }

    public static void Dispatch(KernelID id, int size)
    {
        id.shader.GetKernelThreadGroupSizes(id.kernel, out uint threadX, out uint threadY,
            out uint threadZ);
        float root = Mathf.Pow(size, 1f / 3);
        //int x = (int)Mathf.Floor(root);
        //int y = (int)Mathf.Floor(root);
        //int z = size / (x * y);
        int x = size;
        int y = 1;
        int z = 1;
        Assert.IsTrue(x * y * z == size, $"x:{x},y:{y},z:{z}!= {size}");
        id.shader.Dispatch(id.kernel, Mathf.Max(1,(int)(x / threadX)), Mathf.Max(1,(int)(y / threadY)), Mathf.Max(1,(int)(z / threadZ),1));
    }

    public static void Copy(ComputeBuffer input, ComputeBuffer output)
    {
        SetBuffers(CopyKernel, input, output);
        Dispatch(CopyKernel, input.count);
    }
}
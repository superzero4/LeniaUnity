using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

public class PointCloudRendererSimple : MonoBehaviour
{
    [SerializeField] private Texture3D texture;
    [SerializeField] private Shader pointShader;
    [SerializeField] private Color pointTint = Color.white;
    [SerializeField, Range(0.00001f, 10f)] public float pointSize = 0.05f;
    [SerializeField, Range(0.0001f, 20f)] private float _sizeMultiplier = 1f;

    private Material pointMaterial;
    private ComputeBuffer pointBuffer;


    void Start()
    {
        pointMaterial = new Material(pointShader);
        SendToShader();

        // Create the material
    }

    private void SendToShader()
    {
        // Get the pixel array from the texture
        Color[] pixelArray = texture.GetPixels();
        Vector4[] pointDataArray = new Vector4[pixelArray.Length];
        // Create a ComputeBuffer with the same length as the pixel array

        for (int i = 0; i < texture.width; i++)
        {
            for (int j = 0; j < texture.height; j++)
            {
                for (int k = 0; k < texture.depth; k++)
                {
                    int index = i * texture.depth * texture.height + j * texture.depth + k;
                    pointDataArray[index] = new Vector4(i, j, k, pixelArray[index].r);
                }
            }
        }

        if (pointBuffer == null || !pointBuffer.IsValid() || pointBuffer.count != pixelArray.Length)
        {
            if (pointBuffer != null)
                pointBuffer.Release();
            pointBuffer = new ComputeBuffer(pixelArray.Length, sizeof(float) * 4);
        }

        // Set the data of the ComputeBuffer using the pixel array
        pointBuffer.SetData(pointDataArray);
        UpdateView(texture.width, texture.height, texture.depth);
    }


    void OnRenderObject()
    {
        if (pointMaterial == null || pointBuffer == null) return;

        // Set shader properties
        pointMaterial.SetColor("_Tint", pointTint);
        pointMaterial.SetFloat("_PointSize", pointSize);
        pointMaterial.SetMatrix("_Transform", transform.worldToLocalMatrix);
        pointMaterial.SetFloat("_Size", _sizeMultiplier);

        pointMaterial.SetPass(0);
        //Draw the points
        Graphics.DrawProceduralNow(MeshTopology.Points, pointBuffer.count);
    }

    private void UpdateView(int width, int height, int depth)
    {
        pointMaterial.SetInt("_Width", width);
        pointMaterial.SetInt("_Height", height);
        pointMaterial.SetInt("_Depth", depth);
        pointMaterial.SetBuffer("_PointBuffer", pointBuffer);
    }

    void OnDestroy()
    {
        // Release the ComputeBuffer when it's no longer needed
        if (pointBuffer != null)
        {
            pointBuffer.Release();
        }
    }

    public void SetTexture(Texture3D texture3D)
    {
        texture = texture3D;
        UpdateView(texture.width, texture.height, texture.depth);
    }

    public void SetBuffer(ComputeBuffer buff, Vector3Int size)
    {
        pointMaterial = new Material(pointShader);
        pointBuffer = buff;
        UpdateView(size.x, size.y, size.z);
    }
}
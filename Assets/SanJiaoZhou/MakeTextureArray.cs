using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using System.IO;
using Unity.Mathematics;
using UnityEditor.Build;
#if UNITY_EDITOR
using Unity.VisualScripting;
using UnityEditor;
#endif

public class MakeTextureArray : MonoBehaviour
{
    public TerrainData terData;
    public bool EnableHeightBlend = true;
    public int HeightBlendEnd = 400;
    private Texture2DArray albedoArray;
    private Texture2DArray normalArray;
    public Texture2DArray heightArray;
    
    public Material UnityTerrainMaterial;
    public Material CustomTerrainMaterial;

    private Texture2D blendTex;
    private List<float> blendScaleList;
    private List<float> blendSharpnessList;

    private int TotalArrayLength;
    private int AlbedoArrayShaderId = Shader.PropertyToID("_AlbedoArray");
    private int NormalArrayShaderId = Shader.PropertyToID("_NormalArray");
    private int HeightArrayShaderId = Shader.PropertyToID("_HeightArray");
    private int TexArraryBlendShaderId = Shader.PropertyToID("_TexArrayBlend");
    private int TotalArrayLengthShaderId = Shader.PropertyToID("_TotalArrayLength");
    private int AlphaMapSize = Shader.PropertyToID("_AlphaMapSize");
    private int _BlendScaleArrayShaderId = Shader.PropertyToID("_BlendScale");
    private int _BlendSharpnessArrayShaderId = Shader.PropertyToID("_BlendSharpness");
    private int _HeightBlendEndShaderId = Shader.PropertyToID("_HeightBlendEnd");

    private Terrain ter;

    private struct LayerWeight
    {
        public int index;
        public float weight;
    }

    // Start is called before the first frame update
    private void Awake()
    {
        this.ter = GetComponent<Terrain>();
        if (blendScaleList == null || blendScaleList.Count == 0)
        {
            blendScaleList = new List<float>();
            for (var i = 0; i < 8; i++)
                blendScaleList.Add(1);
        }

        if (blendSharpnessList == null || blendSharpnessList.Count == 0)
        {
            blendSharpnessList = new List<float>();
            for (var i = 0; i < 8; i++)
                blendSharpnessList.Add(1);
        }

        var layers = terData.terrainLayers;
        Debug.Log("层数：" + layers.Length);
        TotalArrayLength = layers.Length; //8
        if (TotalArrayLength == 0)
            return;
        var firstLayer = layers[0];
        albedoArray = new Texture2DArray(firstLayer.diffuseTexture.width, firstLayer.diffuseTexture.height,
            TotalArrayLength, firstLayer.diffuseTexture.format, true, false);
        normalArray = new Texture2DArray(firstLayer.normalMapTexture.width, firstLayer.normalMapTexture.height,
            TotalArrayLength, firstLayer.normalMapTexture.format, true, true);
        for (var i = 0; i < layers.Length; i++)
        for (var m = 0; m < layers[i].diffuseTexture.mipmapCount; m++)
        {
            Graphics.CopyTexture(layers[i].diffuseTexture, 0, m, albedoArray, i, m);
            Graphics.CopyTexture(layers[i].normalMapTexture, 0, m, normalArray, i, m);
        }

        Shader.SetGlobalTexture(AlbedoArrayShaderId, albedoArray);
        Shader.SetGlobalTexture(NormalArrayShaderId, normalArray);
        Shader.SetGlobalTexture(HeightArrayShaderId, albedoArray); //此处应该使用alpha8的HeightMap,测试暂用AlbedoArray
        Shader.SetGlobalInt(TotalArrayLengthShaderId, TotalArrayLength);

        var width = terData.alphamapWidth;
        var height = terData.alphamapHeight;
        var maps = terData.GetAlphamaps(0, 0, width, height);
        var splatCount = terData.alphamapLayers;
        Debug.Log("图层数量:" + splatCount); //8
        blendTex = new Texture2D(width, height, TextureFormat.R16, false, true)
        {
            name = "_TexArrayBlend",
            anisoLevel = 0,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var tileIndex = new int2[width * height]; //记录每个像素处权重最大的两张tex的Id
        var weights = new LayerWeight[splatCount];
        var weightVal = new int[width * height];

        for (var i = 0; i < width; i++)
        for (var j = 0; j < height; j++)
        {
            for (var k = 0; k < splatCount; k++)
            {
                weights[k].weight = maps[i, j, k];
                weights[k].index = k;
            }

            Array.Sort(weights, (a, b) => { return -a.weight.CompareTo(b.weight); });
            var tw = 0f;
            var blendCount = 2;
            for (var k = 0; k < blendCount; k++)
                tw += weights[k].weight;
            if (tw == 0)
            {
                tw = 1;
                weights[0].weight = 1;
            }
            else
            {
                for (var k = 0; k < blendCount; k++)
                    weights[k].weight /= tw; //只考虑权重最大的两层，权重归一化
            }

            if (weights[1].weight == 0)
                weights[1].index = weights[0].index;

            tileIndex[i * height + j] = new int2(weights[0].index, weights[1].index);
            var weightDiff = Mathf.Clamp(weights[0].weight - weights[1].weight, 0, 0.999999f);
            weightVal[i * height + j] = Mathf.FloorToInt(64f * weightDiff); //把权重差值映射到0-64之间，可以用 6bit 来表示
        }

        //保存主纹理的索引+次纹理的索引+权重插值
        var texByte = new ushort[width * height];
        for (var i = 0; i < tileIndex.Length; i++)
            texByte[i] = (ushort)(weightVal[i] + (tileIndex[i].y << 6) + (tileIndex[i].x << 11));

        var texBytes = new byte[texByte.Length * 2];
        Buffer.BlockCopy(texByte, 0, texBytes, 0, texByte.Length * 2);
        blendTex.LoadRawTextureData(texBytes);
        blendTex.Apply(false, false);
        ter.materialTemplate.SetTexture(TexArraryBlendShaderId, blendTex);
        // transform.GetComponent<MeshRenderer>().sharedMaterial.SetTexture(TexArraryBlendShaderId, blendTex);
        Shader.SetGlobalVector(AlphaMapSize, new Vector4(width, 1.0f / width, 0, 0));

        Shader.SetGlobalFloat(_HeightBlendEndShaderId, HeightBlendEnd);
        Shader.SetGlobalFloatArray(_BlendScaleArrayShaderId, blendScaleList);
        Shader.SetGlobalFloatArray(_BlendSharpnessArrayShaderId, blendSharpnessList);
        if (EnableHeightBlend)
            Shader.EnableKeyword("_HeightBlend");
        else
            Shader.DisableKeyword("_HeightBlend");
    }


#if UNITY_EDITOR
    private void Update()
    {
        Shader.SetGlobalFloat(_HeightBlendEndShaderId, HeightBlendEnd);
        Shader.SetGlobalFloatArray(_BlendScaleArrayShaderId, blendScaleList);
        Shader.SetGlobalFloatArray(_BlendSharpnessArrayShaderId, blendSharpnessList);
        if (EnableHeightBlend)
            Shader.EnableKeyword("_HeightBlend");
        else
            Shader.DisableKeyword("_HeightBlend");

        if (Input.GetKeyDown(KeyCode.P))
        {
            ter.materialTemplate= UnityTerrainMaterial;
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            ter.materialTemplate = CustomTerrainMaterial;
            // ter.materialTemplate.SetTexture(TexArraryBlendShaderId, blendTex);
        }
    }
    
#endif

    private void OnDestroy()
    {
        DestroyImmediate(albedoArray);
        DestroyImmediate(normalArray);
        DestroyImmediate(blendTex);
    }
}
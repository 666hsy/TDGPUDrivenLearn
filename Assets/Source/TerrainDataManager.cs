using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

/// <summary>
/// ��ͼ�ߴ�10240*10240
/// </summary>
public class TerrainDataManager 
{
    public static string HEIGHT_MAP_FILE = "Assets/Texture/heightfield.png";
    public static string CS_GPU_DRIVEN_TERRAIN = "Assets/ComputeShader/GpuDrivenTerrain.compute";
    public static string CS_BUILD_HIZ_MAP = "Assets/ComputeShader/BuildHizMap.compute";
    public static string GPU_TERRAIN_MATERIAL = "Assets/Material/GPUDriven_GpuDrivenTerrain.mat";
    public static string MIN_MAX_HEIGHT_MAP = "Assets/Texture/MinMaxHeightMap.asset";
    private static string Addr_CopyDepthTexture = "Assets/Material/Unlit_CopyDepthTexture.mat";
    private static string Addr_TerrainDebugMat = "Assets/Material/TerrainDebug.mat";

    private Texture2D _heightFieldMap;
    private ComputeShader _CS_gpuDrivenTerrain;
    private ComputeShader _CS_BuildHizMap;
    private Texture2D _minMaxHeightMap;
    private Material _TerrainMat;
    private Material _MT_CopyDepthTexture;

    /// <summary>
    /// ��ͼ�ߴ�
    /// </summary>
    public static float TERRAIN_SIZE = 10240;

    /// <summary>
    /// Ϊ������node�ĳߴ磬��node����������������Ҫ����terrain�ĳߴ硣
    /// </summary>
    public static float REAL_TERRAIN_SIZE
    {
        get
        {
            float nodesize = MAX_LOD_PATCH_SIZE * PATCH_NUM_IN_NODE * (1 << MIN_LOD);
            return Mathf.Ceil(TERRAIN_SIZE / nodesize - 0.1f) * nodesize;
        }
    }

    /// <summary>
    /// LOD������� 0,1,2,3,4,5
    /// </summary>
    public static int MIN_LOD = 5;

    // <summary>
    /// LOD0 ʱ һ��patch�ĳߴ���8m x 8m
    /// </summary>
    public static float MAX_LOD_PATCH_SIZE = 8f;

    /// <summary>
    /// һ��patch��һ�����ж��ٸ�����
    /// </summary>
    public static int PATCH_GRID_NUM = 17;

    /// <summary>
    ///һ��node���а�����path������8 X 8��
    /// </summary>
    public static int PATCH_NUM_IN_NODE = 8;

    /// <summary>
    /// ���ڵ����Ĳ���������
    /// </summary>
    public static float LodJudgeFector = 1f;

    /// <summary>
    /// �߶�ͼ  [-WorldHeightScale��WorldHeightScale] ת��Ϊ�߶�ͼ�е�[0,1]
    /// </summary>
    public static float WorldHeightScale = 100f;

    public static Vector2Int HIZMapSize = new Vector2Int(2048, 1024);


    public RenderTexture HIZ_MAP;

    private static TerrainDataManager _instance;
    private TerrainDataManager()
    {

    }

    public static TerrainDataManager GetInstance()
    {
        if(_instance == null)
        {
            _instance = new TerrainDataManager();
        }
        return _instance;
    }

    public void Reset()
    {
        _NodeIndexOffsetList.Clear();
    }

    public Material TerrainMaterial
    {
        get
        {
            if(_TerrainMat == null)
            {
                var handler = Addressables.LoadAssetAsync<Material>(GPU_TERRAIN_MATERIAL);
                _TerrainMat = handler.WaitForCompletion();
            }
            return _TerrainMat;
        }
    }

    private Material _TerrainDebugMaterial;
    public Material TerrainDebugMaterial
    {
        get
        {
            if (_TerrainDebugMaterial == null)
            {
                var handler = Addressables.LoadAssetAsync<Material>(Addr_TerrainDebugMat);
                _TerrainDebugMaterial = handler.WaitForCompletion();
            }
            return _TerrainDebugMaterial;
        }
    }

    private Material _DefaultWhiteMaterial;
    public Material DefaultWhiteMaterial
    {
        get
        {
            if(_DefaultWhiteMaterial == null)
            {
                var handler = Addressables.LoadAssetAsync<Material>("Assets/Material/DefaultWhite.mat");
                _DefaultWhiteMaterial = handler.WaitForCompletion();
            }
            return _DefaultWhiteMaterial;
        }
    }

    /// <summary>
    /// ��ȡ��ǰ����߶�ͼ��֮������Ҫ�ĳ���ʽ����
    /// </summary>
    /// <returns></returns>
    public Texture2D TerrainHeightMap
    {
        get
        {
            if (_heightFieldMap == null)
            {
                var handler = Addressables.LoadAssetAsync<Texture2D>(HEIGHT_MAP_FILE);
                _heightFieldMap = handler.WaitForCompletion();
            }
            return _heightFieldMap;
        }
    }

    public Texture2D TerrainMinMaxHeightMap
    {
        get
        { 
            if(_minMaxHeightMap == null)
            {
                var handler = Addressables.LoadAssetAsync<Texture2D>(MIN_MAX_HEIGHT_MAP);
                _minMaxHeightMap = handler.WaitForCompletion();
            }
            return _minMaxHeightMap;
        }
    }

    /// <summary>
    /// ���εĲ���map
    /// </summary>
    /// <returns></returns>
    public Texture GetTerrainMateiralIDMap()
    {
        return null;
    }

    /// <summary>
    /// ��ȡĳ��LOD����Node�ĳߴ磨1��ά�ȣ�
    /// </summary>
    /// <param name="LOD"></param>
    /// <returns></returns>
    public float GetNodeSizeInLod(int LOD)
    {
        return MAX_LOD_PATCH_SIZE * PATCH_NUM_IN_NODE * (1 << LOD);
    }

    /// <summary>
    /// ��ȡĳ��LOD����Terrain��һ��ά����NODE����������LOD���𣬰������ܹ��������� result * result
    /// </summary>
    /// <param name="LOD"></param>
    /// <returns></returns>
    public int GetNodeNumInLod(int LOD)
    {
        return Mathf.FloorToInt(REAL_TERRAIN_SIZE / GetNodeSizeInLod(LOD) + 0.1f);
    }

    /// <summary>
    /// ��ȡĳ��LOD������һ��ά����PATCH�ĳ��ȣ��ߴ磩����patch�������result*result
    /// </summary>
    /// <param name="LOD"></param>
    /// <returns></returns>
    public float GetPatchSizeInLod(int LOD)
    {
        return MAX_LOD_PATCH_SIZE * (1 << LOD);
    }

    /// <summary>
    /// ����LOD�����NODE�洢����һ��һά�����У�Ϊ�˷�����ң���Ҫ��¼ÿ��LOD����ε���ʼindex
    /// </summary>
    /// <param name="LOD"></param>
    /// <returns></returns>
    public int GetNodeIndexOffset(int LOD)
    {
        int result = 0;
        for(int i= 0; i< LOD; i++)
        {
            int nodenum = GetNodeNumInLod(i);
            result += nodenum * nodenum;
        }
        return result; 
    }



    private List<int> _NodeIndexOffsetList =new List<int>();

    public List<int> NodeIndexOffsetList
    {
        get { 
            if(_NodeIndexOffsetList.Count == 0)
            {
                for (int i=0;i<=MIN_LOD;i++)
                {
                    _NodeIndexOffsetList.Add(GetNodeIndexOffset(i));
                }
            }
            return _NodeIndexOffsetList;
        }
    }



    public ComputeShader CS_GPUDrivenTerrain
    {
        get
        {
            if(_CS_gpuDrivenTerrain == null)
            {
                var handler = Addressables.LoadAssetAsync<ComputeShader>(CS_GPU_DRIVEN_TERRAIN);
                _CS_gpuDrivenTerrain = handler.WaitForCompletion();
            }
            return _CS_gpuDrivenTerrain;
        }
    }

    public void InitCS_GPUBuildHizMap(Action callback)
    {
        var handler = Addressables.LoadAssetAsync<ComputeShader>(CS_BUILD_HIZ_MAP);
        handler.Completed += (cs) =>{
            _CS_BuildHizMap = cs.Result;
            if (callback!=null) callback();
        };
    }

    public ComputeShader CS_GPUBuildHizMap
    {
        get
        {
            return _CS_BuildHizMap;
        }
    }
    

    public Material MT_CopyDepthTexture
    {
        get
        {
            if(_MT_CopyDepthTexture == null)
            {
                var handler = Addressables.LoadAssetAsync<Material>(Addr_CopyDepthTexture);
                _MT_CopyDepthTexture = handler.WaitForCompletion();
            }
            return _MT_CopyDepthTexture;
        }
    }

    public void ReleaseResource()
    {
        if(_heightFieldMap) Addressables.Release(_heightFieldMap);
        if (_CS_gpuDrivenTerrain) Addressables.Release(_CS_gpuDrivenTerrain);
        if (_CS_BuildHizMap) Addressables.Release(_CS_BuildHizMap);
        if (_TerrainMat) Addressables.Release(_TerrainMat);
        if (_minMaxHeightMap) Addressables.Release(_minMaxHeightMap);
        if (_MT_CopyDepthTexture) Addressables.Release(_MT_CopyDepthTexture);
        if (_TerrainDebugMaterial) Addressables.Release(_TerrainDebugMaterial);
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.TerrainUtils;
using UnityEngine.UIElements;

public class TerrainRenderer : MonoBehaviour
{
    GPUDrivenTerrainImpl mGPUDrivenTerrainImpl = new GPUDrivenTerrainImpl();

    public float LodJudgeFector = 100f;

    

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 200;

        TerrainDataManager.GetInstance().Reset();

        mGPUDrivenTerrainImpl.Init();
    }

    private void OnPreRender()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        TerrainDataManager.LodJudgeFector = LodJudgeFector;

        if (mGPUDrivenTerrainImpl.CheckRender())
        {
            mGPUDrivenTerrainImpl.ClearCommandBuffer();

            mGPUDrivenTerrainImpl.SetGlobaleValue();

            //��ʼ��LODMINʱ5x5��node
            mGPUDrivenTerrainImpl.CopyInputBuffer();

            //�����Ĳ���LOD��֮���Node�б�
            mGPUDrivenTerrainImpl.CreateLODNodeList();

            //���ɼ�¼LOD��sector�б�
            mGPUDrivenTerrainImpl.CreateSectorLodMap();

            //��׶�ü�
            mGPUDrivenTerrainImpl.CalFrustumCulledPatchList();

            //Node��չ��Patch
            mGPUDrivenTerrainImpl.CreatePatch();

            //Hiz�ڵ��޳�
            mGPUDrivenTerrainImpl.CalHizCulledPatchList();

            //ִ��commandbuffer
            mGPUDrivenTerrainImpl.ExecuteCommand();

            //��ComputeShader�ļ��������µ�������Ⱦshader
            mGPUDrivenTerrainImpl.UpdateTerrainMaterialParams();

            //���Ƶ���
            mGPUDrivenTerrainImpl.DrawTerrainInstance();

            //mGPUDrivenTerrainImpl.UpdateDebugMaterialParams();

            //mGPUDrivenTerrainImpl.DrawDebugCubeInstance();
        }
    }

    private void OnDestroy()
    {
        TerrainDataManager.GetInstance().ReleaseResource();

        mGPUDrivenTerrainImpl.Dispose();
    }

}

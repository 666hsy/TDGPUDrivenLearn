using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCreator
{
    private static MeshCreator instance;

    private MeshCreator()
    {
    }

    public static MeshCreator getInstance()
    {
        if (instance == null) instance = new MeshCreator();
        return instance;
    }

    public Mesh cacheQuadMesh = null;

    public Mesh debugCubeMesh = null;

    /// <summary>
    /// ����һ������ƽ������
    /// </summary>
    /// <param name="size">��������ĳߴ磺��x��</param>
    /// <param name="gridNum">��������������ٶ���, ǿ��Ϊ����</param>
    /// <returns></returns>
    public Mesh CreateQuardMesh(Vector2 size, Vector2Int gridNum)
    {
        var mesh = new Mesh();
        var grid_size = size / (gridNum - Vector2.one);

        var vertices = new Vector3[gridNum.x * gridNum.y];
        var uvs = new Vector2[gridNum.x * gridNum.y];
        for (var i = 0; i < gridNum.x; i++)
        for (var j = 0; j < gridNum.y; j++)
        {
            var posx = grid_size.x * (i - gridNum.x / 2);
            var posz = grid_size.y * (j - gridNum.y / 2);
            var pos = new Vector3(posx, 0, posz);
            var uv = new Vector2(i * 1.0f / (gridNum.x - 1), j * 1.0f / (gridNum.y - 1));
            vertices[j * gridNum.x + i] = pos;
            uvs[j * gridNum.x + i] = uv;
        }

        mesh.vertices = vertices;

        var indexs = new int[(gridNum.x - 1) * (gridNum.y - 1) * 6];

        for (var i = 0; i < gridNum.x - 1; i++)
        for (var j = 0; j < gridNum.y - 1; j++)
        {
            var tri_index = j * (gridNum.x - 1) + i;

            indexs[tri_index * 6] = j * gridNum.x + i;
            indexs[tri_index * 6 + 1] = (j + 1) * gridNum.x + i;
            indexs[tri_index * 6 + 2] = (j + 1) * gridNum.x + i + 1;

            indexs[tri_index * 6 + 3] = (j + 1) * gridNum.x + i + 1;
            indexs[tri_index * 6 + 4] = j * gridNum.x + i + 1;
            indexs[tri_index * 6 + 5] = j * gridNum.x + i;
        }

        mesh.triangles = indexs;
        //mesh.uv = uvs;
        mesh.RecalculateNormals();

        cacheQuadMesh = mesh;

        return mesh;
    }

    public Mesh CreateCube(float size)
    {
        var mesh = new Mesh();
        var vertices = new List<Vector3>();
        var extent = size * 0.5f;

        vertices.Add(new Vector3(-extent, -extent, -extent));
        vertices.Add(new Vector3(extent, -extent, -extent));
        vertices.Add(new Vector3(extent, extent, -extent));
        vertices.Add(new Vector3(-extent, extent, -extent));

        vertices.Add(new Vector3(-extent, extent, extent));
        vertices.Add(new Vector3(extent, extent, extent));
        vertices.Add(new Vector3(extent, -extent, extent));
        vertices.Add(new Vector3(-extent, -extent, extent));

        var indices = new int[6 * 6];

        int[] triangles =
        {
            0, 2, 1, //face front
            0, 3, 2,
            2, 3, 4, //face top
            2, 4, 5,
            1, 2, 5, //face right
            1, 5, 6,
            0, 7, 4, //face left
            0, 4, 3,
            5, 4, 7, //face back
            5, 7, 6,
            0, 6, 7, //face bottom
            0, 1, 6
        };

        mesh.SetVertices(vertices);
        mesh.triangles = triangles;
        mesh.UploadMeshData(false);
        debugCubeMesh = mesh;
        return mesh;
    }
}
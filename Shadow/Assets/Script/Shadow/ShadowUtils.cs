using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 常用方法
/// </summary>
public static class ShadowUtils
{
    public static float zMin = float.MaxValue, zMax = float.MinValue;

    /// <summary>
    /// 查找场景中所有游戏物体并返回所有游戏物体场景AABB包围盒顶点
    /// </summary>
    /// <param name="allGameObjects">所有游戏物体</param>
    /// <param name="receiveShadowObjects">接收阴影游戏物体</param>
    /// <param name="castLayerMasks">投射阴影层</param>
    /// <param name="receiveLayerMasks">接收阴影层</param>
    /// <param name="AABBBounds">AABB包围盒游戏物体</param>
    /// <returns>返回包围盒顶点</returns>
    public static void FindAllGameObject(Transform[] allGameObjects, List<GameObject> receiveShadowObjects, LayerMask receiveLayerMasks)
    {
        Shader.DisableKeyword("ReceiveLayer");
        List<GameObject> sceneObject = new List<GameObject>();
        if (allGameObjects.Length == 0)
        {
            Debug.LogError("你不会走的这儿吧");
            return;
        }
        else
        {
            for (int i = 0; i < allGameObjects.Length; i++)
            {
                sceneObject.Add(allGameObjects[i].gameObject);
                receiveShadowObjects.Add(allGameObjects[i].gameObject);
                if ((0x1 << allGameObjects[i].gameObject.layer & receiveLayerMasks.value) == (0x1 << allGameObjects[i].gameObject.layer)) //接收阴影
                {
                    if (allGameObjects[i].gameObject.GetComponent<MeshRenderer>() == null)
                        Shader.EnableKeyword("ReceiveLayer");
                    else
                        allGameObjects[i].gameObject.GetComponent<MeshRenderer>().material.EnableKeyword("ReceiveLayer");
                }
            }
        }
    }

    /// <summary>
    /// 获取场景AABB包围盒大小
    /// </summary>
    /// <param name="go">所有能被光源相机渲染的游戏物体</param>
    /// <param name="AABBBounds">场景包围盒游戏物体</param>
    /// <returns></returns>
    public static GameObject GetSceneAABBBounds(List<GameObject> go, GameObject AABBBounds)
    {
        Vector3 center = Vector3.zero;
        Vector3 size = Vector3.zero;
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < go.Count; i++)
        {
            min = ComputeVectorMin(go[i].transform.position - go[i].transform.localScale / 2, min);        //用游戏物体自身的坐标 - 游戏物体自身的大小获取最小值
            max = ComputeVectorMax(go[i].transform.position + go[i].transform.localScale / 2, max);        //用游戏物体自身的坐标 + 游戏物体自身的大小获取最大值
            center = new Vector3((min.x + max.x) / 2, (min.y + max.y) / 2, (min.z + max.z) / 2);           //获取包围盒中心点
        }
        float xSize = max.x - min.x;
        float ySize = max.y - min.y;
        float zSize = max.z - min.z;
        size = new Vector3(xSize, ySize, zSize);
        Renderer[] renders = new Renderer[go.Count];
        for (int i = 0; i < go.Count; i++)
        {
            renders[i] = go[i].GetComponent<Renderer>();
        }
        Bounds bounds = new Bounds(center, size);
        foreach (Renderer child in renders)
        {
            bounds.Encapsulate(child.bounds);
        }
        AABBBounds.transform.position = bounds.center;
        AABBBounds.transform.localScale = bounds.size;
        if (AABBBounds.GetComponent<BoxCollider>() == null)
            AABBBounds.AddComponent<BoxCollider>();
        return AABBBounds;
    }

    /// <summary>
    /// 世界空间包围盒顶点转换到光源空间顶点
    /// </summary>
    /// <param name="light">光源</param>
    /// <param name="sceneAABBBoundsVertexs">场景AABB包围盒顶点</param>
    public static void SceneVertexsToLightSpaceVertexs(GameObject light, List<Vector3> sceneAABBBoundsVertexs)
    {
        for (int i = 0; i < sceneAABBBoundsVertexs.Count; i++)
        {
            UpdateZ(light.transform.worldToLocalMatrix.MultiplyPoint(sceneAABBBoundsVertexs[i]).z);
        }
    }

    /// <summary>
    /// 更新zMin，zMax
    /// </summary>
    /// <param name="value"></param>
    public static void UpdateZ(float value)
    {
        zMin = Mathf.Min(zMin, value);
        zMax = Mathf.Max(zMax, value);
    }

    /// <summary>
    /// 计算最小值
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="min"></param>
    /// <returns></returns>
    public static Vector3 ComputeVectorMin(Vector3 v1, Vector3 min)
    {
        Vector3 temp;
        temp.x = v1.x < min.x ? v1.x : min.x;                           //v1.x < float.MaxValue ? v1.x : float.MaxValue
        temp.y = v1.y < min.y ? v1.y : min.y;
        temp.z = v1.z < min.z ? v1.z : min.z;
        return temp;
    }

    /// <summary>
    /// 计算最大值
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static Vector3 ComputeVectorMax(Vector3 v1, Vector3 max)
    {
        Vector3 temp;
        temp.x = v1.x > max.x ? v1.x : max.x;                           //v1.x > float.MinValue ? v1.x : float.MinValue
        temp.y = v1.y > max.y ? v1.y : max.y;
        temp.z = v1.z > max.z ? v1.z : max.z;
        return temp;
    }

    /// <summary>
    /// 把光源相机纹理贴图传入Shader
    /// </summary>
    /// <param name="lightCamera">光源相机</param>
    /// <param name="shadowMap">RenderTexture</param>
    public static void RenderTextureToShader(Camera lightCamera, RenderTexture shadowMap)
    {
        lightCamera.targetTexture = shadowMap;
        Shader.SetGlobalTexture("_ShadowMap", lightCamera.targetTexture);
    }

    /// <summary>
    /// 把方差相机纹理贴图传入Shader
    /// </summary>
    /// <param name="varianceCamera">方差相机</param>
    /// <param name="shadowMap">RenderTexture</param>
    public static void RenderTextureVarianceToShader(Camera varianceCamera, RenderTexture shadowMap)
    {
        varianceCamera.targetTexture = shadowMap;
        Shader.SetGlobalTexture("_ShadowMapVariance", varianceCamera.targetTexture);
    }

    /// <summary>
    /// 把方差相机投影矩阵传入shader
    /// </summary>
    /// <param name="varianceCamera">方差相机</param>
    public static void VarianceMatrixToShader(Camera varianceCamera)
    {
        Matrix4x4 worldToView = varianceCamera.worldToCameraMatrix;
        Matrix4x4 projection = GL.GetGPUProjectionMatrix(varianceCamera.projectionMatrix, false);
        Shader.SetGlobalMatrix("_VarianceLightProjectionMatrix", projection * worldToView);
    }

    /// <summary>
    /// 把光源相机投影矩阵传入shader
    /// </summary>
    /// <param name="lightCamera">光源相机</param>
    public static void MatrixToShader(Camera lightCamera)
    {
        Matrix4x4 worldToView = lightCamera.worldToCameraMatrix;
        Matrix4x4 projection = GL.GetGPUProjectionMatrix(lightCamera.projectionMatrix, false);
        Shader.SetGlobalMatrix("_LightProjectionMatrix", projection * worldToView);
    }

    /// <summary>
    /// 添加原点为中心的正方形 --- 构建一个正方形
    /// </summary>
    /// <param name="list">存放顶点数据</param>
    /// <param name="trans">视野相机从局部坐标转换到世界坐标矩阵</param>
    /// <param name="xSize"></param>
    /// <param name="ySize"></param>
    /// <param name="z">相机深度</param>
    public static void AddOriginCenteredSquare(List<Vector3> list, Matrix4x4 trans, float xSize, float ySize, float z)
    {
        list.Add(trans.MultiplyPoint(new Vector3(-xSize, -ySize, z)));
        list.Add(trans.MultiplyPoint(new Vector3(-xSize, ySize, z)));
        list.Add(trans.MultiplyPoint(new Vector3(xSize, -ySize, z)));
        list.Add(trans.MultiplyPoint(new Vector3(xSize, ySize, z)));
    }

    /// <summary>
    /// 构建正交相机视锥体
    /// </summary>
    /// <param name="sceneAABB">AABB包围盒</param>
    /// <returns></returns>
    public static List<Vector3> GetSceneBoundVertexs(GameObject sceneAABB)
    {
        List<Vector3> vertexs = new List<Vector3>();

        Matrix4x4 sceneAABBWorldMatrix = sceneAABB.transform.localToWorldMatrix;//局部坐标转换世界坐标矩阵
        AddOriginCenteredSquare(vertexs, sceneAABBWorldMatrix, 1, 1, -0.5f);
        AddOriginCenteredSquare(vertexs, sceneAABBWorldMatrix, 1, 1, 0.5f);

        return vertexs;
    }

    /// <summary>
    /// 得到视野相机视锥体截面大小
    /// </summary>
    /// <param name="camera">主摄像机</param>
    /// <param name="z">摄像机近远平面大小</param>
    /// <returns></returns>
    public static Vector3 GetViewCameraFrustumSectionSize(Camera camera, float z)
    {
        Vector3 v;
        v.y = z * Mathf.Tan(camera.fieldOfView / 2 * Mathf.Deg2Rad);
        v.x = v.y * camera.aspect;//camera.aspect -- 屏幕宽高比
        v.z = z;
        return v;
    }

    /// <summary>
    /// 得到透视相机视锥体8个顶点
    /// </summary>
    /// <param name="viewCamera">视野相机</param>
    /// <param name="lightCamera">光源相机</param>
    /// <param name="farClipPlane">远平面</param>
    /// <returns></returns>
    public static List<Vector3> GetPerspectiveCameraFrustumVertexs(Camera viewCamera, float farClipPlane)
    {
        List<Vector3> vertexs = new List<Vector3>();
        Vector3 nearSize = GetViewCameraFrustumSectionSize(viewCamera, viewCamera.nearClipPlane);
        Vector3 farSize = GetViewCameraFrustumSectionSize(viewCamera, farClipPlane);

        Matrix4x4 trans = viewCamera.transform.localToWorldMatrix;
        AddOriginCenteredSquare(vertexs, trans, nearSize.x, nearSize.y, nearSize.z);
        AddOriginCenteredSquare(vertexs, trans, farSize.x, farSize.y, farSize.z);

        return vertexs;
    }

    /// <summary>
    /// 正交摄影机自适应
    /// </summary>
    /// <param name="light">光源</param>
    /// <param name="vertexs">顶点</param>
    public static void OrthogonalLightCameraSelfAdaption(Camera lightCamera, GameObject light, List<Vector3> vertexs, float shadowResolution, float farClipPlane)
    {
        lightCamera.orthographicSize = 1;
        lightCamera.ResetProjectionMatrix();
        List<Vector3> lightSpaceVertexs = new List<Vector3>();                              //光源空间顶点
        Vector3 minValue = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxValue = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < vertexs.Count; i++)
        {
            lightSpaceVertexs.Add(light.transform.worldToLocalMatrix.MultiplyPoint(vertexs[i]));
            minValue = ShadowUtils.ComputeVectorMin(lightSpaceVertexs[i], minValue);
            maxValue = ShadowUtils.ComputeVectorMax(lightSpaceVertexs[i], maxValue);
        }

        float radius = ComputeSphereRadius(lightSpaceVertexs, lightSpaceVertexs.Count);

        Vector3 controlRotatingShadowWithoutJitterValue = new Vector3(radius + radius * 0.45f, radius + radius * 0.45f, radius + radius * 0.45f);      //控制旋转主相机阴影不抖动值
        //Vector3 controlRotatingShadowWithoutJitterValue = new Vector3(radius, radius, radius);      //控制旋转主相机阴影不抖动值
        Vector3 CRSWJV = controlRotatingShadowWithoutJitterValue;
        float f = 1f / (float)shadowResolution;
        Shader.SetGlobalFloat("_shadowMapSize", f);
        Vector3 v = new Vector3(f, f, f);

        Vector3 eachPixelIsInWorldSpaceUnitSize = Vector3.Scale(CRSWJV * 2, v);             //每个像素处于世界空间单位大小

        minValue.x /= eachPixelIsInWorldSpaceUnitSize.x;
        minValue.y /= eachPixelIsInWorldSpaceUnitSize.y;
        minValue.z /= eachPixelIsInWorldSpaceUnitSize.z;
        minValue = ComputeFloor(minValue);
        minValue = Vector3.Scale(minValue, eachPixelIsInWorldSpaceUnitSize);                //控制移动相机阴影不抖动

        maxValue.x /= eachPixelIsInWorldSpaceUnitSize.x;
        maxValue.y /= eachPixelIsInWorldSpaceUnitSize.y;
        maxValue.z /= eachPixelIsInWorldSpaceUnitSize.z;
        maxValue = ComputeFloor(maxValue);
        maxValue = Vector3.Scale(maxValue, eachPixelIsInWorldSpaceUnitSize);                //控制移动相机阴影不抖动

        CRSWJV.x /= eachPixelIsInWorldSpaceUnitSize.x;
        CRSWJV = ComputeFloor(CRSWJV);
        CRSWJV = Vector3.Scale(CRSWJV, eachPixelIsInWorldSpaceUnitSize);                    //控制旋转相机阴影不抖动

        lightCamera.transform.localPosition = new Vector3((minValue.x + maxValue.x) / 2, (minValue.y + maxValue.y) / 2, (minValue.z + maxValue.z) / 2);             //设置光源相机位置

        lightCamera.nearClipPlane = -farClipPlane * 2f;                                   //设置近平面
        lightCamera.farClipPlane = farClipPlane * 2f;                                     //设置远平面

        Vector3 scale;
        scale.x = 2f / CRSWJV.x;
        Matrix4x4 croppedMatrix = Matrix4x4.identity;
        croppedMatrix.m00 = scale.x;
        croppedMatrix.m11 = scale.x;
        croppedMatrix.m22 = 1;
        Matrix4x4 projectionMatrix = lightCamera.projectionMatrix;
        lightCamera.projectionMatrix = croppedMatrix * projectionMatrix;                    //设置光源相机投影矩阵
    }

    /// <summary>
    /// 计算包围球半径 --- 用作旋转相机阴影不抖动
    /// </summary>
    /// <param name="vertexs">顶点</param>
    /// <param name="vertex_Count">顶点个数</param>
    /// <returns></returns>
    public static float ComputeSphereRadius(List<Vector3> vertexs, int vertex_Count)
    {
        Vector3 total = Vector3.zero;
        Vector3 center = Vector3.zero;
        float[] distance = new float[vertex_Count];
        for (int i = 0; i < vertex_Count; i++)
        {
            total += vertexs[i];
        }
        center = total / vertex_Count;
        for (int j = 0; j < vertex_Count; j++)
        {
            distance[j] = Vector3.Distance(center, vertexs[j]);
        }
        return Sort(distance)[0];
    }

    /// <summary>
    /// 从大到小排序
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    public static float[] Sort(float[] f)
    {
        for (int i = 0; i < f.Length; i++)
        {
            for (int j = 0; j < f.Length - 1; j++)
            {
                if (f[j] < f[j + 1])
                {
                    float temp = f[j];
                    f[j] = f[j + 1];
                    f[j + 1] = temp;
                }
            }
        }
        return f;
    }

    /// <summary>
    /// 取整 --- 用于匹配像素单位大小倍数
    /// </summary>
    /// <param name="vector3"></param>
    /// <returns></returns>
    public static Vector3 ComputeFloor(Vector3 vector3)
    {
        Vector3 v;
        v.x = (float)Math.Floor(vector3.x);
        v.y = (float)Math.Floor(vector3.y);
        v.z = (float)Math.Floor(vector3.z);
        return v;
    }

    /// <summary>
    /// 更改所有子物体layer
    /// </summary>
    /// <param name="trans"></param>
    /// <param name="targetLayer"></param>
    public static void ChangeLayer(Transform trans, string targetLayer)
    {
        if (LayerMask.NameToLayer(targetLayer) == -1)
        {
            Debug.LogError("Layer中不存在" + targetLayer + "，请手动添加" + targetLayer);
            return;
        }
        //遍历更改所有子物体layer
        trans.gameObject.layer = LayerMask.NameToLayer(targetLayer);
        foreach (Transform child in trans)
        {
            ChangeLayer(child, targetLayer);
        }
    }
}

/// <summary>
/// PCF模糊
/// </summary>
public enum PCFState
{
    NO_PCF,
    PCF_2X2,
    PCF_4X4,
    PCF_8X8,
    PCF_16X16
}

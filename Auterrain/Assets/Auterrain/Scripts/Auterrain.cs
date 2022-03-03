using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class Auterrain: MonoBehaviour
{
    // Internal
    Terrain terrain;
    float[] terrainHeightList;
    float[,] terrainHeightMap;

    int mapSizeWithBorder;

    [Header("[-- Terrain Properties --]")]
    [Header("# Do Gaussian Blur: false, Apply gaussian smoothing to terrain(Recommended for 1024 or higher resolution)")]
    [Header("# Erosion Eterations: 250000, The eteration of hydraulic erosion")]
    [Header("# Terrain Size: 256, The size of terrain(= x Length = z Length)")]
    [Header(": plz set \"Terrain Resolution\" to a power of two")]
    [Header("# Terrain Resolution: 512(About 500,000 Polygon), The resolution of terrain(Higher resolution takes longer to compute)")]
    [Header("# Terrain Height: 100, The max height of terrain")]
    [Header("[ Recommendation ]")]
    [Header("Copyright ⓒ 2020. KangZingu All Rights Reserved.")]
    
    public int terrainHeight = 100;
    public int terrainResolution = 512;
    public int terrainSize = 256;
    Texture2D terrainImage;

    [Header("[-- Hydraulic Erosion Properties --]")]
    public ComputeShader erosionComputeShader;
    public int erosionIterations = 250000;
    private int erosionBrushRadius = 3;

    private int maxLifetime = 30;
    private float sedimentCapacityFactor = 3;
    private float minSedimentCapacity = 0.01f;
    private float depositSpeed = 0.3f;
    private float erodeSpeed = 0.3f;

    private float evaporateSpeed = 0.01f;
    private float gravity = 4;
    private float startSpeed = 1;
    private float startWater = 1;
    [Range(0, 1)]
    private float inertia = 0.3f;

    [Header("[-- Additional Properties --]")]
    public bool doGaussianBlur = false;

    void Start()
    {
        InitBeforeGenerate();
        InitBeforeErode();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            DoHydraulicErode();
        }
        else if (Input.GetKeyDown(KeyCode.G))
        {
            DoGenerateTerrainFromImage();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            DoRunPix2PixGenerator();
        }
    }
    public void DoRunPix2PixGenerator()
    {
        try
        {
            Process psi = new Process();
            psi.StartInfo.FileName = Application.dataPath + "/Auterrain/Env~/TensorFlow/python.exe";
            // Connect python env
            psi.StartInfo.Arguments = Application.dataPath + "/Auterrain/Model~/RunGenerator.py";
            // file to run
            psi.StartInfo.CreateNoWindow = true;
            psi.StartInfo.UseShellExecute = true;
            psi.Start();
            UnityEngine.Debug.Log("[Notice] Run .py file");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("[Notice] Error occured: " + e.Message);
        }
    }
    void InitBeforeGenerate()
    {
        terrain = GetComponent<Terrain>();
        terrainHeightMap = new float[terrainResolution, terrainResolution];
        terrainHeightMap = terrain.terrainData.GetHeights(0, 0, terrainResolution, terrainResolution);
    }
    void InitBeforeErode()
    {
        mapSizeWithBorder = terrainResolution + erosionBrushRadius * 2;
        terrainHeightList = new float[mapSizeWithBorder * mapSizeWithBorder];
    }
    void DoGaussianBlur()
    {
        float valResult = 0;
        float[,] copiedHeightMap = new float[terrainResolution, terrainResolution];
        for (int i = 1; i < terrainResolution - 1; i++)
        {
            for (int j = 1; j < terrainResolution - 1; j++)
            {
                valResult = 0;
                for (int k = -1; k <= 1; k++)
                {
                    for (int l = -1; l <= 1; l++)
                    {
                        int denominator = 4 * (int)Mathf.Pow(2, Mathf.Abs(k) + Mathf.Abs(l));
                        valResult += terrainHeightMap[i + k, j + l] / denominator;
                    }
                }
                copiedHeightMap[i, j] = valResult;
            }
        }
        for (int i = 1; i < terrainResolution - 1; i++)
        {
            for (int j = 1; j < terrainResolution - 1; j++)
            {
                terrainHeightMap[i, j] = copiedHeightMap[i, j];
            }
        }
    }
    void DoLoadOutputImage()
    {
        WWW www = new WWW(Application.dataPath+"/Auterrain/OutputHeightMapImage/HeightMapImage.jpg");
        terrainImage = www.texture;
    }
    public void DoGenerateTerrainFromImage()
    {
        //InitBeforeGenerate();
        DoLoadOutputImage();
        terrainHeightMap = new float[terrainResolution, terrainResolution];

        float pixelVal = 0;
        float readJustCoordVal = (float)terrainImage.width / (float)terrainResolution;
        for (int i = 0; i < terrainResolution; i++)
        {
            for (int j = 0; j < terrainResolution; j++)
            {
                pixelVal = (
                terrainImage.GetPixel((int)(j * readJustCoordVal), (int)(i * readJustCoordVal)).r +
                terrainImage.GetPixel((int)(j * readJustCoordVal), (int)(i * readJustCoordVal)).g +
                terrainImage.GetPixel((int)(j * readJustCoordVal), (int)(i * readJustCoordVal)).b)
                / 3;
                terrainHeightMap[i, j] = pixelVal;

            }
        }
        if (doGaussianBlur)
            DoGaussianBlur();

        terrain.terrainData.heightmapResolution = terrainResolution;
        terrain.terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);
        terrain.terrainData.SetHeights(0, 0, terrainHeightMap);

        DoHydraulicErode();
    }


    public void DoHydraulicErode()
    {
        InitBeforeErode();
        // Load height info
        for (int i = 0; i < terrainResolution; i++)
        {
            for (int j = 0; j < terrainResolution; j++)
            {
                terrainHeightList[(i + erosionBrushRadius) * mapSizeWithBorder + (j + erosionBrushRadius)] = terrainHeightMap[i, j];
            }
        }
        List<float> brushWeights = new List<float>();
        List<int> brushIndexOffsets = new List<int>();
        float weightSum = 0;

        for (int brushY = -erosionBrushRadius; brushY <= erosionBrushRadius; brushY++)
        {
            for (int brushX = -erosionBrushRadius; brushX <= erosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < erosionBrushRadius * erosionBrushRadius)
                {
                    brushIndexOffsets.Add(brushY * terrainResolution + brushX);
                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionBrushRadius;
                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }
        }
        for (int i = 0; i < brushWeights.Count; i++)
        {
            brushWeights[i] /= weightSum;
        }
        
        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
        brushWeightBuffer.SetData(brushWeights);
        brushIndexBuffer.SetData(brushIndexOffsets);
        erosionComputeShader.SetBuffer(0, "brushWeight", brushWeightBuffer);
        erosionComputeShader.SetBuffer(0, "brushIndice", brushIndexBuffer);
        
        int[] randomIndices = new int[erosionIterations];
        for (int i = 0; i < erosionIterations; i++)
        {
            int randomX = UnityEngine.Random.Range(erosionBrushRadius, terrainResolution + erosionBrushRadius);
            int randomY = UnityEngine.Random.Range(erosionBrushRadius, terrainResolution + erosionBrushRadius);
            randomIndices[i] = randomY * terrainResolution + randomX;
        }

        ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
        randomIndexBuffer.SetData(randomIndices);
        
        erosionComputeShader.SetBuffer(0, "randomIndice", randomIndexBuffer);

        ComputeBuffer mapBuffer = new ComputeBuffer(terrainHeightList.Length, sizeof(float));
        mapBuffer.SetData(terrainHeightList);
        
        erosionComputeShader.SetBuffer(0, "map", mapBuffer);

        erosionComputeShader.SetInt("borderSize", erosionBrushRadius);
        erosionComputeShader.SetInt("mapSize", mapSizeWithBorder);
        erosionComputeShader.SetInt("brushLength", brushIndexOffsets.Count);
        erosionComputeShader.SetInt("maxLifetime", maxLifetime);
        erosionComputeShader.SetFloat("inertia", inertia);
        erosionComputeShader.SetFloat("sedimentCapacityFactor", sedimentCapacityFactor);
        erosionComputeShader.SetFloat("minSedimentCapacity", minSedimentCapacity);
        erosionComputeShader.SetFloat("depositSpeed", depositSpeed);
        erosionComputeShader.SetFloat("erodeSpeed", erodeSpeed);
        erosionComputeShader.SetFloat("evapSpeed", evaporateSpeed);
        erosionComputeShader.SetFloat("gravity", gravity);
        erosionComputeShader.SetFloat("startSpeed", startSpeed);
        erosionComputeShader.SetFloat("startWater", startWater);

        int threadCount = erosionIterations / 1024;
        erosionComputeShader.Dispatch(0, threadCount, 1, 1);
        
        mapBuffer.GetData(terrainHeightList);

        DoUpdateErodedTerrain();

        mapBuffer.Release();
        randomIndexBuffer.Release();
        brushIndexBuffer.Release();
        brushWeightBuffer.Release();
    }
    void DoUpdateErodedTerrain()
    {
        for (int i = 0; i < terrainResolution; i++)
        {
            for (int j = 0; j < terrainResolution; j++)
            {
                terrainHeightMap[i, j] = terrainHeightList[(i + erosionBrushRadius) * mapSizeWithBorder + (j + erosionBrushRadius)];
            }
        }
        terrain.terrainData.SetHeights(0, 0, terrainHeightMap);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GPUSandPiles : MonoBehaviour
{
    public ComputeShader sandpileCompShad;
    [Header("UI")]
    public RawImage displayImage;
    private RenderTexture displayRt;

    [Header("Inputs")]
    public int numRows = 64;
    public int numCols = 64;

    [Range(1, 1000)]
    public int numTopplesPerFrame = 1;
    [SerializeField] private Color[] sandGradient = new Color[5];
    public Gradient displayGradient;

    //Lasting BUffers
    ComputeBuffer colorBuffer;
    //record keeping
    //NOTE: That the sand gird is 1D, even though its visualized as a 2D. This is because we need to upload it to the GPU and that needs a 1D Buffer
    int[] sandGrid;
    public int initNumGrains = 10000;

    bool shouldPile = false;
    // Start is called before the first frame update
    void Start()
    {
        displayRt = new RenderTexture(numCols, numRows, 24);
        displayRt.enableRandomWrite = true;
        displayRt.filterMode = FilterMode.Point;
        displayImage.texture = displayRt;

        sandGrid = new int[numRows * numCols];
        int midY = numRows / 2;
        int midX = numCols / 2;
        int linearMid = (midY * numRows) + midX;
        //Add a ton
        sandGrid[linearMid] = 1000000;
        int kernelIdx = sandpileCompShad.FindKernel("CSMain");
        sandpileCompShad.SetTexture(kernelIdx, "Result", displayRt);
        sandpileCompShad.SetInt("numRows", numRows);
        sandpileCompShad.SetInt("numCols", numCols);

        int numColors = 5;
        sandGradient = new Color[numColors];
        for (int i = 0; i < numColors; i++)
        {
            Color c = displayGradient.Evaluate((i / (float)numColors));
            sandGradient[i] = c;
        }

        int sizeOfColor = sizeof(float) * 4;
        ComputeBuffer colorBuffer = new ComputeBuffer(numColors, sizeOfColor);
        colorBuffer.SetData(sandGradient);
        sandpileCompShad.SetBuffer(kernelIdx, "colors", colorBuffer);

    }

    void OnDestroy()
    {
        if (colorBuffer != null)
            colorBuffer.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        //Upload contents to Comp Shader
        if (Input.GetKeyDown(KeyCode.P))
        {
            shouldPile = !shouldPile;
        }

        if (shouldPile)
        {
            updatePiles();
        }
    }

    private void updatePiles()
    {
        int kernelIdx = sandpileCompShad.FindKernel("CSMain");
        uint numThreadsX;
        uint numThreadsY;
        uint numThreadsZ;
        sandpileCompShad.GetKernelThreadGroupSizes(kernelIdx, out numThreadsX, out numThreadsY, out numThreadsZ);
        int numThreadGroupsX = Mathf.CeilToInt(numCols / numThreadsX);
        int numThreadGroupsY = Mathf.CeilToInt(numRows / numThreadsY);
        int numThreadGroupsZ = 1;
        ComputeBuffer sandBuffer = new ComputeBuffer(sandGrid.Length, 4); //Each int is 4 bytes
        ComputeBuffer modifiedBuffer = new ComputeBuffer(sandGrid.Length, 4); //Each int is 4 bytes
        int[] tempSandGrid = new int[sandGrid.Length];
        for (int pile = 0; pile < numTopplesPerFrame; pile++)
        {


            sandBuffer.SetData(sandGrid);
            //Upload the buffer to the GPU
            sandpileCompShad.SetBuffer(kernelIdx, "inputSandGrid", sandBuffer);



            modifiedBuffer.SetData(tempSandGrid);
            sandpileCompShad.SetBuffer(kernelIdx, "outputSandGrid", modifiedBuffer);


            sandpileCompShad.Dispatch(kernelIdx, numThreadGroupsX, numThreadGroupsY, numThreadGroupsZ);

            modifiedBuffer.GetData(sandGrid);
        }
        //This compute shader is pulling double data, computing the next value of each pixel after an iteration AND displays it
        //But we need to get the buffer data back...I think? What happens if we don't do that, only upload it once and keep hitting update?
        //TODO: Try that

        sandBuffer.Dispose();
        modifiedBuffer.Dispose();


    }
}

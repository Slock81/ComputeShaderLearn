using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CPUSandPileController : MonoBehaviour
{

    [Header("UI")]
    public RawImage groundPileImage;

    [Header("INPUTS")]
    public int numRows = 20;
    public int numCols = 20;

    [Range(1, 1000)]
    public int numTopplesPerFrame = 1;

    [Range(4, 12)]
    public int maxGrainCount = 4; //4 Numberhiles/Coding train, maybe it's fun to play with
    public Gradient grainGradient;
    //In the future make this based on the mouse position
    public int initNumGrains = 10000;

    public float updateTimeSec = 1 / 30f;

    public bool runSim = true;

    [Header("RENDERING")]
    //Stuff for the rendering
    private Texture2D displayRt;
    private int[,] sandGrid;

    // Start is called before the first frame update
    void Start()
    {

        sandGrid = new int[numRows, numCols];
        if(1<0)
        {
        int numRandomPiles = 32;
        for (int n = 0; n < numRandomPiles; n++)
        {
            int seedX = Random.Range(0, numCols);
            int seedY = Random.Range(0, numRows);
            sandGrid[seedY, seedX] = Random.Range(100, initNumGrains);
        }
        }
        sandGrid[numRows/2,numCols/4] = initNumGrains;
        sandGrid[numRows/2,(int)(numCols*.75f)] = initNumGrains;
        StartCoroutine(topplePiles());
    }


    /**
    */
    private IEnumerator topplePiles()
    {
        WaitForSeconds wait = new WaitForSeconds(updateTimeSec);
        while (runSim)
        {
            if (displayRt == null)
            {
                displayRt = new Texture2D(numCols, numRows);
                displayRt.filterMode = FilterMode.Point; //We want to see the grains, no interpolation
                groundPileImage.material.mainTexture = displayRt;
            }

            for (int t = 0; t < numTopplesPerFrame; t++)
            {
                //Originally intended this to be a compute shader and it could have worked, but with the amount of IF statements in it, it wouldn't really have made sense to run
                //on the GPU. If's for checking yours and neighborvalues and array limits, it was all Ifs. Nothing was worth the GPUs time, but I want to see it, so CPU
                List<Vector2> toppleLocs = new List<Vector2>();
                //To pre-test for GPU, we're going to take out the borders (0,and image size-1)
                for (int row = 1; row < numRows-1; row++)
                {
                    for (int col = 1; col < numCols-1; col++)
                    {
                        //We have to iterate to find topple locations, because if we toppled as we traveresed, then topples could cause more topples but we'd get into an uneven state.
                        int pileCount = sandGrid[row, col];
                        if (pileCount >= maxGrainCount)
                        {
                            toppleLocs.Add(new Vector2(col, row));
                        }
                    }
                }
                //Now we topple
                for (int k = 0; k < toppleLocs.Count; k++)
                {
                    Vector2 toppleLoc = toppleLocs[k];
                    int tx = (int)toppleLoc.x;
                    int ty = (int)toppleLoc.y;
                    sandGrid[ty, tx] -= 4;//4 comes off the topple
                    if (ty > 0)
                        sandGrid[ty - 1, tx]++;

                    if (ty < numRows - 2)
                        sandGrid[ty + 1, tx]++;
                    if (tx > 0)
                        sandGrid[ty, tx - 1]++;
                    if (tx < numCols - 2)
                        sandGrid[ty, tx + 1]++;
                    //If any of these are false, we just assume the grain fell off the table.
                }
            }

            //Ok, everything is in a settled step. Visualize it
            float maxPixValue = (float)maxGrainCount;
            for (int row = 1; row < numRows-1; row++)
            {
                for (int col = 1; col < numCols-1; col++)
                {
                    //We have to iterate to find topple locations, because if we toppled as we traveresed, then topples could cause more topples but we'd get into an uneven state.
                    int pileCount = sandGrid[row, col];
                    float percThru = Mathf.Min(1, (pileCount / maxPixValue));// Cap it at 1
                    Color pixelColor = grainGradient.Evaluate(percThru);
                    displayRt.SetPixel(col, row, pixelColor);
                }
            }
            displayRt.Apply();
            yield return wait;
        }

        yield return null;
    }

}

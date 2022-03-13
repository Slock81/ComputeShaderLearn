using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class InvertTest : MonoBehaviour
{

    [SerializeField] RawImage rawImage;
    [SerializeField] RenderTexture modRT;
    public ComputeShader meanCompShader;

    // Start is called before the first frame update
    void Start()
    {
        rawImage = GetComponent<RawImage>();
    }


    public void applyFilter()
    {


        if (modRT == null)
        {
            Texture2D currTexture = rawImage.mainTexture as Texture2D;
            int texWidth = currTexture.width;
            int texHeight = currTexture.height;
            modRT = new RenderTexture(texWidth, texHeight, 24);
            modRT.enableRandomWrite = true;
            modRT.Create();

            Graphics.Blit(currTexture, modRT);
            rawImage.texture = modRT;
        }
        //Upload the texture to the GPU to be modified
        meanCompShader.SetTexture(0, "Result", modRT);

        int width = modRT.width;
        int height = modRT.height;
        //We want to make sure we dispatch enough threads to cover our image. Get the number of thread groups from
        //The shader itself.
        //For some reason almost all tutorials just hard code this, not sure why.
        int kernelIndex = 0;
        meanCompShader.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);

        int numGroupsX = (int)Mathf.Ceil(width / (float)x);
        int numGroupsY = (int)Mathf.Ceil(height / (float)y);
        meanCompShader.Dispatch(kernelIndex, numGroupsX, numGroupsY, 1);

    }
}

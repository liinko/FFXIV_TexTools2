sampler2D inputImage : register(S0);
float4 channelMasks : register(C0);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 outCol = tex2D(inputImage, uv);

    if (!any(channelMasks.rgba - float4(1, 0, 0, 0)))
    {
        outCol.rgb = float3(outCol.r , outCol.r, outCol.r);
    }
    else if (!any(channelMasks.rgba - float4(0, 1, 0, 0)))
    {
        outCol.rgb = float3(outCol.g, outCol.g, outCol.g);
    }
    else if (!any(channelMasks.rgba - float4(0, 0, 1, 0)))
    {
        outCol.rgb = float3(outCol.b, outCol.b, outCol.b);
    }
    else if (!any(channelMasks.rgba - float4(0, 0, 0, 1)))
    {
        outCol.rgb = float3(outCol.a, outCol.a, outCol.a);
    }
    else
    {
        outCol *= channelMasks; 
    }

    outCol.a = 1;

    return outCol;
}
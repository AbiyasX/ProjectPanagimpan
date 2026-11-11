#ifndef ALLIN1VFXSRPBATCH_HDRPHELPER
#define ALLIN1VFXSRPBATCH_HDRPHELPER

float4 CustomComputeScreenPos(float4 clipPos, float sign)
{
	float4 res = clipPos * 0.5f;
	res.xy = float2(res.x, res.y * sign) + res.w;
	res.zw = clipPos.zw;
	
	return res;
}

SAMPLER(sampler_ColorPyramidTexture);
float3 CustomSampleSceneColor(float2 uv)
{
	float3 res = SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, sampler_ColorPyramidTexture, uv, 0);
	return res;
}

float3 GetPositionWS(float4 positionOS)
{
	return TransformObjectToWorld(positionOS.xyz);
}

#endif
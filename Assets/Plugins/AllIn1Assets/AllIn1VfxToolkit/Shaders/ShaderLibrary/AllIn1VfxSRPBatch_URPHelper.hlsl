#ifndef ALLIN1VFXSRPBATCH_URPHELPER
#define ALLIN1VFXSRPBATCH_URPHELPER

float4 CustomComputeScreenPos(float4 clipPos, float sign)
{
	float4 res = ComputeScreenPos(clipPos, sign);
	return res;
}

float3 CustomSampleSceneColor(float2 uv)
{
	float3 res = SampleSceneColor(uv);
	return res;
}

float3 GetNormalWS(float3 normalOS)
{
	float3 normalWS = TransformObjectToWorldNormal(normalOS);
	return normalWS;
}

float3 GetViewDirWS(float3 vertexWS)
{
	float3 res = GetWorldSpaceViewDir(vertexWS);
	return res;
}

float3 GetPositionVS(float3 positionOS)
{
	float3 res = TransformWorldToView(positionOS);
	return res;
}

float3 GetPositionWS(float4 positionOS)
{
	return TransformObjectToWorld(positionOS.xyz);
}

float3 GetPositionOS(float4 positionWS)
{
	return TransformWorldToObject(positionWS.xyz);

}

float3 GetDirWS(float4 dirOS)
{
	return TransformObjectToWorldDir(dirOS.xyz);
}

#endif
#ifndef ALLIN1VFXSRPBATCH_VERTEXPASS
#define ALLIN1VFXSRPBATCH_VERTEXPASS

v2f vert(appdata v)
{
	v2f o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	o.uvSeed = v.uv;
	o.color = v.color;

	
	float3 worldPosition = GetPositionWS(v.vertex);
#if VERTOFFSET_ON
	#if TIMEISCUSTOM_ON
		//time must be float: half loses precision after a few minutes of runtime and overflows to inf after ~18h on platforms with real fp16
		const float time = v.uv.z + globalCustomTime.y;
	#else
		const float time = v.uv.z + _Time.y;
	#endif
	
    half4 offsetUv = half4(TRANSFORM_TEX(v.uv.xy, _VertOffsetTex), 0, 0);
    offsetUv.x += (time * ACCESS_PROP_FLOAT(_VertOffsetTexXSpeed)) % 1;
    offsetUv.y += (time * ACCESS_PROP_FLOAT(_VertOffsetTexYSpeed)) % 1;
	
    half offsetAmount = pow(saturate(tex2Dlod(_VertOffsetTex, offsetUv).r), ACCESS_PROP_FLOAT(_VertOffsetPower));
    v.vertex.xyz += v.normal * ACCESS_PROP_FLOAT(_VertOffsetAmount) * offsetAmount;
#endif

	o.vertex = TransformObjectToHClip(v.vertex.xyz);

#if CAMDISTFADE_ON || SHAPE1SCREENUV_ON || SHAPE2SCREENUV_ON || SHAPE3SCREENUV_ON
	o.worldPos = float4(worldPosition.x, worldPosition.y, worldPosition.z, 1.0);
#endif
            	
#if OFFSETSTREAM_ON || SHAPEWEIGHTS_ON
	o.offsetCustomData = half3(0, 0, 0);
#endif

#if OFFSETSTREAM_ON
	o.offsetCustomData.x = v.customData1.x;
	o.offsetCustomData.y = v.customData1.y;
#endif

#if SHAPEWEIGHTS_ON
	o.offsetCustomData.z = v.customData1.z;
#endif

#if SHAPE1DISTORT_ON && !POLARUVDISTORT_ON
	o.uvSh1DistTex = TRANSFORM_TEX(v.uv.xy, _ShapeDistortTex);
#endif

#if SHAPE2_ON
#if SHAPE2DISTORT_ON && !POLARUVDISTORT_ON
	o.uvSh2DistTex = TRANSFORM_TEX(v.uv.xy, _Shape2DistortTex);
#endif
#endif

#if SHAPE3_ON
#if SHAPE3DISTORT_ON && !POLARUVDISTORT_ON
	o.uvSh3DistTex = TRANSFORM_TEX(v.uv.xy, _Shape3DistortTex);
#endif
#endif

#if SCREENDISTORTION_ON || SHAPE1SCREENUV_ON || SHAPE2SCREENUV_ON || SHAPE3SCREENUV_ON || SOFTPART_ON || DEPTHGLOW_ON || (SCREENDISTORTION_ON && DISTORTONLYBACK_ON)
	o.screenCoord = CustomComputeScreenPos(o.vertex, _ProjectionParams.x);
#endif

#if DISTORT_ON && !POLARUVDISTORT_ON
	o.uvDistTex = TRANSFORM_TEX(v.uv, _DistortTex);
#endif

#if FOG_ON
	o.fogFactor = ComputeFogFactor(o.vertex.z);
#endif

#if RIM_ON || BACKFACETINT_ON || LIGHTANDSHADOW_ON
	o.viewDir = normalize(GetWorldSpaceViewDir(worldPosition));
	o.normal = TransformObjectToWorldNormal(v.normal);
#endif

	return o;
}

#endif
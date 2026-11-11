#ifndef ALLIN1VFXSRPBATCH_STRUCTS
#define ALLIN1VFXSRPBATCH_STRUCTS

struct appdata
{
	float4 vertex : POSITION;
            	
	//z component is custom data from particle system and will be used as Timing Seed
	//w is potentially used for alpha custom stream input
	float4 uv : TEXCOORD0;

	#if OFFSETSTREAM_ON && !SHAPEWEIGHTS_ON
		half2 customData1 : TEXCOORD1; //x and y are the shapes uv offset
	#elif SHAPEWEIGHTS_ON
	half3 customData1 : TEXCOORD1; //z is the shapes weight offset
	#endif

	#if VERTOFFSET_ON || RIM_ON || BACKFACETINT_ON || LIGHTANDSHADOW_ON
		half3 normal : NORMAL;
	#endif
            	
	half4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
	//z component is custom data from particle system and will be used as Timing Seed
	//w is potentially used for alpha custom stream input
	float4 uvSeed : TEXCOORD0;
	float4 vertex : SV_POSITION;
	half4 color : COLOR;

	#if OFFSETSTREAM_ON && !SHAPEWEIGHTS_ON
	half2 offsetCustomData : TEXCOORD1;
	#elif SHAPEWEIGHTS_ON
	half3 offsetCustomData : TEXCOORD1;
	#endif
            	
	#if SHAPE1DISTORT_ON && !POLARUVDISTORT_ON
		half2 uvSh1DistTex : TEXCOORD2;
	#endif
	#if SHAPE2DISTORT_ON && !POLARUVDISTORT_ON
		half2 uvSh2DistTex : TEXCOORD3;
	#endif
	#if SHAPE3DISTORT_ON && !POLARUVDISTORT_ON
		half2 uvSh3DistTex : TEXCOORD4;
	#endif

	#if SCREENDISTORTION_ON || SHAPE1SCREENUV_ON || SHAPE2SCREENUV_ON || SHAPE3SCREENUV_ON || SOFTPART_ON || DEPTHGLOW_ON || (SCREENDISTORTION_ON && DISTORTONLYBACK_ON)
		float4 screenCoord : TEXCOORD5;
	#endif

	#if DISTORT_ON && !POLARUVDISTORT_ON
	half2 uvDistTex : TEXCOORD6;
	#endif

	#if CAMDISTFADE_ON || SHAPE1SCREENUV_ON || SHAPE2SCREENUV_ON || SHAPE3SCREENUV_ON
	half4 worldPos : TEXCOORD7;
	#endif

	#if RIM_ON || BACKFACETINT_ON || LIGHTANDSHADOW_ON
	half3 viewDir : TEXCOORD8;
	half3 normal : NORMAL;
	#endif

	#if FOG_ON
		half fogFactor : TEXCOORD9;
	#endif
	
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

#endif
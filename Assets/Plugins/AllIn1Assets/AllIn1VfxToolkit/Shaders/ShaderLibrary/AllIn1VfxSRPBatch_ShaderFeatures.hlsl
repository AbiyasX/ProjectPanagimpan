#ifndef ALLIN1VFXSRPBATCH_SHADERFEATURES
#define ALLIN1VFXSRPBATCH_SHADERFEATURES

#pragma shader_feature_local TIMEISCUSTOM_ON
#pragma shader_feature_local ADDITIVECONFIG_ON
#pragma shader_feature_local PREMULTIPLYALPHA_ON
#pragma shader_feature_local PREMULTIPLYCOLOR_ON
#pragma shader_feature_local SPLITRGBA_ON
#pragma shader_feature_local SHAPEADD_ON

#pragma shader_feature_local FOG_ON /////////////////Pipeline specific implementation
#pragma shader_feature_local SCREENDISTORTION_ON /////////////////Pipeline specific implementation
#pragma shader_feature_local DISTORTUSECOL_ON /////////////////Pipeline specific implementation, inside SCREENDISTORTION_ON
#pragma shader_feature_local DISTORTONLYBACK_ON /////////////////Pipeline specific implementation, inside SCREENDISTORTION_ON
#pragma shader_feature_local SHAPE1SCREENUV_ON /////////////////Pipeline specific implementation
#pragma shader_feature_local SHAPE2SCREENUV_ON /////////////////Pipeline specific implementation
#pragma shader_feature_local SHAPE3SCREENUV_ON /////////////////Pipeline specific implementation

#pragma shader_feature_local SHAPEDEBUG_ON
            
#pragma shader_feature_local SHAPE1CONTRAST_ON
#pragma shader_feature_local SHAPE1DISTORT_ON
#pragma shader_feature_local SHAPE1ROTATE_ON
#pragma shader_feature_local SHAPE1SHAPECOLOR_ON

#pragma shader_feature_local SHAPE2_ON
#pragma shader_feature_local SHAPE2CONTRAST_ON
#pragma shader_feature_local SHAPE2DISTORT_ON
#pragma shader_feature_local SHAPE2ROTATE_ON
#pragma shader_feature_local SHAPE2SHAPECOLOR_ON

#pragma shader_feature_local SHAPE3_ON
#pragma shader_feature_local SHAPE3CONTRAST_ON
#pragma shader_feature_local SHAPE3DISTORT_ON
#pragma shader_feature_local SHAPE3ROTATE_ON
#pragma shader_feature_local SHAPE3SHAPECOLOR_ON

#pragma shader_feature_local GLOW_ON
#pragma shader_feature_local GLOWTEX_ON
#pragma shader_feature_local SOFTPART_ON /////////////////Pipeline specific implementation
#pragma shader_feature_local DEPTHGLOW_ON /////////////////Pipeline specific implementation
#pragma shader_feature_local MASK_ON
#pragma shader_feature_local COLORRAMP_ON
#pragma shader_feature_local COLORRAMPGRAD_ON
#pragma shader_feature_local COLORGRADING_ON
#pragma shader_feature_local HSV_ON
#pragma shader_feature_local POSTERIZE_ON
#pragma shader_feature_local PIXELATE_ON
#pragma shader_feature_local DISTORT_ON
#pragma shader_feature_local SHAKEUV_ON
#pragma shader_feature_local WAVEUV_ON
#pragma shader_feature_local ROUNDWAVEUV_ON
#pragma shader_feature_local TWISTUV_ON
#pragma shader_feature_local DOODLE_ON
#pragma shader_feature_local OFFSETSTREAM_ON
#pragma shader_feature_local TEXTURESCROLL_ON
#pragma shader_feature_local VERTOFFSET_ON
#pragma shader_feature_local RIM_ON /////////////////Pipeline specific implementation
#pragma shader_feature_local BACKFACETINT_ON /////////////////Pipeline specific implementation
#pragma shader_feature_local POLARUV_ON
#pragma shader_feature_local POLARUVDISTORT_ON
#pragma shader_feature_local SHAPE1MASK_ON
#pragma shader_feature_local TRAILWIDTH_ON
#pragma shader_feature_local LIGHTANDSHADOW_ON
#pragma shader_feature_local SHAPETEXOFFSET_ON
#pragma shader_feature_local SHAPEWEIGHTS_ON
            
#pragma shader_feature_local ALPHACUTOFF_ON
#pragma shader_feature_local ALPHASMOOTHSTEP_ON
#pragma shader_feature_local FADE_ON
#pragma shader_feature_local FADEBURN_ON
#pragma shader_feature_local ALPHAFADE_ON
#pragma shader_feature_local ALPHAFADEUSESHAPE1_ON
#pragma shader_feature_local ALPHAFADEUSEREDCHANNEL_ON
#pragma shader_feature ALPHAFADETRANSPARENCYTOO_ON
#pragma shader_feature ALPHAFADEINPUTSTREAM_ON
#pragma shader_feature CAMDISTFADE_ON


//UNITY_DOTS_INSTANCING_ENABLED is not defined yet here, so AllIn1VfxSRPBatch_BuffersConfig.hlsl switches these to DOTS
#define BATCHING_BUFFER_START	CBUFFER_START(UnityPerMaterial)
#define BATCHING_BUFFER_END		CBUFFER_END

#define DECLARE_PROPERTY_FLOAT(name)	float name;
#define DECLARE_PROPERTY_FLOAT2(name)	float2 name;
#define DECLARE_PROPERTY_FLOAT3(name)	float3 name;
#define DECLARE_PROPERTY_FLOAT4(name)	float4 name;

#define ACCESS_PROP_FLOAT(name)				name
#define ACCESS_PROP_FLOAT2(name)			name
#define ACCESS_PROP_FLOAT3(name)			name
#define ACCESS_PROP_FLOAT4(name)			name
#define ACCESS_PROP_TILING_AND_OFFSET(name)	name

#if SOFTPART_ON || DEPTHGLOW_ON || (SCREENDISTORTION_ON && DISTORTONLYBACK_ON)
#define REQUIRE_DEPTH_TEXTURE 1
#endif

#endif
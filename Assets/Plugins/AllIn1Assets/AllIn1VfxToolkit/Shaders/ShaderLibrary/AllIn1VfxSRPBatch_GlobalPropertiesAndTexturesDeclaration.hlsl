#ifndef ALLIN1VFXSRPBATCH_GLOBALPROPERTIESANDTEXTURESDECLARATION
#define ALLIN1VFXSRPBATCH_GLOBALPROPERTIESANDTEXTURESDECLARATION

//Set by scripts through Shader.SetGlobalVector, so they must stay out of the material buffer and the Properties block
#if TIMEISCUSTOM_ON
	float4 globalCustomTime;
#endif

#if LIGHTANDSHADOW_ON
	float3 _All1VfxLightDir;
#endif

sampler2D _MainTex;
#if SHAPE1DISTORT_ON
	sampler2D _ShapeDistortTex;
#endif

#if SHAPE2_ON
	sampler2D _Shape2Tex;
	#if SHAPE2DISTORT_ON
		sampler2D _Shape2DistortTex;
	#endif
#endif

#if SHAPE3_ON
	sampler2D _Shape3Tex;
	#if SHAPE3DISTORT_ON
		sampler2D _Shape3DistortTex;
	#endif
#endif

#if GLOW_ON
	#if GLOWTEX_ON
		sampler2D _GlowTex;
	#endif
#endif
	
#if MASK_ON
	sampler2D _MaskTex;
#endif

#if COLORRAMP_ON
	sampler2D _ColorRampTex;
#endif

#if COLORRAMPGRAD_ON
	sampler2D _ColorRampTexGradient;
#endif
	
#if DISTORT_ON
	sampler2D _DistortTex;
#endif

#if SCREENDISTORTION_ON
	sampler2D _DistNormalMap, _GrabTexture;
#endif
	
#if VERTOFFSET_ON
	sampler2D _VertOffsetTex;
#endif

#if FADE_ON
	sampler2D _FadeTex;
	#if FADEBURN_ON
		sampler2D _FadeBurnTex;
	#endif
#endif
	
#if SHAPE1MASK_ON
	sampler2D _Shape1MaskTex;
#endif

#if TRAILWIDTH_ON
	sampler2D _TrailWidthGradient;
#endif

#endif //ALLIN1VFXSRPBATCH_GLOBALPROPERTIESANDTEXTURESDECLARATION
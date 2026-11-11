Shader "AllIn1Vfx/AllIn1VfxDOTS"
{
    Properties
    {
        _RenderingMode("Rendering Mode", float) = 0 // 0
        _SrcMode("SrcMode", float) = 5 // 1
        _DstMode("DstMode", float) = 10 // 2
        _CullingOption("Culling Option", float) = 0 // 3
        _ZWrite("Depth Write", float) = 0.0 // 4
        _ZTestMode("Z Test Mode", float) = 4 // 5
        _ColorMask("Color Write Mask", float) = 15 // 6

        _Alpha("Global Alpha", Range(0, 1)) = 1 //7
        _Color("Global Color", Color) = (1,1,1,1) //8
        
        _TimingSeed("Random Seed", Float) = 0.0 //9
        _EditorDrawers("Editor Drawers", Int) = 60 //10

        _MainTex("Shape1 Texture", 2D) = "white" {} //11
        [HDR] _ShapeColor("Shape1 Color", Color) = (1,1,1,1) //12
        _ShapeXSpeed("Shape1 X Speed", Float) = 0 //13
        _ShapeYSpeed("Shape1 Y Speed", Float) = 0 //14
        _ShapeContrast("Shape1 Contrast", Range (0, 10)) = 1 //15
        _ShapeBrightness("Shape1 Brightness", Range (-1, 1)) = 0 //16
        _ShapeDistortTex("Distortion Texture", 2D) = "black" {} //17
        _ShapeDistortAmount("Distortion Amount", Range(0, 10)) = 0.5 //18
        _ShapeDistortXSpeed("Scroll speed X", Float) = 0.1 //19
        _ShapeDistortYSpeed("Scroll speed Y", Float) = 0.1 //20
        _ShapeColorWeight("Shape1 RGB Weight", Range(0, 5)) = 1 //21
        _ShapeAlphaWeight("Shape1 A Weight", Range(0, 5)) = 1 //22

        _Shape2Tex ("Shape2 Texture", 2D) = "white" {} //23
        [HDR] _Shape2Color("Shape2 Color", Color) = (1,1,1,1)
        _Shape2XSpeed("Shape2 X Speed", Float) = 0
        _Shape2YSpeed("Shape2 Y Speed", Float) = 0
        _Shape2Contrast("Shape2 Contrast", Range (0, 10)) = 1
        _Shape2Brightness("Shape2 Brightness", Range (-1, 1)) = 0
        _Shape2DistortTex("Distortion Texture", 2D) = "black" {}
        _Shape2DistortAmount("Distortion Amount", Range(0,10)) = 0.5
        _Shape2DistortXSpeed("Scroll Speed X", Float) = 0.1
        _Shape2DistortYSpeed("Scroll Speed Y", Float) = 0.1
        _Shape2ColorWeight("Shape2 RGB Weight", Range(0, 5)) = 2
        _Shape2AlphaWeight("Shape2 A Weight", Range(0, 5)) = 2 //34

        _Shape3Tex("Shape3 Texture", 2D) = "white" {} //35
        [HDR] _Shape3Color("Shape3 Color", Color) = (1,1,1,1)
        _Shape3XSpeed("Shape3 X Speed", Float) = 0
        _Shape3YSpeed("Shape3 Y Speed", Float) = 0
        _Shape3Contrast("Shape3 Contrast", Range (0, 10)) = 1
        _Shape3Brightness("Shape3 Brightness", Range (-1, 1)) = 0
        _Shape3DistortTex("Distortion Texture", 2D) = "black" {}
        _Shape3DistortAmount("Distortion Amount", Range(0, 10)) = 0.5
        _Shape3DistortXSpeed("Scroll Speed X", Float) = 0.1
        _Shape3DistortYSpeed("Scroll Speed Y", Float) = 0.1
        _Shape3ColorWeight("Shape3 RGB Weight", Range(0, 5)) = 2
        _Shape3AlphaWeight("Shape3 A Weight", Range(0, 5)) = 2 //46

        _SoftFactor("Soft Particles Factor", Range(0.01, 3.0)) = 0.5 //47

        [NoScaleOffset] _ColorRampTex("Color Ramp Texture", 2D) = "white" {} //48
        _ColorRampLuminosity("Color Ramp luminosity", Range(-1, 1)) = 0 //49
        [AllIn1VfxGradient] _ColorRampTexGradient("Color Ramp Gradient", 2D) = "white" {} //50
        _ColorRampBlend ("Color Ramp Blend", Range(0, 1)) = 1 // 51

        _AlphaCutoffValue("Alpha cutoff value", Range(0, 1)) = 0.25 //52
        _AlphaStepMin("Smoothstep Min", Range(0, 1)) = 0.0 //53
        _AlphaStepMax("Smoothstep Max", Range(0, 1)) = 0.075 //54
        _AlphaFadeAmount("Fade Amount", Range(-0.1, 1)) = -0.1 //55
        _AlphaFadeSmooth("Fade Transition", Range(0.0, 1.5)) = 0.075 //56
        _AlphaFadePow("Fade Power", Range(0.001, 10)) = 1 //57
    	
    	_TrailWidthPower("Trail Width Power", Range(0.1, 5.0)) = 1.0 //58
    	[AllIn1VfxGradient] _TrailWidthGradient("Trail Width Gradient", 2D) = "white" {} //59

        _GlowColor("Glow Color", Color) = (1,1,1,1) //60
        _Glow("Glow Color Intensity", float) = 0 //61
        _GlowGlobal("Global Glow Intensity", float) = 1 //62
        _GlowTex("Glow Mask Texture", 2D) = "white" {} //63

        _DepthGlowDist("Depth Distance", Range(0.01, 10.0)) = 0.5 //64
        _DepthGlowPow("Depth Power", Range(0.01, 10.0)) = 1 //65
        _DepthGlowColor("Glow Color", Color) = (1,1,1,1) //66
        _DepthGlow("Glow Color Intensity", float) = 1 //67
        _DepthGlowGlobal("Global Glow Intensity", float) = 1 //68
        
        _MaskTex("Mask Texture", 2D) = "white" {} //69
        _MaskPow("Mask Power", Range(0.001, 10)) = 1 //70
        
        _HsvShift("Hue Shift", Range(0, 360)) = 180 //71
		_HsvSaturation("Saturation", Range(0, 2)) = 1 //72
		_HsvBright("Brightness", Range(0, 2)) = 1 //73
        
        _RandomSh1Mult("Shape 1 Mult", Range(0, 1)) = 1.0 //74
        _RandomSh2Mult("Shape 2 Mult", Range(0, 1)) = 1.0 //75
        _RandomSh3Mult("Shape 3 Mult", Range(0, 1)) = 1.0 //76
        
        _PixelateSize("Pixelate size", Range(4, 512)) = 32 //77
        
        _DistortTex("Distortion Texture", 2D) = "black" {} //78
		_DistortAmount("Distortion Amount", Range(0, 10)) = 0.5 //79
		_DistortTexXSpeed("Scroll Speed X", Range(-50, 50)) = 5 //80
		_DistortTexYSpeed("Scroll Speed Y", Range(-50, 50)) = 5 //81
        
        [HDR] _BackFaceTint("Backface Tint", Color) = (0.5, 0.5, 0.5, 1) //82
    	[HDR] _FrontFaceTint("Frontface Tint", Color) = (1, 1, 1, 1) //83
        
        _ShakeUvSpeed("Shake Speed", Range(0, 50)) = 20 //84
		_ShakeUvX("X Multiplier", Range(-15, 15)) = 5 //85
		_ShakeUvY("Y Multiplier", Range(-15, 15)) = 4 //86
        
        _WaveAmount("Wave Amount", Range(0, 25)) = 7 //87
		_WaveSpeed("Wave Speed", Range(0, 25)) = 10 //88
		_WaveStrength("Wave Strength", Range(0, 25)) = 7.5 //89
		_WaveX("Wave X Axis", Range(0, 1)) = 0 //90
		_WaveY("Wave Y Axis", Range(0, 1)) = 0.5 //91
        
        _RoundWaveStrength("Wave Strength", Range(0, 1)) = 0.7 //92
		_RoundWaveSpeed("Wave Speed", Range(0, 5)) = 2 //93
        
        _TwistUvAmount("Twist Amount", Range(0, 3.1416)) = 1 //94
		_TwistUvPosX("Twist Pos X Axis", Range(0, 1)) = 0.5 //95
		_TwistUvPosY("Twist Pos Y Axis", Range(0, 1)) = 0.5 //96
		_TwistUvRadius("Twist Radius", Range(0, 3)) = 0.75 //97
        
        _HandDrawnAmount("Hand Drawn Amount", Range(0, 40)) = 10 //98
		_HandDrawnSpeed("Hand Drawn Speed", Range(1, 30)) = 5 //99
    	
    	_OffsetSh1("Shape 1 Offset Mult", Range(-5, 5)) = 1 //100
    	_OffsetSh2("Shape 2 Offset Mult", Range(-5, 5)) = 1 //101
    	_OffsetSh3("Shape 3 Offset Mult", Range(-5, 5)) = 1 //102
    	
    	_DistNormalMap("Normal Map", 2D) = "bump" {} //103
		_DistortionPower("Distortion Power", Float) = 10 //104
		_DistortionBlend("Distortion Blend", Range(0, 1)) = 1 //105
    	_DistortionScrollXSpeed("Scroll speed X Axis", Float) = 0 //106
		_DistortionScrollYSpeed("Scroll speed Y Axis", Float) = 0 //107
    	
    	_TextureScrollXSpeed("Speed X Axis", Float) = 1 //108
		_TextureScrollYSpeed("Speed Y Axis", Float) = 0 //109
    	
    	_VertOffsetTex("Offset Noise Texture", 2D) = "white" {} //110
		_VertOffsetAmount("Offset Amount", Range(0, 2)) = 0.5 //111
		_VertOffsetPower("Offset Power", Range(0.01, 10)) = 1 //112
		_VertOffsetTexXSpeed("Scroll Speed X", Range(-2, 2)) = 0.1 //113
		_VertOffsetTexYSpeed("Scroll Speed Y", Range(-2, 2)) = 0.1 //114
    	
    	_FadeTex("Fade Texture", 2D) = "white" {} //115
		_FadeAmount("Fade Amount", Range(-0.1, 1)) = -0.1 //116
		_FadeTransition("Fade Transition", Range(0.01, 0.75)) = 0.075 //117
		_FadePower("Fade Power", Range(0.001, 10)) = 1 //118
    	_FadeScrollXSpeed("Speed X Axis", Float) = 0 //119
		_FadeScrollYSpeed("Speed Y Axis", Float) = 0 //120
		_FadeBurnTex("Fade Burn Texture", 2D) = "white" {} //121
		[HDR] _FadeBurnColor("Fade Burn Color", Color) = (1,1,0,1) //122
		_FadeBurnWidth("Fade Burn Width", Range(0, 0.2)) = 0.01 //123
		_FadeBurnGlow("Fade Burn Glow", Range(1, 250)) = 5//124
    	
    	[HDR] _ColorGradingLight("Light Color Tint", Color) = (1,1,1,1) //125
    	[HDR] _ColorGradingMiddle("Mid Tone Color Tint", Color) = (1,1,1,1) //126
    	[HDR] _ColorGradingDark("Dark/Shadow Color Tint", Color) = (1,1,1,1) //127
    	_ColorGradingMidPoint("Mid Point", Range(0.01, 0.99)) = 0.5 //128
    	
    	_CamDistFadeStepMin("Far Fade Start Point", Range(0, 1000)) = 0.0 //129
        _CamDistFadeStepMax("Far Fade End Point", Range(0, 1000)) = 100 //130
        _CamDistProximityFade("Close Fade Start Point", Range(0, 250)) = 0.0 //131
    	
    	_ScreenUvShDistScale("Scale With Dist Amount", Range(0, 1)) = 1 //132
    	_ScreenUvSh2DistScale("Scale With Dist Amount", Range(0, 1)) = 1 //133
    	_ScreenUvSh3DistScale("Scale With Dist Amount", Range(0, 1)) = 1 //134
    	
    	[HDR] _RimColor("Rim Color", Color) = (1, 1, 1, 1) //135
        _RimBias("Rim Bias", Range(0, 1)) = 0 //136
        _RimScale("Rim Scale", Range(0, 25)) = 1 //137
        _RimPower("Rim Power", Range(0.1, 20.0)) = 5.0 //138
        _RimIntensity("Rim Intensity", Range(0.0, 50.0)) = 1  //139
        _RimAddAmount("Add Amount (0 is mult)", Range(0.0, 1.0)) = 1  //140
        _RimErodesAlpha("Erode Transparency", Range(0.0, 2.0)) = 0  //141
    	
        _Shape1MaskTex("Shape 1 Mask Texture", 2D) = "white" {} //142
        _Shape1MaskPow("Shape 1 Mask Power", Range(0.001, 10)) = 1 //143
    	
        _LightAmount("Light Amount", Range(0, 1)) = 0//144
        [HDR] _LightColor("Light Color", Color) = (1,1,1,1) //145
    	_ShadowAmount("Shadow Amount", Range(0, 1)) = 0.4//146
    	_ShadowStepMin("Shadow Min", Range(0, 1)) = 0.0 //147
        _ShadowStepMax("Shadow Max", Range(0, 1)) = 1.0 //148
        
        _PosterizeNumColors("Number of Colors", Range(1, 30)) = 5 //149
    	
    	_ShapeRotationOffset("Rotation Offset", Range(0, 6.28318530718)) = 0 //150
    	_ShapeRotationSpeed("Rotation Speed", Float) = 0 //151
    	_Shape2RotationOffset("Rotation Offset", Range(0, 6.28318530718)) = 0 //152
    	_Shape2RotationSpeed("Rotation Speed", Float) = 0 //153
    	_Shape3RotationOffset("Rotation Offset", Range(0, 6.28318530718)) = 0 //154
    	_Shape3RotationSpeed("Rotation Speed", Float) = 0 //155
    	
    	_Sh1BlendOffset("Shape 1 Blend Offset", Range(-5, 5)) = 0 //156
		_Sh2BlendOffset("Shape 2 Blend Offset", Range(-5, 5)) = 0 //157
		_Sh3BlendOffset("Shape 3 Blend Offset", Range(-5, 5)) = 0 //158

		_DebugShape("Shape Debug Number", Int) = 1 //159 Needs to be last property
    }

	SubShader
	{
		PackageRequirements
		{
			"com.unity.render-pipelines.high-definition" : "12.1"
		}

		Blend [_SrcMode] [_DstMode]
        Cull [_CullingOption]
        ZWrite [_ZWrite]
        ZTest [_ZTestMode]
        ColorMask [_ColorMask]
        Lighting Off

		Pass
        {
            HLSLPROGRAM
			#pragma multi_compile _ DOTS_INSTANCING_ON
			#pragma target 4.5
			#pragma multi_compile_instancing
            #pragma multi_compile_fog


			#include_with_pragmas "ShaderLibrary/AllIn1VfxSRPBatch_ShaderFeatures.hlsl"


			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
			
			#include "ShaderLibrary/AllIn1VfxSRPBatch_HDRPHelper.hlsl"
			#include "ShaderLibrary/AllIn1VfxSRPBatch_CommonFunctions.hlsl" 

			#include "ShaderLibrary/AllIn1VfxSRPBatch_Structs.hlsl"
			#include "ShaderLibrary/AllIn1VfxSRPBatch_BuffersConfig.hlsl"

			#include "ShaderLibrary/AllIn1VfxSRPBatch_VertexPass.hlsl"
			#include "ShaderLibrary/AllIn1VfxSRPBatch_FragmentPass.hlsl"

			#pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }
	}

	SubShader
    {	
		PackageRequirements
		{
			"com.unity.render-pipelines.universal" : "12.0"
		}

        Tags
        {
            "RenderPipeline"="UniversalPipeline" "Queue" = "Transparent" "CanUseSpriteAtlas" = "True" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane"
        }

        Blend [_SrcMode] [_DstMode]
        Cull [_CullingOption]
        ZWrite [_ZWrite]
        ZTest [_ZTestMode]
        ColorMask [_ColorMask]
        Lighting Off

        Pass
        {
            HLSLPROGRAM
			#pragma multi_compile _ DOTS_INSTANCING_ON
			#pragma target 4.5
			#pragma multi_compile_instancing
            #pragma multi_compile_fog

			#include_with_pragmas "ShaderLibrary/AllIn1VfxSRPBatch_ShaderFeatures.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
			
			#include "ShaderLibrary/AllIn1VfxSRPBatch_URPHelper.hlsl"
			#include "ShaderLibrary/AllIn1VfxSRPBatch_CommonFunctions.hlsl" 

			#include "ShaderLibrary/AllIn1VfxSRPBatch_Structs.hlsl"
			#include "ShaderLibrary/AllIn1VfxSRPBatch_BuffersConfig.hlsl"

			#include "ShaderLibrary/AllIn1VfxSRPBatch_VertexPass.hlsl"
			#include "ShaderLibrary/AllIn1VfxSRPBatch_FragmentPass.hlsl"

			#pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }
    }

    CustomEditor "AllIn1Assets.AllIn1VfxToolkit.AllIn1VfxCustomMaterialEditor"
	Fallback "AllIn1Vfx/AllIn1Vfx"
}
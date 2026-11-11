#ifndef ALLIN1VFXSRPBATCH_FRAGMENTPASS
#define ALLIN1VFXSRPBATCH_FRAGMENTPASS

half4 frag(v2f i) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
	float seed = i.uvSeed.z + ACCESS_PROP_FLOAT(_TimingSeed);
#if TIMEISCUSTOM_ON
	const float4 shaderTime = globalCustomTime;
#else
	const float4 shaderTime = _Time;
#endif
	float time = shaderTime.y + seed;

#if SHAPE1SCREENUV_ON || SHAPE2SCREENUV_ON || SHAPE3SCREENUV_ON
	half2 originalUvs = i.uvSeed.xy;
#endif

#if PIXELATE_ON
	half aspectRatio = ACCESS_PROP_FLOAT4(_MainTex_TexelSize).x / ACCESS_PROP_FLOAT4(_MainTex_TexelSize).y;
	half2 pixelSize = float2(ACCESS_PROP_FLOAT(_PixelateSize), ACCESS_PROP_FLOAT(_PixelateSize) * aspectRatio);
	i.uvSeed.xy = floor(i.uvSeed.xy * pixelSize) / pixelSize;
#endif

#if TWISTUV_ON
	half2 tempUv = i.uvSeed.xy - half2(ACCESS_PROP_FLOAT(_TwistUvPosX) * ACCESS_PROP_FLOAT4(_MainTex_ST).x, ACCESS_PROP_FLOAT(_TwistUvPosY) * ACCESS_PROP_FLOAT4(_MainTex_ST).y);
	half twistUvRadius = ACCESS_PROP_FLOAT(_TwistUvRadius) * ((ACCESS_PROP_FLOAT4(_MainTex_ST).x + ACCESS_PROP_FLOAT4(_MainTex_ST).y) / 2);
	half percent = saturate((twistUvRadius - length(tempUv)) / SafeDivisor(twistUvRadius));
	half theta = percent * percent * (2.0 * sin(ACCESS_PROP_FLOAT(_TwistUvAmount))) * 8.0;
	half s = sin(theta);
	half c = cos(theta);
	half beta = max(sign(twistUvRadius - length(tempUv)), 0.0);
	tempUv = half2(dot(tempUv, half2(c, -s)), dot(tempUv, half2(s, c))) * beta +	tempUv * (1 - beta);
	tempUv += half2(ACCESS_PROP_FLOAT(_TwistUvPosX) * ACCESS_PROP_FLOAT4(_MainTex_ST).x, ACCESS_PROP_FLOAT(_TwistUvPosY) * ACCESS_PROP_FLOAT4(_MainTex_ST).y);
	i.uvSeed.xy = tempUv;
#endif

#if DOODLE_ON
	half2 uvCopy = i.uvSeed.xy;
	//fmod by tau/4 (the *4 below makes it a full 2*PI period) keeps the value bounded so it never overflows to inf in half precision, which turns the uv into nan
	float handDrawnTime = fmod(floor((shaderTime.x + seed) * 20 * ACCESS_PROP_FLOAT(_HandDrawnSpeed)), 1.57079632679);
	uvCopy.x = sin((uvCopy.x * ACCESS_PROP_FLOAT(_HandDrawnAmount) + handDrawnTime) * 4);
	uvCopy.y = cos((uvCopy.y * ACCESS_PROP_FLOAT(_HandDrawnAmount) + handDrawnTime) * 4);
	i.uvSeed.xy = lerp(i.uvSeed.xy, i.uvSeed.xy + uvCopy, 0.0005 * ACCESS_PROP_FLOAT(_HandDrawnAmount));
#endif

#if SHAKEUV_ON
	//fmod by 2*PI keeps the trig input bounded: unbounded time values eventually lose precision or overflow on some platforms
	float shakeTime = fmod((shaderTime.x + seed) * ACCESS_PROP_FLOAT(_ShakeUvSpeed) * 50, 6.28318530718);
	half xShake = sin(shakeTime) * ACCESS_PROP_FLOAT(_ShakeUvX);
	half yShake = cos(shakeTime) * ACCESS_PROP_FLOAT(_ShakeUvY);
	i.uvSeed.xy += half2(xShake * 0.012, yShake * 0.01);
#endif

#if WAVEUV_ON
	half2 uvWave = half2(ACCESS_PROP_FLOAT(_WaveX) * ACCESS_PROP_FLOAT4(_MainTex_ST).x, ACCESS_PROP_FLOAT(_WaveY) * ACCESS_PROP_FLOAT4(_MainTex_ST).y) - i.uvSeed.xy;
#if ATLAS_ON
	uvWave = half2(ACCESS_PROP_FLOAT(_WaveX), ACCESS_PROP_FLOAT(_WaveY)) - uvRect;
#endif
	uvWave.x *= _ScreenParams.x / _ScreenParams.y;
	//wrap by 2*PI instead of 360: keeps the trig input bounded and, being a whole sin period, avoids the visual pop 360 caused on every wrap
	half angWave = (sqrt(dot(uvWave, uvWave)) * ACCESS_PROP_FLOAT(_WaveAmount)) - ((time *  ACCESS_PROP_FLOAT(_WaveSpeed)) % 6.28318530718);
	i.uvSeed.xy = i.uvSeed.xy + normalize(uvWave) * sin(angWave) * (ACCESS_PROP_FLOAT(_WaveStrength) / 1000.0);
#endif

#if ROUNDWAVEUV_ON
	half xWave = ((0.5 * ACCESS_PROP_FLOAT4(_MainTex_ST).x) - i.uvSeed.x);
	half yWave = ((0.5 * ACCESS_PROP_FLOAT4(_MainTex_ST).y) - i.uvSeed.y) * (ACCESS_PROP_FLOAT4(_MainTex_TexelSize).w / ACCESS_PROP_FLOAT4(_MainTex_TexelSize).z);
	half ripple = -sqrt(xWave*xWave + yWave* yWave);
	//fmod by 2*PI keeps the trig input bounded: unbounded time values eventually lose precision or overflow on some platforms
	float roundWaveTime = fmod(time * (ACCESS_PROP_FLOAT(_RoundWaveSpeed) / 10.0) / 0.015, 6.28318530718);
    i.uvSeed.xy += (sin((ripple / 0.015) + roundWaveTime) * (ACCESS_PROP_FLOAT(_RoundWaveStrength)/10.0)) % 1;
#endif

#if POLARUV_ON
    half2 prePolarUvs = i.uvSeed.xy;
    i.uvSeed.xy = i.uvSeed.xy - half2(0.5, 0.5);
	i.uvSeed.xy = half2(atan2(i.uvSeed.y, i.uvSeed.x) / (1.0 * 6.28318530718), length(i.uvSeed.xy) * 2.0);
    i.uvSeed.xy *= ACCESS_PROP_FLOAT4(_MainTex_ST).xy;
#endif

#if DISTORT_ON
	#if POLARUVDISTORT_ON
		half2 distortUvs = TRANSFORM_TEX(i.uvSeed.xy, _DistortTex);
	#else
		half2 distortUvs = i.uvDistTex.xy;
	#endif
	distortUvs.x += ((shaderTime.x + seed) * ACCESS_PROP_FLOAT(_DistortTexXSpeed)) % 1;
	distortUvs.y += ((shaderTime.x + seed) * ACCESS_PROP_FLOAT(_DistortTexYSpeed)) % 1;
	#if ATLAS_ON
		i.uvDistTex = half2((i.uvDistTex.x - _MinXUV) / (_MaxXUV - _MinXUV), (i.uvDistTex.y - _MinYUV) / (_MaxYUV - _MinYUV));
	#endif
	half distortAmnt = (tex2D(_DistortTex, distortUvs).r - 0.5) * 0.2 * ACCESS_PROP_FLOAT(_DistortAmount);
	i.uvSeed.x += distortAmnt;
	i.uvSeed.y += distortAmnt;
#endif

#if TEXTURESCROLL_ON
	i.uvSeed.y += (time * ACCESS_PROP_FLOAT(_TextureScrollYSpeed)) % 1;
	i.uvSeed.x += (time * ACCESS_PROP_FLOAT(_TextureScrollXSpeed)) % 1;
#endif

#if TRAILWIDTH_ON
    half width = pow(tex2D(_TrailWidthGradient, i.uvSeed).r, ACCESS_PROP_FLOAT(_TrailWidthPower));
    i.uvSeed.y = (i.uvSeed.y * 2 - 1) / width * 0.5 + 0.5;
    clip(i.uvSeed.y);
    clip(1 - i.uvSeed.y);
#endif

	float2 shape1Uv = i.uvSeed.xy;
#if SHAPE2_ON
	float2 shape2Uv = shape1Uv;
#endif
#if SHAPE3_ON
	float2 shape3Uv = shape1Uv;
#endif

#if CAMDISTFADE_ON || SHAPE1SCREENUV_ON || SHAPE2SCREENUV_ON || SHAPE3SCREENUV_ON
	half camDistance = distance(i.worldPos.xyz, _WorldSpaceCameraPos.xyz);
#endif

#if SHAPE1SCREENUV_ON || SHAPE2SCREENUV_ON || SHAPE3SCREENUV_ON
    half2 uvOffsetPostFx = i.uvSeed.xy - originalUvs;
	i.uvSeed.xy = i.screenCoord.xy / i.screenCoord.w;
	i.uvSeed.x = i.uvSeed.x * (_ScreenParams.x / _ScreenParams.y);
	i.uvSeed.x -= 0.5;
    i.uvSeed.xy -= uvOffsetPostFx;
    originalUvs += uvOffsetPostFx;
    half distanceZoom = camDistance * 0.1;
    half2 scaleWithDistUvs = i.uvSeed.xy * distanceZoom + ((-distanceZoom * 0.5) + 0.5);
	#if SHAPE1SCREENUV_ON
		shape1Uv = lerp(i.uvSeed.xy, scaleWithDistUvs, ACCESS_PROP_FLOAT(_ScreenUvShDistScale));
	#else
		shape1Uv = originalUvs;
	#endif
	#if SHAPE2SCREENUV_ON && SHAPE2_ON
		shape2Uv = lerp(i.uvSeed.xy, scaleWithDistUvs, ACCESS_PROP_FLOAT(_ScreenUvSh2DistScale));
	#else
	#if SHAPE2_ON
		shape2Uv = originalUvs;
	#endif
#endif
#if SHAPE3SCREENUV_ON && SHAPE3_ON
             	shape3Uv = lerp(i.uvSeed.xy, scaleWithDistUvs, ACCESS_PROP_FLOAT(_ScreenUvSh3DistScale));
#else
#if SHAPE3_ON
             	shape3Uv = originalUvs;
#endif
#endif
#endif

	shape1Uv = TRANSFORM_TEX(shape1Uv, _MainTex);
#if OFFSETSTREAM_ON
			 	shape1Uv.x += i.offsetCustomData.x * ACCESS_PROP_FLOAT(_OffsetSh1);
                 shape1Uv.y += i.offsetCustomData.y * ACCESS_PROP_FLOAT(_OffsetSh1);
#endif
#if SHAPETEXOFFSET_ON
			 	shape1Uv += seed * ACCESS_PROP_FLOAT(_RandomSh1Mult);
#endif
#if SHAPE1DISTORT_ON
#if POLARUVDISTORT_ON
             	half2 sh1DistortUvs = TRANSFORM_TEX(i.uvSeed.xy, _ShapeDistortTex);
#else
             	half2 sh1DistortUvs = i.uvSh1DistTex.xy;
#endif
                 sh1DistortUvs.x += ((time + seed) * ACCESS_PROP_FLOAT(_ShapeDistortXSpeed)) % 1;
                 sh1DistortUvs.y += ((time + seed) * ACCESS_PROP_FLOAT(_ShapeDistortYSpeed)) % 1;
                 half distortAmount = (tex2D(_ShapeDistortTex, sh1DistortUvs).r - 0.5) * 0.2 * ACCESS_PROP_FLOAT(_ShapeDistortAmount);
                 shape1Uv.x += distortAmount;
                 shape1Uv.y += distortAmount;
#endif
#if SHAPE1ROTATE_ON
             	shape1Uv = RotateUvs(shape1Uv, _ShapeRotationOffset + ((ACCESS_PROP_FLOAT(_ShapeRotationSpeed) * time) % 6.28318530718), ACCESS_PROP_FLOAT4(_MainTex_ST));
#endif
	half4 shape1 = SampleTextureWithScroll(_MainTex, shape1Uv, ACCESS_PROP_FLOAT(_ShapeXSpeed), ACCESS_PROP_FLOAT(_ShapeYSpeed), time);
#if SHAPE1SHAPECOLOR_ON
                 shape1.a = shape1.r;
                 shape1.rgb = ACCESS_PROP_FLOAT4(_ShapeColor).rgb;
#else
	shape1 *= _ShapeColor;
#endif
#if SHAPE1CONTRAST_ON
#if SHAPE1SHAPECOLOR_ON
                 shape1.a = saturate((shape1.a - 0.5) * _ShapeContrast + 0.5 + _ShapeBrightness);
#else
                 shape1.rgb = max(0, (shape1.rgb - half3(0.5, 0.5, 0.5)) * ACCESS_PROP_FLOAT(_ShapeContrast) + half3(0.5, 0.5, 0.5) + ACCESS_PROP_FLOAT(_ShapeBrightness));
#endif
#endif

#if SHAPE2_ON
             	shape2Uv = TRANSFORM_TEX(shape2Uv, _Shape2Tex);
#if OFFSETSTREAM_ON
			 	shape2Uv.x += i.offsetCustomData.x * ACCESS_PROP_FLOAT(_OffsetSh2);
                 shape2Uv.y += i.offsetCustomData.y * ACCESS_PROP_FLOAT(_OffsetSh2);
#endif
#if SHAPETEXOFFSET_ON
			 	shape2Uv += seed * _RandomSh2Mult;
#endif
#if SHAPE2DISTORT_ON
#if POLARUVDISTORT_ON
             	half2 sh2DistortUvs = TRANSFORM_TEX(i.uvSeed.xy, _Shape2DistortTex);
#else
             	half2 sh2DistortUvs = i.uvSh2DistTex.xy;
#endif
                 sh2DistortUvs.x += ((time + seed) * ACCESS_PROP_FLOAT(_Shape2DistortXSpeed)) % 1;
                 sh2DistortUvs.y += ((time + seed) * ACCESS_PROP_FLOAT(_Shape2DistortYSpeed)) % 1;
                 half distortAmnt2 = (tex2D(_Shape2DistortTex, sh2DistortUvs).r - 0.5) * 0.2 * ACCESS_PROP_FLOAT(_Shape2DistortAmount);
                 shape2Uv.x += distortAmnt2;
                 shape2Uv.y += distortAmnt2;
#endif
#if SHAPE2ROTATE_ON
             	shape2Uv = RotateUvs(shape2Uv, ACCESS_PROP_FLOAT(_Shape2RotationOffset) + ((ACCESS_PROP_FLOAT(_Shape2RotationSpeed) * time) % 6.28318530718), ACCESS_PROP_FLOAT4(_Shape2Tex_ST));
#endif
                 half4 shape2 = SampleTextureWithScroll(_Shape2Tex, shape2Uv, ACCESS_PROP_FLOAT(_Shape2XSpeed), ACCESS_PROP_FLOAT(_Shape2YSpeed), time);
#if SHAPE2SHAPECOLOR_ON
                 shape2.a = shape2.r;
                 shape2.rgb = ACCESS_PROP_FLOAT4(_Shape2Color).rgb;
#else
                 shape2 *= ACCESS_PROP_FLOAT4(_Shape2Color);
#endif
#if SHAPE2CONTRAST_ON
#if SHAPE2SHAPECOLOR_ON
                 shape2.a = max(0, (shape2.a - 0.5) * ACCESS_PROP_FLOAT(_Shape2Contrast) + 0.5 + ACCESS_PROP_FLOAT(_Shape2Brightness));
#else
                 shape2.rgb = max(0, (shape2.rgb - half3(0.5, 0.5, 0.5)) * ACCESS_PROP_FLOAT(_Shape2Contrast) + half3(0.5, 0.5, 0.5) + ACCESS_PROP_FLOAT(_Shape2Brightness));
#endif
#endif
#endif

#if SHAPE3_ON
                 shape3Uv = TRANSFORM_TEX(shape3Uv, _Shape3Tex);
#if OFFSETSTREAM_ON
				shape3Uv.x += i.offsetCustomData.x * ACCESS_PROP_FLOAT(_OffsetSh3);
				shape3Uv.y += i.offsetCustomData.y * ACCESS_PROP_FLOAT(_OffsetSh3);
#endif
#if SHAPETEXOFFSET_ON
			 	shape3Uv += seed * ACCESS_PROP_FLOAT(_RandomSh3Mult);
#endif
#if SHAPE3DISTORT_ON
#if POLARUVDISTORT_ON
             	half2 sh3DistortUvs = TRANSFORM_TEX(i.uvSeed.xy, _Shape3DistortTex);
#else
             	half2 sh3DistortUvs = i.uvSh3DistTex.xy;
#endif
                 sh3DistortUvs.x += ((time + seed) * ACCESS_PROP_FLOAT(_Shape3DistortXSpeed)) % 1;
                 sh3DistortUvs.y += ((time + seed) * ACCESS_PROP_FLOAT(_Shape3DistortYSpeed)) % 1;
                 half distortAmnt3 = (tex2D(_Shape3DistortTex, sh3DistortUvs).r - 0.5) * 0.3 * ACCESS_PROP_FLOAT(_Shape3DistortAmount);
                 shape3Uv.x += distortAmnt3;
                 shape3Uv.y += distortAmnt3;
#endif
#if SHAPE3ROTATE_ON
             	shape3Uv = RotateUvs(shape3Uv, ACCESS_PROP_FLOAT(_Shape3RotationOffset) + ((ACCESS_PROP_FLOAT(_Shape3RotationSpeed) * time) % 6.28318530718), ACCESS_PROP_FLOAT4(_Shape3Tex_ST));
#endif
                 half4 shape3 = SampleTextureWithScroll(_Shape3Tex, shape3Uv, ACCESS_PROP_FLOAT(_Shape3XSpeed), ACCESS_PROP_FLOAT(_Shape3YSpeed), time);
#if SHAPE3SHAPECOLOR_ON
                 shape3.a = shape3.r;
                 shape3.rgb = ACCESS_PROP_FLOAT4(_Shape3Color).rgb;
#else
                 shape3 *= ACCESS_PROP_FLOAT4(_Shape3Color);
#endif
#if SHAPE3CONTRAST_ON
#if SHAPE3SHAPECOLOR_ON
                 shape3.a = max(0, (shape3.a - 0.5) * ACCESS_PROP_FLOAT(_Shape3Contrast) + 0.5 + ACCESS_PROP_FLOAT(_Shape3Brightness));
#else
                 shape3.rgb = max(0, (shape3.rgb - half3(0.5, 0.5, 0.5)) * ACCESS_PROP_FLOAT(_Shape3Contrast) + half3(0.5, 0.5, 0.5) + ACCESS_PROP_FLOAT(_Shape3Brightness));
#endif
#endif
#endif

                 //ALWAYS UNCOMMENT THE FOLLOWING CODE BLOCK-------------------------------------------
#if SHAPEDEBUG_ON
                 if (ACCESS_PROP_FLOAT(_DebugShape) < 1.5) return shape1;
#if SHAPE2_ON
                 else if (ACCESS_PROP_FLOAT(_DebugShape) < 2.5) return shape2;
#endif
#if SHAPE3_ON
                 else return shape3;
#endif
#endif

	half4 col = shape1;
	//Mix all shapes pre: change weights if custom vertex effect active
	float shapeColorWeight = ACCESS_PROP_FLOAT(_ShapeColorWeight);
	float shapeAlphaWeight = ACCESS_PROP_FLOAT(_ShapeAlphaWeight);
	float shape2ColorWeight = ACCESS_PROP_FLOAT(_Shape2ColorWeight);
	float shape2AlphaWeight = ACCESS_PROP_FLOAT(_Shape2AlphaWeight);
	float shape3ColorWeight = ACCESS_PROP_FLOAT(_Shape3ColorWeight);
	float shape3AlphaWeight = ACCESS_PROP_FLOAT(_Shape3AlphaWeight);
#if SHAPEWEIGHTS_ON
	half shapeWeightOffset;
	#if SHAPE2_ON

		shapeWeightOffset = i.offsetCustomData.z * ACCESS_PROP_FLOAT(_Sh1BlendOffset);
		shapeColorWeight = max(0, shapeColorWeight + shapeWeightOffset);
		shapeAlphaWeight = max(0, shapeAlphaWeight + shapeWeightOffset);
		shapeWeightOffset = i.offsetCustomData.z * _Sh2BlendOffset;
		shape2ColorWeight = max(0, shape2ColorWeight + shapeWeightOffset);
		shape2AlphaWeight = max(0, shape2AlphaWeight + shapeWeightOffset);
	#endif
	#if SHAPE3_ON
		shapeWeightOffset = i.offsetCustomData.z * _Sh3BlendOffset;
		shape3ColorWeight = max(0, shape3ColorWeight + shapeWeightOffset);
		shape3AlphaWeight = max(0, shape3AlphaWeight + shapeWeightOffset);
	#endif
#endif

//Mix all shapes
#if SHAPE2_ON
	#if !SPLITRGBA_ON
		shapeAlphaWeight = shapeColorWeight;
		shape2AlphaWeight = shape2ColorWeight;
	#endif
	#if SHAPE3_ON //Shape3 On
		#if !SPLITRGBA_ON
			shape3AlphaWeight = shape3ColorWeight;
		#endif
		#if SHAPEADD_ON
			col.rgb = ((shape1.rgb * shapeColorWeight) + (shape2.rgb * shape2ColorWeight)) + (shape3.rgb * shape3ColorWeight);
			col.a = saturate(max(shape3.a * shape3AlphaWeight, max(shape1.a * shapeAlphaWeight, shape2.a * shape2AlphaWeight)));
		#else
			col.rgb = ((shape1.rgb * shapeColorWeight) * (shape2.rgb * shape2ColorWeight)) * (shape3.rgb * shape3ColorWeight);
			col.a = saturate(((shape1.a * shapeAlphaWeight) * (shape2.a * shape2AlphaWeight)) * (shape3.a * shape3AlphaWeight));
		#endif
	#else //Shape3 Off
		#if SHAPEADD_ON
			col.rgb = (shape1.rgb * shapeColorWeight) + (shape2.rgb * shape2ColorWeight);
			col.a = saturate(max(shape1.a * shapeAlphaWeight, shape2.a * shape2AlphaWeight));
		#else
			col.rgb = (shape1.rgb * shapeColorWeight) * (shape2.rgb * shape2ColorWeight);
			col.a = saturate((shape1.a * shapeAlphaWeight) * (shape2.a * shape2AlphaWeight));
		#endif
	#endif
#endif

#if SHAPE1MASK_ON
	col = lerp(col, shape1, pow(tex2D(_Shape1MaskTex, TRANSFORM_TEX(i.uvSeed.xy, _Shape1MaskTex)).r, ACCESS_PROP_FLOAT(_Shape1MaskPow)));
#endif

#if PREMULTIPLYCOLOR_ON
	half luminance = 0;
	luminance = 0.3 * col.r + 0.59 * col.g + 0.11 * col.b;
	luminance *= col.a;
	col.a = min(luminance, col.a);
#endif

	col.rgb *= ACCESS_PROP_FLOAT4(_Color).rgb * i.color.rgb;
#if PREMULTIPLYALPHA_ON
	col.rgb *= col.a;
#endif
	//col.rgb = saturate(col.rgb); //Removed to allow for HDR shape color. Contrast and shape combinations can now overexpose the result

#if !PREMULTIPLYCOLOR_ON && (COLORRAMP_ON || ALPHAFADE_ON || COLORGRADING_ON || FADE_ON || (ADDITIVECONFIG_ON && (GLOW_ON || DEPTHGLOW_ON)))
	half luminance = 0;
	luminance = 0.3 * col.r + 0.59 * col.g + 0.11 * col.b;
	luminance *= col.a;
#endif

#if (FADE_ON || ALPHAFADE_ON) && ALPHAFADEINPUTSTREAM_ON
             	col.a *= i.color.a;
			 	i.color.a = i.uvSeed.w;
#endif

#if FADE_ON
             	half preFadeAlpha = col.a;
				float fadeAmount = ACCESS_PROP_FLOAT(_FadeAmount);
             	fadeAmount = saturate(fadeAmount + (1 - i.color.a));
             	float fadeTransition = max(0.01, ACCESS_PROP_FLOAT(_FadeTransition) * EaseOutQuint(saturate(fadeAmount)));
             	half2 fadeUv;
             	fadeUv = i.uvSeed.xy + seed;
             	fadeUv.x += (time * _FadeScrollXSpeed) % 1;
                 fadeUv.y += (time * _FadeScrollYSpeed) % 1;
             	half2 tiledUvFade1 = TRANSFORM_TEX(fadeUv, _FadeTex);
#if ADDITIVECONFIG_ON && !PREMULTIPLYCOLOR_ON
                 preFadeAlpha *= luminance;
#endif
             	fadeAmount = saturate(pow(fadeAmount, _FadePower));
#if FADEBURN_ON
			 	half2 tiledUvFade2 = TRANSFORM_TEX(fadeUv, _FadeBurnTex);
			 	half fadeSample = tex2D(_FadeTex, tiledUvFade1).r;
			 	half fadeNaturalEdge = saturate(smoothstep(0.0 , fadeTransition, RemapFloat(1.0 - fadeAmount, 0.0, 1.0, -1.0, 1.0) + fadeSample));
             	col.a *= fadeNaturalEdge;
             	half fadeBurn = saturate(smoothstep(0.0 , fadeTransition + ACCESS_PROP_FLOAT(_FadeBurnWidth), RemapFloat(1.0 - fadeAmount, 0.0, 1.0, -1.0, 1.0) + fadeSample));
             	fadeBurn = fadeNaturalEdge - fadeBurn;
			 	half3 fadeBurnColor = ACCESS_PROP_FLOAT4(_FadeBurnColor).rgb * ACCESS_PROP_FLOAT(_FadeBurnGlow);
			 	col.rgb += fadeBurn * tex2D(_FadeBurnTex, tiledUvFade2).rgb * fadeBurnColor * preFadeAlpha;
#else
			 	half fadeSample = tex2D(_FadeTex, tiledUvFade1).r;
			 	float fade = saturate(smoothstep(0.0 , fadeTransition, RemapFloat(1.0 - fadeAmount, 0.0, 1.0, -1.0, 1.0) + fadeSample));
			 	col.a *= fade;
#endif
#if ALPHAFADETRANSPARENCYTOO_ON
                 col.a *= 1 - fadeAmount;
#endif
#endif

#if ALPHAFADE_ON
				half alphaFadeLuminance;
				float alphaFadeAmount = ACCESS_PROP_FLOAT(_AlphaFadeAmount);
				float alphaFadeSmooth = ACCESS_PROP_FLOAT(_AlphaFadeSmooth);
             	alphaFadeAmount = saturate(alphaFadeAmount + (1 - i.color.a));
             	alphaFadeAmount = saturate(pow(alphaFadeAmount, _AlphaFadePow));
             	alphaFadeSmooth = max(0.01, alphaFadeSmooth * EaseOutQuint(saturate(alphaFadeAmount)));
#if ALPHAFADEUSESHAPE1_ON
                 alphaFadeLuminance = shape1.r;
#else
                 alphaFadeLuminance = luminance;
#endif
                 alphaFadeLuminance = saturate(alphaFadeLuminance - 0.001);
#if ALPHAFADEUSEREDCHANNEL_ON
                 col.a *= col.r;
#endif
                 col.a = saturate(col.a);
			 	float alphaFade = saturate(smoothstep(0.0 , alphaFadeSmooth, RemapFloat(1.0 - alphaFadeAmount, 0.0, 1.0, -1.0, 1.0) + alphaFadeLuminance));
			 	col.a *= alphaFade;
#if ALPHAFADETRANSPARENCYTOO_ON
                 col.a *= 1 - alphaFadeAmount;
#endif
#endif

#if BACKFACETINT_ON
             	col.rgb = lerp(col.rgb * ACCESS_PROP_FLOAT4(_BackFaceTint), col.rgb * ACCESS_PROP_FLOAT4(_FrontFaceTint), step(0, dot(i.normal, i.viewDir)));
#endif

#if LIGHTANDSHADOW_ON
	half NdL = saturate(dot(i.normal, -_All1VfxLightDir));
	col.rgb += ACCESS_PROP_FLOAT4(_LightColor).rgb * ACCESS_PROP_FLOAT(_LightAmount) * NdL;
	NdL = max(ACCESS_PROP_FLOAT(_ShadowAmount), NdL);
	NdL = SafeSmoothstep(ACCESS_PROP_FLOAT(_ShadowStepMin), ACCESS_PROP_FLOAT(_ShadowStepMax), NdL);
	col.rgb *= NdL;
#endif

#if COLORGRADING_ON
	col.rgb *= lerp(lerp(ACCESS_PROP_FLOAT3(_ColorGradingDark), ACCESS_PROP_FLOAT3(_ColorGradingMiddle), luminance/ACCESS_PROP_FLOAT(_ColorGradingMidPoint)),
	lerp(ACCESS_PROP_FLOAT3(_ColorGradingMiddle), ACCESS_PROP_FLOAT3(_ColorGradingLight), (luminance - ACCESS_PROP_FLOAT(_ColorGradingMidPoint))/(1.0 - ACCESS_PROP_FLOAT(_ColorGradingMidPoint))), step(ACCESS_PROP_FLOAT(_ColorGradingMidPoint), luminance));
#endif

#if COLORRAMP_ON
                 half colorRampLuminance = saturate(luminance + ACCESS_PROP_FLOAT(_ColorRampLuminosity));
#if COLORRAMPGRAD_ON
                 half4 colorRampRes = tex2D(_ColorRampTexGradient, half2(colorRampLuminance, 0));
#else
             	half4 colorRampRes = tex2D(_ColorRampTex, half2(colorRampLuminance, 0));
#endif
             	col.rgb = saturate(lerp(saturate(col.rgb), saturate(colorRampRes.rgb), ACCESS_PROP_FLOAT(_ColorRampBlend))); //Saturate to avoid SRP volume glow scene view fullscreen bug
                 col.a = saturate(lerp(col.a, saturate(col.a * colorRampRes.a), ACCESS_PROP_FLOAT(_ColorRampBlend)));
#endif

#if POSTERIZE_ON && !POSTERIZEOUTLINE_ON
             	col.rgb = floor(col.rgb / (1.0 / ACCESS_PROP_FLOAT(_PosterizeNumColors))) * (1.0 / ACCESS_PROP_FLOAT(_PosterizeNumColors));
#endif

#if SOFTPART_ON || DEPTHGLOW_ON
             	float4 depthScreenPos = i.screenCoord;
			 	float4 depthScreenPosNorm = depthScreenPos / depthScreenPos.w;
			 	//depthScreenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? depthScreenPosNorm.z : depthScreenPosNorm.z * 0.5 + 0.5;
			 	half sceneDepthDiff = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(depthScreenPosNorm.xy),_ZBufferParams) - ComputeGrabScreenPos(depthScreenPos).a;
#endif

#if SOFTPART_ON
                 half sceneZMult = saturate(ACCESS_PROP_FLOAT(_SoftFactor) * sceneDepthDiff);
                 col.a *= sceneZMult;
#endif

#if RIM_ON
			 	half NdV = 1 - abs(dot(i.normal, i.viewDir));
             	half rimFactor = saturate(ACCESS_PROP_FLOAT(_RimBias) + ACCESS_PROP_FLOAT(_RimScale) * pow(NdV, ACCESS_PROP_FLOAT(_RimPower)));
				half4 rimCol = ACCESS_PROP_FLOAT4(_RimColor) * rimFactor;
				rimCol.rgb *= ACCESS_PROP_FLOAT(_RimIntensity);
             	col.rgb = lerp(col.rgb * (rimCol.rgb + half3(1,1,1)), col.rgb + rimCol.rgb, ACCESS_PROP_FLOAT(_RimAddAmount));
             	col.a = saturate(col.a * (1 - rimFactor * ACCESS_PROP_FLOAT(_RimErodesAlpha)));
#endif

#if DEPTHGLOW_ON
				half depthGlowMask = saturate(ACCESS_PROP_FLOAT(_DepthGlowDist) * pow((1 - sceneDepthDiff), ACCESS_PROP_FLOAT(_DepthGlowPow)));
				col.rgb = lerp(col.rgb, ACCESS_PROP_FLOAT(_DepthGlowGlobal) * col.rgb, depthGlowMask);
             	half depthGlowMult = 1;
#if ADDITIVECONFIG_ON
             	depthGlowMult = luminance;
#endif
                 col.rgb += ACCESS_PROP_FLOAT4(_DepthGlowColor).rgb * ACCESS_PROP_FLOAT(_DepthGlow) * depthGlowMask * col.a * depthGlowMult;
#endif

#if GLOW_ON
                 half glowMask = 1;
#if GLOWTEX_ON
                 glowMask = tex2D(_GlowTex, TRANSFORM_TEX(i.uvSeed.xy, _GlowTex));
#endif
                 col.rgb *= ACCESS_PROP_FLOAT(_GlowGlobal) * glowMask;
			 	half glowMult = 1;
#if ADDITIVECONFIG_ON
             	glowMult = luminance;
#endif
                 col.rgb += ACCESS_PROP_FLOAT4(_GlowColor).rgb * ACCESS_PROP_FLOAT(_Glow) * glowMask * col.a * glowMult;
#endif

#if HSV_ON
			 	half3 resultHsv = half3(col.rgb);
			 	half cosHsv = ACCESS_PROP_FLOAT(_HsvBright) * _HsvSaturation * cos(ACCESS_PROP_FLOAT(_HsvShift) * 3.14159265 / 180);
			 	half sinHsv = ACCESS_PROP_FLOAT(_HsvBright) * _HsvSaturation * sin(ACCESS_PROP_FLOAT(_HsvShift) * 3.14159265 / 180);
			 	resultHsv.x = (.299 * ACCESS_PROP_FLOAT(_HsvBright) + .701 * cosHsv + .168 * sinHsv) * col.x
			 		+ (.587 * ACCESS_PROP_FLOAT(_HsvBright) - .587 * cosHsv + .330 * sinHsv) * col.y
			 		+ (.114 * ACCESS_PROP_FLOAT(_HsvBright) - .114 * cosHsv - .497 * sinHsv) * col.z;
			 	resultHsv.y = (.299 * ACCESS_PROP_FLOAT(_HsvBright) - .299 * cosHsv - .328 * sinHsv) *col.x
			 		+ (.587 * ACCESS_PROP_FLOAT(_HsvBright) + .413 * cosHsv + .035 * sinHsv) * col.y
			 		+ (.114 * ACCESS_PROP_FLOAT(_HsvBright) - .114 * cosHsv + .292 * sinHsv) * col.z;
			 	resultHsv.z = (.299 * ACCESS_PROP_FLOAT(_HsvBright) - .3 * cosHsv + 1.25 * sinHsv) * col.x
			 		+ (.587 * ACCESS_PROP_FLOAT(_HsvBright) - .588 * cosHsv - 1.05 * sinHsv) * col.y
			 		+ (.114 * ACCESS_PROP_FLOAT(_HsvBright) + .886 * cosHsv - .203 * sinHsv) * col.z;
			 	col.rgb = resultHsv;
#endif

#if CAMDISTFADE_ON
             	col.a *= 1 - saturate(SafeSmoothstep(ACCESS_PROP_FLOAT(_CamDistFadeStepMin), ACCESS_PROP_FLOAT(_CamDistFadeStepMax), camDistance));
             	col.a *= SafeSmoothstep(0.0, ACCESS_PROP_FLOAT(_CamDistProximityFade), camDistance);
#endif

#if MASK_ON
	half2 maskUv = i.uvSeed.xy;
#if POLARUV_ON
	maskUv = prePolarUvs;
#endif
	half4 maskSample = tex2D(_MaskTex, TRANSFORM_TEX(maskUv, _MaskTex));
	half mask = pow(min(maskSample.r, maskSample.a), ACCESS_PROP_FLOAT(_MaskPow));
	col.a *= mask;
#endif

#if ALPHASMOOTHSTEP_ON
                 col.a = SafeSmoothstep(ACCESS_PROP_FLOAT(_AlphaStepMin), ACCESS_PROP_FLOAT(_AlphaStepMax), col.a);
#endif
                
#if ALPHACUTOFF_ON
                 clip((1 - ACCESS_PROP_FLOAT(_AlphaCutoffValue)) - (1 - col.a) - 0.01);
#endif

#if FOG_ON
                 col.rgb = MixFog(col.rgb, i.fogFactor);
#endif

                 //Don't use a starting i.color.a lower than 1 unless using vertex stream dissolve when using a FADE effect
#if !FADE_ON && !ALPHAFADE_ON
	col.a *= ACCESS_PROP_FLOAT(_Alpha) * i.color.a;
#endif
#if FADE_ON || ALPHAFADE_ON
	col.a *= ACCESS_PROP_FLOAT(_Alpha);
#endif
#if ADDITIVECONFIG_ON
	col.rgb *= col.a;
#endif

#if SCREENDISTORTION_ON
	#if DISTORTUSECOL_ON
		half4 normalMap = col;
		normalMap.b = 1;
		normalMap *= col.r;
	#else
		half2 distortionUv = TRANSFORM_TEX(i.uvSeed.xy, _DistNormalMap);
		distortionUv.x += (time * ACCESS_PROP_FLOAT(_DistortionScrollXSpeed)) % 1;
		distortionUv.y += (time * ACCESS_PROP_FLOAT(_DistortionScrollYSpeed)) % 1;
		half4 normalMap = tex2D(_DistNormalMap, distortionUv);
	#endif
	#if DISTORTONLYBACK_ON
		half4 originalScreenCoord = i.screenCoord;
	#endif
		half3 usableNormals = UnpackNormal(normalMap);
		i.screenCoord.xy /= i.screenCoord.w;
		half2 distortUvOffset = usableNormals.rg * ACCESS_PROP_FLOAT(_DistortionPower) * i.color.a * i.screenCoord.z * normalMap.a;
		i.screenCoord.xy = distortUvOffset + i.screenCoord.xy;
		half3 distortCol = CustomSampleSceneColor(i.screenCoord.xy);
	#if DISTORTONLYBACK_ON
		float4 backDistortScreenPos = i.screenCoord;
		float4 backDistortScreenPosNorm = originalScreenCoord / originalScreenCoord.w;
		backDistortScreenPosNorm.xy += distortUvOffset;
		half frontMask = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(backDistortScreenPosNorm.xy),_ZBufferParams) - ComputeGrabScreenPos(backDistortScreenPos).a;
		frontMask = 1 - saturate(frontMask);
		col.rgb = lerp(lerp(col.rgb, saturate(distortCol.rgb), ACCESS_PROP_FLOAT(_DistortionBlend)), lerp(col.rgb, CustomSampleSceneColor((originalScreenCoord / originalScreenCoord.w).xy).rgb, ACCESS_PROP_FLOAT(_DistortionBlend)), frontMask);
	#else
		col.rgb = lerp(col.rgb, saturate(distortCol.rgb), ACCESS_PROP_FLOAT(_DistortionBlend));
	#endif
#endif
	
	return col;
}

#endif
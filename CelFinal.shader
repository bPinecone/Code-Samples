//Author: Brian Herman
//Last Updated: 6/10/2015
//Summary: ShaderLab Shader for CelShaded models with textured outline. Desired effect is a Toon shading with a sketched outline.

Shader "Custom/CelFinal" {
	Properties {
		_Color ("Diffuse Color", Color) = (1,1,1,1) 
	    _DiffuseLightRamp ("Lighting Ramp Strength", Range(-1.1,1)) = 0.1
	    _SpecColor ("Specular Color", Color) = (1,1,1,1) 
	    _Shininess ("Shininess", Range(0.5,1)) = 1	
	    _OutlineThickness ("Outline Thickness", Range(0,1)) = 0.1
	    _MainTex ("Main Texture", 2D) = "" {}
	    _InkColor ("Sketch Outline Color", Color) = (0,0,0,1)
	    _InkTex ("Sketch Outline Texture", 2D) = "" {}
	}
	SubShader 
	{
		Pass
		{
			Tags { "Queue"="Transparent" "RenderType"="TransparentCutout" "LightMode"="ForwardBase" }
			LOD 200
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
	      	uniform float4 _Color;
	      	uniform float _DiffuseLightRamp;
	      	uniform float4 _SpecColor;
	      	uniform float _Shininess;
	      	uniform float _OutlineThickness;
	      
	      	uniform float4 _LightColor0;
	      	uniform sampler2D _MainTex;	
	      	uniform sampler2D _InkTex;
	      	uniform float4 _InkColor;

			struct vertInput
			{
				float4 vertex : POSITION;
      			float3 normal : NORMAL;
      			float4 texcoord : TEXCOORD0;
			};
			
			struct vertOutput
			{
				float4 pos : SV_POSITION;
				float3 unlitColor : COLOR;
				float2 uv : TEXCOORD0; 
           		float3 normalDir : TEXCOORD1;
           		float4 lightDir : TEXCOORD2;
           		float3 viewDir : TEXCOORD3;
           		float2 clipSpace : TEXCOORD4;
      			
			};
			
			vertOutput vert(vertInput i)
			{
				vertOutput o;
				
				o.pos = mul( UNITY_MATRIX_MVP, i.vertex );  
				o.normalDir = normalize(mul(float4(i.normal, 0.0), _World2Object).xyz);
				
				float4 posWorld = mul(_Object2World, i.vertex);
				o.viewDir = normalize(_WorldSpaceCameraPos.xyz - posWorld.xyz); 
      		
      			float3 dirLightN = (_WorldSpaceLightPos0.xyz - posWorld.xyz);
      			o.lightDir = float4(normalize( lerp(_WorldSpaceLightPos0.xyz, dirLightN, _WorldSpaceLightPos0.w)),
      			lerp(1.0, 1.0/length(dirLightN),
      			 _WorldSpaceLightPos0.w));
      			o.unlitColor = float3(clamp(_Color.r - .3f,0, 1.0f), clamp(_Color.g - .3f,0,1.0f), clamp(_Color.b - .3f,0,1.0f));
      			o.uv = i.texcoord;
      			o.clipSpace =  mul(UNITY_MATRIX_MVP, i.vertex);
      		
      			return o;
			}
			
			float4 frag(vertOutput i) : COLOR
			{
				float NdotL = saturate(dot(i.normalDir, i.lightDir.xyz)); 
				
				float diffuseRamp = saturate((max(_DiffuseLightRamp, NdotL) - _DiffuseLightRamp)*1000);
				float specularRamp = saturate((max(_Shininess, dot(reflect(-i.lightDir.xyz, i.normalDir), i.viewDir))-_Shininess )*1000);
				float outlineRamp = saturate((dot(i.normalDir, i.viewDir ) - _OutlineThickness)*1000);
				
				float3 ambientL = (1 - diffuseRamp) * i.unlitColor.rgb;
				float3 diffuseL = (1 - specularRamp) * _Color.rgb * diffuseRamp;
				float3 specularL = _SpecColor.xyz * specularRamp;
		
				float3 final = (ambientL + diffuseL + specularL);// * outlineRamp;
				
				
				
				final = (final + tex2D(_MainTex, i.uv))*outlineRamp;
				if(outlineRamp == 0.0f)
				{
					final += (tex2D(_InkTex, i.clipSpace) + _InkColor);
				}
					return float4(final, 1.0f);
			}
			
			ENDCG
		}
	} 
	FallBack "Diffuse"
}

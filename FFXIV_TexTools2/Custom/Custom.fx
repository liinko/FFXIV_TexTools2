// FFXIV TexTools
// Copyright © 2017 Rafael Gonzalez - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

struct VSInputCustom
{
	float4 p			: POSITION;
	float4 c			: COLOR;
	float2 t			: TEXCOORD;
	float3 n			: NORMAL;
	float3 t1			: TANGENT;
	float3 t2			: BINORMAL;
	float4 customParams : COLOR1;
	float4 mr0			: TEXCOORD1;
	float4 mr1			: TEXCOORD2;
	float4 mr2			: TEXCOORD3;
	float4 mr3			: TEXCOORD4;
};

//--------------------------------------------------------------------------------------
struct PSInputCustom
{
	float4 p			: SV_POSITION;
	float4 wp			: POSITION0;
	float4 ws			: POSITIONT;
	float4 sp			: TEXCOORD1;
	float3 n			: NORMAL;	    // normal
	float2 t			: TEXCOORD0;	// tex coord	
	float3 t1			: TANGENT;		// tangent
	float3 t2			: BINORMAL;		// bi-tangent	
	float4 c			: COLOR;		// solid color
};

//------------------------------------------------------------------------------------
// Custom Pixel Shader
//------------------------------------------------------------------------------------
float4 PShaderCustom(PSInputCustom input) : SV_Target
{
	//convert a PSInputCustom to a PSInput
	PSInput standardInput = (PSInput)0;

standardInput.p = input.p;
standardInput.wp = input.wp;
standardInput.sp = input.sp;
standardInput.n = input.n;
standardInput.t = input.t;
standardInput.t1 = input.t1;
standardInput.t2 = input.t2;
standardInput.c = input.c;

//calculate lighting vectors - renormalize vectors	
input.n = calcNormal(standardInput);

// get per pixel vector to eye-position
float3 eye = normalize(vEyePos - input.wp.xyz);

// get shadow color
float s = 1;
if (bHasShadowMap)
{
	s = shadowStrength(input.sp);
}

float4 vMaterialTexture = 0.0f;
float4 vSpecularColor = 0.0f;
float4 shine = 1.0f;
float alpha = 1.0f;
float4 vEmissiveColor = 0.0f;

// add diffuse sampling
if (bHasDiffuseMap)
{
	//SamplerState is defined in Common.fx.
	vMaterialTexture = texDiffuseMap.Sample(LinearSampler, input.t);
}

if (bHasAlphaMap)
{
	float4 color = texAlphaMap.Sample(LinearSampler, input.t);
	alpha = color.r;
	clip(alpha <= 0.3 ? -1 : 1);
}

if (bHasSpecularMap)
{
	shine = sMaterialShininess;
	vSpecularColor = texSpecularMap.Sample(LinearSampler, input.t);
}

if (bHasEmissiveMap)
{
	vEmissiveColor = texEmissiveMap.Sample(LinearSampler, input.t);
}


// if(bHasMaskMap)
// {

// }

// if(bHasNormalMap)
// {

// }

// if(bHasColorTable)
// {

// }

// light emissive and ambient intensity
// this variable can be used for light accumulation
//float4 I = vMaterialEmissive + vMaterialAmbient * vLightAmbient;
float4 I = vEmissiveColor + vMaterialAmbient * vLightAmbient;

// loop over lights
for (int i = 0; i < LIGHTS; i++)
{
	// This framework calculates lighting in world space.
	// For every light type, you should calculate the input values to the
	// calcPhongLighting function, namely light direction and the reflection vector.
	// For computuation of attenuation and the spot light factor, use the
	// model from the DirectX documentation:
	// http://msdn.microsoft.com/en-us/library/windows/desktop/bb172279(v=vs.85).aspx

	if (iLightType[i] == 1) // directional
	{
		float3 d = normalize((float3)vLightDir[i]);
		float3 r = reflect(-d, input.n);
		I += s * calcPhongLighting(vLightColor[i], vMaterialTexture, vSpecularColor, shine, input.n, d, eye, r);
	}
	else if (iLightType[i] == 2)  // point
	{
		float3 d = (float3)(vLightPos[i] - input.wp);	 // light dir	
		float dl = length(d);
		d = normalize(d);
		float3 r = reflect(-d, input.n);
		float att = 1.0f / (vLightAtt[i].x + vLightAtt[i].y * dl + vLightAtt[i].z * dl * dl);
		I += att * calcPhongLighting(vLightColor[i], vMaterialTexture, input.n, d, eye, r);
	}
	else if (iLightType[i] == 3)  // spot
	{
		float3 d = (float3)(vLightPos[i] - input.wp);	 // light dir
		float dl = length(d);
		d = normalize(d);
		float3 r = reflect(-d, input.n);
		float3 sd = normalize((float3)vLightDir[i]);	// missuse the vLightDir variable for spot-dir

														/* --- this is the OpenGL 1.2 version (not so nice) --- */
														//float spot = (dot(-d, sd));
														//if(spot > cos(vLightSpot[i].x))
														//	spot = pow( spot, vLightSpot[i].y );
														//else
														//	spot = 0.0f;	
														/* --- */

														/* --- this is the  DirectX9 version (better) --- */
		float rho = dot(-d, sd);
		float spot = pow(saturate((rho - vLightSpot[i].x) / (vLightSpot[i].y - vLightSpot[i].x)), vLightSpot[i].z);
		float att = spot / (vLightAtt[i].x + vLightAtt[i].y * dl + vLightAtt[i].z * dl * dl);
		I += att * calcPhongLighting(vLightColor[i], vMaterialTexture, input.n, d, eye, r);
	}
	else
	{
		//I += 0;
	}
}

// set diffuse alpha
I.a = vMaterialDiffuse.a;

// multiply by vertex colors
//I = I * input.c;

return I;
}


//------------------------------------------------------------------------------------
// Default Vertex Shader
//------------------------------------------------------------------------------------
PSInputCustom VShaderCustom(VSInput input)
{
	PSInputCustom output;
	float4 inputp = input.p;

	// compose instance matrix
	if (bHasInstances)
	{
		matrix mInstance =
		{
			input.mr0.x, input.mr1.x, input.mr2.x, input.mr3.x, // row 1
			input.mr0.y, input.mr1.y, input.mr2.y, input.mr3.y, // row 2
			input.mr0.z, input.mr1.z, input.mr2.z, input.mr3.z, // row 3
			input.mr0.w, input.mr1.w, input.mr2.w, input.mr3.w, // row 4
		};
		inputp = mul(mInstance, input.p);
	}

	//set position into camera clip space	
	output.p = mul(inputp, mWorld);
	output.wp = output.p;
	output.p = mul(output.p, mView);
	output.p = mul(output.p, mProjection);

	//set position into light-clip space
	if (bHasShadowMap)
	{
		//for (int i = 0; i < 1; i++)
		{
			output.sp = mul(inputp, mWorld);
			output.sp = mul(output.sp, mLightView[0]);
			output.sp = mul(output.sp, mLightProj[0]);
		}
	}

	//set texture coords and color
	output.t = input.t;
	output.c = input.c;

	//set normal for interpolation	
	output.n = normalize(mul(input.n, (float3x3)mWorld));


	if (bHasNormalMap)
	{
		// transform the tangents by the world matrix and normalize
		output.t1 = normalize(mul(input.t1, (float3x3)mWorld));
		output.t2 = normalize(mul(input.t2, (float3x3)mWorld));
	}
	else
	{
		output.t1 = 0.0f;
		output.t2 = 0.0f;
	}

	return output;
}

technique11 RenderCustom
{
	pass P0
	{
		//SetRasterizerState	( RSSolid );
		SetDepthStencilState(DSSDepthLess, 0);
		SetBlendState(BSBlending, float4(0.0f, 0.0f, 0.0f, 0.0f), 0xFFFFFFFF);
		SetVertexShader(CompileShader(vs_4_0, VShaderCustom()));
		SetHullShader(NULL);
		SetDomainShader(NULL);
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PShaderCustom()));
	}
	pass P1
	{
		SetRasterizerState(RSWire);
		SetDepthStencilState(DSSDepthLess, 0);
		SetBlendState(BSBlending, float4(0.0f, 0.0f, 0.0f, 0.0f), 0xFFFFFFFF);
		SetVertexShader(CompileShader(vs_4_0, VShaderCustom()));
		SetHullShader(NULL);
		SetDomainShader(NULL);
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, PShaderCustom()));
	}
}
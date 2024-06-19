Shader "Custom/Shell"
{
  Properties
  {
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" }
    LOD 100

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      struct appdata
      {
        fixed4 vertex : POSITION;
        fixed3 normal : NORMAL;
      };

      struct v2f
      {
        fixed4 vertex : SV_POSITION;
        fixed3 normal : TEXCOORD1;
        fixed4 color : COLOR;
      };

      fixed DeRez(fixed value)
      {
        return fixed(int(value * 10)) / 10;
      }

      fixed flash;

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.normal = UnityObjectToWorldNormal(v.normal);
        o.normal.x = DeRez(o.normal.x);
        o.normal.y = DeRez(o.normal.y);
        o.normal.z = DeRez(o.normal.z);
        fixed c = saturate(dot(fixed3(0, 0, -1), o.normal));
        o.color = fixed4(1, 1, 1, 1) * c * c;
        o.color = lerp(o.color, fixed4(1, 1, 1, 1), flash);
        return o;
      }

      fixed4 frag (v2f i) : SV_Target
      {
        // sample the texture
        // i.color *= tex2D(_MainTex, i.uv);
        
        return i.color;
      }
      ENDCG
    }
  }
}

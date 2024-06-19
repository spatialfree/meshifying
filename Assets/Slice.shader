Shader "Custom/Slice"
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
        return fixed(int(value * 100)) / 100;
      }

      fixed3 hsv2rgb(fixed3 c)
      {
        c = fixed3(c.x, clamp(c.yz, 0.0, 1.0));
        fixed4 K = fixed4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
        fixed3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
        return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
      }

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        // o.vertex.x = DeRez(o.vertex.x);
        // o.vertex.y = DeRez(o.vertex.y);
        
        o.normal = UnityObjectToWorldNormal(v.normal);
        o.color = fixed4(hsv2rgb(fixed3(saturate(dot(fixed3(0,0,-1), o.normal)), 1, 1)), 1);
        return o;
      }

      fixed4 frag (v2f i) : SV_Target
      {
        return i.color;
      }
      ENDCG
    }
  }
}

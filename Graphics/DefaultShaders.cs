using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine
{
    public static class DefaultShaders
    {
        public static string DefaultSpriteVS = @"
            #version 450

            layout(set = 0, binding = 0) uniform mProjectionViewBuffer {
                mat4x4 mProjection;
                mat4x4 mView;
            };

            layout (location = 0) in vec2 vPosition;
            layout (location = 1) in vec2 vTexCoords;
            layout (location = 2) in vec4 vColor;

            layout (location = 0) out vec2 fTexCoords;
            layout (location = 1) out vec4 fColor;

            void main()
            {
                fTexCoords = vTexCoords;
                fColor = vColor;
                gl_Position = mProjection * mView * vec4(vPosition.x, vPosition.y, 0.0, 1.0);
            }
        ";

        public static string DefaultSpriteFS = @"
            #version 450

            layout (set = 1, binding = 0) uniform texture2D fTexture;
            layout (set = 1, binding = 1) uniform sampler fTextureSampler;

            layout (location = 0) in vec2 fTexCoords;
            layout (location = 1) in vec4 fColor;

            layout (location = 0) out vec4 fFragColor;

            void main()
            {
                fFragColor = texture(sampler2D(fTexture, fTextureSampler), fTexCoords) * fColor;
            }
        ";

        public static string DefaultTileVS = @"
            #version 450

            precision mediump float;

            layout (location = 0) in vec2 vPosition;
            layout (location = 1) in vec2 vTexture;

            layout(set = 0, binding = 0) uniform TransformBuffer {
                vec2 inverseTileTextureSize;
                vec2 inverseSpriteTextureSize;
                vec2 tileSize;
                vec2 viewOffset;
                vec2 viewportSize;
                vec2 inverseTileSize;
            };

            layout (location = 0) out vec2 fPixelCoord;
            layout (location = 1) out vec2 fTexCoord;
            layout (location = 2) out vec2 fTileSize;
            layout (location = 3) out vec2 fInverseSpriteTextureSize;

            void main()
            {
               fPixelCoord = (vTexture * viewportSize) + viewOffset;
               fTexCoord = fPixelCoord * inverseTileTextureSize * inverseTileSize;
               fTileSize = tileSize;
               fInverseSpriteTextureSize = inverseSpriteTextureSize;
               gl_Position = vec4(vPosition, 0.0, 1.0);
            }
        ";

        public static string DefaultTileFS = @"
            #version 450

            precision mediump float;

            layout (location = 0) in vec2 fPixelCoord;
            layout (location = 1) in vec2 fTexCoord;
            layout (location = 2) in vec2 fTileSize;
            layout (location = 3) in vec2 fInverseSpriteTextureSize;

            layout (set = 1, binding = 0) uniform texture2D fAtlasImage;
            layout (set = 1, binding = 1) uniform sampler fAtlasImageSampler;

            layout (set = 2, binding = 0) uniform texture2D fDataImage;
            layout (set = 2, binding = 1) uniform sampler fDataImageSampler;

            layout (location = 0) out vec4 fFragColor;

            void main()
            {
               if(fTexCoord.x < 0 || fTexCoord.y < 0 || fTexCoord.x > 1 || fTexCoord.y > 1) { discard; }

               vec4 tile = texture(sampler2D(fDataImage, fDataImageSampler), fTexCoord);
               if(tile.x == 1.0 && tile.y == 1.0) { discard; }
               
               vec2 testfInverseSpriteTextureSize = vec2(0.000600961561, 0.00026709403);

               vec2 spriteOffset = floor(tile.xy * 256.0) * fTileSize;
               vec2 spriteCoord = mod(fPixelCoord, fTileSize);

               fFragColor = texture(sampler2D(fAtlasImage, fAtlasImageSampler), (spriteOffset + spriteCoord) * fInverseSpriteTextureSize);
            }
        ";

    } // DefaultShaders
}

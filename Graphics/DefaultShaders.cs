using System;
using System.Collections.Generic;
using System.Text;

namespace ElementEngine
{
    public static class DefaultShaders
    {
        public static string DefaultSpriteVS = @"
            #version 450

            layout(set = 0, binding = 0) uniform ProjectionViewBuffer {
                mat4x4 uProjection;
                mat4x4 uView;
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
                gl_Position = uProjection * uView * vec4(vPosition.x, vPosition.y, 0.0, 1.0);
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
                vec2 uInverseTileTextureSize;
                vec2 uInverseSpriteTextureSize;
                vec2 uTileSize;
                vec2 uViewOffset;
                vec2 uViewportSize;
                vec2 uInverseTileSize;
            };

            layout (location = 0) out vec2 fPixelCoord;
            layout (location = 1) out vec2 fTexCoord;

            void main()
            {
                fPixelCoord = (vTexture * uViewportSize) + uViewOffset;
                fTexCoord = fPixelCoord * uInverseTileTextureSize * uInverseTileSize;
                gl_Position = vec4(vPosition, 0.0, 1.0);
            }
        ";

        public static string DefaultTileFS = @"
            #version 450

            precision mediump float;

            layout (location = 0) in vec2 fPixelCoord;
            layout (location = 1) in vec2 fTexCoord;

            layout(set = 0, binding = 0) uniform TransformBuffer {
                vec2 uInverseTileTextureSize;
                vec2 uInverseSpriteTextureSize;
                vec2 uTileSize;
                vec2 uViewOffset;
                vec2 uViewportSize;
                vec2 uInverseTileSize;
            };

            layout(set = 1, binding = 0) uniform AnimationBuffer {
                vec4[{ANIM_COUNT}] uAnimationOffsets;
            };

            layout (set = 2, binding = 0) uniform texture2D fAtlasImage;
            layout (set = 2, binding = 1) uniform sampler fAtlasImageSampler;

            layout (set = 3, binding = 0) uniform texture2D fDataImage;
            layout (set = 3, binding = 1) uniform sampler fDataImageSampler;

            layout (location = 0) out vec4 fFragColor;

            void main()
            {
                bool wrapX = {WRAP_X};
                bool wrapY = {WRAP_Y};

                if (!wrapX && (fTexCoord.x < 0 || fTexCoord.x > 1)) { discard; }
                if (!wrapY && (fTexCoord.y < 0 || fTexCoord.y > 1)) { discard; }

                vec4 tile = texture(sampler2D(fDataImage, fDataImageSampler), fTexCoord);
                if(tile.x == 1.0 && tile.y == 1.0) { discard; }

                int animIndex = int(tile.z * 256.0);

                vec2 spriteOffset = (floor(tile.xy * 256.0) * uTileSize) + uAnimationOffsets[animIndex].xy;
                vec2 spriteCoord = mod(fPixelCoord, uTileSize);

                fFragColor = texture(sampler2D(fAtlasImage, fAtlasImageSampler), (spriteOffset + spriteCoord) * uInverseSpriteTextureSize);
            }
        ";

        public static string DefaultPrimitiveVS = @"
            #version 450

            layout(set = 0, binding = 0) uniform ProjectionViewBuffer2 {
                mat4x4 uProjection;
                mat4x4 uView;
            };

            layout (location = 0) in vec2 vPosition;
            layout (location = 1) in vec2 vPadding;
            layout (location = 2) in vec4 vColor;

            layout (location = 1) out vec4 fColor;

            void main()
            {
                fColor = vColor;
                gl_Position = uProjection * uView * vec4(vPosition.x, vPosition.y, 0.0, 1.0);
            }
        ";

        public static string DefaultPrimitiveFS = @"
            #version 450

            layout (location = 1) in vec4 fColor;

            layout (location = 0) out vec4 fFragColor;

            void main()
            {
                fFragColor = fColor;
            }
        ";

    } // DefaultShaders
}

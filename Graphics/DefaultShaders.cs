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
    }
}

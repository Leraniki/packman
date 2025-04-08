#version 460 core

out vec4 FragColor;

in vec2 texCoord;
in vec3 fragPosModel; 

uniform sampler2D texture0;
uniform float mouthAngle; 

const float PI = 3.14159265359;
const float MOUTH_TALLNESS = 0.8; 

void main()
{
    
    float horizontalAngle = atan(fragPosModel.y, fragPosModel.x);

    float y_normalized = clamp(fragPosModel.y / 0.5, -1.0, 1.0); 
    float verticalAngle = asin(y_normalized);


    bool isHorizontallyInMouth = abs(horizontalAngle) < mouthAngle / 2.0;

    
    float maxVerticalForTriangle = MOUTH_TALLNESS * max(0.0, mouthAngle / 2.0 - abs(horizontalAngle));

    bool isVerticallyInMouth = abs(verticalAngle) < maxVerticalForTriangle;

    bool isInMouth = isHorizontallyInMouth && isVerticallyInMouth;

    if (isInMouth && mouthAngle > 0.001) { 
        discard;
    }

    FragColor = texture(texture0, texCoord);
}
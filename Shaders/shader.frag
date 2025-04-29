    #version 460 core

    out vec4 FragColor;

    in vec2 texCoord;
    in vec3 fragPosModel;

    uniform sampler2D texture0;
    uniform sampler2D texture1;
    uniform bool applyNoise; 
    uniform float mouthAngle;

    const float PI = 3.14159265359;
    const float MOUTH_TALLNESS = 0.8;


    void main()
    {

        vec4 baseColor = texture(texture0, texCoord);
        vec4 noise = texture(texture1, texCoord);

        float horizontalAngle = atan(fragPosModel.y, fragPosModel.x);

        float y_normalized = clamp(fragPosModel.y / 0.5, -1.0, 1.0);
        float verticalAngle = asin(y_normalized);

        bool isHorizontallyInMouth = abs(horizontalAngle) < mouthAngle / 2.0;
            
        float maxVerticalAngle = (mouthAngle / 2.0) * MOUTH_TALLNESS * max(0.0, 1.0 - abs(horizontalAngle) / (mouthAngle / 2.0 + 0.0001));

        bool isVerticallyInMouth = abs(verticalAngle) < maxVerticalAngle;

        bool isInMouth = isHorizontallyInMouth && isVerticallyInMouth;

        if (isInMouth && mouthAngle > 0.001) {
            discard;
        }

        vec4 finalColor = baseColor; 


        if (applyNoise)
        {
            
            vec4 noise = texture(texture1, texCoord);
            finalColor = baseColor * noise; 
        }
        

        FragColor = finalColor;     

  }
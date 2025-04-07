using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;



namespace lab2_ver2
{
    public class Shader
    {

        int shaderHandle;

        public int GetShader() { 
            return shaderHandle;
        }
        public static string LoadShaderSource(string filepath)
        {
            string shaderSource = "";
            try
            {
                using (StreamReader reader = new
                StreamReader("../../../Shaders/" + filepath))
                {
                    shaderSource = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load shader source file:" + e.Message);
            }
            return shaderSource;
        }

        public void UseShader()
        {
            GL.UseProgram(shaderHandle);
        }
        public void DeleteShader()
        {
            GL.DeleteProgram(shaderHandle);
        }
        public void LoadShader()
        {
            shaderHandle = GL.CreateProgram();
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, LoadShaderSource("shader.vert"));
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success1);
            if (success1 == 0)
            {
                string infoLog = GL.GetShaderInfoLog(vertexShader);
                Console.WriteLine(infoLog);
            }

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, LoadShaderSource("shader.frag"));
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int success2);
            if (success2 == 0)
            {
                string infoLog = GL.GetShaderInfoLog(fragmentShader);
                Console.WriteLine(infoLog);
            }

            GL.AttachShader(shaderHandle, vertexShader);
            GL.AttachShader(shaderHandle, fragmentShader);
            GL.LinkProgram(shaderHandle);


            GL.DetachShader(shaderHandle, vertexShader);
            GL.DetachShader(shaderHandle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        
    }
    internal class Game : GameWindow
    {
        int width, height;

        //float[] vertices = {
        //    -0.5f, 0.5f, 0f, // top left vertex - 0
        //    0.5f, 0.5f, 0f, // top right vertex - 1
        //    0.5f, -0.5f, 0f, // bottom right vertex - 2
        //    -0.5f, -0.5f, 0f // bottom left vertex - 3
        //};

        uint[] indices =
        {
            // Передняя грань
            0, 1, 2, //top triangle
            2, 3, 0, //bottom triangle

            // Правая грань
            4, 5, 6, 
            6, 7, 4,

            // Задняя грань
            8, 9, 10,
            10, 11, 8,

            // Левая грань
            12, 13, 14,
            14, 15, 12,

            // Верхняя грань
            16, 17, 18, 
            18, 19, 16,

            // Нижняя грань
            20, 21, 22, 
            22, 23, 20

        };

        //float[] texCoords =
        //{
        //    0f, 1f, 
        //    1f, 1f,
        //    1f, 0f,
        //    0f, 0f
        //};
        List<Vector2> texCoords = new List<Vector2>()
        {
            //передняя грань
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            //правая гарнь
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            //левая грань
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),

            //верхняя грань
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            //нижняя грань
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

        };


        int VAO;
        int VBO;
        Shader shader;
        int EBO;

        int textureID;
        int textureVBO;

        float yRot = 0f;
        float rotationSpeed = MathHelper.DegreesToRadians(45.0f);

        Camera camera;

        List<Vector3> vertices = new List<Vector3>()
        {
            //front face 0-3
            new Vector3(-0.5f, 0.5f, 0.5f), //top-left vertice
            new Vector3( 0.5f, 0.5f, 0.5f), //top-right vertice
            new Vector3( 0.5f, -0.5f, 0.5f), //bottom-right vertice
            new Vector3(-0.5f, -0.5f, 0.5f), //bottom-left vertice

            // 4-7: Правая грань (X = 0.5)
            new Vector3( 0.5f,  0.5f,  0.5f), // Верхняя-левая (была 1) (4)
            new Vector3( 0.5f,  0.5f, -0.5f), // Верхняя-правая    (5)
            new Vector3( 0.5f, -0.5f, -0.5f), // Нижняя-правая     (6)
            new Vector3( 0.5f, -0.5f,  0.5f), // Нижняя-левая (была 2)  (7)

            // 8-11: Задняя грань (Z = -0.5)
            new Vector3( 0.5f,  0.5f, -0.5f), // Верхняя-левая (была 5) (8)
            new Vector3(-0.5f,  0.5f, -0.5f), // Верхняя-правая    (9)
            new Vector3(-0.5f, -0.5f, -0.5f), // Нижняя-правая     (10)
            new Vector3( 0.5f, -0.5f, -0.5f), // Нижняя-левая (была 6) (11)

            // 12-15: Левая грань (X = -0.5)
            new Vector3(-0.5f,  0.5f, -0.5f), // Верхняя-левая (была 9) (12)
            new Vector3(-0.5f,  0.5f,  0.5f), // Верхняя-правая (была 0) (13)
            new Vector3(-0.5f, -0.5f,  0.5f), // Нижняя-правая (была 3) (14)
            new Vector3(-0.5f, -0.5f, -0.5f), // Нижняя-левая (была 10) (15)

            // 16-19: Верхняя грань (Y = 0.5)
            new Vector3(-0.5f,  0.5f, -0.5f), // Верхняя-левая (была 9/12) (16)
            new Vector3( 0.5f,  0.5f, -0.5f), // Верхняя-правая (была 5/8) (17)
            new Vector3( 0.5f,  0.5f,  0.5f), // Нижняя-правая (была 1/4) (18)
            new Vector3(-0.5f,  0.5f,  0.5f), // Нижняя-левая (была 0/13) (19)

            // 20-23: Нижняя грань (Y = -0.5)
            new Vector3(-0.5f, -0.5f,  0.5f), // Верхняя-левая (была 3/14) (20)
            new Vector3( 0.5f, -0.5f,  0.5f), // Верхняя-правая (была 2/7) (21)
            new Vector3( 0.5f, -0.5f, -0.5f), // Нижняя-правая (была 6/11) (22)
            new Vector3(-0.5f, -0.5f, -0.5f)  // Нижняя-левая (была 10/15)(23)
        };


        public Game(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.CenterWindow(new Vector2i(width, height));
            this.height = height;
            this.width = width;

        }

        
        protected override void OnLoad()
        {
            base.OnLoad();

            textureID = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            StbImage.stbi_set_flip_vertically_on_load(1);
            ImageResult boxTexture = ImageResult.FromStream(File.OpenRead("../../../Textures/photo.jpg"),
                ColorComponents.RedGreenBlueAlpha);
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                PixelInternalFormat.Rgba, boxTexture.Width, boxTexture.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture.Data);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vector3.SizeInBytes,
                vertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            //GL.EnableVertexArrayAttrib(VAO, 0);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            //GL.BindVertexArray(0);


            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length *sizeof(uint),
                indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);


            textureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Count * Vector2.SizeInBytes, texCoords.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexArrayAttrib(VAO, 1);

            GL.EnableVertexArrayAttrib(VAO, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);


            shader = new Shader();
            shader.LoadShader();

            GL.Enable(EnableCap.DepthTest);

            camera = new Camera(width, height, Vector3.Zero);
            //CursorState = CursorState.Grabbed;


        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteBuffer(VAO);
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(EBO);
            GL.DeleteTexture(textureID);

            shader.DeleteShader();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.ClearColor(0.8f, 0.7f, 0.9f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.UseShader();
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.BindVertexArray(VAO);

            //Transformation
           
            Matrix4 model = Matrix4.CreateRotationY(yRot);
            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjection();
            Matrix4 translation = Matrix4.CreateTranslation(0f, 0f, -2f);
            model *= translation;

            int modelLocation = GL.GetUniformLocation(shader.GetShader(), "model");
            int viewLocation = GL.GetUniformLocation(shader.GetShader(), "view");
            int projectionLocation = GL.GetUniformLocation(shader.GetShader(), "projection");

            GL.UniformMatrix4(modelLocation, true, ref model);
            GL.UniformMatrix4(viewLocation, true, ref view);
            GL.UniformMatrix4(projectionLocation, true, ref projection);
           

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            Context.SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            yRot += rotationSpeed * (float)args.Time;
            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            MouseState mouse = MouseState;
            KeyboardState input = KeyboardState;
            base.OnUpdateFrame(args);
            camera.Update(input, mouse, args);

        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            this.width = e.Width;
            this.height = e.Height;
        }


    }

    
}
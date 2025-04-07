using System;
using System.Collections.Generic;
using System.IO; // Добавлено для StreamReader и File
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
                
                string fullPath = Path.Combine(AppContext.BaseDirectory, "../../../Shaders", filepath);
                using (StreamReader reader = new StreamReader(fullPath))
                {
                    shaderSource = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load shader source file: " + filepath + " - " + e.Message);
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

        List<Vector3> sphereVertices;
        List<Vector2> sphereTexCoords;
        List<uint> sphereIndices;

        int VAO;
        int VBO;
        Shader shader;
        int EBO;

        int textureID;
        int textureVBO; 

        float yRot = 0f;
        float rotationSpeed = MathHelper.DegreesToRadians(45.0f);

        Camera camera;



        public Game(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.CenterWindow(new Vector2i(width, height));
            this.height = height;
            this.width = width;

        }

        //создание сферы 
        private void GenerateSphere(float radius, out List<Vector3> vertices, out List<Vector2> texCoords,
       out List<uint> indices, int sectorCount, int stackCount)  //int sectorCount, int stackCount -- широта и долгота.
                                                                 //Делают сферу более гладкой. Разбивает ее на
                                                                 //прямоугольники--> треугольники
        {
            vertices = new List<Vector3>();
            texCoords = new List<Vector2>();
            indices = new List<uint>();

            float x, y, z, xy;  //хранение координат текущей вершины. xy -- проекция радиуса
                                //float nx, ny, nz, lengthInv = 1.0f / radius; // нормали
            float s, t; // //хранения текстурных координат. t-- vertikal, s -- gorizont
            float sectorStep = 2 * MathF.PI / sectorCount; // угловой шаг между секторами
            float stackStep = MathF.PI / stackCount; //угловой шаг между слоями
            float sectorAngle, stackAngle;// текущий угол сектора и слоя

            for (int i = 0; i <= stackCount; i++) // от верхнего полюса к нижнему. по слоям ()
            {
                stackAngle = MathF.PI / 2 - i * stackStep;
                xy = radius * MathF.Cos(stackAngle);
                z = radius * MathF.Sin(stackAngle);

                // по секторам
                for (int j = 0; j <= sectorCount; j++)
                {
                    sectorAngle = j * sectorStep;

                    x = xy * MathF.Cos(sectorAngle);
                    y = xy * MathF.Sin(sectorAngle);
                    vertices.Add(new Vector3(x, z, y));

                    s = (float)j / sectorCount;
                    t = (float)i / stackCount;

                    texCoords.Add(new Vector2(s, t));


                }
            }

            // Генерация индексов для сборки треугольников.
            uint now, next; //хранения индексов вершин на текущем и следующем слоях.

            for (int i = 0; i < stackCount; i++)
            {
                now = (uint)(i * (sectorCount + 1));
                next = (uint)(now + (sectorCount + 1));

                for (int j = 0; j < sectorCount; ++j, ++now, ++next)
                {
                    //Формируем два треугольника для каждого четырехугольника

                    // Первый треугольник: (текущий слой, текущий сектор) -> (след. слой, текущий сектор)
                    // -> (текущий слой, след. сектор)
                    if (i != 0)
                    { // Не создаем треугольники, примыкающие к самому верхнему полюсу

                        //полюс
                        indices.Add(now);
                        indices.Add(next);
                        indices.Add(now + 1);
                    }

                    //2 треуголбник. (текущий слой, след. сектор) -> (след. слой, текущий сектор)
                    //-> (след. слой, след. сектор)
                    if (i != (stackCount - 1))
                    {

                        indices.Add(now + 1);
                        indices.Add(next);
                        indices.Add(next + 1);

                    }
                }

            }

        }





        protected override void OnLoad()
        {
            base.OnLoad();

            GenerateSphere(0.5f, out sphereVertices, out sphereTexCoords, out sphereIndices, 100, 95);

            //загрузка текстуры
            textureID = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear); // Лучше для мипмапов
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            StbImage.stbi_set_flip_vertically_on_load(0);
        ImageResult boxTexture = ImageResult.FromStream(File.OpenRead("../../../Textures/photo.jpg"),
            ColorComponents.RedGreenBlueAlpha);
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                PixelInternalFormat.Rgba, boxTexture.Width, boxTexture.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture.Data);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);//генерирует мипмап-уровни для текстуры.
                                                              //Это набор уменьшенных копий текстуры.
                                                              // Улучшает качество (убирает муар) и производительность.

            GL.BindTexture(TextureTarget.Texture2D, 0);

        


            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, sphereVertices.Count * Vector3.SizeInBytes, sphereVertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0); // location = 0
            GL.EnableVertexAttribArray(0);


            textureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, sphereTexCoords.Count * Vector2.SizeInBytes, sphereTexCoords.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0); // location = 1
            GL.EnableVertexAttribArray(1);


            // EBO для индексов
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO); // Привязываем EBO
            GL.BufferData(BufferTarget.ElementArrayBuffer, sphereIndices.Count * sizeof(uint), 
                sphereIndices.ToArray(), BufferUsageHint.StaticDraw);
            // --- EBO остается привязанным, пока VAO привязан ---

            // --- Отвязка ---
            GL.BindVertexArray(0); // Отвязываем VAO (сохраняет состояние VBO и EBO)
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0); // Отвязываем ArrayBuffer (VBO)
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0); // Отвязываем ElementArrayBuffer (EBO)

            shader = new Shader();
            shader.LoadShader();

            GL.Enable(EnableCap.DepthTest);

            camera = new Camera(width, height, new Vector3(0, 0, 3f));

            CursorState = CursorState.Grabbed;
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteBuffer(VAO);
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(textureVBO);
            GL.DeleteBuffer(EBO);
            GL.DeleteTexture(textureID);

            shader.DeleteShader();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {

            base.OnRenderFrame(args);

            GL.ClearColor(0.8f, 0.7f, 0.9f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.UseShader();

            GL.ActiveTexture(TextureUnit.Texture0); 
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            int texUniformLocation = GL.GetUniformLocation(shader.GetShader(), "texture0");
            GL.Uniform1(texUniformLocation, 0);

            GL.BindVertexArray(VAO);

            //Transformation

            Matrix4 model = Matrix4.CreateRotationY(yRot);
            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjectionMatrix();
            

            int modelLocation = GL.GetUniformLocation(shader.GetShader(), "model");
            int viewLocation = GL.GetUniformLocation(shader.GetShader(), "view");
            int projectionLocation = GL.GetUniformLocation(shader.GetShader(), "projection");

            GL.UniformMatrix4(modelLocation, false, ref model);
            GL.UniformMatrix4(viewLocation, false, ref view);
            GL.UniformMatrix4(projectionLocation, false, ref projection);



            GL.DrawElements(PrimitiveType.Triangles, sphereIndices.Count, 
                DrawElementsType.UnsignedInt, 0);

            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            Context.SwapBuffers();

            
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
         

            if (!IsFocused) // Не обновляем, если окно не в фокусе
            {
                return;
            }

            yRot += rotationSpeed * (float)args.Time;

            KeyboardState input = KeyboardState; // Получаем состояние клавиатуры
            MouseState mouse = MouseState;     // Получаем состояние мыши

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            camera.Update(input, mouse, args); // Обновляем камеру

            base.OnUpdateFrame(args); 
        }


        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            // Обновляем размеры для камеры, чтобы соотношение сторон было правильным
            this.width = e.Width;
            this.height = e.Height;
            if (camera != null) // Проверяем, что камера уже создана
            {
                 camera.UpdateScreenSize(e.Width, e.Height);
            }
        }
    }
}
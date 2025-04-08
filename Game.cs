using System;
using System.Collections.Generic;
using System.IO; // Добавлено для StreamReader и File
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
        Vector3 sphereCenterCoords;

        int VAO;
        int VBO;
        Shader shader;
        int EBO;

        int textureID;
        int textureVBO; 

        float yRot = 0f;
        float rotationSpeed = MathHelper.DegreesToRadians(45.0f);

        Camera camera;
        float cameraDistance = 4.0f;    // Расстояние от камеры до сферы
        float cameraHeightOffset = 1.0f; // Насколько камера выше центра сферы

        Vector3 front = -Vector3.UnitZ;
        Vector3 right = Vector3.UnitX;
        private float SPEED = 0.003f;


        //дорога(?)
        List<Vector3> roadVertices;
        List<Vector2> roadTexCoords;
        List<uint> roadIndices;

        int roadVAO;
        int roadVBO;
        int roadEBO;
        int roadtextureID;
        int roadtextureVBO;


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



        private void GenerateRoad(float size, float yLevel, out List<Vector3> vertices, out List<Vector2> texCoords,
       out List<uint> indices)
        {
            float halfSize = size / 2.0f;

            vertices = new List<Vector3>
            {
            new Vector3(-halfSize, yLevel, -halfSize), // Нижний левый
            new Vector3( halfSize, yLevel, -halfSize), // Нижний правый
            new Vector3( halfSize, yLevel,  halfSize), // Верхний правый
            new Vector3(-halfSize, yLevel,  halfSize)  // Верхний левый
            };

    
            texCoords = new List<Vector2>
            {
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 1.0f)
            };

            // Индексы для двух треугольников
            indices = new List<uint>
            {
            0, 1, 2,  // Первый треугольник
            0, 2, 3   // Второй треугольник
            };

        }

        //private void Buffers(int VBO, int VAO, int textureVBO, int EBO, int textureID, string path)
        //{
        //    //загрузка текстуры
        //    textureID = GL.GenTexture();
        //    GL.ActiveTexture(TextureUnit.Texture0);
        //    GL.BindTexture(TextureTarget.Texture2D, textureID);

        //    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        //    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        //    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear); // Лучше для мипмапов
        //    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        //    StbImage.stbi_set_flip_vertically_on_load(0);
            
        //    ImageResult boxTexture = ImageResult.FromStream(File.OpenRead(path),
        //        ColorComponents.RedGreenBlueAlpha);
        //    GL.TexImage2D(TextureTarget.Texture2D, 0,
        //        PixelInternalFormat.Rgba, boxTexture.Width, boxTexture.Height, 0,
        //        PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture.Data);

        //    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        //    GL.BindTexture(TextureTarget.Texture2D, 0);




        //    VAO = GL.GenVertexArray();
        //    GL.BindVertexArray(VAO);

        //    VBO = GL.GenBuffer();
        //    GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        //    GL.BufferData(BufferTarget.ArrayBuffer, sphereVertices.Count * Vector3.SizeInBytes, sphereVertices.ToArray(), BufferUsageHint.StaticDraw);
        //    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0); // location = 0
        //    GL.EnableVertexAttribArray(0);


        //    textureVBO = GL.GenBuffer();
        //    GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
        //    GL.BufferData(BufferTarget.ArrayBuffer, sphereTexCoords.Count * Vector2.SizeInBytes, sphereTexCoords.ToArray(), BufferUsageHint.StaticDraw);
        //    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0); // location = 1
        //    GL.EnableVertexAttribArray(1);


        //    // EBO для индексов
        //    EBO = GL.GenBuffer();
        //    GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO); // Привязываем EBO
        //    GL.BufferData(BufferTarget.ElementArrayBuffer, sphereIndices.Count * sizeof(uint),
        //        sphereIndices.ToArray(), BufferUsageHint.StaticDraw);
        //    // --- EBO остается привязанным, пока VAO привязан ---

        //    // --- Отвязка ---
        //     // Отвязываем VAO (сохраняет состояние VBO и EBO)

            
        //}

        //private void Binding(int VBO, int VAO, int textureVBO, int EBO, int textureID)
        //{
        //    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        //    GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        //    GL.BindTexture(TextureTarget.Texture2D, 0);
        //}

        private int Loadtexture(string path)
        {
            int textureID = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear); // Лучше для мипмапов
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            StbImage.stbi_set_flip_vertically_on_load(0);
            ImageResult boxTexture = ImageResult.FromStream(File.OpenRead(path),
                ColorComponents.RedGreenBlueAlpha);
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                PixelInternalFormat.Rgba, boxTexture.Width, boxTexture.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture.Data);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);//генерирует мипмап-уровни для текстуры.
                                                              //Это набор уменьшенных копий текстуры.
                                                              // Улучшает качество (убирает муар) и производительность.

            GL.BindTexture(TextureTarget.Texture2D, 0);

            return textureID;
        }
        protected override void OnLoad()
        {
            base.OnLoad();
            
            GenerateSphere(0.5f, out sphereVertices, out sphereTexCoords, out sphereIndices, 36, 18);

            //Buffers(VBO, VAO, textureVBO, EBO, textureID, "../../../Textures/photo.jpg");
            //GL.BindVertexArray(0);


            //Buffers(roadVBO, roadVAO, roadtextureVBO, roadEBO, roadtextureID, "../../../Textures/labirint2.jpg");

            //загрузка текстуры
            //textureID = GL.GenTexture();
            //GL.ActiveTexture(TextureUnit.Texture0);
            //GL.BindTexture(TextureTarget.Texture2D, textureID);

            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear); // Лучше для мипмапов
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            //StbImage.stbi_set_flip_vertically_on_load(0);
            //ImageResult boxTexture = ImageResult.FromStream(File.OpenRead("../../../Textures/photo.jpg"),
            //    ColorComponents.RedGreenBlueAlpha);
            //GL.TexImage2D(TextureTarget.Texture2D, 0,
            //    PixelInternalFormat.Rgba, boxTexture.Width, boxTexture.Height, 0,
            //    PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture.Data);

            //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);//генерирует мипмап-уровни для текстуры.
            //                                                  //Это набор уменьшенных копий текстуры.
            //                                                  // Улучшает качество (убирает муар) и производительность.

            //GL.BindTexture(TextureTarget.Texture2D, 0);


            //**************************************************************************************
            
            this.textureID = Loadtexture("../../../Textures/photo.jpg");

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



            //**************ROAD*************

            GenerateRoad(30.0f, -0.51f, out roadVertices, out roadTexCoords, out roadIndices);

            this.roadtextureID = Loadtexture("../../../Textures/labirint2.jpg");

            //roadtextureID = GL.GenTexture();
            //GL.ActiveTexture(TextureUnit.Texture1);
            //GL.BindTexture(TextureTarget.Texture2D, roadtextureID);

            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear); // Лучше для мипмапов
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            //StbImage.stbi_set_flip_vertically_on_load(0);

            //ImageResult boxTexture2 = ImageResult.FromStream(File.OpenRead("../../../Textures/labirint2.jpg"),
            //    ColorComponents.RedGreenBlueAlpha);
            //GL.TexImage2D(TextureTarget.Texture2D, 0,
            //    PixelInternalFormat.Rgba, boxTexture2.Width, boxTexture2.Height, 0,
            //    PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture2.Data);
            //GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            roadVAO = GL.GenVertexArray();
            GL.BindVertexArray(roadVAO);

            roadVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, roadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, roadVertices.Count * Vector3.SizeInBytes, roadVertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
            GL.EnableVertexAttribArray(0);

            roadtextureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, roadtextureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, roadTexCoords.Count * Vector2.SizeInBytes, roadTexCoords.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0);
            GL.EnableVertexAttribArray(1);

            roadEBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, roadEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, roadIndices.Count * sizeof(uint), roadIndices.ToArray(), BufferUsageHint.StaticDraw);
            GL.BindVertexArray(0);

            // --- Отвязка ---
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            //Binding(VBO, VAO, textureVBO, EBO, textureID);


            shader = new Shader();
            shader.LoadShader();

            GL.Enable(EnableCap.DepthTest);

           
            Vector3 initialCameraPos = sphereCenterCoords - Vector3.UnitZ * cameraDistance 
                + Vector3.UnitY * cameraHeightOffset;
            camera = new Camera(width, height, initialCameraPos);


            //CursorState = CursorState.Grabbed;
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteBuffer(VAO);
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(textureVBO);
            GL.DeleteBuffer(EBO);
            GL.DeleteTexture(textureID);

            GL.DeleteBuffer(roadVBO);
            GL.DeleteBuffer(roadtextureVBO);
            GL.DeleteBuffer(roadEBO);
            GL.DeleteVertexArray(roadVAO);
            GL.DeleteTexture(roadtextureID);

            shader.DeleteShader();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {

            base.OnRenderFrame(args);

            GL.ClearColor(0.8f, 0.7f, 0.9f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.UseShader();

           //*********************СФЕРА****************************
            Matrix4 view = camera.GetViewMatrix(sphereCenterCoords);
            Matrix4 projection = camera.GetProjectionMatrix();

            int modelLocation = GL.GetUniformLocation(shader.GetShader(), "model");
            int viewLocation = GL.GetUniformLocation(shader.GetShader(), "view");
            int projectionLocation = GL.GetUniformLocation(shader.GetShader(), "projection");
            int texUniformLocation = GL.GetUniformLocation(shader.GetShader(), "texture0");

            
            GL.UniformMatrix4(projectionLocation, false, ref projection);
            GL.UniformMatrix4(viewLocation, false, ref view);

            GL.ActiveTexture(TextureUnit.Texture0); 
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.Uniform1(texUniformLocation, 0);

            Matrix4 spheremodel = Matrix4.CreateTranslation(sphereCenterCoords);
            GL.UniformMatrix4(modelLocation, false, ref spheremodel);

            GL.BindVertexArray(VAO);

            GL.DrawElements(PrimitiveType.Triangles, sphereIndices.Count,
               DrawElementsType.UnsignedInt, 0);

            GL.BindVertexArray(0);

            //*********************ДОРОГА*************************  

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, roadtextureID);
            GL.Uniform1(texUniformLocation, 1);

            Matrix4 roadModel = Matrix4.Identity;
            GL.UniformMatrix4(modelLocation, false, ref roadModel);

            GL.BindVertexArray(roadVAO);
            GL.DrawElements(PrimitiveType.Triangles, roadIndices.Count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.ActiveTexture(TextureUnit.Texture0);
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

            camera.UpdateRotation(mouse, args);

            if (input.IsKeyDown(Keys.W))
            {
                sphereCenterCoords += front * SPEED;

            }

            if (input.IsKeyDown(Keys.A))
            {
                sphereCenterCoords -= right * SPEED;

            }

            if (input.IsKeyDown(Keys.S))
            {
                sphereCenterCoords -= front * SPEED;

            }
            if (input.IsKeyDown(Keys.D))
            {
                sphereCenterCoords += right * SPEED;

            }

            Vector3 targetCameraPosition = sphereCenterCoords - camera.front * cameraDistance + 
                Vector3.UnitY * cameraHeightOffset;
            camera.position = targetCameraPosition;

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
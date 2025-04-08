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

        public int GetShader()
        {
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

        //Пакмен
        List<Vector3> sphereVertices;
        List<Vector2> sphereTexCoords;
        List<uint> sphereIndices;
        Vector3 sphereCenterCoords = Vector3.Zero;

        private float mouthTimer = 0.0f;                 // Таймер для анимации
        private float maxMouthAngle = MathHelper.DegreesToRadians(150.0f); // Максимальный угол открытия рта
        private float mouthSpeed = 6.0f;                 // Скорость открытия/закрытия рта
        private int mouthAngleLocation = -1;             // ID uniform-переменной mouthAngle в шейдере

        int VAO;
        int VBO;
        Shader shader;
        int EBO;

        int textureID;
        int textureVBO;

        //Пидорки
        List<Vector3> ghostPositions = new List<Vector3>();
        List<int> ghostTextureIDs = new List<int>();
        float ghostScale = 0.4f; // Масштаб призраков относительно Пакмана
        private Random random = new Random();



        //float yRot = 0f;
        //float rotationSpeed = MathHelper.DegreesToRadians(45.0f);

        Camera camera;
        float cameraDistance = 4.0f;    // Расстояние от камеры до сферы
        //float cameraHeightOffset = 1.0f; // Насколько камера выше центра сферы

        Vector3 front = -Vector3.UnitZ;
        Vector3 right = Vector3.UnitX;
        private float SPEED = 2f;


        //дорога(?)
        List<Vector3> roadVertices;
        List<Vector2> roadTexCoords;
        List<uint> roadIndices;
        float roadSize = 30.0f;

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

        //создание тел

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


        //буферы
        private int VAOBuf(int VAO)
        {

            VAO = GL.GenVertexArray();
            //GL.BindVertexArray(VAO);

            return VAO;

        }

        private int VBOBuf(int VBO, List<Vector3> vertices)
        {
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vector3.SizeInBytes, vertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0); // location = 0
            GL.EnableVertexAttribArray(0);

            return VBO;
        }

        private int texVBOBuf(int textureVBO, List<Vector2> texCoords)
        {
            textureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Count * Vector2.SizeInBytes, texCoords.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, 0); // location = 1
            GL.EnableVertexAttribArray(1);

            return textureVBO;
        }

        private int EBOBuf(int EBO, List<uint> indices)
        {
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO); // Привязываем EBO
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint),
                indices.ToArray(), BufferUsageHint.StaticDraw);
            return EBO;
        }

        private void Binding()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

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

            //*********************Пакмен*******************************
            GenerateSphere(0.5f, out sphereVertices, out sphereTexCoords, out sphereIndices, 36, 18);
            sphereCenterCoords = new Vector3(0, 0, 0);


            this.textureID = Loadtexture("../../../Textures/packman.jpg");
            this.VAO = VAOBuf(VAO);
            GL.BindVertexArray(this.VAO);
            this.VBO = VBOBuf(VBO, sphereVertices);
            this.textureVBO = texVBOBuf(textureVBO, sphereTexCoords);
            this.EBO = EBOBuf(EBO, sphereIndices);
            // --- Отвязка ---
            GL.BindVertexArray(0); // Отвязываем VAO (сохраняет состояние VBO и EBO)

            //*******************Пидорасики*********************
            float halfRoadSize = roadSize / 2.0f;
            float ghostY = 0.0f;

            // **ЗАГРУЗКА ТЕКСТУР ПРИЗРАКОВ**
            // Убедитесь, что у вас есть эти файлы текстур!
            string[] ghostTexturePaths = {
            "../../../Textures/red.jpg",    // Пример пути
            "../../../Textures/pink.jpg",   // Пример пути
            "../../../Textures/cyan.jpg",   // Пример пути
            "../../../Textures/orange.jpg"  // Пример пути
        };

            for (int i = 0; i < ghostTexturePaths.Length; ++i)
            {
                int currentGhostTexId = Loadtexture(ghostTexturePaths[i]);
                if (currentGhostTexId != 0) // Проверка успешности загрузки
                {
                    ghostTextureIDs.Add(currentGhostTexId);
                }
                else
                {
                    Console.WriteLine($"Warning: Failed to load ghost texture: {ghostTexturePaths[i]}");
                    // Можно добавить ID "запасной" текстуры или пропустить этого призрака
                    // ghostTextureIDs.Add(textureID); // Использовать текстуру Пакмана как запасную?
                }
            }

            // Генерируем позиции призраков
            for (int i = 0; i < ghostTextureIDs.Count; i++) // Используем количество загруженных текстур
            {
                float randomX = (float)(random.NextDouble() * roadSize - halfRoadSize);
                float randomZ = (float)(random.NextDouble() * roadSize - halfRoadSize);
                Vector3 potentialPos = new Vector3(randomX, ghostY, randomZ);
                while (Vector3.DistanceSquared(potentialPos, sphereCenterCoords) < 4.0f)
                {
                    randomX = (float)(random.NextDouble() * roadSize - halfRoadSize);
                    randomZ = (float)(random.NextDouble() * roadSize - halfRoadSize);
                    potentialPos = new Vector3(randomX, ghostY, randomZ);
                }
                ghostPositions.Add(potentialPos);
                Console.WriteLine($"Ghost {i} generated at: {ghostPositions[i]} with texture ID {ghostTextureIDs[i]}");
            }



            //**************ДОРОГА*************

            GenerateRoad(30.0f, -0.51f, out roadVertices, out roadTexCoords, out roadIndices);

            this.roadtextureID = Loadtexture("../../../Textures/labirint2.jpg");
            this.roadVAO = VAOBuf(roadVAO);
            GL.BindVertexArray(this.roadVAO);
            this.roadVBO = VBOBuf(roadVBO, roadVertices);
            this.roadtextureVBO = texVBOBuf(roadtextureVBO, roadTexCoords);
            this.roadEBO = EBOBuf(roadEBO, roadIndices);
            GL.BindVertexArray(0);

            // --- Отвязка ---
            //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            //GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            //GL.BindTexture(TextureTarget.Texture2D, 0);

            Binding();


            shader = new Shader();
            shader.LoadShader();


            mouthAngleLocation = GL.GetUniformLocation(shader.GetShader(), "mouthAngle");
           
            GL.Enable(EnableCap.DepthTest);


            //Vector3 initialCameraPos = sphereCenterCoords - Vector3.UnitZ * cameraDistance
            //    + Vector3.UnitY * cameraHeightOffset;
            camera = new Camera(Size.X, Size.Y, cameraDistance);


            //CursorState = CursorState.Grabbed;
        }

        private void DelBuf(int VAO, int VBO, int textureVBO, int EBO, int textureID)
        {
            GL.DeleteBuffer(VAO);
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(textureVBO);
            GL.DeleteBuffer(EBO);
            GL.DeleteTexture(textureID);
        }
        protected override void OnUnload()
        {
            base.OnUnload();

            DelBuf(VAO, VBO, textureVBO, EBO, textureID);

            foreach (int ghostTexId in ghostTextureIDs)
            {
                GL.DeleteTexture(ghostTexId);
            }

            DelBuf(roadVAO, roadVBO, roadtextureVBO, roadEBO, roadtextureID);

            shader.DeleteShader();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {

            base.OnRenderFrame(args);

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.UseShader();

            //*********************СФЕРА****************************
            float currentMouthAngle = (MathF.Sin(mouthTimer * mouthSpeed) + 1.0f) / 2.0f * maxMouthAngle;
            // --- ДОБАВИТЬ ВЫВОД В КОНСОЛЬ ---
            //Console.WriteLine($"Current Mouth Angle: {currentMouthAngle} radians"); // Раскомментируй для отладки
            Matrix4 view = camera.GetViewMatrix(sphereCenterCoords);
            Matrix4 projection = camera.GetProjectionMatrix();

            int modelLocation = GL.GetUniformLocation(shader.GetShader(), "model");
            int viewLocation = GL.GetUniformLocation(shader.GetShader(), "view");
            int projectionLocation = GL.GetUniformLocation(shader.GetShader(), "projection");
            int texUniformLocation = GL.GetUniformLocation(shader.GetShader(), "texture0");


            GL.UniformMatrix4(projectionLocation, false, ref projection);
            GL.UniformMatrix4(viewLocation, false, ref view);


            // --- УСТАНАВЛИВАЕМ УГОЛ ДЛЯ ПАКМАНА ---
            if (mouthAngleLocation != -1)
            {
                GL.Uniform1(mouthAngleLocation, currentMouthAngle); // Передаем актуальный угол
            }
            else // Добавим вывод, если location не найдена
            {
                Console.WriteLine("Mouth Angle Location IS -1!"); // Раскомментируй для отладки
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.Uniform1(texUniformLocation, 0);

            Matrix4 spheremodel = Matrix4.CreateTranslation(sphereCenterCoords);
            GL.UniformMatrix4(modelLocation, false, ref spheremodel);

            GL.BindVertexArray(VAO);

            GL.DrawElements(PrimitiveType.Triangles, sphereIndices.Count,
               DrawElementsType.UnsignedInt, 0);

            //******************************Пидоры************************
            // --- УСТАНАВЛИВАЕМ УГОЛ РТА В 0 ДЛЯ ПРИЗРАКОВ (чтобы у них не было рта) ---
            if (mouthAngleLocation != -1)
            {
                GL.Uniform1(mouthAngleLocation, 0.0f); // Закрытый рот = нет выреза
            }
            for (int i = 0; i < ghostPositions.Count; i++)
            {
                // GL.Uniform1(useColorLocation, 1); // УДАЛЕНО
                // GL.Uniform4(colorLocation, ghostColors[i]); // УДАЛЕНО

                // Привязываем ТЕКСТУРУ текущего призрака
                GL.BindTexture(TextureTarget.Texture2D, ghostTextureIDs[i]);

                Matrix4 scale = Matrix4.CreateScale(ghostScale);
                Matrix4 trans = Matrix4.CreateTranslation(ghostPositions[i]);
                Matrix4 ghostModel = scale * trans;
                GL.UniformMatrix4(modelLocation, false, ref ghostModel);

                // VAO уже привязан
                GL.DrawElements(PrimitiveType.Triangles, sphereIndices.Count, DrawElementsType.UnsignedInt, 0);
            }


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

            // yRot += rotationSpeed * (float)args.Time;

            KeyboardState input = KeyboardState; // Получаем состояние клавиатуры
            MouseState mouse = MouseState;     // Получаем состояние мыши

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            camera.Update(mouse, args, this.Size);

            mouthTimer += (float)args.Time;
            float moveAmount = SPEED * (float)args.Time; // Расстояние перемещения за кадр (не зависит от FPS)
            Vector3 moveDirection = Vector3.Zero; // Вектор направления движения за этот кадр

            if (input.IsKeyDown(Keys.W))
            {
                moveDirection += front;

            }

            if (input.IsKeyDown(Keys.A))
            {
                moveDirection -= right;

            }

            if (input.IsKeyDown(Keys.S))
            {
                moveDirection -= front;

            }
            if (input.IsKeyDown(Keys.D))
            {
                moveDirection += right;

            }

            if (moveDirection.LengthSquared > 0) // Проверяем, было ли нажатие клавиш движения
            {
                sphereCenterCoords += Vector3.Normalize(moveDirection) * moveAmount;
            }


            base.OnUpdateFrame(args);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButton.Left)
            {
                if (camera != null)
                {
                    // Переключаем состояние блокировки в камере
                    camera.ToggleRotationLock();

                    // Опционально: можно менять вид курсора при блокировке/разблокировке,
                    // но CursorState.Normal подходит для обоих случаев, т.к. мышь должна быть видна.
                    // CursorState = camera.IsRotationLocked ? CursorState.Normal : CursorState.Normal; // Остается Normal
                    Console.WriteLine($"Camera rotation locked: {camera.IsRotationLocked}"); // Выводим в консоль для отладки
                }
            }
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


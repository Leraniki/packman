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

    //еда
    public struct Food
    {
        public Vector3 Position; 
        public bool IsEaten; // Флаг, показывающий, съедена ли эта еда
        public static float Radius = 0.15f;
    }

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

        // План лабиринта (W - стена, ' ' - путь)
        private char[,] mazeLayout = new char[,]
        {
        {'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W'},
        {'W', ' ', ' ', ' ', ' ', 'W', ' ', ' ', ' ', ' ', ' ', ' ', 'W'},
        {'W', ' ', 'W', 'W', ' ', 'W', ' ', 'W', 'W', 'W', ' ', 'W', 'W'},
        {'W', ' ', 'W', ' ', ' ', ' ', ' ', ' ', 'W', ' ', ' ', ' ', 'W'},
        {'W', ' ', 'W', ' ', 'W', 'W', 'W', ' ', 'W', ' ', 'W', ' ', 'W'},
        {'W', ' ', ' ', ' ', 'W', ' ', ' ', ' ', ' ', ' ', 'W', ' ', 'W'},
        {'W', 'W', 'W', ' ', 'W', ' ', 'W', 'W', 'W', ' ', 'W', ' ', 'W'},
        {'W', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'W', ' ', ' ', ' ', 'W'},
        {'W', ' ', 'W', 'W', 'W', 'W', 'W', ' ', 'W', 'W', 'W', ' ', 'W'},
        {'W', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'W'},
        {'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W', 'W'}
        };

        private float mazeCellSize = 2.0f; // Размер одной клетки лабиринта в мире
        private float mazeWallHeight = 1.0f; // Высота стен

        List<Vector3> mazeVertices;
        List<Vector2> mazeTexCoords;
        List<uint> mazeIndices;

        int mazeVAO;
        int mazeVBO;
        int mazetextureVBO; // Отдельный VBO для текстурных координат лабиринта
        int mazeEBO;
        int mazeTextureID; // Текстура для стен лабиринта

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

        //Призраки
        List<Vector3> ghostPositions = new List<Vector3>();
        List<int> ghostTextureIDs = new List<int>();
        float ghostScale = 0.4f; // Масштаб призраков относительно Пакмана
        private Random random = new Random();


        Camera camera;
        float cameraDistance = 4.0f;    // Расстояние от камеры до сферы
        private float SPEED = 2f;
        Vector3 front = -Vector3.UnitZ;
        Vector3 right = Vector3.UnitX;

        //дорога
        List<Vector3> roadVertices;
        List<Vector2> roadTexCoords;
        List<uint> roadIndices;
        float roadSize = 30.0f;

        int roadVAO;
        int roadVBO;
        int roadEBO;
        int roadtextureID;
        int roadtextureVBO;

        //еда
        private List<Food> food = new List<Food>();
        private int foodTextureID;
        private float foodScale = 0.3f;
        public Game(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.CenterWindow(new Vector2i(width, height));
            this.height = height;
            this.width = width;

        }

        // --- Состояние Пакмана ---
        private float pacmanScale = 1.0f;
        private const float BASE_PACMAN_RADIUS = 0.5f; // Изначальный радиус пакмена
        private float scaleIncreasePerCollectible = 0.05f; // Насколько увеличивается масштаб за 1 еду
        private Vector3 initialPacmanPosition; //начальную позицию для сброса

        // --- Состояние Призраков ---
        private List<Vector3> initialGhostPositions = new List<Vector3>(); // Начальные позиции призраков
        private List<int> initialGhostTextureIDs = new List<int>();   
        private const float BASE_GHOST_RADIUS = 0.5f; // Изначальный радиус призрака (до масштабирования)

        // --- Состояние игры ---
        private int totalfood = 0; // Общее количество еды на уровне
        private int foodEaten = 0;
        private int initialGhostCount = 0; // Начальное количество призраков


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

        private void GenerateMazeGeometry(char[,] layout, float cellSize, float wallHeight, float floorY,
                                 out List<Vector3> vertices, out List<Vector2> texCoords, out List<uint> indices)
        {
            vertices = new List<Vector3>();
            texCoords = new List<Vector2>();
            indices = new List<uint>();

            int rows = layout.GetLength(0);
            int cols = layout.GetLength(1);

            float offsetX = -cols / 2.0f * cellSize; // смещение
            float offsetZ = -rows / 2.0f * cellSize;

            uint vertexCount = 0; // Будем считать добавленные вершины

            // Стандартные UV-координаты для квадрата
            Vector2 uv00 = new Vector2(0.0f, 0.0f);
            Vector2 uv10 = new Vector2(1.0f, 0.0f);
            Vector2 uv01 = new Vector2(0.0f, 1.0f);
            Vector2 uv11 = new Vector2(1.0f, 1.0f);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (layout[r, c] == 'W') // Если это стена
                    {
                        float worldX = offsetX + c * cellSize;
                        float worldZ = offsetZ + r * cellSize;
                        float worldY = floorY;

                        // Определяем 8 угловых позиций куба
                        Vector3 p0 = new Vector3(worldX, worldY, worldZ);
                        Vector3 p1 = new Vector3(worldX + cellSize, worldY, worldZ);
                        Vector3 p2 = new Vector3(worldX + cellSize, worldY + wallHeight, worldZ);
                        Vector3 p3 = new Vector3(worldX, worldY + wallHeight, worldZ);
                        Vector3 p4 = new Vector3(worldX, worldY, worldZ + cellSize);
                        Vector3 p5 = new Vector3(worldX + cellSize, worldY, worldZ + cellSize);
                        Vector3 p6 = new Vector3(worldX + cellSize, worldY + wallHeight, worldZ + cellSize);
                        Vector3 p7 = new Vector3(worldX, worldY + wallHeight, worldZ + cellSize);

                        // Добавляем 24 вершины (по 4 на грань) и 24 UV

                        // Передняя грань (+Z направление в OpenGL каноническом виде, но -Z в нашем мире)
                        vertices.Add(p0); texCoords.Add(uv00); // Нижний левый
                        vertices.Add(p1); texCoords.Add(uv10); // Нижний правый
                        vertices.Add(p2); texCoords.Add(uv11); // Верхний правый
                        vertices.Add(p3); texCoords.Add(uv01); // Верхний левый
                        // Добавляем 6 индексов для двух треугольников этой грани.
                        indices.AddRange(new uint[] { vertexCount + 0, vertexCount + 1, vertexCount + 2, vertexCount + 0, vertexCount + 2, vertexCount + 3 });
                        vertexCount += 4;

                        // Задняя грань (-Z направление OpenGL, +Z в нашем мире)
                        vertices.Add(p5); texCoords.Add(uv00); // Нижний левый (сзади) -> (0,0)
                        vertices.Add(p4); texCoords.Add(uv10); // Нижний правый (сзади) -> (1,0) // Перевернуто UV для правильной ориентации
                        vertices.Add(p7); texCoords.Add(uv11); // Верхний правый (сзади) -> (1,1)
                        vertices.Add(p6); texCoords.Add(uv01); // Верхний левый (сзади) -> (0,1)
                        indices.AddRange(new uint[] { vertexCount + 0, vertexCount + 1, vertexCount + 2, vertexCount + 0, vertexCount + 2, vertexCount + 3 });
                        vertexCount += 4;

                        // Левая грань (-X)
                        vertices.Add(p4); texCoords.Add(uv00);
                        vertices.Add(p0); texCoords.Add(uv10);
                        vertices.Add(p3); texCoords.Add(uv11);
                        vertices.Add(p7); texCoords.Add(uv01);
                        indices.AddRange(new uint[] { vertexCount + 0, vertexCount + 1, vertexCount + 2, vertexCount + 0, vertexCount + 2, vertexCount + 3 });
                        vertexCount += 4;

                        // Правая грань (+X)
                        vertices.Add(p1); texCoords.Add(uv00);
                        vertices.Add(p5); texCoords.Add(uv10);
                        vertices.Add(p6); texCoords.Add(uv11);
                        vertices.Add(p2); texCoords.Add(uv01);
                        indices.AddRange(new uint[] { vertexCount + 0, vertexCount + 1, vertexCount + 2, vertexCount + 0, vertexCount + 2, vertexCount + 3 });
                        vertexCount += 4;

                        // Верхняя грань (+Y)
                        vertices.Add(p3); texCoords.Add(uv00);
                        vertices.Add(p2); texCoords.Add(uv10);
                        vertices.Add(p6); texCoords.Add(uv11);
                        vertices.Add(p7); texCoords.Add(uv01);
                        indices.AddRange(new uint[] { vertexCount + 0, vertexCount + 1, vertexCount + 2, vertexCount + 0, vertexCount + 2, vertexCount + 3 });
                        vertexCount += 4;

                        // Нижняя грань (-Y)
                        vertices.Add(p4); texCoords.Add(uv00);
                        vertices.Add(p5); texCoords.Add(uv10);
                        vertices.Add(p1); texCoords.Add(uv11);
                        vertices.Add(p0); texCoords.Add(uv01);
                        indices.AddRange(new uint[] { vertexCount + 0, vertexCount + 1, vertexCount + 2, vertexCount + 0, vertexCount + 2, vertexCount + 3 });
                        vertexCount += 4;
                    }
                }
            }
            
        }

        //метод проверки столкновений
        private bool CheckWallCollision(Vector3 position, float radius)
        {
            // Определяем границы Пакмана с учетом радиуса
            float minX = position.X - radius;
            float maxX = position.X + radius;
            float minZ = position.Z - radius;
            float maxZ = position.Z + radius;

            // Рассчитываем смещения для центрирования лабиринта
            int mazeRows = mazeLayout.GetLength(0);
            int mazeCols = mazeLayout.GetLength(1);
            float startOffsetX = -mazeCols / 2.0f * mazeCellSize;
            float startOffsetZ = -mazeRows / 2.0f * mazeCellSize;

            // Находим диапазон клеток сетки, которые могут пересекаться с Пакманом
            int minCol = (int)Math.Floor((minX - startOffsetX) / mazeCellSize);
            int maxCol = (int)Math.Floor((maxX - startOffsetX) / mazeCellSize);
            int minRow = (int)Math.Floor((minZ - startOffsetZ) / mazeCellSize);
            int maxRow = (int)Math.Floor((maxZ - startOffsetZ) / mazeCellSize);

            // Проверяем каждую клетку в этом диапазоне
            for (int r = minRow; r <= maxRow; r++)
            {
                for (int c = minCol; c <= maxCol; c++)
                {
                    // Проверяем, находится ли клетка в пределах лабиринта
                    if (r < 0 || r >= mazeRows || c < 0 || c >= mazeCols)
                    {
                        continue; 
                    }

                    // Если клетка является стеной ('W'), то произошло столкновение
                    if (mazeLayout[r, c] == 'W')
                    {
                        
                        return true; 
                    }
                }
            }

            return false; 
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
            GenerateSphere(BASE_PACMAN_RADIUS, out sphereVertices, out sphereTexCoords, out sphereIndices, 36, 18);
            sphereCenterCoords = new Vector3(0, 0, 0);

            this.textureID = Loadtexture("../../../Textures/packman.jpg");
            this.VAO = VAOBuf(VAO);
            GL.BindVertexArray(this.VAO);
            this.VBO = VBOBuf(VBO, sphereVertices);
            this.textureVBO = texVBOBuf(textureVBO, sphereTexCoords);
            this.EBO = EBOBuf(EBO, sphereIndices);
            // --- Отвязка ---
            GL.BindVertexArray(0); // Отвязываем VAO (сохраняет состояние VBO и EBO)


            //***********************ОПРЕДЕЛЕНИЕ СТАРТОВОЙ ПОЗИЦИИ ПАКМЕНА*********************************

            int mazeRows = mazeLayout.GetLength(0);
            int mazeCols = mazeLayout.GetLength(1);
            float startOffsetX = -mazeCols / 2.0f * mazeCellSize;
            float startOffsetZ = -mazeRows / 2.0f * mazeCellSize;
            float objectYLevel = 0.0f; // Уровень Y для Пакмана и призраков (можно настроить)
            List<Vector2i> emptyCells = new List<Vector2i>(); // Список пустых клеток (col, row)

            // --- Находим все пустые клетки ---
            for (int r = 0; r < mazeRows; r++)
            {
                for (int c = 0; c < mazeCols; c++)
                {
                    if (mazeLayout[r, c] == ' ')
                    {
                        emptyCells.Add(new Vector2i(c, r)); // Сохраняем координаты (столбец, строка)
                    }
                }
            }

                
            if (emptyCells.Count > 0)
            {
                Vector2i pacmanStartCell = emptyCells[0];
                emptyCells.RemoveAt(0); //чтобы не было еды и призраков в этой ячейке

                sphereCenterCoords = new Vector3(
                    startOffsetX + (pacmanStartCell.X + 0.5f) * mazeCellSize, 
                    objectYLevel,                                            
                    startOffsetZ + (pacmanStartCell.Y + 0.5f) * mazeCellSize  
                );
                Console.WriteLine($"Pacman start position at cell ({pacmanStartCell.X},{pacmanStartCell.Y}): {sphereCenterCoords}");
            }
            else
            {
                sphereCenterCoords = Vector3.Zero;
                Console.WriteLine("No empty cells found in maze for Pacman!");
            }

            //*******************Призраки********************
            float ghostY = 0.0f;

     
            string[] ghostTexturePaths = {
            "../../../Textures/red.jpg",    
            "../../../Textures/pink.jpg",   
            "../../../Textures/cyan.jpg",   
            "../../../Textures/orange.jpg"  
            };

            initialGhostCount = ghostTexturePaths.Length; // Сохраняем изначальное кол-во призраков
            initialGhostTextureIDs.Clear(); // Чистим перед загрузкой
            foreach (var path in ghostTexturePaths)
            {
                int texID = Loadtexture(path);
                if (texID != 0)
                {
                    initialGhostTextureIDs.Add(texID); // Сохраняем в список для сброса
                }
            }


            // Генерируем позиции призраков
            ghostPositions.Clear(); 
            float minSpawnDistSq = (mazeCellSize * 2.0f) * (mazeCellSize * 2.0f); // Мин. кв. дистанция от Пакмана

            List<Vector2i> availableSpawnCells = new List<Vector2i>(emptyCells);

            for (int i = 0; i < ghostTextureIDs.Count; i++)
            {
                if (availableSpawnCells.Count == 0)
                {
                    Console.WriteLine("Not enough empty cells to spawn ghost.");
                    break; 
                }

                int retryCount = 0;
                const int maxRetries = 10; 
                Vector3 potentialPos = Vector3.Zero;
                Vector2i chosenCell = Vector2i.Zero;

                do
                {
                    if (availableSpawnCells.Count == 0)
                    { 
                        retryCount = maxRetries; 
                        break;
                    }
                    int randomIndex = random.Next(availableSpawnCells.Count);
                    chosenCell = availableSpawnCells[randomIndex];

                    // Вычисляем мировую позицию
                    potentialPos = new Vector3(
                        startOffsetX + (chosenCell.X + 0.5f) * mazeCellSize,
                        objectYLevel, // Уровень пола для призраков
                        startOffsetZ + (chosenCell.Y + 0.5f) * mazeCellSize
                    );

                    // Проверяем дистанцию до Пакмана
                    if (Vector3.DistanceSquared(potentialPos, sphereCenterCoords) >= minSpawnDistSq)
                    {
                        availableSpawnCells.RemoveAt(randomIndex); 
                        break; 
                    }
                    else
                    {
                        retryCount++;
                    }

                } while (retryCount < maxRetries);

                if (retryCount < maxRetries) 
                {
                    ghostPositions.Add(potentialPos);
                    Console.WriteLine($"Ghost {i} generated at cell ({chosenCell.X},{chosenCell.Y}): {ghostPositions.Last()}");
                }
                else
                {
                    Console.WriteLine("Could not find suitable spawn location for ghost {i} after {maxRetries} retries.");
                    if (availableSpawnCells.Count > 0)
                    {
                        int fallbackIndex = random.Next(availableSpawnCells.Count);
                        chosenCell = availableSpawnCells[fallbackIndex];
                        potentialPos = new Vector3(
                           startOffsetX + (chosenCell.X + 0.5f) * mazeCellSize,
                           objectYLevel,
                           startOffsetZ + (chosenCell.Y + 0.5f) * mazeCellSize
                       );
                        availableSpawnCells.RemoveAt(fallbackIndex);
                        ghostPositions.Add(potentialPos);
                        Console.Write($"Ghost {i} generated at fallback cell ({chosenCell.X},{chosenCell.Y}): {ghostPositions.Last()} (might be close to player)");
                    }
                }
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

            GenerateMazeGeometry(mazeLayout, mazeCellSize, mazeWallHeight, -0.51f, 
                          out mazeVertices, out mazeTexCoords, out mazeIndices);

            this.mazeTextureID = Loadtexture("../../../Textures/normlab.png"); 
            this.mazeVAO = VAOBuf(mazeVAO); 
            GL.BindVertexArray(this.mazeVAO);
            this.mazeVBO = VBOBuf(mazeVBO, mazeVertices);
            this.mazetextureVBO = texVBOBuf(mazetextureVBO, mazeTexCoords);
            this.mazeEBO = EBOBuf(mazeEBO, mazeIndices);
            GL.BindVertexArray(0);

            //*****************ЕДА*******************
            this.foodTextureID = Loadtexture("../../../Textures/white.jpg");

            Binding();


            shader = new Shader();
            shader.LoadShader();


            mouthAngleLocation = GL.GetUniformLocation(shader.GetShader(), "mouthAngle");
           
            GL.Enable(EnableCap.DepthTest);


            camera = new Camera(Size.X, Size.Y, cameraDistance);

            PlaceObjects();
            //CursorState = CursorState.Grabbed;
        }
        // Метод для начального размещения и сброса
        private void PlaceObjects()
        {
            food.Clear();
            ghostPositions.Clear();
            ghostTextureIDs.Clear();
            initialGhostPositions.Clear(); // Очищаем перед заполнением

            int mazeRows = mazeLayout.GetLength(0);
            int mazeCols = mazeLayout.GetLength(1);
            float startOffsetX = -mazeCols / 2.0f * mazeCellSize;
            float startOffsetZ = -mazeRows / 2.0f * mazeCellSize;
            float objectYLevel = 0.3f; // Уровень Y для Пакмана, призраков, коллектиблов

            List<Vector2i> emptyCells = new List<Vector2i>();
            for (int r = 0; r < mazeRows; r++)
            {
                for (int c = 0; c < mazeCols; c++)
                {
                    if (mazeLayout[r, c] == ' ')
                    {
                        emptyCells.Add(new Vector2i(c, r));
                    }
                }
            }

            if (emptyCells.Count == 0)
            {
                Console.WriteLine("Error: No empty cells in maze!");
                Close(); // Выход, если лабиринт некорректен
                return;
            }

            // --- Размещение Пакмана ---
            Vector2i pacmanStartCell = emptyCells[0]; // Берем первую пустую клетку
            emptyCells.RemoveAt(0); // Убираем ее из доступных
            initialPacmanPosition = new Vector3(
                startOffsetX + (pacmanStartCell.X + 0.5f) * mazeCellSize,
                objectYLevel,
                startOffsetZ + (pacmanStartCell.Y + 0.5f) * mazeCellSize
            );
            sphereCenterCoords = initialPacmanPosition; // Устанавливаем текущую позицию
            pacmanScale = 1.0f; // Сбрасываем масштаб
            Console.WriteLine($"Pacman start position set to: {sphereCenterCoords}");


            // --- Размещение Коллектиблов ---
            totalfood = 0;
            foodEaten = 0;
            // Размещаем коллектибл в КАЖДОЙ оставшейся пустой клетке
            foreach (var cell in emptyCells)
            {
                Vector3 pos = new Vector3(
                    startOffsetX + (cell.X + 0.5f) * mazeCellSize,
                    objectYLevel,
                    startOffsetZ + (cell.Y + 0.5f) * mazeCellSize
                );
                food.Add(new Food { Position = pos, IsEaten = false });
                totalfood++;
            }
            Console.WriteLine($"Spawned {totalfood} collectibles.");


            // --- Размещение Призраков ---
            float minSpawnDistSq = (mazeCellSize * 3.0f) * (mazeCellSize * 3.0f); // Увеличим мин. дистанцию
            List<Vector2i> availableSpawnCells = new List<Vector2i>(emptyCells); // Используем копию для спавна призраков

            ghostTextureIDs.AddRange(initialGhostTextureIDs); // Копируем текстуры для текущей игры

            for (int i = 0; i < initialGhostCount; i++)
            {
                if (availableSpawnCells.Count == 0) break; // Если клетки кончились

                Vector2i chosenCell = Vector2i.Zero;
                Vector3 potentialPos = Vector3.Zero;
                bool positionFound = false;
                int attempts = 0;
                const int maxAttempts = 20;

                while (attempts < maxAttempts && availableSpawnCells.Count > 0)
                {
                    int randomIndex = random.Next(availableSpawnCells.Count);
                    chosenCell = availableSpawnCells[randomIndex];
                    potentialPos = new Vector3(
                        startOffsetX + (chosenCell.X + 0.5f) * mazeCellSize,
                        objectYLevel,
                        startOffsetZ + (chosenCell.Y + 0.5f) * mazeCellSize
                    );

                    // Убедимся, что не спавнимся слишком близко к пакману И на месте коллектибла
                    bool overlapsfood = false;
                    foreach (var coll in food)
                    {
                        if (Vector3.DistanceSquared(potentialPos, coll.Position) < 0.1f) // Маленький допуск
                        {
                            overlapsfood = true;
                            break;
                        }
                    }


                    if (Vector3.DistanceSquared(potentialPos, sphereCenterCoords) >= minSpawnDistSq && !overlapsfood)
                    {
                        availableSpawnCells.RemoveAt(randomIndex); // Убираем клетку из доступных для других призраков
                        positionFound = true;
                        break;
                    }
                    availableSpawnCells.RemoveAt(randomIndex); // Убираем, даже если не подошло, чтобы не пробовать ее вечно
                    attempts++;
                }

                if (positionFound)
                {
                    ghostPositions.Add(potentialPos);
                    initialGhostPositions.Add(potentialPos); // Сохраняем для сброса
                    Console.WriteLine($"Ghost {i} spawned at cell ({chosenCell.X},{chosenCell.Y}): {potentialPos}");
                }
                else
                {
                    // Если не нашли подходящее место (маловероятно, но возможно)
                    Console.WriteLine($"Warning: Could not find ideal spawn for ghost {i}. Placing randomly.");
                    if (emptyCells.Count > 0) // Берем любую оставшуюся пустую клетку
                    {
                        int fallbackIndex = random.Next(emptyCells.Count);
                        Vector2i fallbackCell = emptyCells[fallbackIndex];
                        potentialPos = new Vector3(
                           startOffsetX + (fallbackCell.X + 0.5f) * mazeCellSize,
                           objectYLevel,
                           startOffsetZ + (fallbackCell.Y + 0.5f) * mazeCellSize
                       );
                        ghostPositions.Add(potentialPos);
                        initialGhostPositions.Add(potentialPos);
                        emptyCells.RemoveAt(fallbackIndex); // Убираем использованную клетку
                        Console.WriteLine($"Ghost {i} fallback spawn at cell ({fallbackCell.X},{fallbackCell.Y}): {potentialPos}");
                    }
                    else
                    {
                        Console.WriteLine($"Error: No empty cells left for ghost {i} fallback spawn!");
                        // Возможно, нужно удалить текстуру этого призрака из ghostTextureIDs, если его не удалось создать
                        if (ghostTextureIDs.Count > ghostPositions.Count)
                            ghostTextureIDs.RemoveAt(ghostTextureIDs.Count - 1);
                    }
                }
            }
            // Убедимся, что количество текстур соответствует количеству позиций
            while (ghostTextureIDs.Count > ghostPositions.Count)
            {
                ghostTextureIDs.RemoveAt(ghostTextureIDs.Count - 1);
                Console.WriteLine("Removed ghost texture due to spawn failure.");
            }
            initialGhostCount = ghostPositions.Count; // Обновляем реальное количество созданных призраков
        }

        // Метод сброса игры
        private void ResetGame()
        {
            Console.WriteLine("Resetting game...");
            PlaceObjects(); // Перемещаем все объекты на начальные позиции/состояния
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

            DelBuf(mazeVAO, mazeVBO, mazeVBO, mazeEBO, mazeTextureID);

            shader.DeleteShader();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {

            base.OnRenderFrame(args);

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.UseShader();

            Matrix4 view = camera.GetViewMatrix(sphereCenterCoords);
            Matrix4 projection = camera.GetProjectionMatrix();

            int modelLocation = GL.GetUniformLocation(shader.GetShader(), "model");
            int viewLocation = GL.GetUniformLocation(shader.GetShader(), "view");
            int projectionLocation = GL.GetUniformLocation(shader.GetShader(), "projection");
            int texUniformLocation = GL.GetUniformLocation(shader.GetShader(), "texture0"); 

            GL.UniformMatrix4(projectionLocation, false, ref projection);
            GL.UniformMatrix4(viewLocation, false, ref view);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Uniform1(texUniformLocation, 0); 

            //*********************Рендеринг Пакмана****************************
            float currentMouthAngle = (MathF.Sin(mouthTimer * mouthSpeed) + 1.0f) / 2.0f * maxMouthAngle;
            if (mouthAngleLocation != -1)
            {
                GL.Uniform1(mouthAngleLocation, currentMouthAngle);
            }

            GL.BindTexture(TextureTarget.Texture2D, textureID); // Текстура Пакмана

            Matrix4 pacmanScaleMatrix = Matrix4.CreateScale(pacmanScale);
            Matrix4 pacmanTranslationMatrix = Matrix4.CreateTranslation(sphereCenterCoords);
            Matrix4 pacmanModel = pacmanScaleMatrix * pacmanTranslationMatrix; // Масштаб ДО переноса
            GL.UniformMatrix4(modelLocation, false, ref pacmanModel);

            GL.BindVertexArray(VAO); // VAO Пакмана/Сферы
            GL.DrawElements(PrimitiveType.Triangles, sphereIndices.Count, DrawElementsType.UnsignedInt, 0);

            //******************************Рендеринг Призраков************************
            if (mouthAngleLocation != -1)
            {
                GL.Uniform1(mouthAngleLocation, 0.0f); // Закрытый рот для призраков
            }
            for (int i = 0; i < ghostPositions.Count; i++)
            {
                GL.BindTexture(TextureTarget.Texture2D, ghostTextureIDs[i]); // Текстура призрака

                Matrix4 scale = Matrix4.CreateScale(ghostScale);
                Matrix4 trans = Matrix4.CreateTranslation(ghostPositions[i]);
                Matrix4 ghostModel = scale * trans;
                GL.UniformMatrix4(modelLocation, false, ref ghostModel);

                // VAO Пакмана все еще привязан
                GL.DrawElements(PrimitiveType.Triangles, sphereIndices.Count, DrawElementsType.UnsignedInt, 0);
            }
            


            // --- Рендеринг еды ---
            if (mouthAngleLocation != -1)
            {
                GL.Uniform1(mouthAngleLocation, 0.0f); // Закрытый рот для коллектиблов
            }
            GL.BindTexture(TextureTarget.Texture2D, foodTextureID); // Текстура коллектибла
            Matrix4 foodScaleMatrix = Matrix4.CreateScale(foodScale); // Масштаб для коллектиблов

            // VAO Сферы все еще привязан (VAO)
            foreach (var collectible in food)
            {
                if (!collectible.IsEaten) // Рисуем только несъеденные
                {
                    Matrix4 trans = Matrix4.CreateTranslation(collectible.Position);
                    Matrix4 foodModel = foodScaleMatrix * trans; // Масштаб ДО переноса
                    GL.UniformMatrix4(modelLocation, false, ref foodModel);
                    GL.DrawElements(PrimitiveType.Triangles, sphereIndices.Count, DrawElementsType.UnsignedInt, 0);
                }
            }

            GL.BindVertexArray(0); // Отвязываем VAO Пакмана/призраков
            //*********************Рендеринг Дороги (Пола)*************************

            GL.BindTexture(TextureTarget.Texture2D, roadtextureID); // Текстура пола

            Matrix4 roadModel = Matrix4.Identity;
            GL.UniformMatrix4(modelLocation, false, ref roadModel);

            GL.BindVertexArray(roadVAO); // VAO пола
            GL.DrawElements(PrimitiveType.Triangles, roadIndices.Count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0); // Отвязываем VAO пола

            //*********************Рендеринг Лабиринта (Стен)*************************
            GL.BindTexture(TextureTarget.Texture2D, mazeTextureID); // Текстура стен

            Matrix4 mazeModel = Matrix4.Identity;
            GL.UniformMatrix4(modelLocation, false, ref mazeModel);

            GL.BindVertexArray(mazeVAO); // VAO лабиринта
            GL.DrawElements(PrimitiveType.Triangles, mazeIndices.Count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0); // Отвязываем VAO лабиринта


            // Отвязываем текстуру в конце кадра (от юнита 0)
            GL.BindTexture(TextureTarget.Texture2D, 0);

            Context.SwapBuffers();

        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {


            if (!IsFocused) // Не обновляем, если окно не в фокусе
            {
                return;
            }

            

            KeyboardState input = KeyboardState; // Получаем состояние клавиатуры
            MouseState mouse = MouseState;     // Получаем состояние мыши

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            camera.Update(mouse, args, this.Size);

            mouthTimer += (float)args.Time;
            float moveAmount = SPEED * (float)args.Time; // Расстояние перемещения за кадр 
            Vector3 moveDirection = Vector3.Zero; // Вектор направления движения за этот кадр
            float currentPacmanRadius = BASE_PACMAN_RADIUS * pacmanScale;



            if (input.IsKeyDown(Keys.W))
            {
                moveDirection += right;

            }

            if (input.IsKeyDown(Keys.A))
            {
                moveDirection += front;

            }

            if (input.IsKeyDown(Keys.S))
            {
                moveDirection -= right;

            }
            if (input.IsKeyDown(Keys.D))
            {
                moveDirection -= front;

            }

            if (input.IsKeyDown(Keys.F12))
            {
                if (WindowState != WindowState.Fullscreen)
                    WindowState = WindowState.Fullscreen;
                else
                    WindowState = WindowState.Normal;
            }

          

                moveDirection.Y = 0;
            if (moveDirection.LengthSquared > 0.001f) // Если есть движение
            {
                moveDirection.Normalize();

                Vector3 nextPos = sphereCenterCoords + moveDirection * moveAmount;

                // Проверка столкновения со стенами с учетом текущего радиуса
                if (!CheckWallCollision(nextPos, currentPacmanRadius))
                {
                    sphereCenterCoords = nextPos; // Двигаем Пакмана
                }
                else
                {
                    // Попытка скольжения вдоль стены (простой вариант)
                    Vector3 slideX = sphereCenterCoords + new Vector3(moveDirection.X, 0, 0) * moveAmount;
                    if (!CheckWallCollision(slideX, currentPacmanRadius))
                    {
                        sphereCenterCoords = slideX;
                    }
                    else
                    {
                        Vector3 slideZ = sphereCenterCoords + new Vector3(0, 0, moveDirection.Z) * moveAmount;
                        if (!CheckWallCollision(slideZ, currentPacmanRadius))
                        {
                            sphereCenterCoords = slideZ;
                        }
                    }
                }
            }

            // --- Столкновение с едой ---
            for (int i = 0; i < food.Count; i++)
            {
                // Проверяем только если коллектибл еще не съеден
                if (!food[i].IsEaten)
                {
                    float distSq = Vector3.DistanceSquared(sphereCenterCoords, food[i].Position);
                    float radiiSum = currentPacmanRadius + Food.Radius; // Радиус коллектибла маленький
                    if (distSq < radiiSum * radiiSum)
                    {
                        // Съели!
                        Food collected = food[i]; // Копируем структуру
                        collected.IsEaten = true;             // Меняем флаг в копии
                        food[i] = collected;          // Записываем измененную структуру обратно в список

                        foodEaten++;
                        pacmanScale += scaleIncreasePerCollectible; // Увеличиваем масштаб Пакмана
                        Console.WriteLine($"Collectible eaten! Total: {foodEaten}/{totalfood}. New scale: {pacmanScale:F2}");

                    }
                }
            }

            float currentGhostRadius = BASE_GHOST_RADIUS * ghostScale; // Эффективный радиус призрака
                                                                       // Итерируем в обратном порядке, чтобы безопасно удалять элементы
            for (int i = ghostPositions.Count - 1; i >= 0; i--)
            {
                float distSq = Vector3.DistanceSquared(sphereCenterCoords, ghostPositions[i]);
                float radiiSum = currentPacmanRadius + currentGhostRadius;

                if (distSq < radiiSum * radiiSum)
                {
                    // Съели призрака!
                    Console.WriteLine($"Ghost eaten!");
                    ghostPositions.RemoveAt(i);
                    ghostTextureIDs.RemoveAt(i); // Удаляем соответствующую текстуру

                    // Опционально: добавить звук поедания призрака, очки и т.д.
                }
            }

            if (foodEaten >= totalfood && ghostPositions.Count == 0)
            {
                Console.WriteLine("Level Cleared!");
                ResetGame(); // Сбрасываем игру
            }

            base.OnUpdateFrame(args);
        }

        public float GetCurrentPacmanRadius()
        {
            return BASE_PACMAN_RADIUS * pacmanScale;
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


using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab2_ver2
{
    internal class Camera
    {
        private float SPEED = 3.0f; 
        private int SCREENWIDTH;
        private int SCREENHEIGHT;
        private float SENSITIVITY = 0.1f; 
        public Vector3 position;
        Vector3 up = Vector3.UnitY;
        Vector3 front = -Vector3.UnitZ;
        Vector3 right = Vector3.UnitX;
        private float pitch;
        private float yaw = -90.0f; 
        private bool firstMove = true;
        public Vector2 lastPos;

        public Camera(int width, int height, Vector3 position)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
            this.position = position;
            // Инициализация lastPos начальными координатами центра экрана
            lastPos = new Vector2(width / 2.0f, height / 2.0f);
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }
        public Matrix4 GetProjectionMatrix()
        {
            float aspectRatio = SCREENHEIGHT > 0 ? (float)SCREENWIDTH / SCREENHEIGHT : 1.0f;
             return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), // Угол обзора поменьше
                 aspectRatio, 0.1f, 100f);
        }

        // Новый метод для обновления размеров экрана при ресайзе
        public void UpdateScreenSize(int width, int height)
        {
             SCREENWIDTH = width;
             SCREENHEIGHT = height;
             
        }


        public void InputController(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            float cameraSpeed = SPEED * (float)e.Time; // Скорость, зависящая от времени кадра

            if (input.IsKeyDown(Keys.W))
            {
                position += front * cameraSpeed;
            }
            if (input.IsKeyDown(Keys.A))
            {
                position -= right * cameraSpeed;
            }
            if (input.IsKeyDown(Keys.S))
            {
                position -= front * cameraSpeed;
            }
            if (input.IsKeyDown(Keys.D))
            {
                position += right * cameraSpeed;
            }
            if (input.IsKeyDown(Keys.Space)) // Движение вверх
            {
                position += up * cameraSpeed;
            }
            if (input.IsKeyDown(Keys.LeftShift)) // Движение вниз
            {
                position -= up * cameraSpeed;
            }

            // Управление мышью
            if (firstMove)
            {
                lastPos = new Vector2(mouse.X, mouse.Y);
                firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - lastPos.X;
                var deltaY = mouse.Y - lastPos.Y; 
                lastPos = new Vector2(mouse.X, mouse.Y);

                
                yaw += deltaX * SENSITIVITY;
                pitch -= deltaY * SENSITIVITY; 

                pitch = Clamp(pitch, -89.0f, 89.0f); 
            }
            UpdateVectors();

        }
        public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            InputController(input, mouse, e);
        }

        private void UpdateVectors()
        {
            front.X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw));
            front = Vector3.Normalize(front);

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY)); 
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        private float Clamp(float pitch, float min, float max)
        {
            if (pitch < min)
            {
                pitch = min;
            }

            if (pitch > max)
            {
                pitch = max;
            }

            return pitch;
        }
    }
}
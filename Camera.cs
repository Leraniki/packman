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
        private int SCREENWIDTH;
        private int SCREENHEIGHT;
        private float SENSITIVITY = 0.005f; 
        Vector3 up = Vector3.UnitY;
        public Vector2 lastPos;
        private float distance = 4.0f; // Дистанция до цели по умолчанию
        // Углы, определяющие положение камеры относительно цели
        private float orbitYaw = -MathHelper.PiOver2; // Горизонтальный угол: Начать позади цели (вдоль -Z)
        private float orbitPitch = MathHelper.DegreesToRadians(15f); // Вертикальный угол: Начать немного приподнято

        // Ограничения вертикального угла (pitch)
        private const float MIN_PITCH = -MathHelper.PiOver2 + 0.1f; // Примерно -85 градусов
        private const float MAX_PITCH = MathHelper.PiOver2 - 0.1f; // Примерно +85 градусов



        public Camera(int width, int height, float initialDistance)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
            distance = initialDistance;
            // Инициализация lastPos начальными координатами центра экрана
            lastPos = new Vector2(width / 2.0f, height / 2.0f);
        }

        // Вычисляет позицию камеры на основе углов орбиты и положения цели
        private Vector3 CalculatePosition(Vector3 targetPosition)
        {
            // Расчет смещений по осям на основе сферических координат
            float offsetX = distance * MathF.Cos(orbitPitch) * MathF.Cos(orbitYaw);
            float offsetY = distance * MathF.Sin(orbitPitch);
            float offsetZ = distance * MathF.Cos(orbitPitch) * MathF.Sin(orbitYaw);
           
            return targetPosition + new Vector3(offsetX, offsetY, offsetZ);
        }

        public Matrix4 GetViewMatrix(Vector3 targetPosition)
        {
            // Вычисляем текущую позицию камеры на орбите
            Vector3 calculatedPosition = CalculatePosition(targetPosition);
            // Создаем матрицу LookAt, которая смотрит из вычисленной позиции на цель
            // Vector3.UnitY - стандартный вектор "вверх" для орбитальных камер
            return Matrix4.LookAt(calculatedPosition, targetPosition, up);
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


        public void UpdateRotation(MouseState mouse, FrameEventArgs e, Vector2i windowSize)
        {

            int winWidth = windowSize.X;
            int winHeight = windowSize.Y;

            bool mouseInside = mouse.X > 0 && mouse.X < winWidth - 1 &&
                               mouse.Y > 0 && mouse.Y < winHeight - 1;


            // Управление мышью
            // Вычисляем смещение мыши с прошлого кадра
            var deltaX = mouse.X - lastPos.X;
            var deltaY = mouse.Y - lastPos.Y;

            // Обновляем углы ТОЛЬКО если вращение НЕ заблокировано И мышь ВНУТРИ окна
            if (mouse.IsButtonDown(MouseButton.Left) && mouseInside)
            {
                // Yaw (рыскание) - горизонтальное вращение
                orbitYaw += deltaX * SENSITIVITY;

                // Pitch (тангаж) - вертикальное вращение
                orbitPitch -= deltaY * SENSITIVITY;

                // Ограничиваем вертикальный угол (pitch)
                orbitPitch = MathHelper.Clamp(orbitPitch, MIN_PITCH, MAX_PITCH);
            }

            // Всегда обновляем lastPos, чтобы избежать скачков при разблокировке
            // или при начале движения после паузы.
            lastPos = new Vector2(mouse.X, mouse.Y);

        }
        public void Update(MouseState mouse, FrameEventArgs e, Vector2i windowSize)
        {
            UpdateRotation(mouse, e, windowSize);
            distance -= mouse.ScrollDelta.Y * 0.1f; // Пример масштабирования
            distance = Math.Max(1.0f, distance); // Ограничение минимальной дистанции
        }


    }
}
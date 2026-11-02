using System;
using UnityEngine;

namespace MyFrame
{
    public static class FrameMathTool
    {
        /// <summary>
        /// 旋转一个点绕固定点旋转指定角度
        /// </summary>
        /// <param name="point">旋转点</param>
        /// <param name="fixedPoint">固定点</param>
        /// <param name="angle">旋转角度(注意这里是指角度而不是弧度,角度的正负也关系到顺逆旋转)</param>
        /// <returns>旋转后的点</returns>
        public static Vector2 RotatePointOn2DByAngle(Vector2 point,Vector2 fixedPoint,float angle)
        {
            var dir = point - fixedPoint;
            var rad = angle * Mathf.Deg2Rad;
            var cos = Mathf.Cos(rad);
            var sin = Mathf.Sin(rad);
            //旋转矩阵[cos,-sin
            //      sin,cos]
            var rotateDir = new Vector2(dir.x * cos - dir.y * sin, dir.x * sin + dir.y * cos);
            return rotateDir + fixedPoint;
        }

        
        /// <summary>
        /// 获取贝塞尔曲线的点
        /// </summary>
        /// <param name="order">阶数</param>
        /// <param name="t">参数t</param>
        /// <returns>贝塞尔曲线的权重组</returns>
        public static float[] GetBezierCurvePoints(int order,float t)
        {
            var result = new float[order + 1];
            int C(int n, int k)
            {
                if (k < 0 || k > n) return 0;
                if (k == 0 || k == n) return 1;
                return C(n - 1, k - 1) + C(n - 1, k);
            }
            for (int i = 0; i <= order; i++)
            {
                result[i] = C(order, i) * Mathf.Pow(t, i) * Mathf.Pow(1- t, order - i);
            }
            return result;
        }
    }
}
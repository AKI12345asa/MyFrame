using UnityEngine;

public static class MeshGenerator
{/*Mesh的显示规则应该包含如下原则外部表面原则：

    1.所有三角形都应该从外部看向物体时呈顺时针方向

    2.连续性原则：相邻三角形的共享边要保持顶点顺序一致

    3.法线一致性：所有三角形法线应该指向该图形外部

    4.右手法则验证：用右手手指沿顶点顺序弯曲，拇指指向即为法线方向*/
    public static Mesh GenerateFanMesh(float insideRadius, float radius, float height, float angle)
    {
        var fanMesh = new Mesh();
        var centerPos = Vector3.zero;
        var direction = Vector3.forward;
        var rightDir = Quaternion.AngleAxis(angle / 2, Vector3.up) * direction;
        var deltaAngle = 2.5f;
        var rects = (int)(angle / deltaAngle);
        var lines = rects + 1;
        var vertices = new Vector3[2 * lines * 2];
        var triangles = new int[rects * 4 * 3 * 2 + 6 + 12];
        
        //底面
        for (var i = 0; i < lines; i++)
        {
            var dir = Quaternion.AngleAxis(-deltaAngle * i, Vector3.up) * rightDir;
            var minPos = centerPos + dir * insideRadius;
            var maxPos = centerPos + dir * radius;
            vertices[2 * i] = minPos;
            vertices[2 * i + 1] = maxPos;

            //处理三角形,因为line比rects多1，所以只处理到lines-1
            // 2i + 1 2i + 2 2i
            // 2i + 1 2i + 3 2i + 2
            if (i < lines - 1)
            {
                triangles[6 * i + 0] = 2 * i + 1;
                triangles[6 * i + 1] = 2 * i + 2;
                triangles[6 * i + 2] = 2 * i + 0;
                triangles[6 * i + 3] = 2 * i + 1;
                triangles[6 * i + 4] = 2 * i + 3;
                triangles[6 * i + 5] = 2 * i + 2;
            }
        }
        
        //顶面
        for (var i = lines; i < lines * 2; i++)
        {
            var dir = Quaternion.AngleAxis(-deltaAngle * (i - lines), Vector3.up) * rightDir;
            var minPos = centerPos + dir * insideRadius;
            var maxPos = centerPos + dir * radius;
            
            minPos.y += height;
            maxPos.y += height;
            vertices[2 * i] = minPos;
            vertices[2 * i + 1] = maxPos;

            //处理三角形,因为line比rects多1，所以只处理到lines-1
            // 2i 2i + 2 2i + 1 
            // 2i + 2 2i + 3 2i + 1  
            if (i < 2 * lines - 1)
            {
                triangles[6 * i + 0] = 2 * i + 0;
                triangles[6 * i + 1] = 2 * i + 2;
                triangles[6 * i + 2] = 2 * i + 1;
                triangles[6 * i + 3] = 2 * i + 2;
                triangles[6 * i + 4] = 2 * i + 3;
                triangles[6 * i + 5] = 2 * i + 1;
            }
        }
        
        //右面
        //0 2 1
        //2 3 1
        var start = 2 * lines - 1;
        triangles[start * 6 + 0] = 0;
        triangles[start * 6 + 1] = 2 * lines;
        triangles[start * 6 + 2] = 1;
        triangles[start * 6 + 3] = 2 * lines;
        triangles[start * 6 + 4] = 2 * lines + 1;
        triangles[start * 6 + 5] = 1;
        
        
        //左面
        //1 2 0
        //1 3 2
        triangles[start * 6 + 6] = 2 * (lines - 1) + 1;
        triangles[start * 6 + 7] = 2 * (2 * lines - 1);
        triangles[start * 6 + 8] = 2 * (lines - 1) ;
        triangles[start * 6 + 9] = (lines - 1)* 2 + 1;
        triangles[start * 6 + 10] = 2 * (2 * lines - 1) + 1;
        triangles[start * 6 + 11] = 2 * (2 * lines - 1);
        
        
        //后面 
        //0 2 1
        //2 3 1
        start += 2;
        for (var i = 0; i < rects; i++)
        {
            var index = start + i;
            triangles[index * 6 + 0] = i * 2 + 0;
            triangles[index * 6 + 1] = i * 2 + 2;
            triangles[index * 6 + 2] = 2 * lines + i * 2;
            triangles[index * 6 + 3] = i * 2 + 2;
            triangles[index * 6 + 4] = 2 * lines + i * 2 + 2;
            triangles[index * 6 + 5] = 2 * lines + i * 2;
        }
        
        //前面
        //1 2 0
        //1 3 2
        start += rects;
        for (var i = 0; i < rects; i++)
        {
            var index = start + i;
            triangles[index * 6 + 0] = 2 * lines + i * 2 + 1;
            triangles[index * 6 + 1] = i * 2 + 2 + 1;
            triangles[index * 6 + 2] = i * 2 + 1;
            triangles[index * 6 + 3] = 2 * lines + i * 2 + 1;
            triangles[index * 6 + 4] = 2 * lines + i * 2 + 2 + 1;
            triangles[index * 6 + 5] = i * 2 + 2 + 1;
        }
        
        
        fanMesh.vertices = vertices;
        fanMesh.triangles = triangles;
        fanMesh.RecalculateNormals();
        return fanMesh;
    }
}

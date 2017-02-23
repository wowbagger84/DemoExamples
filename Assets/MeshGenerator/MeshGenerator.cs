using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour {

    public MeshFilter cave;         //Child object so we can rotate level for 2D
    public bool gizmos;             //Turn Gizmos on or off
    public SquareGrid squareGrid;   //Our grid of squares, that are on or off.
    List<Vector3> vertices;         //List of all mesh vertices that we will create
    List<int> triangles;            //not sure right now

    //Mesh information so that we can find the outlines.
    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();


    //Main function to start the mesh generation.
    public void GenerateMesh(int[,] map, float squareSize)
    {
        //If we have run before, get rid of old outlines (If we have generated stuff before)
        outlines.Clear();
        checkedVertices.Clear();
        triangleDictionary.Clear();

        //Create grid
        squareGrid = new SquareGrid(map, squareSize);

        //Holder for vertices and triangles
        vertices = new List<Vector3>();
        triangles = new List<int>();

        //Itterate grid and triangulate each square depending on control nodes.
        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangluateSquare(squareGrid.squares[x, y]);
            }
        }

        //Create the mesh and asign it to the child object.
        Mesh mesh = new Mesh();
        cave.mesh = mesh;

        //fill mesh with data
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        //Set up mesh UVs
        int tileAmount = 10;    //UV map tiling
        Vector2[] uvs = new Vector2[vertices.Count];

        for (int i = 0; i < vertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0)/2*squareSize, vertices[i].x) * tileAmount;
            float percentY = Mathf.InverseLerp(-map.GetLength(1) / 2 * squareSize, map.GetLength(1)/2*squareSize, vertices[i].z) * tileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }

        mesh.uv = uvs;

        //Generate mesh collider
        Generate2DCollider();

        //CreateWallMesh(); //skipped this part of the tutorial E04: 26:20
    }

    private void Generate2DCollider()
    {
        EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D>();

        for (int i = 0; i < currentColliders.Length; i++)
        {
            Destroy(currentColliders[i]);
        }

        CalculateMeshOutline();

        foreach (List<int> outline in outlines)
        {
            EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            Vector2[] edgePoints = new Vector2[outline.Count];
            for (int i = 0; i < outline.Count; i++)
            {
                edgePoints[i] = new Vector2(vertices[outline[i]].x, vertices[outline[i]].z);
            }
            edgeCollider.points = edgePoints;
        }
    }

    private void CalculateMeshOutline()
    {
        for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            if (!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);

                if(newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);
                    List<int> newOutline = new List<int>();

                    newOutline.Add(vertexIndex);

                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
            
        }
    }

    private void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if (nextVertexIndex != -1)
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;

        int[] vertices;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public int this[int i]
        {
            get { return vertices[i]; }
        }

        public bool contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }


    //Depending on active control points, send the correct nodes on to MeshFromPoints
    void TriangluateSquare(Square square)
    {
        switch(square.configuration)
        {
            //No active points
            case 0:
                break;

            //1 point acitve on square
            case 1:
                MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                break;

            //2 points active
            case 3:
                MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 6:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
                break;
            case 5: //Diagonal
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 10: //Diagonal
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            //3 points active
            case 7:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;


            //4 points active
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);

                //Small optimization to say that these cant be edge vertecies
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }
    }


    //Wrapper for creating mesh from points.
    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3) //we have 3 points, make a triangle
        {
            CreateTriangle(points[0], points[1], points[2]);
        }
        if (points.Length >= 4) //We have more, make another triangle
        {
            CreateTriangle(points[0], points[2], points[3]);
        }
        if (points.Length >= 5) //We have more, make another triangle
        {
            CreateTriangle(points[0], points[3], points[4]);
        }
        if (points.Length >= 6) // we have 5 triangles at most, diagonal shape
        {
            CreateTriangle(points[0], points[4], points[5]);
        }
    }


    //add the points to the vertices list.
    private void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }


    //Create the mesh triangles
    void CreateTriangle(Node a, Node b, Node c)
    {
        //Triangle need 3 nodes, points, vertices. Add the index???
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if(triangleDictionary.ContainsKey(vertexIndexKey))
        {
            triangleDictionary[vertexIndexKey].Add(triangle);
        }
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDictionary.Add(vertexIndexKey, triangleList);
        }
    }

    int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex];

        for (int i = 0; i < trianglesContainingVertex.Count; i++)
        {
            Triangle triangle = trianglesContainingVertex[i];

            for (int j = 0; j < 3; j++)
            {
                int vertexB = triangle[j];
                if (vertexB!=vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutlineEdge(vertexIndex, vertexB))
                        return vertexB;
                }
            }
        }

        return -1;
    }

    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        List<Triangle> trianglesContainingA = triangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        for (int i = 0; i < trianglesContainingA.Count; i++)
        {
            if (trianglesContainingA[i].contains(vertexB))
            {
                sharedTriangleCount++;
                if(sharedTriangleCount > 0)
                {
                    break;
                }
            }
        }
        return sharedTriangleCount == 1;
    }

    // Grid of squares
    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidht = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    float adjust = squareSize / 2;
                    Vector3 pos = new Vector3(-mapWidht / 2 + x * squareSize + adjust, 0, -mapHeight / 2 + y * squareSize + adjust);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX - 1, nodeCountY - 1];

            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }
        }
    }


    //Base node structure
    public class Node
    {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 _pos)
        {
            position = _pos;
        }
    }

    //Control nodes are in the corner and keep track of the above and right regular node.
    public class ControlNode : Node
    {
        public bool active;

        public Node above, right;

        public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos)
        {
            active = _active;
            above = new Node(position + Vector3.forward * squareSize / 2);
            right = new Node(position + Vector3.right * squareSize / 2);
        }
    }


    //Each square consists of 4 control nodes and 4 regular nodes between
    // C - N - C
    // |       |
    // N       N
    // |       |
    // C - N - C
    public class Square
    {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centerTop, centerRight, centerBottom, centerLeft;

        public int configuration = 0; //0 - 15 one for each combination of active control nodes.

        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft)
        {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomRight = _bottomRight;
            bottomLeft = _bottomLeft;

            centerTop = topLeft.right;
            centerRight = bottomRight.above;
            centerBottom = _bottomLeft.right;
            centerLeft = bottomLeft.above;

            if (topLeft.active)
                configuration += 8;

            if (topRight.active)
                configuration += 4;

            if (bottomRight.active)
                configuration += 2;

            if (bottomLeft.active)
                configuration++;
        }
    }

    //Draw the debug information in the editor
    private void OnDrawGizmos()
    {
        if (squareGrid != null && gizmos) //Only if we have a squareGrid (program is running)
        {
            for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
            {
                for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
                {
                    Gizmos.color = (squareGrid.squares[x, y].topLeft.active) ? Color.red : Color.blue;
                    if (squareGrid.squares[x, y].topLeft.active)
                        Gizmos.DrawCube(squareGrid.squares[x, y].topLeft.position, Vector3.one * 0.4f);
                    
                    Gizmos.color = (squareGrid.squares[x, y].topRight.active) ? Color.red : Color.blue;
                    if (squareGrid.squares[x, y].topRight.active)
                        Gizmos.DrawCube(squareGrid.squares[x, y].topRight.position, Vector3.one * 0.4f);

                    Gizmos.color = (squareGrid.squares[x, y].bottomRight.active) ? Color.red : Color.blue;
                    if (squareGrid.squares[x, y].bottomRight.active)
                        Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * 0.4f);

                    Gizmos.color = (squareGrid.squares[x, y].bottomLeft.active) ? Color.red : Color.blue;
                    if (squareGrid.squares[x, y].bottomLeft.active)
                        Gizmos.DrawCube(squareGrid.squares[x, y].bottomLeft.position, Vector3.one * 0.4f);

                    if (squareGrid.squares[x, y].bottomLeft.active || squareGrid.squares[x, y].bottomRight.active ||
                        squareGrid.squares[x, y].topLeft.active || squareGrid.squares[x, y].bottomRight.active)
                    {
                        Gizmos.color = Color.gray;
                        Gizmos.DrawCube(squareGrid.squares[x, y].centerTop.position, Vector3.one * 0.15f);
                        Gizmos.DrawCube(squareGrid.squares[x, y].centerRight.position, Vector3.one * 0.15f);
                        Gizmos.DrawCube(squareGrid.squares[x, y].centerBottom.position, Vector3.one * 0.15f);
                        Gizmos.DrawCube(squareGrid.squares[x, y].centerLeft.position, Vector3.one * 0.15f);
                    }
                }
            }
        }
    }

}

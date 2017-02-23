using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

    [Header("Size")]
    public int width;                   //Map width
    public int height;                  //Map height

    [Header("Random")]
    public string seed;                 //Use a random string to get the same map
    public bool useRandomSeed;          //Use time to create a random seed;

    [Range(0,100)]
    public int randomFillPercent;       //How much to start with
    public int smoothingIterations;     //Number of smoothing passes to remove small stuff

    [Header("Border")]
    public int borderSize = 2;
    public bool topBorder;              //Generate a top border
    public bool sideBoarder;            //Generate side borders

    [Header("Gizmos")]
    public bool showGizmos;             //Toggle editor gizmos.

    int[,] map;                         //Matrix for our map
    int[,] borderedMap;

    public GameObject toggle;

    private void Start()
    {
        //Generate a map on start
        GenerateMap();
    }


    private void Update()
    {
        //Generate a new map on mouse click
        if (Input.GetButtonDown("Jump"))
            GenerateMap();
    }


    private void GenerateMap()
    {
        //Create our matrix
        map = new int[width, height];

        //Fill the map with noise
        RandomFillMap();

        //Smooth noise to larger areas
        for (int i = 0; i < smoothingIterations; i++)
            SmoothMap();

        //Add a border around the generated cave
        borderedMap = AddMapBorder();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if(borderedMap[x + borderSize, y + borderSize] == 1)
                    Instantiate(toggle, new Vector3(x-width/2 + 0.5f,y-height/ 2 + 0.5f, 0), Quaternion.identity);
            }
        }


        //Generate a mesh from our map
        GenerateMesh();
    }


    public void ToggleControlNode(int x, int y)
    {
        x = x + width / 2 + borderSize;
        y = y + height / 2 + borderSize;
        try
        {
            if (borderedMap[x, y] == 1)
            {
                borderedMap[x, y] = 0;
                GenerateMesh();
            }
        }
        catch (Exception)
        {
            Debug.Log("invalid cords");
        }

    }


    void GenerateMesh()
    {
        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderedMap, 1);
    }


    //add a border to our map
    private int[,] AddMapBorder()
    {
        //create a slightly larger map with a border size.
        int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                //If we are inside orginal map, copy it's content
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                    borderedMap[x, y] = map[x - borderSize, y - borderSize];
                else
                    borderedMap[x, y] = 1; //else fill in the border.

                //Remove top border :)
                if (!topBorder && y >= height + borderSize)
                    borderedMap[x, y] = 0;
                
                //fix sides
                if (!sideBoarder && (x < borderSize || x > width+1))
                    borderedMap[x, y] = 0;

            }
        }

        return borderedMap;
    }


    //Fill the map with some noise to start with.
    void RandomFillMap()
    {
        if (useRandomSeed)
            seed = Time.time.ToString();

        //Cool way to create a random but being able to recreate it.
        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //Randomize if we are adding a active or passive node. Fill all edges with walls.
                if (pseudoRandom.Next(0, 100) < randomFillPercent || (x == 0 || x == width - 1 || y == 0 || y == height - 1))
                    map[x, y] = 1; 
                else
                    map[x, y] = 0;

                //Remove top if we dont want it
                if (!topBorder && y == 0)
                    map[x, y] = 0;

                //Remove sides
                if (!sideBoarder && (x == 0 || x == width - 1))
                    map[x, y] = 0;
            }
        }
    }


    //Remove small stuff
    void SmoothMap()
    {
        //Check each node for number of active neighbours, remove if needed.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int wallNeighbours = GetSurroundingWallCount(x, y);

                if (wallNeighbours > 4)
                    map[x, y] = 1;
                else if (wallNeighbours < 4)
                    map[x, y] = 0;
            }
        }
    }


    //Get number of surrounding wall cubes.
    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;

        for(int neighbourX = gridX -1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (neighbourX >= 0 && neighbourX <width && neighbourY >= 0 && neighbourY < height)
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    wallCount++;
                }

                if (!topBorder && neighbourY >= height)
                    wallCount--;

                if (!sideBoarder && neighbourX < 0)
                    wallCount--;

            }
        }
        return wallCount;
    }


    //Draw some helper cubes in the editor
    void OnDrawGizmos()
    {
        if (map != null && showGizmos)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if(map[x, y] == 1)
                        Gizmos.color = Color.black;
                    else
                        Gizmos.color = Color.white;

                    Vector3 pos = new Vector3(-width / 2 + x + 0.5f, -height / 2 + y + 0.5f, 0);
                    Gizmos.DrawCube(pos, Vector3.one * .5f);
                }
            }
        }
    }
}

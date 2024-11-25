using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WalkerGenerator : MonoBehaviour
{
    public enum Grid
    {
        FLOOR,
        WALL,
        EMPTY
    }

    //Variables
    public Grid[,] gridHandler;
    public List<WalkerObject> Walkers;
    public Tilemap tileMap;
    public Tile Floor;
    public Tile Wall;
    public int MapWidth = 30;
    public int MapHeight = 30;

    public int MaximumWalkers = 10;
    public int TileCount = default;
    public float FillPercentage = 0.4f;
    public float WaitTime = 0.05f;

    void Start()
    {
        InitializeGrid();
    }

    void InitializeGrid()
    {
        gridHandler = new Grid[MapWidth, MapHeight];

        for (int x = 0; x < gridHandler.GetLength(0); x++)
        {
            for (int y = 0; y < gridHandler.GetLength(1); y++)
            {
                gridHandler[x, y] = Grid.EMPTY;
            }
        }

        Walkers = new List<WalkerObject>();

        Vector3Int TileCenter = new Vector3Int(gridHandler.GetLength(0) / 2, gridHandler.GetLength(1) / 2, 0);

        WalkerObject curWalker = new WalkerObject(new Vector2(TileCenter.x, TileCenter.y), GetDirection(), 0.5f);
        gridHandler[TileCenter.x, TileCenter.y] = Grid.FLOOR;
        tileMap.SetTile(TileCenter, Floor);
        PlaceSymmetricTile(TileCenter, Grid.FLOOR);
        Walkers.Add(curWalker);

        TileCount++;

        StartCoroutine(CreateFloors());
    }
    public Grid[,] GetGridHandler()
    {
        return gridHandler;
    }

    void PlaceSymmetricTile(Vector3Int position, Grid gridType)
    {
        // Determine the mirrored position (for vertical symmetry)
        Vector3Int mirroredPos = new Vector3Int(
            gridHandler.GetLength(0) - 1 - position.x, // Flip horizontally
            position.y,                                // Same vertical position
            position.z);

        // Place the symmetric tile if it's within bounds
        if (mirroredPos.x >= 0 && mirroredPos.x < MapWidth &&
            mirroredPos.y >= 0 && mirroredPos.y < MapHeight)
        {
            if (gridType == Grid.FLOOR)
            {
                tileMap.SetTile(mirroredPos, Floor);
                gridHandler[mirroredPos.x, mirroredPos.y] = Grid.FLOOR;
            }
            else if (gridType == Grid.WALL)
            {
                tileMap.SetTile(mirroredPos, Wall);
                gridHandler[mirroredPos.x, mirroredPos.y] = Grid.WALL;
            }
        }
    }


    Vector2 GetDirection()
    {
        int choice = Mathf.FloorToInt(UnityEngine.Random.value * 3.99f);

        switch (choice)
        {
            case 0:
                return Vector2.down;
            case 1:
                return Vector2.left;
            case 2:
                return Vector2.up;
            case 3:
                return Vector2.right;
            default:
                return Vector2.zero;
        }
    }

    IEnumerator CreateFloors()
    {
        while ((float)TileCount / (float)gridHandler.Length < FillPercentage)
        {
            bool hasCreatedFloor = false;
            foreach (WalkerObject curWalker in Walkers)
            {
                Vector3Int curPos = new Vector3Int((int)curWalker.Position.x, (int)curWalker.Position.y, 0);

                if (gridHandler[curPos.x, curPos.y] != Grid.FLOOR)
                {
                    tileMap.SetTile(curPos, Floor);
                    TileCount++;
                    gridHandler[curPos.x, curPos.y] = Grid.FLOOR;
                    PlaceSymmetricTile(curPos, Grid.FLOOR);
                    hasCreatedFloor = true;
                }
            }

            //Walker Methods
            ChanceToRemove();
            ChanceToRedirect();
            ChanceToCreate();
            UpdatePosition();

            if (hasCreatedFloor)
            {
                yield return new WaitForSeconds(WaitTime);
            }
        }
        ValidateSymmetry();


        StartCoroutine(CreateWalls());
    }

    void ChanceToRemove()
    {
        int updatedCount = Walkers.Count;
        for (int i = 0; i < updatedCount; i++)
        {
            if (UnityEngine.Random.value < Walkers[i].ChanceToChange && Walkers.Count > 1)
            {
                Walkers.RemoveAt(i);
                break;
            }
        }
    }
    void ValidateSymmetry()
    {
        for (int x = 0; x < MapWidth / 2; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                Vector3Int mirroredPos = new Vector3Int(MapWidth - 1 - x, y, 0);

                if (gridHandler[x, y] != gridHandler[mirroredPos.x, mirroredPos.y])
                {
                    Debug.LogError($"Symmetry error at {pos} and {mirroredPos}");
                }
            }
        }
    }

    void ChanceToRedirect()
    {
        for (int i = 0; i < Walkers.Count; i++)
        {
            if (UnityEngine.Random.value < Walkers[i].ChanceToChange)
            {
                WalkerObject curWalker = Walkers[i];
                curWalker.Direction = GetDirection();
                Walkers[i] = curWalker;
            }
        }
    }

    void ChanceToCreate()
    {
        int updatedCount = Walkers.Count;
        for (int i = 0; i < updatedCount; i++)
        {
            if (UnityEngine.Random.value < Walkers[i].ChanceToChange && Walkers.Count < MaximumWalkers)
            {
                Vector2 newDirection = GetDirection();
                Vector2 newPosition = Walkers[i].Position;

                WalkerObject newWalker = new WalkerObject(newPosition, newDirection, 0.5f);
                Walkers.Add(newWalker);
            }
        }
    }

    void UpdatePosition()
    {
        for (int i = 0; i < Walkers.Count; i++)
        {
            WalkerObject FoundWalker = Walkers[i];

            // Update position
            FoundWalker.Position += FoundWalker.Direction;

            // Clamp within bounds
            FoundWalker.Position.x = Mathf.Clamp(FoundWalker.Position.x, 1, gridHandler.GetLength(0) - 2);
            FoundWalker.Position.y = Mathf.Clamp(FoundWalker.Position.y, 1, gridHandler.GetLength(1) - 2);

            // Update mirrored walker position
            Vector3Int mirroredPos = new Vector3Int(
                gridHandler.GetLength(0) - 1 - (int)FoundWalker.Position.x,
                (int)FoundWalker.Position.y,
                0
            );

            if (gridHandler[mirroredPos.x, mirroredPos.y] == Grid.EMPTY)
            {
                gridHandler[mirroredPos.x, mirroredPos.y] = Grid.FLOOR;
                tileMap.SetTile(mirroredPos, Floor);
            }

            Walkers[i] = FoundWalker;
        }
    }


    IEnumerator CreateWalls()
    {
        for (int x = 0; x < gridHandler.GetLength(0) - 1; x++)
        {
            for (int y = 0; y < gridHandler.GetLength(1) - 1; y++)
            {
                if (gridHandler[x, y] == Grid.FLOOR)
                {
                    if (gridHandler[x + 1, y] == Grid.EMPTY)
                    {
                        Vector3Int wallPos = new Vector3Int(x + 1, y, 0);
                        tileMap.SetTile(wallPos, Wall);
                        gridHandler[x + 1, y] = Grid.WALL;

                        // Place mirrored wall
                        PlaceSymmetricTile(wallPos, Grid.WALL);
                    }
                    if (gridHandler[x - 1, y] == Grid.EMPTY)
                    {
                        Vector3Int wallPos = new Vector3Int(x - 1, y, 0);
                        tileMap.SetTile(wallPos, Wall);
                        gridHandler[x - 1, y] = Grid.WALL;

                        // Place mirrored wall
                        PlaceSymmetricTile(wallPos, Grid.WALL);
                    }
                    if (gridHandler[x, y + 1] == Grid.EMPTY)
                    {
                        Vector3Int wallPos = new Vector3Int(x, y + 1, 0);
                        tileMap.SetTile(wallPos, Wall);
                        gridHandler[x, y + 1] = Grid.WALL;

                        // Place mirrored wall
                        PlaceSymmetricTile(wallPos, Grid.WALL);
                    }
                    if (gridHandler[x, y - 1] == Grid.EMPTY)
                    {
                        Vector3Int wallPos = new Vector3Int(x, y - 1, 0);
                        tileMap.SetTile(wallPos, Wall);
                        gridHandler[x, y - 1] = Grid.WALL;

                        // Place mirrored wall
                        PlaceSymmetricTile(wallPos, Grid.WALL);
                    }
                }
            }
        }

        yield return null;
    }


}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static WalkerGenerator;

public class characterScript : MonoBehaviour
{
    public Tilemap tileMap;
    public float moveSpeed = 5f;
    public WalkerGenerator.Grid[,] gridHandler;
    private bool isMoving = false;
    private Vector3 targetPosition;

    void Start()
    {
        WalkerGenerator generator = FindObjectOfType<WalkerGenerator>();
        if (generator != null)
        {
            gridHandler = generator.GetGridHandler();

            if (gridHandler == null)
            {
                Debug.LogError("gridHandler is null! Ensure WalkerGenerator initializes it before this script runs.");
            }
        }
        else
        {
            Debug.LogError("WalkerGenerator not found in the scene!");
        }

        if (tileMap == null)
        {
            Debug.LogError("Tilemap reference is missing! Please assign it in the Inspector.");
        }

        SpawnOnRandomFloor();
        targetPosition = transform.position;
    }

    void Update()
    {
        HandleMovement();
    }

    public WalkerGenerator.Grid[,] GetGridHandler()
    {
        return gridHandler;
    }

    void SpawnOnRandomFloor()
    {
        if (gridHandler == null)
        {
            Debug.LogError("gridHandler is null! Cannot spawn character.");
            return;
        }

        if (tileMap == null)
        {
            Debug.LogError("tileMap is null! Cannot spawn character.");
            return;
        }

        List<Vector3Int> floorTiles = new List<Vector3Int>();

        for (int x = 0; x < gridHandler.GetLength(0); x++)
        {
            for (int y = 0; y < gridHandler.GetLength(1); y++)
            {
                if (gridHandler[x, y] == WalkerGenerator.Grid.FLOOR)
                {
                    floorTiles.Add(new Vector3Int(x, y, 0));
                }
            }
        }

        if (floorTiles.Count > 0)
        {
            Vector3Int randomTile = floorTiles[Random.Range(0, floorTiles.Count)];
            Vector3 spawnPosition = tileMap.CellToWorld(randomTile) + new Vector3(0.5f, 0.5f, 0.1f);
            transform.position = spawnPosition;
        }
        else
        {
            Debug.LogError("No floor tiles found to spawn the character!");
        }
    }

    void HandleMovement()
    {
        if (isMoving) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal != 0 && vertical != 0) return;

        if (horizontal != 0 || vertical != 0)
        {
            Vector3Int currentCell = tileMap.WorldToCell(transform.position);
            Vector3Int targetCell = currentCell + new Vector3Int((int)horizontal, (int)vertical, 0);

            targetCell.x = Mathf.Clamp(targetCell.x, 0, gridHandler.GetLength(0) - 1);
            targetCell.y = Mathf.Clamp(targetCell.y, 0, gridHandler.GetLength(1) - 1);

            if (IsPassable(targetCell))
            {
                targetPosition = tileMap.CellToWorld(targetCell) + new Vector3(0.5f, 0.5f, transform.position.z);
                StartCoroutine(MoveToTarget());
            }
        }
    }

    IEnumerator MoveToTarget()
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;
    }

    bool IsPassable(Vector3Int cell)
    {
        if (cell.x >= 0 && cell.x < gridHandler.GetLength(0) &&
            cell.y >= 0 && cell.y < gridHandler.GetLength(1))
        {
            return gridHandler[cell.x, cell.y] != WalkerGenerator.Grid.WALL;
        }

        return false;
    }
}

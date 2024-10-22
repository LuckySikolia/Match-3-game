using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionBoard : MonoBehaviour
{
    //define the board size
    public int width = 6;
    public int height = 8;

    //define the board spacing
    public float spacingX;
    public float spacingY;

    //referecnce potion prefabs
    public GameObject[] potionPrefabs;

    //reference the colleaction nodes potionBoard + Game Object
    public Node[,] potionBoard;
    public GameObject portionBoardGO;

    //reference layoutArray
    public ArrayLayout arrayLayout;

    //public static of potionBoard
    public static PotionBoard Instance;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        InitializeBoard();
    }

    //logic for spawning the board at game start
    void InitializeBoard()
    {
        potionBoard = new Node[width, height]; // creates an 8x6 board

        //calculate spacing of x and y
        spacingX = (float)(width - 1) / 2;
        spacingY = (float)((height - 1) / 2) + 1;


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //position where the potion will spawn at
                Vector2 position = new Vector2(x - spacingX, y - spacingY);

                if (arrayLayout.rows[y].row[x])
                {
                    potionBoard[x, y] = new Node(false, null);
                }
                else
                {
                    //generate a random portion
                    int randomIndex = Random.Range(0, potionPrefabs.Length);

                    //spawn the potion
                    GameObject potion = Instantiate(potionPrefabs[randomIndex], position, Quaternion.identity);
                    potion.GetComponent<Potion>().SetIndicies(x, y);
                    potionBoard[x, y] = new Node(true, potion);
                }

                

            }
        }
    }
}

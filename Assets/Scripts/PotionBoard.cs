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

    public List<GameObject> potionsToDestroy = new();
    [SerializeField]
    private Potion selectedPotion;

    //processing moves / swapping 
    [SerializeField]
    private bool isProcessingMove;

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

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if(hit.collider != null && hit.collider.gameObject.GetComponent<Potion>())
            {
                if (isProcessingMove)
                {
                    return;
                }

                Potion potion = hit.collider.gameObject.GetComponent<Potion>();
                Debug.Log($"I have clicked a potion it is : {potion.gameObject} ");

                SelectPotion(potion);
            }
        }
    }

    //logic for spawning the board at game start
    void InitializeBoard()
    {
        DestroyPotions();
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
                    potionsToDestroy.Add(potion); 
                }
            }
        }
        
        //ensures the board doesnt have a win situation on start
        if (CheckBoard())
        {
            Debug.Log("We have matches let us recreate the board");
            InitializeBoard();
        }
        else
        {
            Debug.Log("There are no mathces it is time to start the game");
        }

        
    }

    private void DestroyPotions()
    {
        if(potionsToDestroy != null)
        {
            foreach(GameObject potion in potionsToDestroy)
            {
                Destroy(potion);
            }
            potionsToDestroy.Clear();
        }
    }

    //check if we have a match or not
    public bool CheckBoard()
    {
        Debug.Log("Checking Log");
        bool hasMatched = false;

        List<Potion> potionsToRemove = new();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //check if portion node is usable
                if (potionBoard[x, y].isUsable)
                {
                    //thenn proceed to get potion class in node
                    Potion potion = potionBoard[x, y].potion.GetComponent<Potion>();


                    //ensure it is not matched 
                    if (!potion.isMatched)
                    {
                        //run some matching logic
                        MatchResult matchedPotion = IsConnected(potion);

                        //if we have a match
                        if(matchedPotion.connectedPotions.Count >= 3)
                        {
                            //complex matching. Cross matching


                            potionsToRemove.AddRange(matchedPotion.connectedPotions);

                            //mark eatch potion as matched
                            foreach(Potion pot in matchedPotion.connectedPotions)
                            {
                                pot.isMatched = true;   
                            }
                            hasMatched = true;
                        }
                    }
                }
            }
        }

        return hasMatched;
    }

    MatchResult IsConnected(Potion potion)
    {
        //stores any potential matches and its type
        List<Potion> connectedPotions = new();
        PortionType potionType = potion.portionType;


        connectedPotions.Add(potion);

        //checking for matches
        //check right
        CheckDirection(potion, new Vector2Int(1, 0), connectedPotions);
        //check left
        CheckDirection(potion, new Vector2Int(-1, 0), connectedPotions);
        //have we made a three (horizontal) match?
        if(connectedPotions.Count == 3)
        {
            Debug.Log($"Normal Horizontal 3 match, the colour is: + {connectedPotions[0].portionType}");

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.Horizontal,
            };
        }
        //checking for more than three match? long horizontal match?
        else if(connectedPotions.Count > 3)
        {
            Debug.Log($"Long Horizontal 3 match, the colour is: + {connectedPotions[0].portionType}");
            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.LongHorizontal,
            };
        }
        //clear out the connected potions if no match is made
        connectedPotions.Clear();
        //re-add our initial potion
        connectedPotions.Add(potion);


        //check up
        CheckDirection(potion, new Vector2Int(0, 1), connectedPotions);
        //check down
        CheckDirection(potion, new Vector2Int(0, -1), connectedPotions);
        //have we made a three (vertical) match?
        if (connectedPotions.Count == 3)
        {
            Debug.Log($"Normal Vertical 3 match, the colour is: + {connectedPotions[0].portionType}");

            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.Vertical,
            };
        }
        //checking for more than three match? long vertical match?
        else if (connectedPotions.Count > 3)
        {
            Debug.Log($"Long Vertical 3 match, the colour is: + {connectedPotions[0].portionType}");
            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.LongVertical,
            };
        }
        else //if there is no match in any direction
        {
            return new MatchResult
            {
                connectedPotions = connectedPotions,
                direction = MatchDirection.None
            };
        }

    }

    //CheckDirection
    void CheckDirection(Potion pot, Vector2Int direction, List<Potion> connectedPotion)
    {
        PortionType potionType = pot.portionType;
        //check neighnbouring portions
        int x = pot.xIndex + direction.x;
        int y = pot.yIndex + direction.y;

        //TODO!can add a diagonal check here

        //Check that we are within the boundaries of the board
        while(x >= 0 && x < width && y >= 0 && y < height)
        {
            if(potionBoard[x, y].isUsable)
            {
                Potion neighbourPotion = potionBoard[x,y].potion.GetComponent<Potion>();

                //check for matched
                //does out portion type match?
                //ensure it is not previously matched
                if (!neighbourPotion.isMatched && neighbourPotion.portionType == potionType)
                {
                    connectedPotion.Add(neighbourPotion);
                    //move to the next location
                    x += direction.x;
                    y += direction.y;
                }
                else

                {
                    break;
                }
            } 
            else
            {
                break;
            }
        }
    }
    #region Swapping Potions
    //select potion
    public void SelectPotion(Potion _potion)
    {
        //if we dont have a potion currently selected, then set the potion i just clicked to my selected potion
        if(selectedPotion == null)
        {
            Debug.Log(_potion);
            selectedPotion = _potion;
        }
        //to unselect potion (if same potion is selected twice)
        else if(selectedPotion == _potion)
        {
            selectedPotion = null;
        }
        //if selected potion is not nall and is not current potion, attempt a swap
        //set  selected potion back to null
        else if (selectedPotion != _potion)
        {
            SwapPotion(selectedPotion, _potion);
            selectedPotion = null;
        }
    }

    private void SwapPotion(Potion _currentPotion, Potion _targetPotion)
    {
        //is adjacent check
        //if it is not adjacent do nothing.
        if (!IsAdjacent(_currentPotion, _targetPotion))
        {
            return;
        }

        //if it is adjacent then  do the swap
        DoSwap(_currentPotion, _targetPotion);

        isProcessingMove = true;

        //start a coroutine to do the process matches.
        StartCoroutine(ProcessMatches(_currentPotion, _targetPotion));
    }

    

    //used to do the wap ie move the indexes of the potion
    private void DoSwap(Potion _currentPotion, Potion _targetPotion)
    {
        GameObject temp = potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion;
        potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion = potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].potion;
        potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].potion = temp;

        //update indicies 
        int tempXIndex = _currentPotion.xIndex;
        int tempYIndex = _currentPotion.yIndex;
        _currentPotion.xIndex = _targetPotion.xIndex;
        _currentPotion.yIndex = _targetPotion.yIndex;
        _targetPotion.xIndex = tempXIndex;
        _targetPotion.yIndex = tempYIndex;

        _currentPotion.MoveToTarget(potionBoard[_targetPotion.xIndex, _targetPotion.yIndex].potion.transform.position);
        _targetPotion.MoveToTarget(potionBoard[_currentPotion.xIndex, _currentPotion.yIndex].potion.transform.position);
    }

    private IEnumerator ProcessMatches(Potion _currentPotion, Potion _targetPotion)
    {
        //wait before doing any checks for matches to avoid deleting the potions prematurely
        yield return new WaitForSeconds(0.2f);

        bool hasMatch = CheckBoard();

        //if there is no match
        if (!hasMatch)
        {
            //Swap back to previous location
            DoSwap(_currentPotion, _targetPotion);
        }

        isProcessingMove = false;
    }

    private bool IsAdjacent(Potion _currentPotion, Potion _targetPotion)
    {
        return Mathf.Abs(_currentPotion.xIndex - _targetPotion.xIndex) + Mathf.Abs(_currentPotion.yIndex - _targetPotion.yIndex) == 1;
    }

    //swap potion
    //action potion location swapping
    //check for adjasent potion
    //check for processing matches

    #endregion

}

public class MatchResult
{
    public List<Potion> connectedPotions;
    public MatchDirection direction;
}

public enum MatchDirection
{
    Vertical,
    Horizontal,
    LongVertical,
    LongHorizontal,
    Super,  //when the match is in more than one direction
    None

}


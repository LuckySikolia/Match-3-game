using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    public GameObject potionParent;

    [SerializeField]
    private Potion selectedPotion;

    //processing moves / swapping 
    [SerializeField]
    private bool isProcessingMove;

    [SerializeField]
    List<Potion> potionsToRemove = new();

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
                    potion.transform.SetParent(potionParent.transform);
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
        //once game is over dont make any matches
        if (GameManager.Instance.isGameEnded)
        {
            return false;
        }

        Debug.Log("Checking Log");
        bool hasMatched = false;

        //ensure you start with bo potiins
        potionsToRemove.Clear();
        

        foreach(Node nodePotion in potionBoard)
        {
            if(nodePotion.potion != null)
            {
                nodePotion.potion.GetComponent<Potion>().isMatched = false;
            }
        }

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
                            MatchResult superMatchedPotions = SuperMatch(matchedPotion);


                            potionsToRemove.AddRange(superMatchedPotions.connectedPotions);

                            //mark eatch potion as matched
                            foreach(Potion pot in superMatchedPotions.connectedPotions)
                            {
                                pot.isMatched = true;   
                            }
                            hasMatched = true;
                        }
                    }
                }
            }
        }

        //check for brand new match         
        return hasMatched;
    }

    //only gets called when you have a matched board currently (//repeats until we have no more matches)
    public IEnumerator ProcessTurnOnMatchedBoard(bool _subtractMoves)
    {
        foreach (Potion potionToRemove in potionsToRemove)
        {
            //unmatch them 
            potionToRemove.isMatched = false;
        }

        RemoveAndRefill(potionsToRemove);
        //point system (i potion  = 1 point)
        GameManager.Instance.ProcessTurn(potionsToRemove.Count, _subtractMoves);

        yield return new WaitForSeconds(0.4f);

        //check board again for duplicate matches
        if (CheckBoard())
        {
            StartCoroutine(ProcessTurnOnMatchedBoard(false)); 
        }

    }

    private void RemoveAndRefill(List<Potion> _potionsToRemove)
    {
        //removing the portion and clearing the board at that location
        foreach(Potion potion in _potionsToRemove)
        {
            //getting its x and y indices and storing them temporarilt
            int _xIndex = potion.xIndex;
            int _yIndex = potion.yIndex;

            //destory th eportion
            Destroy(potion.gameObject);

            //create a blannk node on the potion board with a null on it
            potionBoard[_xIndex, _yIndex] = new Node(true, null);
        }

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                if (potionBoard[x, y].potion == null)
                {
                    Debug.Log($"The location of x: {x} + y: {y} is empty, attempting to refill it");
                    RefillPotion(x, y);
                }
                
            }
        }
    }

    private void RefillPotion(int x, int y)
    {
        //y offset
        int yOffset = 1;
        
        //while the cell above the current cell == null and we are below the hieight of th eboard then increment the y offset
        while (y + yOffset < height && potionBoard[x,y + yOffset].potion == null)
        {
            //increment y offset
            Debug.Log($"The portion above me is null, but i am not at the top of the board yet so add to my yOffset and try again. Current Offset is {yOffset}. I am about to add 1");
            yOffset++;
        }

        //we've hit the top of the board or found a potion
        if (y + yOffset < height && potionBoard[x, y + yOffset].potion != null)
        {
            //we've hit a portion s get current potion
            Potion potionAbove = potionBoard[x, y + yOffset].potion.GetComponent<Potion>();

            //move it to the correct location
            Vector3 targetPos = new Vector3(x - spacingX, y - spacingY, potionAbove.transform.position.z);
            Debug.Log($"I've found a potion when refilling the board and it was in the location:[ {x}, {y}] i am planning on moving it to this location [ {x}, {y}]");

            //move to location
            potionAbove.MoveToTarget(targetPos);

            //update indicies
            potionAbove.SetIndicies(x, y);
            //update potion board with the new information
            potionBoard[x,y] = potionBoard[x,y + yOffset];
            // set the location the potion came from to null
            potionBoard[x, y + yOffset] = new Node(true, null);
        }

        //if we have hit the top of the board without finding a potion
        if(y + yOffset == height)
        {
            Debug.Log("I've reached the top of the board without finding a potion");
            SpawnPotionAtTop(x);
        }

    }

    private void SpawnPotionAtTop(int x)
    {
        int index = FindIndexOfLowestNull(x); //find the lowest potion that is currently null
        int locationToMoveTo = 8 - index;
        Debug.Log($"About to spawn a potion, ideally i'd like to give it an index of {index}");

        //get random potion to spawn 
        int randomIndex = Random.Range(0, potionPrefabs.Length);

        GameObject newPotion = Instantiate(potionPrefabs[randomIndex], new Vector2(x - spacingX, height - spacingY), Quaternion.identity);
        newPotion.transform.SetParent(potionParent.transform);
        //set indices on the potion 
        newPotion.GetComponent<Potion>().SetIndicies(x, index);

        //set it on the potion board
        potionBoard[x, index] = new Node(true, newPotion);

        //move it to that location
        Vector3 targetPosition = new Vector3(newPotion.transform.position.x, newPotion.transform.position.y - locationToMoveTo, newPotion.transform.position.z);
        newPotion.GetComponent<Potion>().MoveToTarget(targetPosition);


    }

    //template method
    private int FindIndexOfLowestNull(int x)
    {
        int lowestNull = 99;
        for (int y = 7; y >= 0; y--)
        {
            if (potionBoard[x,y].potion == null)
            {
                lowestNull = y;
            }
        }
        return lowestNull;
    }


    #region Cascading Potions

    //FindInxedOfLowestNull


    #endregion

    #region MatchingLogic
    #endregion


    #region Swapping Potions
    #endregion




    private MatchResult SuperMatch(MatchResult _matchedResults)
    {
        //NB: a match result has a direction
        //HORIZONTAL: if  match is horizontal or long horizontal match then do a vertical match against each of the horizontal matches
        if(_matchedResults.direction == MatchDirection.Horizontal || _matchedResults.direction == MatchDirection.LongHorizontal)
        {
            //loop through the portions in the match
            foreach (Potion pot in _matchedResults.connectedPotions)
            {
                //create a new list of potions(extra matches)
                List<Potion> extraConnectedPotion = new();

                //matching logic. call check direction to check up and check down
                //look up
                CheckDirection(pot, new Vector2Int(0, 1), extraConnectedPotion);
                //look down
                CheckDirection(pot, new Vector2Int(0, -1), extraConnectedPotion);

                //if we've made a super match return new matchresult of type super
                if (extraConnectedPotion.Count >= 2)
                {
                    Debug.Log("Ihave a super horizontal match");
                    extraConnectedPotion.AddRange(_matchedResults.connectedPotions);

                    return new MatchResult
                    {
                        connectedPotions = extraConnectedPotion,
                        direction = MatchDirection.Super
                    };
                }

            }
            return new MatchResult
            {
                connectedPotions = _matchedResults.connectedPotions,
                direction = _matchedResults.direction
            };
        }
        //Vertical: if  match is vertical or long vertical match then do a horizontal match against each of the vertical matches
        else if (_matchedResults.direction == MatchDirection.Vertical || _matchedResults.direction == MatchDirection.LongVertical)
        {
            //loop through the portions in the match
            foreach (Potion pot in _matchedResults.connectedPotions)
            {
                //create a new list of potions(extra matches)
                List<Potion> extraConnectedPotion = new();

                //matching logic. call check direction to check left and check right
                //look left
                CheckDirection(pot, new Vector2Int(1, 0), extraConnectedPotion);
                //look right
                CheckDirection(pot, new Vector2Int(-1, 0), extraConnectedPotion);

                //if we've made a super match return new matchresult of type super
                if (extraConnectedPotion.Count >= 2)
                {
                    Debug.Log("Ihave a super vertical match");
                    extraConnectedPotion.AddRange(_matchedResults.connectedPotions);

                    return new MatchResult
                    {
                        connectedPotions = extraConnectedPotion,
                        direction = MatchDirection.Super
                    };
                }

            }
            //if we've not made a super match then return extra matches
            return new MatchResult
            {
                connectedPotions = _matchedResults.connectedPotions,
                direction = _matchedResults.direction
            };
        }

        return null;
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

        if (CheckBoard())
        {
            //if we have a match
            //start a coroutine that is going to process the matches in our turn
            StartCoroutine(ProcessTurnOnMatchedBoard(true));
        }
        else //swap potions back
        {
            DoSwap(_currentPotion, _targetPotion);
        }
        isProcessingMove = false;

        //if there is no match
        //if (!hasMatch)
        //{
        //    //Swap back to previous location
        //    DoSwap(_currentPotion, _targetPotion);
        //}

        //isProcessingMove = false;
    }

    private bool IsAdjacent(Potion _currentPotion, Potion _targetPotion)
    {
        return Mathf.Abs(_currentPotion.xIndex - _targetPotion.xIndex) + Mathf.Abs(_currentPotion.yIndex - _targetPotion.yIndex) == 1;
    }

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


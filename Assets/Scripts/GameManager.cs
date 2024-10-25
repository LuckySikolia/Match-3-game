using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // static reference

    public GameObject backgroundPanel; // grey background
    public GameObject victoryPanel;
    public GameObject losePanel;

    public int goal; //the amount of mpoints neede to win
    public int moves; // no of turns you can make
    public int points; //current points earned

    public bool isGameEnded;
    public TMP_Text pointsText;
    public TMP_Text movesText;
    public TMP_Text goalsText;

    private void Awake()
    {
        Instance = this;
    }
   
    public void Initialize(int _moves, int _goal)
    {
        moves = _moves;
        goal = _goal;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //constantly update the text
        pointsText.text = $"Points: {points.ToString()}";
        movesText.text = $"Moves: {moves.ToString()}";
        goalsText.text = $"Goals: {goal.ToString()}";
    }

    public void ProcessTurn(int _pointsToGain, bool _subtractMoves)
    {
        points += _pointsToGain;
        if (_subtractMoves)
        {
            moves--;
        }

        if(points >= goal)
        {
            //game won
            isGameEnded = true;
            //display a victory screen
            backgroundPanel.SetActive(true);
            victoryPanel.SetActive(true);
            return;
        }
        
        if(moves == 0)
        {
            //game lost
            isGameEnded=true;
            backgroundPanel.SetActive(true);
            losePanel.SetActive(true);
            return;
        }
    }


    //attached to buttons on ui to change scene when winning
    public void WinGame()
    {
        SceneManager.LoadScene(0); //Main menu is the first scene of build.  ///can be used to load next level instead of main menu
    }

    //attached to buttons on ui to change scene when losing
    public void LoseGame()
    {
        SceneManager.LoadScene(0); //Main menu is the first scene of nuild 0
    }
}
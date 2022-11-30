using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Main")]
    [Tooltip("0 - negative, .5 - default, 1 - positive")][SerializeField] private Gradient scoreColour;
    [SerializeField] private TextMeshProUGUI score;
    [SerializeField] private TextMeshProUGUI gameTime;
    [SerializeField] private TextMeshProUGUI pushCount;


    [Header("Start")]
    [SerializeField] private GameObject start_main;
    [SerializeField] private Button start_load;

    [Header("End")]
    [SerializeField] private GameObject end_Main;
    [SerializeField] private TextMeshProUGUI end_Text;


    private int localPoints;
    private float curColour = .5f;


    private void Awake()
    {
        GameManager.pushCallback += (pushes) => gameTime.text = $"Push Count : {pushes}";
        GameManager.gameTimeCallback += (time) => pushCount.text = $"Game Time : {WriteTime(Mathf.RoundToInt(time))}";

        GameManager.scoreCallback += OnPointChange;
        GameManager.gameCompleteCallback += OnGameComplete;


        start_main.SetActive(true);
        start_load.interactable = SaveManager.SaveExists();

        end_Main.SetActive(false);
    }

    private void OnPointChange(int points, int lvl)
    {
        curColour = points > localPoints ? 1 : 0;

        score.text = $"Score : {points} | Level : {lvl + 1}";
        localPoints = points;

    }


    private void Update()
    {
        curColour = Mathf.Lerp(curColour, .5f, Time.deltaTime * 1f);
        score.faceColor = scoreColour.Evaluate(curColour);
    }

    private string WriteTime(int time)
    {
        int mins = Mathf.FloorToInt(time / 60);
        int secs = time - (mins * 60);

        return $"{mins}:{secs.ToString("00")}";
    }


    public void LoadGame()
    {
        start_main.SetActive(false);
        GameManager.instance.StartGame();
    }


    public void StartNewGame()
    {
        start_main.SetActive(false);

        SaveManager.RemoveSave();
        GameManager.instance.StartGame();
    }


    private void OnGameComplete(bool isSuccess, float inTime)
    {
        SaveManager.RemoveSave();

        end_Main.SetActive(true);
        end_Text.text = $"{(isSuccess ? "You Win!" : "You Lose!")} in {WriteTime(Mathf.RoundToInt(inTime))}s";
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Beatmap : MonoBehaviour
{
    public TextAsset clickBeatmap;
    private string[] clickCsv;
    public List<int> clickMeasureList = new List<int>();
    public List<int> clickBeatList = new List<int>();

    public TextAsset playerHitBeatmap;
    private string[] playerCsv;
    public List<int> playerMeasureList = new List<int>();
    public List<int> playerBeatList = new List<int>();


    // Start is called before the first frame update
    void Start()
    {
        clickCsv = System.IO.File.ReadAllLines(AssetDatabase.GetAssetPath(clickBeatmap));
        for(int i = 1; i < clickCsv.Length; i++) {
            string[] nums = clickCsv[i].Split(',');
            clickMeasureList[i-1] = int.Parse(nums[0]);
            clickBeatList[i-1] = int.Parse(nums[1]);
        }

        playerCsv = System.IO.File.ReadAllLines(AssetDatabase.GetAssetPath(playerBeatmap));
        for(int i = 1; i < playerCsv.Length; i++) {
            string[] nums = playerCsv[i].Split(',');
            playerMeasureList[i-1] = int.Parse(nums[0]);
            playerBeatList[i-1] = int.Parse(nums[1]);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModeSelectScreen : MonoBehaviour
{
    public Animator screenAnimator;

    public Button btnGenerateScenarios;
    public Button btnGenerateCrowds;
    public Button btnSimulateCases;
    public Button btnSummarizeCases;
    public Button[] returnToMenuButtons;

    private void OnEnable()
    {
        btnGenerateScenarios.onClick.AddListener(() => { screenAnimator.SetInteger("screenNumber", 1); });
        btnGenerateCrowds.onClick.AddListener(() => { screenAnimator.SetInteger("screenNumber", 3); });
        btnSimulateCases.onClick.AddListener(() => { screenAnimator.SetInteger("screenNumber", 2); });
        btnSummarizeCases.onClick.AddListener(() => { screenAnimator.SetInteger("screenNumber", 4); });
        foreach (var item in returnToMenuButtons)
        {
            item.onClick.AddListener(() => { screenAnimator.SetInteger("screenNumber", 0); });
        }
    }

}

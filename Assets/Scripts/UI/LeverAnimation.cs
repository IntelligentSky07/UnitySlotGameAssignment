using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SlotGame.Gameplay;

public class LeverAnimation : MonoBehaviour
{
    [SerializeField] private SlotMachine machine;
    [SerializeField] private Image leverImage;
    [SerializeField] private Sprite leverUp;
    [SerializeField] private Sprite leverDown;
    [SerializeField] private float downTime = 0.2f; // How long the lever stays pulled down.

    private void OnEnable()  { machine.SpinStarted += PlayPull; }
    private void OnDisable() { machine.SpinStarted -= PlayPull; }

    private void PlayPull()
    {
        StopAllCoroutines();
        StartCoroutine(PullRoutine());
    }

    private IEnumerator PullRoutine()
    {
        leverImage.sprite = leverDown;
        yield return new WaitForSeconds(downTime);
        leverImage.sprite = leverUp;
    }
}
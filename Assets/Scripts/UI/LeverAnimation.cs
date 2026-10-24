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
    [SerializeField] private float downTime = 0.2f; // How long the handle stays in the pulled pose.

    private void OnEnable()  { machine.SpinStarted += PlayPull; }
    private void OnDisable() { machine.SpinStarted -= PlayPull; }

    private void PlayPull()
    {
        StopAllCoroutines();
        StartCoroutine(PullRoutine());
    }

    private IEnumerator PullRoutine()
    {
        // Swap to the down sprite when a spin begins, then snap back to idle.
        leverImage.sprite = leverDown;
        yield return new WaitForSeconds(downTime);
        leverImage.sprite = leverUp;
    }
}

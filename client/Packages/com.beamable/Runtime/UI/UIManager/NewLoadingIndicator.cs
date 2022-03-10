using UnityEngine;


/// <summary>
/// Just a "toggler" between two game objects.
/// TODO: Test out the experimental stuff in <see cref="ViewTransitionCoordinator"/> with this to see how well that stuff generalizes to "componentization" in UIs scripts.
/// </summary>
public class NewLoadingIndicator : MonoBehaviour
{
    public GameObject LoadingRepresentation;
    public GameObject MainGroup;

    private void Awake()
    {
        ToggleLoadingIndicator(true);
    }

    public void ToggleLoadingIndicator(bool isLoading)
    {
        LoadingRepresentation.SetActive(isLoading);
        MainGroup.SetActive(!isLoading);
    }
}

using UnityEngine;

// Entry point for all menu character presentation behaviour. Lives on the
// menu athlete prefab only - never referenced by gameplay code, and never
// references gameplay systems (RaceManager, sprint input, movement, etc.).
//
// Currently only owns rotation. As menu character features grow
// (appearance/customization, model swapping, preview camera controls, idle
// animation), add one focused controller per concern and wire it up here
// rather than growing this class or any single controller.
public class AthletePreviewController : MonoBehaviour
{
    [Header("Preview Systems")]
    [SerializeField] private AthleteRotationController rotationController;

    private void Awake()
    {
        if (rotationController == null)
            rotationController = GetComponentInChildren<AthleteRotationController>();
    }

    public void SetRotationEnabled(bool isEnabled)
    {
        if (rotationController != null)
            rotationController.enabled = isEnabled;
    }
}

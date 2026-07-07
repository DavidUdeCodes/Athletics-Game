using UnityEngine;
using UnityEngine.Splines;

public class SplineLengthFinder : MonoBehaviour
{
    [SerializeField] private SplineContainer mySpline;
    private float splineLength = 0f;

    [SerializeField] private string splineName = "My Spline"; // Name of the spline for identification

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        splineLength = mySpline.CalculateLength();
        Debug.Log(splineName + " length: " + splineLength);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

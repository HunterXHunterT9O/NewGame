using UnityEngine;

[CreateAssetMenu(fileName = "BuildingPiece", menuName = "Building/Building Piece")]
public class BuildingPiece : ScriptableObject
{
    public string pieceName;
    public GameObject prefab;
    public Sprite icon;
    public BuildingCategory category;
    public Vector3 snapOffset = Vector3.zero;
    public bool canRotate = true;
}

public enum BuildingCategory
{
    Floors,
    Walls,
    Roofs,
    Props,
    Machines
}

using UnityEngine;

public class NewMapMenu : MonoBehaviour
{
    public HexGrid grid;

    void CreateMap(int x, int z)
    {
        grid.CreateMap(x, z);
        HexMapCamera.ValidatePosition();
        Close();
    }

    public void CreateSmallMap()
    {
        CreateMap(15, 20);
    }

    public void CreateMediumMap()
    {
        CreateMap(30, 50);
    }

    public void CreateLargeMap()
    {
        CreateMap(150, 150);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }
}
